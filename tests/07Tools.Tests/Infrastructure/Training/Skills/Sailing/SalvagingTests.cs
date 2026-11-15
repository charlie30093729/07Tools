namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Sailing;

[TestFixture]
[Category("Unit")]
public sealed class SalvagingTests
{
    private const decimal ExtractorBonus = 250m * 3_600m / 63m;
    private static TrainingSkillDefinition Definition =>
        new MainEhpCatalogue().Skills.Single(skill => skill.Skill == "Sailing");

    private static TrainingSkillPlanResult Calculate(long start, long target,
        bool extractor = false, decimal? personalRate = null, string method = "salvaging") =>
        new TrainingPlanCalculator().Calculate(Definition, start, target,
            new Dictionary<int, ItemPrice>(), personalRate, method,
            configuration: new Dictionary<string, string> { ["crystal-extractor"] = extractor.ToString() });

    [TestCase(2411L, 2800)]
    [TestCase(5018L, 3200)]
    [TestCase(9730L, 5800)]
    [TestCase(22406L, 11000)]
    [TestCase(55649L, 14500)]
    [TestCase(101333L, 16000)]
    [TestCase(136594L, 24000)]
    [TestCase(247886L, 25000)]
    [TestCase(273742L, 30000)]
    [TestCase(407015L, 47000)]
    [TestCase(992895L, 60000)]
    [TestCase(1096278L, 70000)]
    [TestCase(1986068L, 85000)]
    [TestCase(3972294L, 95000)]
    public void EquipmentAndWreckBandsActivateAtTheirThreshold(long threshold, int rate)
    {
        var result = Calculate(threshold, threshold + 1);
        Assert.That(result.BaseRate, Is.EqualTo((decimal)rate));
        Assert.That(result.Bands.Single().Band.StartExperience, Is.EqualTo(threshold));
        Assert.That(Calculate(threshold - 1, threshold).BaseRate, Is.Not.EqualTo((decimal)rate));
        Assert.That(result.Method.Name, Is.EqualTo("Salvaging"));
        Assert.That(result.NetGp, Is.Null);
        Assert.That(result.IsFullyPriced, Is.False);
        Assert.That(result.ResourceRequirements, Is.Empty, "Do not inherit Gwenith potion flows after unlock");
    }

    [Test]
    public void ExtractorStartsAt73AndAddsToRatherThanMultipliesPersonalRates()
    {
        Assert.That(Calculate(992894, 992895, true).BaseRate, Is.EqualTo(47000m));
        Assert.That(Calculate(992895, 992896, true).BaseRate, Is.EqualTo(60000m + ExtractorBonus));
        var merchant = Calculate(13034431, 14034431, true, 90000m);
        Assert.That(merchant.BaseRate, Is.EqualTo(95000m + ExtractorBonus));
        Assert.That(merchant.UnconfiguredBaseRate, Is.EqualTo(95000m));
        Assert.That(merchant.EffectiveRate, Is.EqualTo(90000m + ExtractorBonus));
        Assert.That(merchant.Hours, Is.EqualTo(1000000m / (90000m + ExtractorBonus)).Within(0.000001m));
        Assert.That(Calculate(13034431, 14034431, false, 90000m).EffectiveRate, Is.EqualTo(90000m));
    }

    [Test]
    public void RouteCrossingExtractorUnlockUsesSeparateSegmentRates()
    {
        var result = Calculate(982895, 1002895, true, 94000m);
        var expected = 10000m / 94000m + 10000m / (120000m + ExtractorBonus);
        Assert.That(result.Bands.Count, Is.EqualTo(2));
        Assert.That(result.Hours, Is.EqualTo(expected).Within(0.000001m));
    }

    [Test]
    public void ExtractorDoesNotChangeGwenithAndFullRouteRemainsUnpriced()
    {
        var on = Calculate(0, 200000000, true, method: "main-ehp");
        var off = Calculate(0, 200000000, false, method: "main-ehp");
        Assert.That(on.Hours, Is.EqualTo(off.Hours));
        Assert.That(on.ResourceRequirements, Is.EqualTo(off.ResourceRequirements));
        var salvaging = Calculate(0, 200000000, true);
        Assert.That(salvaging.Bands.First().Band.Method, Is.EqualTo("Gwenith Glide - rosewood hull"));
        Assert.That(salvaging.IsFullyPriced, Is.False);
    }
}
