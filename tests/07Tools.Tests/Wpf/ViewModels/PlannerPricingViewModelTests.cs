namespace RunescapeTools.Tests.Wpf.ViewModels;

[TestFixture]
public sealed class PlannerPricingViewModelTests
{
    [Test]
    public async Task ToggleKeepsOldSnapshotWhileLoadingThenRepricesAllRowsWithoutChangingHours()
    {
        var pricing = new ControlledPricing();
        var vm = Create(pricing);
        await vm.LoadAsync();
        var hours = vm.Rows.Select(r => r.Result.Hours).ToArray();
        var quantities = vm.Rows.Select(r => r.Result.ResourceRequirements.ToArray()).ToArray();
        var before = vm.TotalNetGp;
        pricing.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.UseThirtyDayPrices = true;
        Assert.That(vm.IsPricingLoading, Is.True);
        Assert.That(vm.TotalNetGp, Is.EqualTo(before));
        pricing.Pending.SetResult(true);
        await WaitForPricing(vm);
        Assert.That(vm.PricingStatus, Does.Contain("30-day"));
        Assert.That(vm.TotalNetGp, Is.Not.EqualTo(before));
        Assert.That(vm.Rows.Select(r => r.Result.Hours), Is.EqualTo(hours));
        Assert.That(vm.Rows.Select(r => r.Result.ResourceRequirements.ToArray()), Is.EqualTo(quantities));
        Assert.That(pricing.SavedMode, Is.EqualTo(PricingMode.ThirtyDayAverage));
        var row = vm.Rows.Single(r => r.Skill == "Prayer");
        Assert.That(new TrainingPriceDialogViewModel(row.Skill, row.Result, pricing.Last!.Prices).PricingNote,
            Does.Contain("not current offers"));
    }

    [Test]
    public async Task FailureRetainsValidPricesAndRestoresAppliedMode()
    {
        var pricing = new ControlledPricing();
        var vm = Create(pricing);
        await vm.LoadAsync();
        var before = vm.TotalNetGp;
        pricing.Fail = true;
        vm.UseThirtyDayPrices = true;
        await WaitForPricing(vm);
        Assert.That(vm.UseThirtyDayPrices, Is.False);
        Assert.That(vm.TotalNetGp, Is.EqualTo(before));
        Assert.That(vm.ErrorMessage, Does.Contain("previous snapshot"));
        Assert.That(pricing.SavedMode, Is.EqualTo(PricingMode.Live));
    }

    [Test]
    public async Task RapidToggleCannotApplyLateHistoricalResult()
    {
        var pricing = new ControlledPricing();
        var vm = Create(pricing);
        await vm.LoadAsync();
        pricing.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.UseThirtyDayPrices = true;
        vm.UseThirtyDayPrices = false;
        pricing.Pending.SetResult(true);
        await WaitForPricing(vm);
        Assert.That(vm.UseThirtyDayPrices, Is.False);
        Assert.That(vm.PricingStatus, Is.EqualTo("Live prices"));
        Assert.That(pricing.SavedMode, Is.EqualTo(PricingMode.Live));
    }

    [Test]
    public async Task SavedAverageModeRestoresOnInitialLoad()
    {
        var pricing = new ControlledPricing { SavedMode = PricingMode.ThirtyDayAverage };
        var vm = Create(pricing);
        await vm.LoadAsync();
        Assert.That(vm.UseThirtyDayPrices, Is.True);
        Assert.That(vm.PricingStatus, Does.Contain("30-day"));
    }

    [Test]
    public async Task AllocatedIncomeIsRepricedInsteadOfReusingLiveProfitAndUpdatesAccounts()
    {
        using var folder = new TemporaryDirectory();
        var store = new JsonPriceHistoryStore(new(folder.Path));
        await store.WriteModeAsync(PricingMode.ThirtyDayAverage, default);
        var market = new FakeMarketDataService
        { History = RunescapeTools.Tests.Core.Market.HistoricalPriceCalculatorTests.History() };
        var service = new PlannerPricingService(market, store,
            new TestTimeProvider(RunescapeTools.Tests.Core.Market.HistoricalPriceCalculatorTests.End));
        var selection = new MoneyMakerSelectionContext();
        var method = new MoneyMakingMethodDefinition("test", "Test alt", "", 88, 5, 0.02m,
            [new(1, "Input", 1, ItemFlowDirection.Input, QuantityBasis.PerAction),
             new(2, "Output", 4, ItemFlowDirection.Output, QuantityBasis.PerAction)]);
        selection.Select("test", "Test alt", 999999, 5, false, method);
        var vm = new XpPlannerViewModel(new PrayerCatalogue(), new TrainingPlanCalculator(),
            new TrainingMoneyMakingCalculator(), market, new TrainingPlanPersistenceService(new MemoryTrainingPlanStore()),
            new CurrentProfileContext(new FakeHiscoreClient(), new HiscoreParser(TimeProvider.System),
                new MemoryProfilePreferenceStore("bottleo")), selection, plannerPricing: service);
        await vm.LoadAsync();
        var row = vm.Rows.Single();
        row.IsMoneyMakingSelected = true;
        var perAccount = (4 * 0.98m - 2) * 88;
        Assert.That(vm.MoneyMakerGpContribution, Is.EqualTo(row.Result.Hours * perAccount * 5));
        selection.Select("test", "Test alt", 999999, 2, false, method with { Accounts = 2 });
        await WaitForPricing(vm);
        Assert.That(vm.MoneyMakerGpContribution, Is.EqualTo(row.Result.Hours * perAccount * 2));
        selection.Clear();
        await WaitForPricing(vm);
        Assert.That(vm.MoneyMakerGpContribution, Is.Zero);
    }

    private sealed class PrayerCatalogue : IEhpCatalogue
    {
        public string Version => "Test";
        public DateOnly VerifiedOn => new(2026, 9, 9);
        public IReadOnlyList<TrainingSkillDefinition> Skills { get; } =
            [new MainEhpCatalogue().Skills.Single(s => s.Skill == "Prayer")];
    }

    private static XpPlannerViewModel Create(IPlannerPricingService pricing) => new(
        new MainEhpCatalogue(), new TrainingPlanCalculator(), new TrainingMoneyMakingCalculator(),
        new FakeMarketDataService(), new TrainingPlanPersistenceService(new MemoryTrainingPlanStore()),
        new CurrentProfileContext(new FakeHiscoreClient(), new HiscoreParser(TimeProvider.System),
            new MemoryProfilePreferenceStore("bottleo")), new MoneyMakerSelectionContext(), plannerPricing: pricing);

    private static async Task WaitForPricing(XpPlannerViewModel vm)
    {
        for (var i = 0; i < 200 && vm.IsPricingLoading; i++) await Task.Delay(10);
        Assert.That(vm.IsPricingLoading, Is.False, "Pricing request should finish");
    }

    private sealed class ControlledPricing : IPlannerPricingService
    {
        public TaskCompletionSource<bool>? Pending { get; set; }
        public bool Fail { get; set; }
        public PricingMode SavedMode { get; set; }
        public PlannerPriceSnapshot? Last { get; private set; }
        public Task<PricingMode> ReadModeAsync(CancellationToken cancellationToken = default) => Task.FromResult(SavedMode);
        public Task SaveModeAsync(PricingMode mode, CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); SavedMode = mode; return Task.CompletedTask; }
        public async Task<PlannerPriceSnapshot> GetAsync(IEnumerable<int> ids, PricingMode mode,
            MoneyMakingMethodDefinition? moneyMaker = null, CancellationToken cancellationToken = default)
        {
            if (Pending is not null) await Pending.Task.WaitAsync(cancellationToken);
            if (Fail) throw new HttpRequestException("Test network failure");
            var price = mode == PricingMode.Live ? 100m : 200.5m;
            return Last = new(mode, DateTimeOffset.UtcNow, ids.Distinct().ToDictionary(id => id,
                id => new ItemPrice(id, price, price, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, mode)), null, false, 0);
        }
    }
}
