using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RunescapeTools.Application.Training;
using RunescapeTools.Core.Market;
using RunescapeTools.Core.Training;

namespace RunescapeTools.Wpf.ViewModels;

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
