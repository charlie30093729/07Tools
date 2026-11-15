namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Woodcutting;

[TestFixture]
[Category("Unit")]
public sealed class WoodcuttingMethodTests
{
    [Test]
    [Property("LegacyScenario", "WoodcuttingAlternativeMethods")]
    [Description("Woodcutting alternatives expose crystal-felling-axe rates and economics")]
    public void WoodcuttingAlternativeMethods()
    {
        var definition = new MainEhpCatalogue().Skills.Single(skill => skill.Skill == "Woodcutting");
        var redwoods = definition.ResolveMethod("redwood-trees-crystal-axe");
        var redwood90 = redwoods.Bands.Single(band => band.StartExperience == 5_346_332);
        var redwood99 = redwoods.Bands.Single(band => band.StartExperience == 13_034_431);
        Assert.That(redwood90.ExperiencePerHour, Is.EqualTo(77_000m).Within(0m), "level 90 redwood rate");
        Assert.That(redwood99.ExperiencePerHour, Is.EqualTo(82_500m).Within(0m), "level 99 redwood rate");
        Assert.That(Resource(redwood99, 19669).QuantityPerExperience, Is.EqualTo(0.8m / 418m).Within(0m), "redwood logs per XP");
        Assert.That(Resource(redwood99, 19669).Direction, Is.EqualTo(TrainingFlowDirection.Output), "redwood log direction");
        Assert.That(Resource(redwood99, 23959).QuantityPerExperience, Is.EqualTo(0.8m / (418m * 15_000m)).Within(0m), "redwood crystal charges per XP");
        Assert.That(Resource(redwood99, 23959).Direction, Is.EqualTo(TrainingFlowDirection.Input), "redwood charge direction");
        Assert.That(Resource(redwood99, 28157).QuantityPerExperience, Is.EqualTo(1m / 418m).Within(0m), "redwood rations per XP");

        var ironwoods = definition.ResolveMethod("ironwood-trees-crystal-axe");
        var ironwood80 = ironwoods.Bands.Single(band => band.StartExperience == 1_986_068);
        var ironwood90 = ironwoods.Bands.Single(band => band.StartExperience == 5_346_332);
        var ironwood99 = ironwoods.Bands.Single(band => band.StartExperience == 13_034_431);
        Assert.That(ironwood80.ExperiencePerHour, Is.EqualTo(82_500m).Within(0m), "level 80 ironwood rate");
        Assert.That(ironwood90.ExperiencePerHour, Is.EqualTo(93_500m).Within(0m), "level 90 ironwood rate");
        Assert.That(ironwood99.ExperiencePerHour, Is.EqualTo(104_500m).Within(0m), "level 99 ironwood rate");
        Assert.That(Resource(ironwood99, 32907).QuantityPerExperience, Is.EqualTo(0.8m / 192.5m).Within(0m), "ironwood logs per XP");
        Assert.That(Resource(ironwood99, 32907).Direction, Is.EqualTo(TrainingFlowDirection.Output), "ironwood log direction");
        Assert.That(Resource(ironwood99, 23959).QuantityPerExperience, Is.EqualTo(0.8m / (192.5m * 15_000m)).Within(0m), "ironwood crystal charges per XP");
        Assert.That(Resource(ironwood99, 23959).Direction, Is.EqualTo(TrainingFlowDirection.Input), "ironwood charge direction");
        Assert.That(Resource(ironwood99, 28157).QuantityPerExperience, Is.EqualTo(1m / 192.5m).Within(0m), "ironwood rations per XP");

        var result = new TrainingPlanCalculator().Calculate(
            definition,
            13_034_431,
            13_138_931,
            new Dictionary<int, ItemPrice>
            {
                [23959] = Quote(23959, 10_000_000),
                [28157] = Quote(28157, 1_000),
                [32907] = Quote(32907, 100)
            },
            methodId: ironwoods.Id);
        Assert.That(result.Hours, Is.EqualTo(1m).Within(0m), "104.5k ironwood calculation hours");
        Assert.That(result.IsFullyPriced, Is.True, "ironwood route should be fully priced");
        Assert.That(result.NetGp < 0m, Is.True, "crystal charges should exceed equal-priced ironwood log proceeds");
    }
}
