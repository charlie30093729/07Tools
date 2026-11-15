namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Hunter;

[TestFixture]
[Category("Unit")]
public sealed class ChinchompaConfigurationTests
{
    private static TrainingSkillDefinition Definition() =>
        new MainEhpCatalogue().Skills.Single(skill => skill.Skill == "Hunter");

    private static Dictionary<string, string> Settings(bool threeTick, bool alt) => new()
    {
        ["three-tick"] = threeTick.ToString(), ["shooting-alt"] = alt.ToString()
    };

    private static Dictionary<int, ItemPrice> Prices() => new()
    {
        [11959] = new(11959, 4000, 3000, null, null),
        [10034] = new(10034, 2000, 1000, null, null)
    };

    [TestCase("main-ehp", true, true, 265000)]
    [TestCase("main-ehp", true, false, 225000)]
    [TestCase("main-ehp", false, true, 200000)]
    [TestCase("red-chinchompas", true, true, 210000)]
    [TestCase("red-chinchompas", false, true, 170000)]
    public void ConfiguredRatesKeepPerGoalCatchQuantitiesAndProfit(string methodId, bool threeTick, bool alt, int rate)
    {
        var calculator = new TrainingPlanCalculator();
        var result = calculator.Calculate(Definition(), 13034431, 14034431, Prices(),
            methodId: methodId, configuration: Settings(threeTick, alt));
        var baseline = calculator.Calculate(Definition(), 13034431, 14034431, Prices(), methodId: methodId);
        Assert.That(result.BaseRate, Is.EqualTo(rate));
        Assert.That(result.Hours, Is.EqualTo(1000000m / rate).Within(0.000001m));
        Assert.That(result.ResourceRequirements, Is.EqualTo(baseline.ResourceRequirements));
        Assert.That(result.NetGp, Is.EqualTo(baseline.NetGp));
        Assert.That(result.IsFullyPriced, Is.True);
        Assert.That(result.GeneratedExperience, Is.Empty);
        var catchXp = methodId == "main-ehp" ? 315m : 265m;
        Assert.That(result.ResourceRequirements.Single().Quantity, Is.EqualTo(1000000m / catchXp).Within(0.000001m));
    }

    [TestCase(368599, 61000)]
    [TestCase(737627, 72000)]
    [TestCase(1986068, 115000)]
    [TestCase(5346332, 136000)]
    public void NonTickRedLevelBands(long start, int rate)
    {
        var result = new TrainingPlanCalculator().Calculate(Definition(), start, start + 1000,
            Prices(), methodId: "red-chinchompas", configuration: Settings(false, false));
        Assert.That(result.BaseRate, Is.EqualTo(rate));
    }

    [TestCase("main-ehp", 992895)]
    [TestCase("red-chinchompas", 368599)]
    public void ConfigurationDoesNotAlterPreUnlockRoute(string id, long unlock)
    {
        var calculator = new TrainingPlanCalculator();
        var before = calculator.Calculate(Definition(), 0, unlock, Prices(), methodId: id);
        var after = calculator.Calculate(Definition(), 0, unlock, Prices(), methodId: id,
            configuration: Settings(false, false));
        Assert.That(after.Hours, Is.EqualTo(before.Hours));
        Assert.That(after.PricedExperience, Is.EqualTo(before.PricedExperience));
        Assert.That(after.ResourceRequirements, Is.EqualTo(before.ResourceRequirements));
    }

    [TestCase("herbiboar")]
    [TestCase("aerial-fishing")]
    public void OtherHunterMethodsAreUnaffected(string id)
    {
        var calculator = new TrainingPlanCalculator();
        var before = calculator.Calculate(Definition(), 13034431, 14034431, Prices(), methodId: id);
        var after = calculator.Calculate(Definition(), 13034431, 14034431, Prices(), methodId: id,
            configuration: Settings(false, false));
        Assert.That(after.Hours, Is.EqualTo(before.Hours));
        Assert.That(after.ResourceRequirements, Is.EqualTo(before.ResourceRequirements));
        Assert.That(after.GeneratedExperience, Is.EqualTo(before.GeneratedExperience));
    }

    [Test]
    public void SavedInvalidDependencyNormalizesToNoAlt()
    {
        var values = Definition().Configurator!.Definition.Normalize(Settings(false, true));
        Assert.That(values.GetToggle("shooting-alt"), Is.False);
        Assert.That(values.GetToggle("three-tick"), Is.False);
    }
}
