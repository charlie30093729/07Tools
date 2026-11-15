namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Fishing;

[TestFixture]
[Category("Unit")]
public sealed class TwoTickSwordfishTests
{
    [Test]
    [Property("LegacyScenario", "PhaseTwoMethodCatalogue")]
    public void ReviewedRatesAndItemFlows()
    {
        var catalogue = new MainEhpCatalogue();

        var fishing = TrainingBand(catalogue, "Fishing", 814_445);

        Assert.That(fishing.ExperiencePerHour, Is.EqualTo(132_800m).Within(0m), "Fishing rate");

        Assert.That(fishing.Method, Is.EqualTo("2t swordfish and tuna - crystal harpoon"), "Fishing method");

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

        var fishing = calculator.Calculate(
            catalogue.Skills.Single(skill => skill.Skill == "Fishing"),
            0,
            TrainingPlanCalculator.MaximumExperience,
            prices);

        Assert.That(fishing.PricedExperience, Is.EqualTo(199_185_555L), "priced Fishing XP");

        Assert.That(fishing.ResourceRequirements.Single(item => item.ItemId == 23959).Quantity,
            Is.EqualTo(33m).Within(0.001m), "Fishing enhanced seeds");

        Assert.That(fishing.NetGp ?? 0m, Is.EqualTo(-(33m * 3_000_000m)).Within(0.01m), "Fishing crystal-charge cost");

        Assert.That(!fishing.IsFullyPriced, Is.True, "early Fishing should remain visibly unpriced");
    }
}
