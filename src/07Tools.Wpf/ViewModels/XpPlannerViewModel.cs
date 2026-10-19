using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RunescapeTools.Application.Market;
using RunescapeTools.Application.Profiles;
using RunescapeTools.Application.Training;
using RunescapeTools.Core.Market;
using RunescapeTools.Core.Training;
using RunescapeTools.Wpf.Services;

namespace RunescapeTools.Wpf.ViewModels;

public partial class XpPlannerViewModel : ObservableObject, IPageViewModel
{
    private const long Level99Experience = 13_034_431;
    private readonly IEhpCatalogue catalogue;
    private readonly TrainingPlanCalculator calculator;
    private readonly TrainingMoneyMakingCalculator moneyMakingCalculator;
    private readonly IMarketDataService marketData;
    private readonly ITrainingPlanPersistenceService plans;
    private readonly ICurrentProfileContext profileContext;
    private readonly MoneyMakerSelectionContext moneyMakerSelection;
    private readonly ITrainingConfigurationDialogService? configurationDialogs;
    private readonly ITrainingPriceDialogService? priceDialogs;
    private readonly IPlannerPricingService? plannerPricing;
    private CancellationTokenSource? pricingCancellation;
    private PlannerPriceSnapshot? pricingSnapshot;
    private SelectedMoneyMaker? pricedMoneyMaker;
    private bool suppressPricingChanges;

    [ObservableProperty]
    private bool useThirtyDayPrices;

    [ObservableProperty]
    private bool isPricingLoading;

    [ObservableProperty]
    private string pricingStatus = "Live prices";
    private CancellationTokenSource? saveCancellation;
    private string? loadedProfileRsn;
    private IReadOnlyDictionary<int, ItemPrice> prices = new Dictionary<int, ItemPrice>();
    private bool initialized;
    private bool suppressRowChanges;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string profileName = "No profile";

    [ObservableProperty]
    private string totalExperienceRemaining = "0";

    [ObservableProperty]
    private string totalHours = "0";

    [ObservableProperty]
    private string totalNetGp = "Not priced";

    [ObservableProperty]
    private string pricedCoverage = "0%";

    [ObservableProperty]
    private bool hasSelectedMoneyMaker;

    [ObservableProperty]
    private string selectedMoneyMakerName = "Choose a method";

    [ObservableProperty]
    private string selectedMoneyMakerRate = "Open Money Makers";

    [ObservableProperty]
    private string moneyMakerContributionSummary = "No money maker selected";

    [ObservableProperty]
    private bool isMoneyMakerProfitPositive = true;

    [ObservableProperty]
    private decimal selectedMoneyMakingHours;

    [ObservableProperty]
    private decimal moneyMakerGpContribution;

    [ObservableProperty]
    private string saveStatus = string.Empty;

    public XpPlannerViewModel(
        IEhpCatalogue catalogue,
        TrainingPlanCalculator calculator,
        TrainingMoneyMakingCalculator moneyMakingCalculator,
        IMarketDataService marketData,
        ITrainingPlanPersistenceService plans,
        ICurrentProfileContext profileContext,
        MoneyMakerSelectionContext moneyMakerSelection,
        ITrainingConfigurationDialogService? configurationDialogs = null,
        ITrainingPriceDialogService? priceDialogs = null,
        IPlannerPricingService? plannerPricing = null)
    {
        this.catalogue = catalogue;
        this.calculator = calculator;
        this.moneyMakingCalculator = moneyMakingCalculator;
        this.marketData = marketData;
        this.plans = plans;
        this.profileContext = profileContext;
        this.moneyMakerSelection = moneyMakerSelection;
        this.configurationDialogs = configurationDialogs;
        this.priceDialogs = priceDialogs;
        this.plannerPricing = plannerPricing;
        profileContext.ProfileChanged += (_, _) =>
        {
            pricingCancellation?.Cancel();
            initialized = false;
        };
        moneyMakerSelection.SelectionChanged += OnMoneyMakerSelectionChanged;
        UpdateMoneyMakerDisplay();
    }

    public ObservableCollection<XpPlannerRowViewModel> Rows { get; } = [];
    public string CatalogueLabel => $"{catalogue.Version} · verified {catalogue.VerifiedOn:yyyy-MM-dd}";

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (initialized)
            return;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            if (!profileContext.HasLoadedProfile)
                await profileContext.LoadSelectedProfileAsync(cancellationToken);

            var profile = profileContext.CurrentProfile
                          ?? throw new InvalidOperationException("Load a profile before opening the XP Planner.");
            var preferences = await plans.GetAsync(profile.Rsn, cancellationToken);
            var itemIds = catalogue.Skills
                .SelectMany(skill => skill.MarketItemIds)
                .Distinct();
            var priceLoadFailed = false;
            try
            {
                if (plannerPricing is not null)
                {
                    var mode = await plannerPricing.ReadModeAsync(cancellationToken);
                    suppressPricingChanges = true;
                    UseThirtyDayPrices = mode == PricingMode.ThirtyDayAverage;
                    suppressPricingChanges = false;
                    var selection = moneyMakerSelection.Current;
                    ApplySnapshot(await plannerPricing.GetAsync(itemIds, mode, selection?.Definition,
                        cancellationToken), selection);
                }
                else prices = await marketData.GetLatestForAsync(itemIds, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                prices = new Dictionary<int, ItemPrice>();
                pricingSnapshot = null;
                pricedMoneyMaker = null;
                PricingStatus = UseThirtyDayPrices ? "30-day averages unavailable" : "Live prices unavailable";
                priceLoadFailed = true;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!ReferenceEquals(profile, profileContext.CurrentProfile))
                return;

            suppressRowChanges = true;
            loadedProfileRsn = profile.Rsn;
            Rows.Clear();
            var profileSkills = profile.Skills.ToDictionary(skill => skill.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var definition in catalogue.Skills)
            {
                profileSkills.TryGetValue(definition.Skill, out var profileSkill);
                preferences.TryGetValue(definition.Skill, out var preference);
                Rows.Add(new XpPlannerRowViewModel(
                    definition,
                    calculator,
                    profileSkill?.Experience ?? 0,
                    preference,
                    prices,
                    OnRowChanged,
                    ConfigureRow,
                    ShowPriceDetails));
            }

            ProfileName = profile.Rsn;
            initialized = true;
            RecalculateSummary();
            if (priceLoadFailed)
                ErrorMessage = "Selected GE prices are unavailable; the planner remains usable and affected methods are shown as unpriced.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            ErrorMessage = "The XP Planner could not load the selected profile or current GE prices.";
        }
        finally
        {
            suppressRowChanges = false;
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshPricesAsync(CancellationToken cancellationToken)
    {
        if (plannerPricing is not null)
        {
            await RefreshPlannerPricingAsync(cancellationToken);
            return;
        }
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var itemIds = catalogue.Skills
                .SelectMany(skill => skill.MarketItemIds)
                .Distinct();
            prices = await marketData.GetLatestForAsync(itemIds, cancellationToken);
            foreach (var row in Rows)
                row.UpdatePrices(prices);
            RecalculateSummary();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            ErrorMessage = "Live GE prices are temporarily unavailable; the last valid calculation is still shown.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnUseThirtyDayPricesChanged(bool value)
    {
        if (!suppressPricingChanges && initialized && plannerPricing is not null)
            _ = RefreshPlannerPricingAsync(CancellationToken.None);
    }

    private void ApplySnapshot(PlannerPriceSnapshot snapshot, SelectedMoneyMaker? selection)
    {
        pricingSnapshot = snapshot;
        pricedMoneyMaker = selection;
        prices = snapshot.Prices;
        PricingStatus = snapshot.Mode == PricingMode.Live ? "Live prices"
            : $"30-day volume-weighted averages · through {snapshot.AsOf:dd MMM HH:mm} UTC · not current offers";
        if (snapshot.IncompleteItems > 0)
            PricingStatus += $" · {snapshot.IncompleteItems} items have incomplete prices";
        UpdateMoneyMakerDisplay();
    }

    private async Task RefreshPlannerPricingAsync(CancellationToken cancellationToken)
    {
        pricingCancellation?.Cancel();
        var request = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        pricingCancellation = request;
        IsPricingLoading = true;
        ErrorMessage = null;
        var mode = UseThirtyDayPrices ? PricingMode.ThirtyDayAverage : PricingMode.Live;
        var selection = moneyMakerSelection.Current;
        try
        {
            var snapshot = await plannerPricing!.GetAsync(
                catalogue.Skills.SelectMany(skill => skill.MarketItemIds), mode,
                selection?.Definition, request.Token);
            request.Token.ThrowIfCancellationRequested();
            if (pricingCancellation != request) return;
            suppressRowChanges = true;
            ApplySnapshot(snapshot, selection);
            foreach (var row in Rows) row.UpdatePrices(prices);
            suppressRowChanges = false;
            RecalculateSummary();
            try { await plannerPricing.SaveModeAsync(mode, request.Token); }
            catch (OperationCanceledException) when (request.IsCancellationRequested) { }
            catch (Exception) { ErrorMessage = "Prices updated, but the pricing preference could not be saved."; }
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (Exception)
        {
            if (pricingCancellation != request) return;
            ErrorMessage = "Pricing could not refresh. The previous snapshot is still displayed; no live/average substitution was made.";
            suppressPricingChanges = true;
            UseThirtyDayPrices = pricingSnapshot?.Mode == PricingMode.ThirtyDayAverage;
            suppressPricingChanges = false;
        }
        finally
        {
            if (pricingCancellation == request)
            {
                suppressRowChanges = false;
                IsPricingLoading = false;
                pricingCancellation = null;
            }
            request.Dispose();
        }
    }

    private decimal? PlannerMoneyMakerRate => plannerPricing is null
        ? moneyMakerSelection.Current?.TotalProfitPerHour
        : pricedMoneyMaker == moneyMakerSelection.Current ? pricingSnapshot?.MoneyMakerProfitPerHour : null;

    [RelayCommand]
    private void SetAllTo99() => SetAllTargets(Level99Experience);

    [RelayCommand]
    private void SetAllTo200M() => SetAllTargets(TrainingPlanCalculator.MaximumExperience);

    [RelayCommand]
    private void ResetFromProfile()
    {
        var skills = profileContext.CurrentProfile?.Skills
            .ToDictionary(skill => skill.Name, StringComparer.OrdinalIgnoreCase);
        if (skills is null)
            return;

        suppressRowChanges = true;
        foreach (var row in Rows)
        {
            skills.TryGetValue(row.Skill, out var skill);
            row.ResetStart(skill?.Experience ?? 0);
        }
        suppressRowChanges = false;
        OnRowChanged();
    }

    [RelayCommand]
    private Task SaveAsync(CancellationToken cancellationToken)
    {
        QueueCurrentPlan();
        return SaveNowAsync(cancellationToken);
    }

    [RelayCommand]
    private void ResetMoneyMaker() => moneyMakerSelection.Clear();

    private void SetAllTargets(long target)
    {
        suppressRowChanges = true;
        foreach (var row in Rows)
            row.SetTarget(Math.Max(row.StartExperience, target));
        suppressRowChanges = false;
        OnRowChanged();
    }

    private void OnRowChanged()
    {
        if (suppressRowChanges || !initialized)
            return;
        RecalculateSummary();
        ScheduleSave();
    }

    private void ConfigureRow(XpPlannerRowViewModel row)
    {
        var definition = row.Definition.Configurator?.GetDefinition(row.SelectedMethodOption?.Id);
        if (definition is null || configurationDialogs is null)
            return;

        var updated = configurationDialogs.Edit(
            row.Skill,
            row.Method,
            row.SelectedMethodOption?.Id,
            definition,
            row.ConfigurationValues);
        if (updated is not null)
            row.ApplyConfiguration(updated);
    }

    private void ShowPriceDetails(XpPlannerRowViewModel row) =>
        priceDialogs?.Show(row.Skill, row.Result, prices);

    private void RecalculateSummary()
    {
        ApplyExperienceDependencies();
        var experience = Rows.Sum(row => row.Result.ExperienceRemaining);
        var hours = Rows.Sum(row => row.Result.Hours);
        var pricedExperience = Rows.Sum(row => row.Result.PricedExperience);
        var gp = Rows.Where(row => row.Result.NetGp.HasValue).Sum(row => row.Result.NetGp ?? 0m);
        var moneyMaking = moneyMakingCalculator.Calculate(
            PlannerMoneyMakerRate,
            Rows.Where(row => row.IsMoneyMakingSelected)
                .Select(row => row.Result.Hours));
        SelectedMoneyMakingHours = moneyMaking.SelectedHours;
        MoneyMakerGpContribution = moneyMaking.NetGp;
        gp += MoneyMakerGpContribution;
        TotalExperienceRemaining = DisplayFormat.Compact(experience);
        TotalHours = $"{hours:N1} h";
        TotalNetGp = pricedExperience > 0 || MoneyMakerGpContribution != 0m
            ? DisplayFormat.Gp(gp)
            : "Not priced";
        PricedCoverage = experience > 0 ? $"{(decimal)pricedExperience / experience:P0}" : "100%";
        MoneyMakerContributionSummary = moneyMakerSelection.Current is null
            ? "No money maker selected"
            : PlannerMoneyMakerRate is null ? "Not priced on the selected pricing basis; excluded from total"
            : SelectedMoneyMakingHours <= 0m
                ? "Select skills below to apply this rate"
                : $"{DisplayFormat.Gp(MoneyMakerGpContribution)} over {SelectedMoneyMakingHours:N1} selected h";
    }

    private void ApplyExperienceDependencies()
    {
        for (var iteration = 0; iteration <= Rows.Count; iteration++)
        {
            var generatedBySkill = Rows
                .SelectMany(source => source.Result.GeneratedExperience
                    .Where(flow => flow.Value > 0m)
                    .Select(flow => new
                    {
                        Skill = flow.Key,
                        Experience = flow.Value,
                        Source = $"{source.Skill}: " +
                                 (source.SelectedMethodOption?.RouteName ?? source.Method)
                    }))
                .GroupBy(flow => flow.Skill, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => new
                    {
                        Experience = group.Sum(flow => flow.Experience),
                        Sources = string.Join(
                            ", ",
                            group.Select(flow => flow.Source)
                                .Distinct(StringComparer.OrdinalIgnoreCase))
                    },
                    StringComparer.OrdinalIgnoreCase);

            var changed = false;
            foreach (var row in Rows)
            {
                generatedBySkill.TryGetValue(row.Skill, out var generated);
                changed |= row.SetPendingExperienceCredit(
                    (long)Math.Floor(generated?.Experience ?? 0m),
                    generated?.Sources);
            }

            if (!changed)
                break;
        }
    }

    private void OnMoneyMakerSelectionChanged(object? sender, EventArgs eventArgs)
    {
        UpdateMoneyMakerDisplay();
        RecalculateSummary();
        if (initialized && plannerPricing is not null)
            _ = RefreshPlannerPricingAsync(CancellationToken.None);
    }

    private void UpdateMoneyMakerDisplay()
    {
        var selection = moneyMakerSelection.Current;
        HasSelectedMoneyMaker = selection is not null;
        SelectedMoneyMakerName = selection?.Name ?? "Choose a method";
        SelectedMoneyMakerRate = selection is null
            ? "Open Money Makers"
            : (PlannerMoneyMakerRate.HasValue ? DisplayFormat.GpPerHour(PlannerMoneyMakerRate.Value) : "Not priced")
              + (selection.HasMissingPrices
                  ? $" | {selection.AccountCount} accounts | partial prices"
                  : selection.AccountCount == 1
                      ? " | 1 account"
                      : $" | {selection.AccountCount} accounts");
        IsMoneyMakerProfitPositive =
            selection is null || PlannerMoneyMakerRate >= 0m;
        if (selection is null)
        {
            SelectedMoneyMakingHours = 0m;
            MoneyMakerGpContribution = 0m;
            MoneyMakerContributionSummary = "No money maker selected";
        }
    }

    private void ScheduleSave()
    {
        QueueCurrentPlan();
        saveCancellation?.Cancel();
        saveCancellation?.Dispose();
        saveCancellation = new CancellationTokenSource();
        var token = saveCancellation.Token;
        _ = SaveAfterDelayAsync(token);
    }

    private async Task SaveAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(600, cancellationToken);
            // Rescheduling the debounce must not cancel a write that has already started.
            await SaveNowAsync(CancellationToken.None);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task SaveNowAsync(CancellationToken cancellationToken)
    {
        try
        {
            saveCancellation?.Cancel();
            await plans.FlushAsync(cancellationToken);
            SaveStatus = $"Saved {DateTime.Now:t}";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            SaveStatus = "Could not save changes";
        }
    }

    private void QueueCurrentPlan()
    {
        // Rows belong to the last successfully loaded profile, not whichever RSN is current now.
        if (initialized && loadedProfileRsn is not null)
            plans.QueueSave(loadedProfileRsn, Rows.Select(row => row.ToPreference()).ToArray());
    }

}
