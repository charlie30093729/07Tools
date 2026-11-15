namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Cooking;

[TestFixture]
[Category("Unit")]
public sealed class OneTickKarambwansTests
{
    [Test]
    [Property("LegacyScenario", "RedChinsAndKarambwans")]
    public void ReviewedRatesAndEconomics()
    {
        var catalogue = new MainEhpCatalogue();

        var calculator = new TrainingPlanCalculator();

        var cooking = catalogue.Skills.Single(skill => skill.Skill == "Cooking");

        var karambwans = cooking.ResolveMethod("one-tick-karambwans");

        var karambwan90 = karambwans.Bands.Single(band => band.StartExperience == 5_346_332);

        var karambwan99 = karambwans.Bands.Single(band => band.StartExperience == 13_034_431);

        Assert.That(karambwan90.ExperiencePerHour, Is.EqualTo(948_100m).Within(0m), "level 90 karambwan rate");

        Assert.That(karambwan99.ExperiencePerHour, Is.EqualTo(980_000m).Within(0m), "level 99 karambwan rate");

        Assert.That(Resource(karambwan90, 3142).QuantityPerExperience, Is.EqualTo(5_000m / 948_100m).Within(0m), "level 90 raw karambwan per XP");

        Assert.That(Resource(karambwan99, 3142).QuantityPerExperience, Is.EqualTo(1m / 190m).Within(0m), "level 99 raw karambwan per XP");

        Assert.That(Resource(karambwan99, 3144).QuantityPerExperience, Is.EqualTo(1m / 190m).Within(0m), "cooked karambwan per XP");

        var karambwanResult = calculator.Calculate(
            cooking,
            13_034_431,
            14_014_431,
            new Dictionary<int, ItemPrice>
            {
                [3142] = Quote(3142, 1_000),
                [3144] = Quote(3144, 1_000)
            },
            methodId: karambwans.Id);

        Assert.That(karambwanResult.Hours, Is.EqualTo(1m).Within(0m), "980k karambwan calculation hours");

        Assert.That(karambwanResult.ResourceRequirements.Single(item => item.ItemId == 3142).Quantity, Is.EqualTo(980_000m / 190m).Within(0.0000001m), "raw karambwan per max-rate hour");

        Assert.That(karambwanResult.IsFullyPriced, Is.True, "karambwan route should be fully priced");
    }
}
