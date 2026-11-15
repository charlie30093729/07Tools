namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Smithing;

[TestFixture]
[Category("Unit")]
public sealed class AnvilMethodsTests
{
    [Test]
    [Property("LegacyScenario", "PracticalBuyableMethods")]
    public void ReviewedUnlocksRatesAndEconomics()
    {
        var catalogue = new MainEhpCatalogue();

        var calculator = new TrainingPlanCalculator();

        var emptyPrices = new Dictionary<int, ItemPrice>();

        var smithing = catalogue.Skills.Single(skill => skill.Skill == "Smithing");

        var adamant = smithing.ResolveMethod("adamant-platebodies").Bands.Last();

        Assert.That(adamant.StartExperience, Is.EqualTo(4_382_299L), "Adamant platebody unlock XP");

        Assert.That(adamant.ExperiencePerHour, Is.EqualTo(260_400m).Within(0m), "Adamant platebody base rate");

        Assert.That(Resource(adamant, 2361).QuantityPerExperience, Is.EqualTo(5m / 312.5m).Within(0m), "adamantite bars per XP");

        Assert.That(Resource(adamant, 1123).QuantityPerExperience, Is.EqualTo(1m / 312.5m).Within(0m), "adamant platebodies per XP");

        var uniformAdamant = calculator.Calculate(
            smithing,
            4_382_299,
            4_707_299,
            emptyPrices,
            methodId: "adamant-platebodies",
            configuration: new Dictionary<string, string>
            {
                ["smiths-uniform"] = bool.TrueString
            });

        Assert.That(uniformAdamant.BaseRate, Is.EqualTo(325_000m).Within(0m), "Adamant platebody uniform rate");

        Assert.That(Resource(uniformAdamant.Method.Bands.Last(), 2361).QuantityPerExperience, Is.EqualTo(5m / 312.5m).Within(0m), "Smiths' uniform changes speed rather than bar consumption");

        var rune = smithing.ResolveMethod("rune-2h-swords").Bands.Last();

        Assert.That(rune.StartExperience, Is.EqualTo(13_034_431L), "Rune 2h unlock XP");

        Assert.That(rune.ExperiencePerHour, Is.EqualTo(217_000m).Within(0m), "Rune 2h base rate");

        Assert.That(Resource(rune, 2363).QuantityPerExperience, Is.EqualTo(3m / 225m).Within(0m), "runite bars per XP");

        Assert.That(Resource(rune, 1319).QuantityPerExperience, Is.EqualTo(1m / 225m).Within(0m), "rune 2h swords per XP");
    }
}
