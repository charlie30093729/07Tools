namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Fletching;

[TestFixture]
[Category("Unit")]
public sealed class AdamantDartsTests
{
    [Test]
    [Property("LegacyScenario", "PracticalBuyableMethods")]
    public void ReviewedUnlocksRatesAndEconomics()
    {
        var catalogue = new MainEhpCatalogue();

        var fletching = catalogue.Skills.Single(skill => skill.Skill == "Fletching");

        var adamantDarts = fletching.ResolveMethod("adamant-darts").Bands.Last();

        Assert.That(adamantDarts.StartExperience, Is.EqualTo(737_627L), "Adamant dart unlock XP");

        Assert.That(adamantDarts.ExperiencePerHour, Is.EqualTo(300_000m).Within(0m), "Adamant dart rate");

        Assert.That(Resource(adamantDarts, 823).QuantityPerExperience, Is.EqualTo(1m / 15m).Within(0m), "adamant dart tips per XP");

        Assert.That(Resource(adamantDarts, 314).QuantityPerExperience, Is.EqualTo(1m / 15m).Within(0m), "adamant dart feathers per XP");

        Assert.That(Resource(adamantDarts, 810).QuantityPerExperience, Is.EqualTo(1m / 15m).Within(0m), "adamant darts per XP");
    }
}
