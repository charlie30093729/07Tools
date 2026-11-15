namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Mining;

[TestFixture]
[Category("Unit")]
public sealed class CalcifiedRocksTests
{
    private const string MethodId = "calcified-rocks-crystal-pickaxe";
    private static TrainingSkillDefinition Definition =>
        new MainEhpCatalogue().Skills.Single(skill => skill.Skill == "Mining");
    private static Dictionary<int, ItemPrice> Prices => new()
    {
        [23959] = new ItemPrice(23959, 3_000_000, 2_000_000, null, null),
        [11920] = Quote(11920, 1_000_000)
    };

    [TestCase(814445L, 34000)]
    [TestCase(1986068L, 39000)]
    [TestCase(5346332L, 44000)]
    [TestCase(13034431L, 49000)]
    public void RatesActivateAtReviewedLevelThresholds(long xp, int rate)
    {
        var result = new TrainingPlanCalculator().Calculate(Definition, xp, xp + 1, Prices, methodId: MethodId);
        Assert.That(result.BaseRate, Is.EqualTo(rate * 1.025m));
        Assert.That(result.Bands.Single().Band.StartExperience, Is.EqualTo(xp));
        Assert.That(result.Method.Name, Is.EqualTo("Calcified rocks - crystal pickaxe"));
        Assert.That(result.IsFullyPriced, Is.True);
    }

    [Test]
    public void CrystalUnlockPreservesExistingLowerLevelRoute()
    {
        var definition = Definition;
        var original = definition.ResolveMethod();
        var method = definition.ResolveMethod(MethodId);
        var fallback = method.Bands.Where(band => band.StartExperience < 814445).ToArray();
        var expected = original.Bands.Where(band => band.StartExperience < 814445).ToArray();
        Assert.That(fallback.Select(band => (band.StartExperience, band.ExperiencePerHour, band.Method)),
            Is.EqualTo(expected.Select(band => (band.StartExperience, band.ExperiencePerHour, band.Method))));
        Assert.That(fallback.SelectMany(band => band.Economics?.Resources ?? []),
            Is.EqualTo(expected.SelectMany(band => band.Economics?.Resources ?? [])));
        var result = new TrainingPlanCalculator().Calculate(definition, 814444, 814446, Prices, methodId: MethodId);
        Assert.That(result.Bands.Count, Is.EqualTo(2));
        Assert.That(result.Bands[0].Band.Method, Is.EqualTo("3t4g granite - infernal pickaxe"));
        Assert.That(result.Bands[1].Band.Method, Is.EqualTo(method.Name));
        Assert.That(result.Hours, Is.EqualTo(1m / 106540m + 1m / (34000m * 1.025m)));
    }

    [Test]
    public void PricesChargesHighAndTracksUntradeableRewardsWithoutPrayerCredit()
    {
        // 100,000 expected successful main-resource rolls, including full Prospector.
        var result = new TrainingPlanCalculator().Calculate(Definition,
            13034431, 13034431 + 3386600, Prices, methodId: MethodId);
        var seeds = result.ResourceRequirements.Single(flow => flow.ItemId == 23959);
        var shards = result.ResourceRequirements.Single(flow => flow.ItemId == 29381);
        var deposits = result.ResourceRequirements.Single(flow => flow.ItemId == 29088);
        Assert.That(seeds.Quantity, Is.EqualTo(100000m / 15000m).Within(0.000001m));
        Assert.That(seeds.Direction, Is.EqualTo(TrainingFlowDirection.Input));
        Assert.That(shards.Quantity, Is.EqualTo(100000m * 148m / 75m).Within(0.000001m));
        Assert.That(deposits.Quantity, Is.EqualTo(100000m / 75m).Within(0.000001m));
        foreach (var reward in new[] { shards, deposits })
        {
            Assert.That(reward.RequiresMarketPrice, Is.False);
            Assert.That(reward.SubjectToGeTax, Is.False);
            Assert.That(reward.Direction, Is.EqualTo(TrainingFlowDirection.Output));
        }
        Assert.That(result.NetGp, Is.EqualTo(-20_000_000m).Within(0.01m));
        Assert.That(result.Hours, Is.EqualTo(3386600m / 50225m));
        Assert.That(result.GeneratedExperience, Is.Empty);
        Assert.That(Definition.MarketItemIds, Is.EquivalentTo(new[] { 11920, 23959 }));
    }

    [Test]
    public void CustomRateChangesHoursNotPerXpCostsOrRewards()
    {
        var calculator = new TrainingPlanCalculator();
        var normal = calculator.Calculate(Definition, 13034431, 200000000, Prices, methodId: MethodId);
        var faster = calculator.Calculate(Definition, 13034431, 200000000, Prices, 98000m, MethodId);
        Assert.That(faster.Hours, Is.EqualTo(normal.Hours / 2m).Within(0.000001m));
        Assert.That(faster.NetGp, Is.EqualTo(normal.NetGp));
        Assert.That(faster.ResourceRequirements, Is.EqualTo(normal.ResourceRequirements));
    }

    [Test]
    public void ProspectorDefaultsOnScalesFlowsAndLeavesGraniteUnchanged()
    {
        var calculator = new TrainingPlanCalculator();
        var without = new Dictionary<string, string> { ["prospector-outfit"] = bool.FalseString };
        var on = calculator.Calculate(Definition, 13034431, 200000000, Prices, methodId: MethodId);
        var off = calculator.Calculate(Definition, 13034431, 200000000, Prices,
            methodId: MethodId, configuration: without);
        Assert.That(off.BaseRate, Is.EqualTo(49000m));
        Assert.That(on.BaseRate, Is.EqualTo(50225m));
        Assert.That(on.NetGp, Is.EqualTo(off.NetGp!.Value / 1.025m).Within(0.01m));
        foreach (var resource in on.ResourceRequirements)
            Assert.That(resource.Quantity, Is.EqualTo(off.ResourceRequirements.Single(
                item => item.ItemId == resource.ItemId).Quantity / 1.025m).Within(0.000001m));
        var granite = calculator.Calculate(Definition, 13034431, 200000000, Prices);
        Assert.That(granite.BaseRate, Is.EqualTo(126000m));
        Assert.That(granite.ResourceRequirements.Single().ItemId, Is.EqualTo(11920));
    }

    [Test]
    public void MissingSeedPriceIsNotSilentlyFree()
    {
        var result = new TrainingPlanCalculator().Calculate(Definition, 13034431, 200000000,
            new Dictionary<int, ItemPrice>(), methodId: MethodId);
        Assert.That(result.NetGp, Is.Null);
        Assert.That(result.HasMissingPrice, Is.True);
        Assert.That(result.IsFullyPriced, Is.False);
        Assert.That(result.ResourceRequirements, Has.Count.EqualTo(3));
    }

    [Test]
    public void FullRouteRetainsUnpricedEarlyLevelsAndIntegratesBands()
    {
        var result = new TrainingPlanCalculator().Calculate(Definition, 0, 200000000, Prices, methodId: MethodId);
        Assert.That(result.IsFullyPriced, Is.False);
        Assert.That(result.PricedExperience, Is.EqualTo(199606515L));
        Assert.That(result.ResourceRequirements.Single(flow => flow.ItemId == 11920).Quantity,
            Is.EqualTo((814445m - 393485m) / 960000m).Within(0.000001m));
    }
}
