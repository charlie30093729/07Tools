namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Prayer;

[TestFixture]
[Category("Unit")]
public sealed class DragonBoneMethodsTests
{
    [Test]
    [Property("LegacyScenario", "PracticalBuyableMethods")]
    public void ReviewedUnlocksRatesAndEconomics()
    {
        var catalogue = new MainEhpCatalogue();

        var calculator = new TrainingPlanCalculator();

        var emptyPrices = new Dictionary<int, ItemPrice>();

        var prayer = catalogue.Skills.Single(skill => skill.Skill == "Prayer");

        var dragonBones = prayer.ResolveMethod("dragon-bones").Bands.Single();

        Assert.That(dragonBones.ExperiencePerHour, Is.EqualTo(642_600m).Within(0m), "Gilded Altar dragon bone rate");

        Assert.That(Resource(dragonBones, 536).QuantityPerExperience, Is.EqualTo(1m / 252m).Within(0m), "Gilded dragon bones per XP");

        var chaosDragonBones = calculator.Calculate(
            prayer,
            0,
            504_000,
            emptyPrices,
            methodId: "dragon-bones",
            configuration: new Dictionary<string, string>
            {
                ["offering-location"] = "chaos-altar"
            });

        Assert.That(chaosDragonBones.BaseRate, Is.EqualTo(504_000m).Within(0m), "Chaos Altar dragon bone rate");

        Assert.That(Resource(chaosDragonBones.Method.Bands.Single(), 536).QuantityPerExperience, Is.EqualTo(1m / 504m).Within(0m), "Chaos Altar effective dragon bones per XP");

        var frostDragonBones = prayer.ResolveMethod("frost-dragon-bones").Bands.Single();

        Assert.That(frostDragonBones.ExperiencePerHour, Is.EqualTo(892_500m).Within(0m), "Gilded Altar Frost dragon bone rate");

        Assert.That(frostDragonBones.Method, Is.EqualTo("Frost dragon bones at the Gilded Altar"), "Gilded Altar Frost dragon bone method");

        Assert.That(Resource(frostDragonBones, 31729).QuantityPerExperience, Is.EqualTo(1m / 350m).Within(0m), "Gilded Frost dragon bones per XP");

        var frostPrices = new Dictionary<int, ItemPrice>
        {
            [31729] = Quote(31729, 6_000)
        };

        var gildedFrostDragonBones = calculator.Calculate(
            prayer,
            0,
            892_500,
            frostPrices,
            methodId: "frost-dragon-bones");

        Assert.That(gildedFrostDragonBones.Hours, Is.EqualTo(1m).Within(0m), "one Gilded Altar Frost dragon bone hour");

        Assert.That(gildedFrostDragonBones.ResourceRequirements.Single(resource => resource.ItemId == 31729).Quantity, Is.EqualTo(2_550m).Within(0.0001m), "Gilded Altar hourly Frost dragon bones");

        Assert.That(gildedFrostDragonBones.NetGp ?? 0m, Is.EqualTo(-15_300_000m).Within(0.01m), "Gilded Altar hourly Frost dragon bone cost");

        var chaosFrostDragonBones = calculator.Calculate(
            prayer,
            0,
            700_000,
            frostPrices,
            methodId: "frost-dragon-bones",
            configuration: new Dictionary<string, string>
            {
                ["offering-location"] = "chaos-altar"
            });

        Assert.That(chaosFrostDragonBones.BaseRate, Is.EqualTo(700_000m).Within(0m), "Chaos Altar Frost dragon bone rate");

        Assert.That(chaosFrostDragonBones.Method.Bands.Single().Method, Is.EqualTo("Frost dragon bones at the Chaos Altar"), "Chaos Altar Frost dragon bone method");

        Assert.That(Resource(chaosFrostDragonBones.Method.Bands.Single(), 31729).QuantityPerExperience, Is.EqualTo(1m / 700m).Within(0m), "Chaos Altar effective Frost dragon bones per XP");

        Assert.That(chaosFrostDragonBones.ResourceRequirements.Single(resource => resource.ItemId == 31729).Quantity, Is.EqualTo(1_000m).Within(0.0001m), "Chaos Altar hourly consumed Frost dragon bones");

        Assert.That(chaosFrostDragonBones.NetGp ?? 0m, Is.EqualTo(-6_000_000m).Within(0.01m), "Chaos Altar hourly Frost dragon bone cost");
    }
}
