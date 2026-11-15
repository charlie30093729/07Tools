namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Woodcutting;

[TestFixture]
[Category("Unit")]
public sealed class TeakTreesTests
{
    [Test]
    [Property("LegacyScenario", "PhaseTwoMethodCatalogue")]
    public void ReviewedRatesAndItemFlows()
    {
        var catalogue = new MainEhpCatalogue();

        var woodcutting = TrainingBand(catalogue, "Woodcutting", 814_445);

        Assert.That(woodcutting.ExperiencePerHour, Is.EqualTo(194_022m).Within(0m), "Woodcutting level-71 rate");

        Assert.That(woodcutting.Method, Is.EqualTo("1.5t teaks - crystal felling axe"), "Woodcutting method");

        Assert.That(catalogue.Skills.Single(skill => skill.Skill == "Woodcutting")
                .Bands.SelectMany(band => band.Economics?.Resources ?? [])
                .All(resource => resource.Direction == TrainingFlowDirection.Input), Is.True, "dropped teak logs should not be valued as outputs");
    }

    [Test]
    [Property("LegacyScenario", "PhaseTwoTrainingCalculations")]
    public void FullRoutePricingRetainsUnpricedEarlyLevels()
    {
        var catalogue = new MainEhpCatalogue();

        var calculator = new TrainingPlanCalculator();

        var prices = new Dictionary<int, ItemPrice>
        {
            [23959] = Quote(23959, 3_000_000),
            [28157] = Quote(28157, 50),
            [11920] = Quote(11920, 1_000_000),
            [11959] = Quote(11959, 3_000),
            [7936] = Quote(7936, 1),
            [557] = Quote(557, 5),
            [5521] = Quote(5521, 1_000),
            [9075] = Quote(9075, 200),
            [556] = Quote(556, 5),
            [564] = Quote(564, 100),
            [4698] = Quote(4698, 100)
        };

        var woodcutting = calculator.Calculate(
            catalogue.Skills.Single(skill => skill.Skill == "Woodcutting"),
            0,
            TrainingPlanCalculator.MaximumExperience,
            prices);

        Assert.That(woodcutting.PricedExperience, Is.EqualTo(199_977_594L), "priced Woodcutting XP");

        Assert.That(woodcutting.ResourceRequirements.Single(item => item.ItemId == 28157).Quantity,
            Is.EqualTo(2_091_504m).Within(0.001m), "Woodcutting Forester's rations");
        Assert.That(woodcutting.ResourceRequirements.Single(item => item.ItemId == 23959).Quantity,
            Is.EqualTo(100m).Within(0.001m), "Woodcutting enhanced seeds");

        Assert.That(woodcutting.NetGp ?? 0m, Is.EqualTo(-(2_091_504m * 50m + 100m * 3_000_000m)).Within(0.01m), "Woodcutting resource cost");

        Assert.That(!woodcutting.IsFullyPriced, Is.True, "early Woodcutting should remain visibly unpriced");
    }
}
