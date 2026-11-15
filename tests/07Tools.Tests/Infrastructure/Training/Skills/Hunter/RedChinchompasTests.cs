namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Hunter;

[TestFixture]
[Category("Unit")]
public sealed class RedChinchompasTests
{
    [Test]
    [Property("LegacyScenario", "RedChinsAndKarambwans")]
    public void ReviewedRatesAndOutput()
    {
        var catalogue = new MainEhpCatalogue();

        var calculator = new TrainingPlanCalculator();

        var hunter = catalogue.Skills.Single(skill => skill.Skill == "Hunter");

        var redChins = hunter.ResolveMethod("red-chinchompas");

        var red63 = redChins.Bands.Single(band => band.StartExperience == 368_599);

        var red80 = redChins.Bands.Single(band => band.StartExperience == 1_986_068);

        var red99 = redChins.Bands.Single(band => band.StartExperience == 13_034_431);

        Assert.That(red63.ExperiencePerHour, Is.EqualTo(70_000m).Within(0m), "level 63 red chin rate");

        Assert.That(red80.ExperiencePerHour, Is.EqualTo(143_900m).Within(0m), "level 80 red chin rate");

        Assert.That(red99.ExperiencePerHour, Is.EqualTo(210_000m).Within(0m), "level 99 red chin rate");

        Assert.That(Resource(red99, 10034).QuantityPerExperience, Is.EqualTo(1m / 265m).Within(0m), "red chins per XP");

        Assert.That(Resource(red99, 10034).Direction, Is.EqualTo(TrainingFlowDirection.Output), "red chin direction");

        var redResult = calculator.Calculate(
            hunter,
            13_034_431,
            13_244_431,
            new Dictionary<int, ItemPrice> { [10034] = Quote(10034, 1_000) },
            methodId: redChins.Id);

        Assert.That(redResult.Hours, Is.EqualTo(1m).Within(0m), "210k red chin calculation hours");

        Assert.That(redResult.ResourceRequirements.Single().Quantity, Is.EqualTo(210_000m / 265m).Within(0.0000001m), "red chin output per hour");

        Assert.That(redResult.IsFullyPriced, Is.True, "red chin route should be fully priced");
    }
}
