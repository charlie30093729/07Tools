using RunescapeTools.Core.Training;

namespace RunescapeTools.Wpf.ViewModels;

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
