namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Herblore;

[TestFixture]
[Category("Unit")]
public sealed class SaradominBrewsTests
{
    [Test]
    [Property("LegacyScenario", "DeterministicMethodCatalogue")]
    public void ReviewedRateAndItemFlows()
    {
        var catalogue = new MainEhpCatalogue();

        var herblore = TrainingBand(catalogue, "Herblore", 2_192_818);

        Assert.That(herblore.ExperiencePerHour, Is.EqualTo(450_000m).Within(0m), "Herblore rate");

        Assert.That(herblore.Method, Is.EqualTo("Saradomin brews"), "Herblore method");

        Assert.That(string.Join('|', herblore.Economics!.Resources.Select(resource => resource.ItemId).Order()), Is.EqualTo("3002|6685|6693|21163"), "Herblore item IDs");

        Assert.That(Resource(herblore, 6693).QuantityPerExperience, Is.EqualTo(0.90m / 180m).Within(0m), "Prescription goggles secondary consumption");

        Assert.That(Resource(herblore, 21163).QuantityPerExperience, Is.EqualTo(0.15m / 10m / 180m).Within(0m), "Alchemist's amulet charge consumption");

        Assert.That(Resource(herblore, 6685).QuantityPerExperience, Is.EqualTo((3m + 0.15m) / 4m / 180m).Within(0m), "four-dose Alchemist's amulet brew output");

    }
}
