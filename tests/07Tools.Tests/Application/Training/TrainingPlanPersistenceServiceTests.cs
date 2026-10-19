namespace RunescapeTools.Tests.Application.Training;

[TestFixture]
[Category("Unit")]
public sealed class TrainingPlanPersistenceServiceTests
{
    [Test]
    public async Task FlushKeepsEachProfilesLatestSnapshotAndDoesNotRewriteUnchangedProfiles()
    {
        var store = new ControlledStore();
        await store.Saved.SaveAsync("Untouched", [Preference(9_000)]);
        var service = new TrainingPlanPersistenceService(store);
        service.QueueSave(" First ", [Preference(1_000)]);
        service.QueueSave("first", [Preference(2_000)]);
        service.QueueSave("Second", [Preference(3_000)]);

        await service.FlushAsync();
        await service.FlushAsync();

        Assert.That(store.Writes, Has.Count.EqualTo(2));
        Assert.That((await store.Saved.GetAsync("First"))["Test skill"].TargetExperience, Is.EqualTo(2_000));
        Assert.That((await store.Saved.GetAsync("Second"))["Test skill"].TargetExperience, Is.EqualTo(3_000));
        Assert.That((await store.Saved.GetAsync("Untouched"))["Test skill"].TargetExperience, Is.EqualTo(9_000));
    }

    [Test]
    public async Task QueuedSnapshotAndItsConfigurationAreIndependentOfCallerAndReadResultMutations()
    {
        var store = new ControlledStore();
        var service = new TrainingPlanPersistenceService(store);
        var configuration = new Dictionary<string, string> { ["equipment"] = "original" };
        var values = new[] { Preference(1_000) with { Configuration = configuration } };
        service.QueueSave("Player", values);
        configuration["equipment"] = "changed";
        values[0] = Preference(8_000);
        var pending = await service.GetAsync("player");
        pending["Test skill"].Configuration!["equipment"] = "changed again";

        await service.FlushAsync();

        var saved = (await store.Saved.GetAsync("Player"))["Test skill"];
        Assert.That(saved.TargetExperience, Is.EqualTo(1_000));
        Assert.That(saved.Configuration!["equipment"], Is.EqualTo("original"));
    }

    [Test]
    public async Task FlushWaitsForAnActiveWriteAndSavesNewerEditsWithoutConcurrentWrites()
    {
        var store = new ControlledStore();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        store.BeforeSave = async (_, token) =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(token);
        };
        var service = new TrainingPlanPersistenceService(store);
        service.QueueSave("Player", [Preference(1_000)]);
        var firstFlush = service.FlushAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        service.QueueSave("Player", [Preference(2_000)]);
        var closingFlush = service.FlushAsync();
        Assert.That(closingFlush.IsCompleted, Is.False, "Shutdown must wait for an active write.");
        release.SetResult();
        await Task.WhenAll(firstFlush, closingFlush).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(store.MaximumConcurrentWrites, Is.EqualTo(1));
        Assert.That(store.Writes.Select(write => write.TargetExperience), Is.EqualTo(new long[] { 1_000, 2_000 }));
        Assert.That((await store.Saved.GetAsync("Player"))["Test skill"].TargetExperience, Is.EqualTo(2_000));
    }

    [Test]
    public async Task FailedFlushRetainsThePendingSnapshotForRetry()
    {
        var store = new ControlledStore { BeforeSave = (_, _) => throw new IOException("Disk unavailable") };
        var service = new TrainingPlanPersistenceService(store);
        service.QueueSave("Player", [Preference(1_000)]);

        Assert.ThrowsAsync<IOException>(new Func<Task>(() => service.FlushAsync()));
        Assert.That((await service.GetAsync("Player"))["Test skill"].TargetExperience, Is.EqualTo(1_000));
        Assert.That(await store.Saved.GetAsync("Player"), Is.Empty);
        store.BeforeSave = null;
        await service.FlushAsync();
        Assert.That((await store.Saved.GetAsync("Player"))["Test skill"].TargetExperience, Is.EqualTo(1_000));
    }

    [Test]
    public async Task CancelledFlushRetainsThePendingSnapshotAndCanBeRetried()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new ControlledStore
        {
            BeforeSave = async (_, token) =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
        };
        var service = new TrainingPlanPersistenceService(store);
        service.QueueSave("Player", [Preference(1_000)]);
        using var closing = new CancellationTokenSource();
        var flush = service.FlushAsync(closing.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        closing.Cancel();
        Assert.CatchAsync<OperationCanceledException>(new Func<Task>(async () => await flush));

        store.BeforeSave = null;
        await service.FlushAsync();
        Assert.That((await store.Saved.GetAsync("Player"))["Test skill"].TargetExperience, Is.EqualTo(1_000));
    }

    private static TrainingSkillPreference Preference(long target) => new("Test skill", target);

    [Test]
    public async Task EditQueuedDuringADiskReadTakesPrecedenceOverTheOldDiskSnapshot()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new ControlledStore
        {
            BeforeRead = async (_, token) =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(token);
            }
        };
        await store.Saved.SaveAsync("Player", [Preference(1_000)]);
        var service = new TrainingPlanPersistenceService(store);
        var read = service.GetAsync("Player");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        service.QueueSave("Player", [Preference(2_000)]);
        var flush = service.FlushAsync();
        release.SetResult();

        Assert.That((await read.WaitAsync(TimeSpan.FromSeconds(5)))["Test skill"].TargetExperience, Is.EqualTo(2_000));
        await flush.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That((await store.Saved.GetAsync("Player"))["Test skill"].TargetExperience, Is.EqualTo(2_000));
    }

    private sealed class ControlledStore : ITrainingPlanStore
    {
        private int activeWrites;
        public MemoryTrainingPlanStore Saved { get; } = new();
        public Func<string, CancellationToken, Task>? BeforeSave { get; set; }
        public Func<string, CancellationToken, Task>? BeforeRead { get; init; }
        public List<TrainingSkillPreference> Writes { get; } = [];
        public int MaximumConcurrentWrites { get; private set; }

        public async Task<IReadOnlyDictionary<string, TrainingSkillPreference>> GetAsync(
            string rsn, CancellationToken cancellationToken = default)
        {
            var saved = await Saved.GetAsync(rsn, cancellationToken);
            if (BeforeRead is not null)
                await BeforeRead(rsn, cancellationToken);
            return saved;
        }

        public async Task SaveAsync(string rsn, IReadOnlyCollection<TrainingSkillPreference> preferences,
            CancellationToken cancellationToken = default)
        {
            var active = Interlocked.Increment(ref activeWrites);
            MaximumConcurrentWrites = Math.Max(MaximumConcurrentWrites, active);
            try
            {
                if (BeforeSave is not null)
                    await BeforeSave(rsn, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await Saved.SaveAsync(rsn, preferences, cancellationToken);
                Writes.AddRange(preferences);
            }
            finally
            {
                Interlocked.Decrement(ref activeWrites);
            }
        }
    }
}
