namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Crafting;

[TestFixture]
[Category("Unit")]
public sealed class AirBattlestavesTests
{
    [Test]
    [Property("LegacyScenario", "PracticalBuyableMethods")]
    public void ReviewedUnlocksRatesAndEconomics()
    {
        var catalogue = new MainEhpCatalogue();

        var crafting = catalogue.Skills.Single(skill => skill.Skill == "Crafting");

        var airStaves = crafting.ResolveMethod("air-battlestaves").Bands.Last();

        Assert.That(airStaves.StartExperience, Is.EqualTo(496_254L), "Air battlestaff unlock XP");

        Assert.That(airStaves.ExperiencePerHour, Is.EqualTo(336_875m).Within(0m), "Air battlestaff rate");

        Assert.That(Resource(airStaves, 1391).QuantityPerExperience, Is.EqualTo(1m / 137.5m).Within(0m), "battlestaves per XP");

        Assert.That(Resource(airStaves, 573).QuantityPerExperience, Is.EqualTo(1m / 137.5m).Within(0m), "air orbs per XP");

        Assert.That(Resource(airStaves, 1397).QuantityPerExperience, Is.EqualTo(1m / 137.5m).Within(0m), "air battlestaves per XP");
    }
}
