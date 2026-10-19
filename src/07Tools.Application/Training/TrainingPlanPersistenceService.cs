namespace RunescapeTools.Application.Training;

// Owns profile-bound snapshots until the existing store has successfully written them.
public sealed class TrainingPlanPersistenceService(ITrainingPlanStore store) : ITrainingPlanPersistenceService
{
    private readonly object sync = new();
    private readonly SemaphoreSlim flushGate = new(1, 1);
    private readonly Dictionary<string, PendingSave> pending = new(StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyDictionary<string, TrainingSkillPreference>> GetAsync(
        string rsn,
        CancellationToken cancellationToken = default)
    {
        var key = NormalizeRsn(rsn);
        cancellationToken.ThrowIfCancellationRequested();
        lock (sync)
        {
            if (pending.TryGetValue(key, out var queued))
                return ToDictionary(queued.Preferences);
        }

        await flushGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var saved = await store.GetAsync(key, cancellationToken).ConfigureAwait(false);
            lock (sync)
            {
                // Hold the write gate so a newer queued edit cannot be flushed away during this read.
                return pending.TryGetValue(key, out var queued)
                    ? ToDictionary(queued.Preferences)
                    : ToDictionary(saved.Values);
            }
        }
        finally
        {
            flushGate.Release();
        }
    }

    public void QueueSave(string rsn, IReadOnlyCollection<TrainingSkillPreference> preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        var key = NormalizeRsn(rsn);
        var snapshot = new PendingSave(key, preferences.Select(Clone).ToArray());
        lock (sync)
            pending[key] = snapshot;
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await flushGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                PendingSave? snapshot;
                lock (sync)
                    snapshot = pending.Values.FirstOrDefault();
                if (snapshot is null)
                    return;

                await store.SaveAsync(snapshot.Rsn, snapshot.Preferences, cancellationToken)
                    .ConfigureAwait(false);
                lock (sync)
                {
                    // Never discard a newer edit that arrived while this write was in flight.
                    if (pending.TryGetValue(snapshot.Rsn, out var current)
                        && ReferenceEquals(current, snapshot))
                        pending.Remove(snapshot.Rsn);
                }
            }
        }
        finally
        {
            flushGate.Release();
        }
    }

    private static string NormalizeRsn(string rsn) =>
        string.IsNullOrWhiteSpace(rsn)
            ? throw new ArgumentException("An RSN is required.", nameof(rsn))
            : rsn.Trim();

    private static TrainingSkillPreference Clone(TrainingSkillPreference preference) => preference with
    {
        Configuration = preference.Configuration is null
            ? null
            : new Dictionary<string, string>(preference.Configuration, StringComparer.OrdinalIgnoreCase)
    };

    private static IReadOnlyDictionary<string, TrainingSkillPreference> ToDictionary(
        IEnumerable<TrainingSkillPreference> preferences) =>
        preferences.Select(Clone).ToDictionary(value => value.Skill, StringComparer.OrdinalIgnoreCase);

    private sealed record PendingSave(string Rsn, TrainingSkillPreference[] Preferences);
}
