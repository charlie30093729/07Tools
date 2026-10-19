using CommunityToolkit.Mvvm.ComponentModel;
using RunescapeTools.Core.Training;

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
