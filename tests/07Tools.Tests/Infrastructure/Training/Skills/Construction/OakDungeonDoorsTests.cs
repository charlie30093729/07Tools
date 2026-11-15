namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Construction;

[TestFixture]
[Category("Unit")]
public sealed class OakDungeonDoorsTests
{
    [Test]
    [Property("LegacyScenario", "PracticalBuyableMethods")]
    public void ReviewedUnlocksRatesAndEconomics()
    {
        var catalogue = new MainEhpCatalogue();

        var construction = catalogue.Skills.Single(skill => skill.Skill == "Construction");

        var doors = construction.ResolveMethod("oak-dungeon-doors").Bands.Last();

        Assert.That(doors.StartExperience, Is.EqualTo(1_210_421L), "Oak dungeon door unlock XP");

        Assert.That(doors.ExperiencePerHour, Is.EqualTo(550_000m).Within(0m), "Oak dungeon door rate");

        Assert.That(Resource(doors, 8778).QuantityPerExperience, Is.EqualTo(1m / 60m).Within(0m), "oak planks per XP");

        Assert.That(doors.Economics!.FixedGpPerExperience, Is.EqualTo(1_250m / 25m / 60m).Within(0m), "Oak dungeon door servant fee");
    }
}
