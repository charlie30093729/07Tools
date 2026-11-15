namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Firemaking;

[TestFixture]
[Category("Unit")]
public sealed class FiremakingConfigurationTests
{
    [Test]
    [Property("LegacyScenario", "TrainingSkillConfiguration")]
    public void PyromancerAndBonfiresAdjustRatesAndConsumption()
    {
        var catalogue = new MainEhpCatalogue();

        var calculator = new TrainingPlanCalculator();

        var prices = new Dictionary<int, ItemPrice>();

        var firemaking = catalogue.Skills.Single(skill => skill.Skill == "Firemaking");

        var pyromancer = calculator.Calculate(
            firemaking,
            13_034_431,
            13_034_431 + 639_292,
            prices);

        Assert.That(pyromancer.BaseRate, Is.EqualTo(623_700m * 1.025m).Within(0m), "Pyromancer default rate");

        Assert.That(Resource(pyromancer.Method.Bands[^1], 32910).QuantityPerExperience, Is.EqualTo(1m / (420m * 1.025m)).Within(0m), "Pyromancer rosewood consumption");

        var bonfire = calculator.Calculate(
            firemaking,
            13_034_431,
            13_034_431 + 267_832,
            prices,
            configuration: new Dictionary<string, string>
            {
                ["pyromancer-outfit"] = bool.TrueString,
                ["bonfire"] = bool.TrueString
            });

        Assert.That(bonfire.BaseRate, Is.EqualTo(268m * 665m * 1.025m).Within(0m), "automatic bonfire rate");

        Assert.That(bonfire.Method.Bands[^1].Method, Is.EqualTo("Rosewood logs - bonfire"), "bonfire method label");

        var redwood = calculator.Calculate(
            firemaking,
            5_346_332,
            5_346_332 + 532_744,
            prices,
            methodId: "redwood-logs");

        Assert.That(redwood.BaseRate, Is.EqualTo(350m * 1_485m * 1.025m).Within(0m), "normal redwood rate");

        Assert.That(redwood.Method.Bands[^1].Method, Is.EqualTo("Redwood logs - normal burning"), "normal redwood method label");

        Assert.That(Resource(redwood.Method.Bands[^1], 19669).QuantityPerExperience, Is.EqualTo(1m / (350m * 1.025m)).Within(0.0000000001m), "Pyromancer redwood consumption");

        var redwoodBonfire = calculator.Calculate(
            firemaking,
            5_346_332,
            5_346_332 + 349_782,
            prices,
            methodId: "redwood-logs",
            configuration: new Dictionary<string, string>
            {
                ["pyromancer-outfit"] = bool.TrueString,
                ["bonfire"] = bool.TrueString
            });

        Assert.That(redwoodBonfire.BaseRate, Is.EqualTo(350m * 665m * 1.025m).Within(0m), "redwood bonfire rate");

        Assert.That(redwoodBonfire.Method.Bands[^1].Method, Is.EqualTo("Redwood logs - bonfire"), "redwood bonfire method label");

        Assert.That(Resource(redwoodBonfire.Method.Bands[^1], 19669).QuantityPerExperience, Is.EqualTo(Resource(redwood.Method.Bands[^1], 19669).QuantityPerExperience).Within(0m), "redwood bonfire preserves XP per log");
    }
}
