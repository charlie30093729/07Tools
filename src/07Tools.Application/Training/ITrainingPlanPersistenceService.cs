namespace RunescapeTools.Application.Training;

public interface ITrainingPlanPersistenceService
{
    Task<IReadOnlyDictionary<string, TrainingSkillPreference>> GetAsync(
        string rsn,
        CancellationToken cancellationToken = default);

    void QueueSave(string rsn, IReadOnlyCollection<TrainingSkillPreference> preferences);

    Task FlushAsync(CancellationToken cancellationToken = default);
}
