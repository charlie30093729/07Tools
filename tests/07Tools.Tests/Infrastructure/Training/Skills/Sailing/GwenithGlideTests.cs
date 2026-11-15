namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Sailing;

[TestFixture]
[Category("Unit")]
public sealed class GwenithGlideTests
{
    [Test]
    [Property("LegacyScenario", "PhaseThreeMethodCatalogue")]
    public void ReviewedRatesAndItemFlows()
    {
        var catalogue = new MainEhpCatalogue();

        var sailing = TrainingBand(catalogue, "Sailing", 0);

        Assert.That(sailing.ExperiencePerHour, Is.EqualTo(240_000m).Within(0m), "Gwenith Glide rate");

        Assert.That(sailing.Method, Is.EqualTo("Gwenith Glide - rosewood hull"), "Sailing method");

        Assert.That(Resource(sailing, 12695).QuantityPerHour, Is.EqualTo(48.12m).Within(0m), "regular potions per hour");

        Assert.That(Resource(sailing, 23685).QuantityPerHour, Is.EqualTo(48.12m).Within(0m), "divine potions per hour");

        Assert.That(Resource(sailing, 12695).Direction, Is.EqualTo(TrainingFlowDirection.Input), "regular potion direction");

        Assert.That(Resource(sailing, 23685).Direction, Is.EqualTo(TrainingFlowDirection.Output), "divine potion direction");

        Assert.That(Resource(sailing, 23685).SubjectToGeTax, Is.True, "divine potions should be GE taxed");

    }

    [Test]
    [Property("LegacyScenario", "PhaseThreeTrainingCalculations")]
    public void FullRouteAndPersonalRateEconomics()
    {
        var catalogue = new MainEhpCatalogue();

        var calculator = new TrainingPlanCalculator();

        var prices = new Dictionary<int, ItemPrice>
        {
            [12695] = Quote(12695, 14_000),
            [23685] = Quote(23685, 18_000)
        };

        var sailingDefinition = catalogue.Skills.Single(skill => skill.Skill == "Sailing");

        var sailing = calculator.Calculate(
            sailingDefinition,
            0,
            TrainingPlanCalculator.MaximumExperience,
            prices);

        Assert.That(sailing.Hours, Is.EqualTo(833.3333m).Within(0.0001m), "Gwenith Glide 0-200m hours");

        Assert.That(sailing.ResourceRequirements.Single(item => item.ItemId == 12695).Quantity,
            Is.EqualTo(40_100m).Within(0.0001m), "Gwenith regular potions");

        Assert.That(sailing.ResourceRequirements.Single(item => item.ItemId == 23685).Quantity,
            Is.EqualTo(40_100m).Within(0.0001m), "Gwenith divine potions");

        Assert.That(sailing.NetGp ?? 0m, Is.EqualTo(40_100m * (18_000m - 360m - 14_000m)).Within(0.01m), "Gwenith potion profit after tax");

        Assert.That(sailing.IsFullyPriced, Is.True, "Gwenith projection should be fully priced");
        Assert.That(sailing.GeneratedExperience, Is.Empty, "Gwenith should not grant secondary skill XP");

        var fasterSailing = calculator.Calculate(
            sailingDefinition,
            0,
            TrainingPlanCalculator.MaximumExperience,
            prices,
            300_000m);

        Assert.That(fasterSailing.Hours, Is.EqualTo(666.6667m).Within(0.0001m), "personal Sailing rate hours");

        Assert.That(fasterSailing.NetGp ?? 0m, Is.EqualTo((sailing.NetGp ?? 0m) * 0.8m).Within(0.01m), "hourly Sailing resources scale with hours");
    }
}
