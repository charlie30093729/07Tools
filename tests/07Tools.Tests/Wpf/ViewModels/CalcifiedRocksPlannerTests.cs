namespace RunescapeTools.Tests.Wpf.ViewModels;

[TestFixture]
[Category("Unit")]
public sealed class CalcifiedRocksPlannerTests
{
    private static XpPlannerRowViewModel Create(TrainingSkillPreference? preference = null) => new(
        new MainEhpCatalogue().Skills.Single(skill => skill.Skill == "Mining"),
        new TrainingPlanCalculator(), 13034431, preference,
        new Dictionary<int, ItemPrice> { [23959] = Quote(23959, 3000000) }, () => { });

    [Test]
    public void SelectionRateOverrideAndResetRoundTripThroughPersistence()
    {
        var row = Create();
        row.SelectedMethodOption = row.MethodOptions.Single(option => option.Id == "calcified-rocks-crystal-pickaxe");
        Assert.That(row.PersonalRate, Is.EqualTo(50225m));
        row.PersonalRate = 45000m;
        row.TargetExperience = 50000000;
        Assert.That(row.HasConfiguration, Is.True);
        row.ApplyConfiguration(new Dictionary<string, string> { ["prospector-outfit"] = "False" });
        Assert.That(row.PersonalRate, Is.EqualTo(45000m));
        row.ApplyConfiguration(new Dictionary<string, string> { ["prospector-outfit"] = "True" });
        Assert.That(row.PersonalRate, Is.EqualTo(46125m));
        var restored = Create(row.ToPreference());
        Assert.That(restored.SelectedMethodOption?.Id, Is.EqualTo("calcified-rocks-crystal-pickaxe"));
        Assert.That(restored.PersonalRate, Is.EqualTo(46125m));
        Assert.That(restored.Result.Hours, Is.EqualTo(row.Result.Hours));
        restored.ResetSkillCommand.Execute(null);
        Assert.That(restored.PersonalRate, Is.EqualTo(50225m));
        Assert.That(restored.ConfigurationValues["prospector-outfit"], Is.EqualTo(bool.TrueString));
        Assert.That(restored.ToPreference().ExperiencePerHourOverride, Is.Null);
    }

}
