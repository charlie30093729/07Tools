namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Fishing;

[TestFixture]
[Category("Unit")]
public sealed class BarbarianFishingTests
{
    [Test]
    [Property("LegacyScenario", "FishingBarbarianMethods")]
    [Description("Barbarian Fishing methods expose calibrated bands and unlock-gated Agility credit")]
    public void FishingBarbarianMethods()
    {
        var definition = new MainEhpCatalogue().Skills.Single(skill => skill.Skill == "Fishing");
        var threeTick = definition.ResolveMethod("three-tick-barbarian-fishing");
        var fiveTick = definition.ResolveMethod("five-tick-barbarian-fishing");
        var starts = new long[] { 83_014, 224_466, 737_627, 1_986_068, 5_346_332, 13_034_431 };
        var threeTickRates = new decimal[] { 45_000m, 72_000m, 95_000m, 103_000m, 110_000m, 115_000m };
        var fiveTickRates = new decimal[] { 25_000m, 40_000m, 52_000m, 56_000m, 58_000m, 60_000m };
        var fishingPerAgility = new decimal[] { 10m, 10.8m, 10.9m, 11.1m, 11.3m, 11.4m };

        for (var index = 0; index < starts.Length; index++)
        {
            var threeTickBand = threeTick.Bands.Single(band => band.StartExperience == starts[index]);
            var fiveTickBand = fiveTick.Bands.Single(band => band.StartExperience == starts[index]);
            Assert.That(threeTickBand.ExperiencePerHour, Is.EqualTo(threeTickRates[index]).Within(0m), $"3-tick rate at {starts[index]}");
            Assert.That(fiveTickBand.ExperiencePerHour, Is.EqualTo(fiveTickRates[index]).Within(0m), $"5-tick rate at {starts[index]}");
            Assert.That(threeTickBand.ExperienceOutputs!.Single(flow => flow.Skill == "Agility").QuantityPerPrimaryExperience, Is.EqualTo(1m / fishingPerAgility[index]).Within(0m), $"3-tick Agility ratio at {starts[index]}");
            Assert.That(fiveTickBand.ExperienceOutputs!.Single(flow => flow.Skill == "Agility").QuantityPerPrimaryExperience, Is.EqualTo(1m / fishingPerAgility[index]).Within(0m), $"5-tick Agility ratio at {starts[index]}");
        }

        var calculator = new TrainingPlanCalculator();
        var unlockedResult = calculator.Calculate(
            definition,
            83_014,
            183_014,
            new Dictionary<int, ItemPrice>(),
            methodId: threeTick.Id);
        Assert.That(unlockedResult.Hours, Is.EqualTo(100_000m / 45_000m).Within(0m), "3-tick Fishing hours");
        Assert.That(unlockedResult.GeneratedExperience["Agility"], Is.EqualTo(10_000m).Within(0m), "3-tick Agility credit");
        Assert.That(!unlockedResult.NetGp.HasValue, Is.True, "unpriced Barbarian supplies remain visibly unpriced");

        var crossingUnlock = calculator.Calculate(
            definition,
            0,
            100_000,
            new Dictionary<int, ItemPrice>(),
            methodId: threeTick.Id);
        Assert.That(crossingUnlock.GeneratedExperience["Agility"], Is.EqualTo((100_000m - 83_014m) / 10m).Within(0m), "pre-unlock fallback XP does not award Barbarian Agility XP");
    }
}
