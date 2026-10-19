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

public partial class TrainingMethodOptionViewModel : ObservableObject
{
    public TrainingMethodOptionViewModel(
        TrainingMethodDefinition definition,
        long? unlockExperience = null)
    {
        Definition = definition;
        UnlockExperience = unlockExperience is > 0 ? unlockExperience : null;
        RouteName = string.Equals(
            definition.Name,
            "Main EHP route",
            StringComparison.OrdinalIgnoreCase)
            ? definition.Bands.LastOrDefault()?.Method ?? definition.Name
            : definition.Name;
        name = RouteName;
    }

    public TrainingMethodDefinition Definition { get; }
    public string Id => Definition.Id;
    public string RouteName { get; }
    public long? UnlockExperience { get; }
    public int? UnlockLevel => UnlockExperience.HasValue
        ? LevelForExperience(UnlockExperience.Value)
        : null;

    [ObservableProperty]
    private string name;

    public void UpdateAvailability(long experience)
    {
        Name = UnlockExperience is { } unlock && experience < unlock
            ? $"{RouteName} — unlocks at {UnlockLevel}"
            : RouteName;
    }

    private static int LevelForExperience(long experience)
    {
        decimal points = 0m;
        for (var level = 1; level < 99; level++)
        {
            points += Math.Floor(level + 300m * (decimal)Math.Pow(2d, level / 7d));
            if (experience < Math.Floor(points / 4m))
                return level;
        }

        return 99;
    }
}

internal static class TrainingMethodAvailability
{
    public static long? FindUnlockExperience(
        TrainingMethodDefinition defaultMethod,
        TrainingMethodDefinition candidate)
    {
        if (string.Equals(defaultMethod.Id, candidate.Id, StringComparison.OrdinalIgnoreCase))
            return null;

        foreach (var experience in defaultMethod.Bands
                     .Select(band => band.StartExperience)
                     .Concat(candidate.Bands.Select(band => band.StartExperience))
                     .Distinct()
                     .Order())
        {
            var defaultBand = ActiveBand(defaultMethod, experience);
            var candidateBand = ActiveBand(candidate, experience);
            if (defaultBand is null || candidateBand is null)
                continue;

            if (!string.Equals(defaultBand.Method, candidateBand.Method, StringComparison.Ordinal)
                || defaultBand.ExperiencePerHour != candidateBand.ExperiencePerHour)
            {
                return experience > 0 ? experience : null;
            }
        }

        return null;
    }

    private static TrainingRateBand? ActiveBand(
        TrainingMethodDefinition method,
        long experience) =>
        method.Bands
            .OrderBy(band => band.StartExperience)
            .LastOrDefault(band => band.StartExperience <= experience)
        ?? method.Bands.FirstOrDefault();
}

public partial class XpPlannerRowViewModel : ObservableObject
{
    private readonly TrainingPlanCalculator calculator;
    private readonly Action changed;
    private readonly Action<XpPlannerRowViewModel>? configure;
    private readonly Action<XpPlannerRowViewModel>? showPricing;
    private IReadOnlyDictionary<int, ItemPrice> prices;
    private long pendingExperienceCredit;
    private string pendingExperienceCreditSource = string.Empty;
    private bool hasPersonalRateOverride;
    private decimal personalBaseRate;
    private bool suppressChanges;

    [ObservableProperty]
    private long startExperience;

    [ObservableProperty]
    private long targetExperience;

    [ObservableProperty]
    private decimal personalRate;

    [ObservableProperty]
    private string method = string.Empty;

    [ObservableProperty]
    private string hours = "0";

    [ObservableProperty]
    private string totalGp = "Not priced";

    [ObservableProperty]
    private string economicRate = "Not priced";

    [ObservableProperty]
    private string creditSummary = string.Empty;

    [ObservableProperty]
    private string availabilitySummary = string.Empty;

    [ObservableProperty]
    private bool hasAvailabilitySummary;

    [ObservableProperty]
    private bool hasExperienceCredit;

    [ObservableProperty]
    private string pricingStatus = "Rate only";

    [ObservableProperty]
    private bool isProfit;

    [ObservableProperty]
    private bool isMoneyMakingSelected;

    [ObservableProperty]
    private TrainingMethodOptionViewModel? selectedMethodOption;

    public XpPlannerRowViewModel(
        TrainingSkillDefinition definition,
        TrainingPlanCalculator calculator,
        long profileExperience,
        TrainingSkillPreference? preference,
        IReadOnlyDictionary<int, ItemPrice> prices,
        Action changed,
        Action<XpPlannerRowViewModel>? configure = null,
        Action<XpPlannerRowViewModel>? showPricing = null)
    {
        Definition = definition;
        this.calculator = calculator;
        this.prices = prices;
        this.changed = changed;
        this.configure = configure;
        this.showPricing = showPricing;
        ProfileExperience = Math.Max(0, profileExperience);
        startExperience = preference?.StartExperienceOverride ?? ProfileExperience;
        targetExperience = preference?.TargetExperience ?? TrainingPlanCalculator.MaximumExperience;
        isMoneyMakingSelected = preference?.IsMoneyMakingSelected ?? false;

        var defaultMethod = definition.ResolveMethod();
        MethodOptions = definition.AvailableMethods
            .Select(method => new TrainingMethodOptionViewModel(
                method,
                TrainingMethodAvailability.FindUnlockExperience(defaultMethod, method)))
            .ToArray();
        selectedMethodOption = MethodOptions.FirstOrDefault(option =>
                                   string.Equals(
                                       option.Id,
                                       preference?.TrainingMethodId,
                                       StringComparison.OrdinalIgnoreCase))
                               ?? MethodOptions.FirstOrDefault(option =>
                                   string.Equals(
                                       option.Id,
                                       definition.DefaultMethodId,
                                       StringComparison.OrdinalIgnoreCase))
                               ?? MethodOptions.First();
        ConfigurationValues =
            definition.Configurator?.GetDefinition(selectedMethodOption.Id).Normalize(preference?.Configuration).ToDictionary()
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var baseline = calculator.Calculate(
            definition,
            startExperience,
            targetExperience,
            prices,
            methodId: selectedMethodOption.Id,
            configuration: ConfigurationValues);
        hasPersonalRateOverride = preference?.ExperiencePerHourOverride is > 0m;
        personalBaseRate = hasPersonalRateOverride
            ? preference!.ExperiencePerHourOverride!.Value
            : UnconfiguredBaseRate(baseline);
        personalRate = hasPersonalRateOverride ? personalBaseRate : baseline.BaseRate;
        Recalculate();
    }

    public TrainingSkillDefinition Definition { get; }
    public string Skill => Definition.Skill;
    public string? IconUrl => OsrsSkillIconMap.GetIconUrl(Skill);
    public string? Note => Definition.Note;
    public IReadOnlyList<TrainingMethodDefinition> AvailableMethods => Definition.AvailableMethods;
    public IReadOnlyList<TrainingMethodOptionViewModel> MethodOptions { get; }
    public bool HasConfiguration => Definition.Configurator is not null;
    public Dictionary<string, string> ConfigurationValues { get; private set; }
    public long ProfileExperience { get; private set; }
    public TrainingSkillPlanResult Result { get; private set; } = null!;

    public TrainingSkillPreference ToPreference()
    {
        decimal? rateOverride = hasPersonalRateOverride ? personalBaseRate : null;
        return new TrainingSkillPreference(
            Skill,
            TargetExperience,
            StartExperience == ProfileExperience ? null : StartExperience,
            rateOverride,
            IsMoneyMakingSelected,
            SelectedMethodOption?.Id,
            new Dictionary<string, string>(
                ConfigurationValues,
                StringComparer.OrdinalIgnoreCase));
    }

    public void UpdatePrices(IReadOnlyDictionary<int, ItemPrice> value)
    {
        prices = value;
        Recalculate();
    }

    public void ResetStart(long profileExperience)
    {
        ProfileExperience = Math.Max(0, profileExperience);
        StartExperience = ProfileExperience;
    }

    public void SetTarget(long value) => TargetExperience = value;

    public bool SetPendingExperienceCredit(long value, string? source = null)
    {
        var normalized = Math.Max(0, value);
        var normalizedSource = normalized > 0 ? source?.Trim() ?? string.Empty : string.Empty;
        if (pendingExperienceCredit == normalized
            && string.Equals(
                pendingExperienceCreditSource,
                normalizedSource,
                StringComparison.Ordinal))
        {
            return false;
        }

        pendingExperienceCredit = normalized;
        pendingExperienceCreditSource = normalizedSource;
        Recalculate();
        return true;
    }

    public void ApplyConfiguration(IReadOnlyDictionary<string, string> values)
    {
        var definition = Definition.Configurator?.GetDefinition(SelectedMethodOption?.Id);
        if (definition is null)
            return;

        ConfigurationValues = definition.Normalize(values).ToDictionary();
        var baseline = calculator.Calculate(
            Definition,
            StartExperience,
            TargetExperience,
            prices,
            methodId: SelectedMethodOption?.Id,
            pendingExperienceCredit: pendingExperienceCredit,
            configuration: ConfigurationValues);
        if (!hasPersonalRateOverride)
        {
            suppressChanges = true;
            personalBaseRate = UnconfiguredBaseRate(baseline);
            PersonalRate = baseline.BaseRate;
            suppressChanges = false;
        }

        ChangedAndRecalculate();
    }

    [RelayCommand]
    private void OpenConfiguration() => configure?.Invoke(this);

    [RelayCommand]
    private void OpenPriceDetails() => showPricing?.Invoke(this);

    [RelayCommand]
    private void ResetSkill()
    {
        suppressChanges = true;
        try
        {
            StartExperience = ProfileExperience;
            TargetExperience = TrainingPlanCalculator.MaximumExperience;
            IsMoneyMakingSelected = false;
            ConfigurationValues =
                Definition.Configurator?.GetDefinition(SelectedMethodOption?.Id).Normalize().ToDictionary()
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            hasPersonalRateOverride = false;
            var baseline = calculator.Calculate(
                Definition,
                StartExperience,
                TargetExperience,
                prices,
                methodId: SelectedMethodOption?.Id,
                pendingExperienceCredit: pendingExperienceCredit,
                configuration: ConfigurationValues);
            personalBaseRate = UnconfiguredBaseRate(baseline);
            PersonalRate = baseline.BaseRate;
        }
        finally
        {
            suppressChanges = false;
        }

        Recalculate();
        changed();
    }

    partial void OnStartExperienceChanged(long value)
    {
        if (suppressChanges)
            return;

        if (!hasPersonalRateOverride)
        {
            var baseline = calculator.Calculate(
                Definition,
                value,
                TargetExperience,
                prices,
                methodId: SelectedMethodOption?.Id,
                pendingExperienceCredit: pendingExperienceCredit,
                configuration: ConfigurationValues);
            suppressChanges = true;
            personalBaseRate = UnconfiguredBaseRate(baseline);
            PersonalRate = baseline.BaseRate;
            suppressChanges = false;
        }

        ChangedAndRecalculate();
    }
    partial void OnTargetExperienceChanged(long value) => ChangedAndRecalculate();
    partial void OnPersonalRateChanged(decimal value)
    {
        if (!suppressChanges)
        {
            hasPersonalRateOverride = value > 0m;
            personalBaseRate = value;
        }
        ChangedAndRecalculate();
    }
    partial void OnIsMoneyMakingSelectedChanged(bool value) => changed();
    partial void OnSelectedMethodOptionChanged(TrainingMethodOptionViewModel? value)
    {
        if (suppressChanges || value is null)
            return;

        ConfigurationValues = Definition.Configurator?.GetDefinition(value.Id)
            .NormalizeForMethodSelection(ConfigurationValues).ToDictionary()
            ?? ConfigurationValues;
        var baseline = calculator.Calculate(
            Definition,
            StartExperience,
            TargetExperience,
            prices,
            methodId: value.Id,
            pendingExperienceCredit: pendingExperienceCredit,
            configuration: ConfigurationValues);
        suppressChanges = true;
        hasPersonalRateOverride = false;
        personalBaseRate = UnconfiguredBaseRate(baseline);
        PersonalRate = baseline.BaseRate;
        suppressChanges = false;
        ChangedAndRecalculate();
    }

    private void ChangedAndRecalculate()
    {
        if (suppressChanges)
            return;
        Recalculate();
        changed();
    }

    private void Recalculate()
    {
        suppressChanges = true;
        try
        {
            Result = calculator.Calculate(
                Definition,
                StartExperience,
                TargetExperience,
                prices,
                hasPersonalRateOverride && personalBaseRate > 0m ? personalBaseRate : null,
                SelectedMethodOption?.Id,
                pendingExperienceCredit: pendingExperienceCredit,
                configuration: ConfigurationValues);

            if (hasPersonalRateOverride)
            {
                PersonalRate = Result.EffectiveRate;
            }
            else
            {
                personalBaseRate = UnconfiguredBaseRate(Result);
                PersonalRate = Result.BaseRate;
            }

            foreach (var option in MethodOptions)
                option.UpdateAvailability(Result.EffectiveStartExperience);

            var activeBand = Result.Method.Bands
                .OrderBy(band => band.StartExperience)
                .LastOrDefault(band => band.StartExperience <= Result.EffectiveStartExperience)
                ?? Result.Method.Bands.FirstOrDefault();
            Method = activeBand?.Method ?? "Passive / zero-time";
            var selectedUnlock = SelectedMethodOption?.UnlockExperience;
            HasAvailabilitySummary = selectedUnlock is { } unlock
                                     && Result.EffectiveStartExperience < unlock;
            AvailabilitySummary = HasAvailabilitySummary
                ? Result.TargetExperience <= selectedUnlock
                    ? $"{SelectedMethodOption!.RouteName} unlocks at level {SelectedMethodOption.UnlockLevel}; " +
                      $"this goal uses {Method}."
                    : $"Using {Method} until level {SelectedMethodOption!.UnlockLevel}."
                : string.Empty;
            Hours = Result.Hours == 0m ? "0" : Result.Hours.ToString("N1");
            TotalGp = Result.NetGp.HasValue ? DisplayFormat.Gp(Result.NetGp) : "Not priced";
            EconomicRate = !Result.IncludesActiveHours
                ? Result.GpPerExperience.HasValue
                    ? DisplayFormat.GpPerExperience(Result.GpPerExperience)
                    : "Not priced"
                : Result.AverageGpPerHour.HasValue
                    ? DisplayFormat.GpPerHour(Result.AverageGpPerHour)
                    : "Not priced";
            HasExperienceCredit = Result.AppliedExperienceCredit > 0;
            CreditSummary = HasExperienceCredit
                ? $"+{Result.AppliedExperienceCredit:N0} XP from " +
                  (string.IsNullOrWhiteSpace(pendingExperienceCreditSource)
                      ? "planned methods"
                      : pendingExperienceCreditSource)
                : string.Empty;
            IsProfit = Result.NetGp >= 0m;
            PricingStatus = Result.IsFullyPriced
                ? "Fully priced"
                : Result.PricedExperience > 0
                    ? $"{(decimal)Result.PricedExperience / Math.Max(1, Result.ExperienceRemaining):P2} priced"
                    : "Rate only";
        }
        finally
        {
            suppressChanges = false;
        }
    }

    private static decimal UnconfiguredBaseRate(TrainingSkillPlanResult result)
        => result.UnconfiguredBaseRate;

}

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
