namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Agility;

[TestFixture]
[Category("Unit")]
public sealed class HallowedSepulchreTests
{
    [Test]
    [Property("LegacyScenario", "PhaseThreeMethodCatalogue")]
    public void ReviewedRatesAndItemFlows()
    {
        var catalogue = new MainEhpCatalogue();

        var agility = TrainingBand(catalogue, "Agility", 0);

        Assert.That(agility.ExperiencePerHour, Is.EqualTo(98_500m).Within(0m), "Agility rate");

        Assert.That(agility.Method, Is.EqualTo("Hallowed Sepulchre - Grand Coffin"), "Agility method");

        Assert.That(Resource(agility, 12625).QuantityPerExperience, Is.EqualTo(0.5m / 11_700m).Within(0m), "stamina potions per XP");

        Assert.That(Resource(agility, 24844).QuantityPerExperience, Is.EqualTo(1m / 200m / 11_700m).Within(0m), "rings per XP");

        Assert.That(Resource(agility, 565).QuantityPerExperience, Is.EqualTo(20m / 11_700m).Within(0m), "blood runes per XP");

        Assert.That(Resource(agility, 10925).QuantityPerExperience, Is.EqualTo(0.15m / 11_700m).Within(0m), "Sanfew serum per XP");

        Assert.That(agility.Economics!.FixedGpOutputPerExperience, Is.EqualTo(2_125m / 11_700m).Within(0m), "coins per XP");

        Assert.That(agility.Economics is { IsComplete: true }, Is.True, "Grand Coffin economics should be fully modelled");
    }

    [Test]
    [Property("LegacyScenario", "PhaseThreeTrainingCalculations")]
    public void FullRouteHoursAndGrandCoffinEconomics()
    {
        var catalogue = new MainEhpCatalogue();

        var calculator = new TrainingPlanCalculator();

        var agilityPrices = new Dictionary<int, ItemPrice>
        {
            [12625] = Quote(12625, 3_000),
            [24844] = Quote(24844, 4_000_000),
            [1319] = Quote(1319, 40_000),
            [1127] = Quote(1127, 40_000),
            [563] = Quote(563, 100),
            [565] = Quote(565, 300),
            [566] = Quote(566, 400),
            [9144] = Quote(9144, 100),
            [7946] = Quote(7946, 200),
            [10925] = Quote(10925, 20_000),
            [5295] = Quote(5295, 30_000)
        };

        var agility = calculator.Calculate(
            catalogue.Skills.Single(skill => skill.Skill == "Agility"),
            0,
            TrainingPlanCalculator.MaximumExperience,
            agilityPrices);

        var reviewedCoffins = TrainingPlanCalculator.MaximumExperience / 11_700m;

        const decimal expectedNetGpPerCoffin =
            19_600m + 3_920m + 3_920m + 1_960m + 5_880m + 7_840m + 1_960m
            + 78.4m + 2_940m + 4_410m + 2_125m - 1_500m;

        Assert.That(agility.Hours, Is.EqualTo(2_030.4569m).Within(0.0001m), "Grand Coffin 0-200m hours");

        Assert.That(agility.ResourceRequirements.Single(item => item.ItemId == 12625).Quantity,
            Is.EqualTo(reviewedCoffins * 0.5m).Within(0.0001m), "Grand Coffin stamina potions");

        Assert.That(agility.NetGp ?? 0m, Is.EqualTo(reviewedCoffins * expectedNetGpPerCoffin).Within(0.01m), "Grand Coffin expected profit");

        Assert.That(agility.IsFullyPriced, Is.True, "Grand Coffin projection should be fully priced");
    }
}
