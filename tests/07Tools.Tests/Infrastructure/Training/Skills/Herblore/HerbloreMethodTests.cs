namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Herblore;

[TestFixture]
[Category("Unit")]
public sealed class HerbloreMethodTests
{
    [Test]
    [Property("LegacyScenario", "HerbloreEquipmentEconomics")]
    [Description("Herblore methods use shared equipment and four-dose economics")]
    public void HerbloreEquipmentEconomics()
    {
        var catalogue = new MainEhpCatalogue();
        var definition = catalogue.Skills.Single(skill => skill.Skill == "Herblore");
        var prices = new Dictionary<int, ItemPrice>
        {
            [3002] = new ItemPrice(3002, 1_000, 900, null, null),
            [6693] = new ItemPrice(6693, 2_000, 1_900, null, null),
            [21163] = new ItemPrice(21163, 3_000, 2_900, null, null),
            [6685] = new ItemPrice(6685, 600, 500, null, null)
        };

        var result = new TrainingPlanCalculator().Calculate(
            definition,
            2_192_818,
            2_642_818,
            prices);

        Assert.That(result.Hours, Is.EqualTo(1m).Within(0m), "one hour of Saradomin brews");
        Assert.That(result.NetGp ?? 0m, Is.EqualTo(-6_147_812.5m).Within(0.01m), "equipment-adjusted brew GP per hour");
        Assert.That(result.AverageGpPerHour ?? 0m, Is.EqualTo(-6_147_812.5m).Within(0.01m), "equipment-adjusted displayed GP per hour");
    }

    [Test]
    [Property("LegacyScenario", "HerbloreAlternativeMethods")]
    [Description("Herblore alternatives preserve unlock routes and reviewed rates")]
    public void HerbloreAlternativeMethods()
    {
        var definition = new MainEhpCatalogue().Skills.Single(skill => skill.Skill == "Herblore");
        var restores = definition.ResolveMethod("super-restores");
        var restoreBand = restores.Bands.Last();
        Assert.That(restoreBand.StartExperience, Is.EqualTo(368_599L), "Super restores unlock XP");
        Assert.That(restoreBand.ExperiencePerHour, Is.EqualTo(356_250m).Within(0m), "Super restores XP/hour");
        Assert.That(Resource(restoreBand, 3004).QuantityPerExperience, Is.EqualTo(1m / 142.5m).Within(0m), "unfinished snapdragon per XP");
        Assert.That(Resource(restoreBand, 223).QuantityPerExperience, Is.EqualTo(0.9m / 142.5m).Within(0m), "goggle-adjusted eggs per XP");
        Assert.That(Resource(restoreBand, 21163).QuantityPerExperience, Is.EqualTo(0.015m / 142.5m).Within(0m), "restore amulet charges per XP");
        Assert.That(Resource(restoreBand, 3024).QuantityPerExperience, Is.EqualTo(0.7875m / 142.5m).Within(0m), "four-dose restores per XP");

        var extended = definition.ResolveMethod("1t-extended-super-antifires");
        var extendedBand = extended.Bands.Last();
        Assert.That(extendedBand.StartExperience, Is.EqualTo(11_805_606L), "Extended super antifires unlock XP");
        Assert.That(extendedBand.ExperiencePerHour, Is.EqualTo(840_000m).Within(0m), "Extended super antifires XP/hour");
        Assert.That(Resource(extendedBand, 21978).QuantityPerExperience, Is.EqualTo(1m / 160m).Within(0m), "super antifire(4) per XP");
        Assert.That(Resource(extendedBand, 11994).QuantityPerExperience, Is.EqualTo(3.6m / 160m).Within(0m), "goggle-adjusted shards per XP");
        Assert.That(Resource(extendedBand, 22209).QuantityPerExperience, Is.EqualTo(1m / 160m).Within(0m), "extended super antifire(4) per XP");
        Assert.That(extendedBand.Economics!.Resources.All(resource => resource.ItemId != 21163), Is.True, "Alchemist's amulet must not apply to extended super antifires");

        Assert.That(definition.AvailableMethods
                .SelectMany(method => method.Bands)
                .SelectMany(band => band.Economics?.Resources ?? [])
                .All(resource => resource.ItemId != 6687), Is.True, "Herblore methods must not sell three-dose potions");

        var calculator = new TrainingPlanCalculator();
        var restorePlan = calculator.Calculate(
            definition,
            0,
            TrainingPlanCalculator.MaximumExperience,
            new Dictionary<int, ItemPrice>(),
            methodId: restores.Id);
        var extendedPlan = calculator.Calculate(
            definition,
            0,
            TrainingPlanCalculator.MaximumExperience,
            new Dictionary<int, ItemPrice>(),
            methodId: extended.Id);

        Assert.That(restorePlan.Hours is > 562m and < 563m, Is.True, "Super restore 0-to-200m hours");
        Assert.That(extendedPlan.Hours is > 251m and < 252m, Is.True, "Extended super antifire 0-to-200m hours");

        var restoreHour = calculator.Calculate(
            definition,
            368_599,
            724_849,
            new Dictionary<int, ItemPrice>
            {
                [3004] = new ItemPrice(3004, 1_000, 900, null, null),
                [223] = new ItemPrice(223, 2_000, 1_900, null, null),
                [21163] = new ItemPrice(21163, 3_000, 2_900, null, null),
                [3024] = new ItemPrice(3024, 600, 500, null, null)
            },
            methodId: restores.Id);
        Assert.That(restoreHour.Hours, Is.EqualTo(1m).Within(0m), "one hour of Super restores");
        Assert.That(restoreHour.NetGp ?? 0m, Is.EqualTo(-6_147_812.5m).Within(0.01m), "Super restore hourly economics");

        var extendedHour = calculator.Calculate(
            definition,
            11_805_606,
            12_645_606,
            new Dictionary<int, ItemPrice>
            {
                [21978] = new ItemPrice(21978, 1_000, 900, null, null),
                [11994] = new ItemPrice(11994, 200, 190, null, null),
                [22209] = new ItemPrice(22209, 2_600, 2_500, null, null)
            },
            methodId: extended.Id);
        Assert.That(extendedHour.Hours, Is.EqualTo(1m).Within(0m), "one hour of extended super antifires");
        Assert.That(extendedHour.NetGp ?? 0m, Is.EqualTo(3_832_500m).Within(0.01m), "extended super antifire hourly economics");
    }
}
