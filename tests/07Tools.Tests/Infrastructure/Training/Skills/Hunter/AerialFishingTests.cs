namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Hunter;

[TestFixture]
[Category("Unit")]
public sealed class AerialFishingTests
{
    [Test]
    [Property("LegacyScenario", "AerialFishingMethodCatalogue")]
    [Description("Aerial Fishing prices potions once and credits proportional Fishing XP")]
    public void AerialFishingMethodCatalogue()
    {
        const long reviewedRateStartExperience = 13_034_431;
        const decimal hunterRate = 129_000m;
        const decimal fishingRate = 99_500m;
        var hunter = new MainEhpCatalogue().Skills.Single(skill => skill.Skill == "Hunter");
        var aerial = hunter.ResolveMethod("aerial-fishing");
        var band = aerial.Bands.Single(rateBand => rateBand.StartExperience == reviewedRateStartExperience);

        Assert.That(aerial.Name, Is.EqualTo("Aerial Fishing"), "Aerial Fishing route name");
        Assert.That(band.ExperiencePerHour, Is.EqualTo(hunterRate).Within(0m), "Aerial Hunter XP/hour");
        Assert.That(band.ExperienceOutputs!.Single(flow => flow.Skill == "Fishing").QuantityPerPrimaryExperience, Is.EqualTo(fishingRate / hunterRate).Within(0m), "Aerial Fishing XP per Hunter XP");
        Assert.That(Resource(band, 31626).QuantityPerHour, Is.EqualTo(2.5m).Within(0m), "super hunter potions per hour");
        Assert.That(Resource(band, 31602).QuantityPerHour, Is.EqualTo(2.5m).Within(0m), "super fishing potions per hour");
        Assert.That(Resource(band, 2434).QuantityPerHour, Is.EqualTo(2m).Within(0m), "prayer potions per hour");
        Assert.That(aerial.Bands.Where(rateBand => rateBand.StartExperience < reviewedRateStartExperience)
                .All(rateBand => rateBand.ExperienceOutputs is null), Is.True, "Hunter fallback bands do not award Fishing XP");

        var prices = new Dictionary<int, ItemPrice>
        {
            [31626] = Quote(31626, 1_000),
            [31602] = Quote(31602, 1_000),
            [2434] = Quote(2434, 1_000)
        };
        var calculator = new TrainingPlanCalculator();
        var result = calculator.Calculate(
            hunter,
            reviewedRateStartExperience,
            reviewedRateStartExperience + 129_000,
            prices,
            methodId: aerial.Id);

        Assert.That(result.Hours, Is.EqualTo(1m).Within(0m), "one hour of Aerial Fishing");
        Assert.That(result.GeneratedExperience["Fishing"], Is.EqualTo(99_500m).Within(0m), "one-hour Fishing credit");
        Assert.That(result.NetGp!.Value, Is.EqualTo(-7_000m).Within(0.000001m), "one-hour potion cost at equal prices");
        Assert.That(result.ResourceRequirements.Single(resource => resource.ItemId == 31626).Quantity, Is.EqualTo(2.5m).Within(0m), "one-hour super hunter potion quantity");
        Assert.That(result.ResourceRequirements.Single(resource => resource.ItemId == 31602).Quantity, Is.EqualTo(2.5m).Within(0m), "one-hour super fishing potion quantity");
        Assert.That(result.ResourceRequirements.Single(resource => resource.ItemId == 2434).Quantity, Is.EqualTo(2m).Within(0m), "one-hour prayer potion quantity");
        Assert.That(result.IsFullyPriced, Is.True, "eligible Aerial Fishing route is fully priced");

        var slowerResult = calculator.Calculate(
            hunter,
            reviewedRateStartExperience,
            reviewedRateStartExperience + 129_000,
            prices,
            personalRate: 64_500m,
            methodId: aerial.Id);
        Assert.That(slowerResult.Hours, Is.EqualTo(2m).Within(0m), "personal Hunter rate scales Aerial hours");
        Assert.That(slowerResult.GeneratedExperience["Fishing"], Is.EqualTo(99_500m).Within(0m), "Fishing credit follows Hunter XP ratio");
        Assert.That(slowerResult.NetGp!.Value, Is.EqualTo(-14_000m).Within(0.000001m), "hourly potion cost follows elapsed time");

        var lockedResult = calculator.Calculate(
            hunter,
            0,
            reviewedRateStartExperience,
            prices,
            methodId: aerial.Id);
        Assert.That(!lockedResult.GeneratedExperience.ContainsKey("Fishing"), Is.True, "pre-unlock route has no Fishing credit");
    }
}
