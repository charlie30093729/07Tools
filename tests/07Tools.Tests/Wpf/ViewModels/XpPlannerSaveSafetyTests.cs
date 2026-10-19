namespace RunescapeTools.Tests.Wpf.ViewModels;

[TestFixture]
[Category("Unit")]
public sealed class XpPlannerSaveSafetyTests
{
    [Test]
    public async Task PendingEditIsSavedForItsOriginalProfileAfterTheCurrentProfileChanges()
    {
        var store = new MemoryTrainingPlanStore();
        await store.SaveAsync("Other player", [new TrainingSkillPreference("Test skill", 8_000)]);
        var context = new CurrentProfileContext(new FakeHiscoreClient(),
            new HiscoreParser(TimeProvider.System), new MemoryProfilePreferenceStore("First player"));
        var planner = new XpPlannerViewModel(new TestCatalogue(), new TrainingPlanCalculator(),
            new TrainingMoneyMakingCalculator(), new FakeMarketDataService(), new TrainingPlanPersistenceService(store), context,
            new MoneyMakerSelectionContext());
        await planner.LoadAsync();

        planner.Rows.Single().TargetExperience = 1_000;
        await context.LoadProfileAsync("Other player");
        await planner.SaveCommand.ExecuteAsync(null);

        var original = await store.GetAsync("First player");
        var other = await store.GetAsync("Other player");
        Assert.That(original.ContainsKey("Test skill"), Is.True,
            "The pending edit must retain its original profile identity.");
        Assert.That(original["Test skill"].TargetExperience, Is.EqualTo(1_000));
        Assert.That(other["Test skill"].TargetExperience, Is.EqualTo(8_000),
            "Switching profiles must not overwrite the other player's settings.");
    }

    [Test]
    public async Task SwitchingBackBeforeTheDebounceRestoresPendingEditsAndFlushesBothProfiles()
    {
        var store = new MemoryTrainingPlanStore();
        var plans = new TrainingPlanPersistenceService(store);
        var context = new CurrentProfileContext(new FakeHiscoreClient(),
            new HiscoreParser(TimeProvider.System), new MemoryProfilePreferenceStore("First player"));
        var planner = new XpPlannerViewModel(new TestCatalogue(), new TrainingPlanCalculator(),
            new TrainingMoneyMakingCalculator(), new FakeMarketDataService(), plans, context,
            new MoneyMakerSelectionContext());
        await planner.LoadAsync();
        planner.Rows.Single().TargetExperience = 1_000;
        await context.LoadProfileAsync("Other player");
        await planner.LoadAsync();
        planner.Rows.Single().TargetExperience = 2_000;
        await context.LoadProfileAsync("First player");
        await planner.LoadAsync();

        Assert.That(planner.Rows.Single().TargetExperience, Is.EqualTo(1_000));
        await plans.FlushAsync();
        Assert.That((await store.GetAsync("First player"))["Test skill"].TargetExperience, Is.EqualTo(1_000));
        Assert.That((await store.GetAsync("Other player"))["Test skill"].TargetExperience, Is.EqualTo(2_000));
    }

    private sealed class TestCatalogue : IEhpCatalogue
    {
        public string Version => "Save safety test";
        public DateOnly VerifiedOn => new(2026, 10, 19);
        public IReadOnlyList<TrainingSkillDefinition> Skills { get; } =
            [new("Test skill", [new TrainingRateBand(0, 100, "Test method")])];
    }

    [Test]
    public async Task ADelayedOldProfileLoadCannotReplaceTheNewProfileOrStealItsSaveIdentity()
    {
        var store = new MemoryTrainingPlanStore();
        var plans = new TrainingPlanPersistenceService(store);
        var delayedPlans = new DelayedReadPersistence(plans);
        var context = new CurrentProfileContext(new FakeHiscoreClient(),
            new HiscoreParser(TimeProvider.System), new MemoryProfilePreferenceStore("First player"));
        var planner = new XpPlannerViewModel(new TestCatalogue(), new TrainingPlanCalculator(),
            new TrainingMoneyMakingCalculator(), new FakeMarketDataService(), delayedPlans, context,
            new MoneyMakerSelectionContext());
        await planner.LoadAsync();
        planner.Rows.Single().TargetExperience = 1_000;
        await context.LoadProfileAsync("Delayed player");
        var oldLoad = planner.LoadAsync();
        await delayedPlans.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await context.LoadProfileAsync("Newest player");
        await planner.LoadAsync();
        planner.Rows.Single().TargetExperience = 3_000;
        delayedPlans.Release.SetResult();
        await oldLoad.WaitAsync(TimeSpan.FromSeconds(5));
        await planner.SaveCommand.ExecuteAsync(null);

        Assert.That(planner.ProfileName, Is.EqualTo("Newest player"));
        Assert.That((await store.GetAsync("First player"))["Test skill"].TargetExperience, Is.EqualTo(1_000));
        Assert.That((await store.GetAsync("Newest player"))["Test skill"].TargetExperience, Is.EqualTo(3_000));
        Assert.That(await store.GetAsync("Delayed player"), Is.Empty);
    }

    private sealed class DelayedReadPersistence(ITrainingPlanPersistenceService inner)
        : ITrainingPlanPersistenceService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<IReadOnlyDictionary<string, TrainingSkillPreference>> GetAsync(
            string rsn, CancellationToken cancellationToken = default)
        {
            var result = await inner.GetAsync(rsn, cancellationToken);
            if (rsn == "Delayed player")
            {
                Started.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
            return result;
        }

        public void QueueSave(string rsn, IReadOnlyCollection<TrainingSkillPreference> preferences) =>
            inner.QueueSave(rsn, preferences);
        public Task FlushAsync(CancellationToken cancellationToken = default) => inner.FlushAsync(cancellationToken);
    }
}
