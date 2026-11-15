namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Construction;

[TestFixture]
[Category("Unit")]
public sealed class ConstructionMethodTests
{
    [Test]
    [Property("LegacyScenario", "ConstructionTrainingCalculation")]
    [Description("Construction route reproduces Main EHP hours and live-price economics")]
    public void ConstructionTrainingCalculation()
    {
        var definition = new MainEhpCatalogue().Skills.Single(skill => skill.Skill == "Construction");
        var prices = new Dictionary<int, ItemPrice>
        {
            [8778] = new ItemPrice(8778, 431, 425, null, null),
            [8782] = new ItemPrice(8782, 1_910, 1_857, null, null)
        };
        var result = new TrainingPlanCalculator().Calculate(definition, 0, 200_000_000, prices);

        Assert.That(result.Hours, Is.EqualTo(142.7895m).Within(0.0001m), "Construction EHP hours");
        Assert.That(result.PricedExperience, Is.EqualTo(199_981_753L), "priced Construction XP");
        Assert.That(!result.IsFullyPriced, Is.True, "low-level furniture should remain visibly unpriced");
        Assert.That(result.NetGp is < -2_800_000_000m and > -2_805_000_000m, Is.True, "Construction cost should match the reviewed 2.8b estimate");

        var mahoganyTables = definition.ResolveMethod("mahogany-tables");
        var mahoganyTableBand = mahoganyTables.Bands.Last();
        Assert.That(mahoganyTableBand.StartExperience, Is.EqualTo(123_660L), "Mahogany table level-52 unlock XP");
        Assert.That(mahoganyTableBand.ExperiencePerHour, Is.EqualTo(940_000m).Within(0m), "Mahogany table XP/hour");
        Assert.That(Resource(mahoganyTableBand, 8782).QuantityPerExperience, Is.EqualTo(1m / 140m).Within(0m), "Mahogany table plank consumption");
        Assert.That(mahoganyTableBand.Economics!.FixedGpPerExperience, Is.EqualTo(1_250m / 24m / 140m).Within(0m), "Mahogany table servant fee");

        var mahoganyTableResult = new TrainingPlanCalculator().Calculate(
            definition,
            123_660,
            1_063_660,
            prices,
            methodId: mahoganyTables.Id);
        Assert.That(mahoganyTableResult.Hours, Is.EqualTo(1m).Within(0m), "one hour of Mahogany tables");
        Assert.That(mahoganyTableResult.ResourceRequirements.Single(item => item.ItemId == 8782).Quantity, Is.EqualTo(940_000m / 140m).Within(0.000001m), "one hour of Mahogany table planks");
        Assert.That(mahoganyTableResult.NetGp ?? 0m, Is.EqualTo(-(940_000m / 140m * 1_910m + 940_000m * 1_250m / 24m / 140m)).Within(0.01m), "one hour of Mahogany table costs");
        Assert.That(mahoganyTableResult.IsFullyPriced, Is.True, "Mahogany tables should be fully priced");
    }
}
