namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Hunter;

[TestFixture]
[Category("Unit")]
public sealed class HerbiboarTests
{
    [Test]
    [Property("LegacyScenario", "HerbiboarMethodCatalogue")]
    [Description("Hunter exposes live-priced level-banded Herbiboar training")]
    public void HerbiboarMethodCatalogue()
    {
        var hunter = new MainEhpCatalogue().Skills.Single(skill => skill.Skill == "Hunter");
        var main = hunter.ResolveMethod();
        var herbiboar = hunter.ResolveMethod("herbiboar");
        var level80 = herbiboar.Bands.Single(band => band.StartExperience == 1_986_068);
        var level99 = herbiboar.Bands.Single(band => band.StartExperience == 13_034_431);

        Assert.That(main.Id, Is.EqualTo("main-ehp"), "Hunter default method remains Main EHP");
        Assert.That(main.Bands.Single(band => band.StartExperience == 992_895).Method, Is.EqualTo("Black chinchompas"), "Hunter default high-level route label");
        Assert.That(herbiboar.Name, Is.EqualTo("Herbiboar"), "Herbiboar method name");
        Assert.That(level80.Method, Is.EqualTo("Herbiboar"), "Herbiboar unlock method");
        Assert.That(level80.ExperiencePerHour, Is.EqualTo(137_148m).Within(0m), "level 80 Herbiboar XP/hour");
        Assert.That(level99.ExperiencePerHour, Is.EqualTo(170_874m).Within(0m), "level 99 Herbiboar XP/hour");
        Assert.That(Resource(level80, 12625).QuantityPerExperience, Is.EqualTo(0.125m / 2_078m).Within(0m), "stamina potions per level 80 Hunter XP");
        Assert.That(Resource(level80, 12625).Direction, Is.EqualTo(TrainingFlowDirection.Input), "stamina direction");
        Assert.That(Resource(level80, 207).QuantityPerExperience, Is.EqualTo(0.246m / 2_078m).Within(0m), "ranarr output per level 80 Hunter XP");
        Assert.That(Resource(level80, 207).Direction, Is.EqualTo(TrainingFlowDirection.Output), "ranarr direction");
        Assert.That(Resource(level80, 207).SubjectToGeTax, Is.True, "Herbiboar herbs should be GE taxed");

        var prices = herbiboar.Bands
            .SelectMany(band => band.Economics?.Resources ?? [])
            .Select(resource => resource.ItemId)
            .Distinct()
            .ToDictionary(id => id, id => Quote(id, 1_000));
        var result = new TrainingPlanCalculator().Calculate(
            hunter,
            1_986_068,
            2_192_818,
            prices,
            methodId: herbiboar.Id);
        var expectedCatches = (2_192_818m - 1_986_068m) / 2_078m;

        Assert.That(result.IsFullyPriced, Is.True, "level 80 Herbiboar segment is fully priced");
        Assert.That(result.Hours, Is.EqualTo(expectedCatches / 66m).Within(0.0000001m), "level 80 Herbiboar hours");
        Assert.That(result.ResourceRequirements.Single(item => item.ItemId == 12625).Quantity, Is.EqualTo(expectedCatches * 0.125m).Within(0.0000001m), "Herbiboar stamina quantity");
        Assert.That(result.ResourceRequirements.Single(item => item.ItemId == 207).Quantity, Is.EqualTo(expectedCatches * 0.246m).Within(0.0000001m), "Herbiboar ranarr quantity");
        Assert.That(result.NetGp > 0m, Is.True, "reviewed Herbiboar outputs exceed stamina cost at equal prices");
    }
}
