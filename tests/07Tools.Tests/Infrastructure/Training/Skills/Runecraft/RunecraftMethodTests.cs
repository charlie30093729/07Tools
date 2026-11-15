namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Runecraft;

[TestFixture]
[Category("Unit")]
public sealed class RunecraftMethodTests
{
    [Test]
    [Property("LegacyScenario", "RunecraftAlternativeMethods")]
    [Description("Runecraft alternatives and Raiments configuration preserve reviewed mechanics")]
    public void RunecraftAlternativeMethods()
    {
        var definition = new MainEhpCatalogue().Skills.Single(skill => skill.Skill == "Runecraft");
        var calculator = new TrainingPlanCalculator();
        var emptyPrices = new Dictionary<int, ItemPrice>();

        var mud = definition.ResolveMethod("main-ehp").Bands.Last();
        Assert.That(mud.ExperiencePerHour, Is.EqualTo(98_200m).Within(0m), "solo mud 99 rate");
        Assert.That(Resource(mud, 4698).QuantityPerExperience, Is.EqualTo(105.3m / 598.5m).Within(0m), "Raiments and lantern mud output");
        var noRaimentsMud = calculator.Calculate(
            definition,
            13_034_431,
            13_132_631,
            emptyPrices,
            methodId: "main-ehp",
            configuration: new Dictionary<string, string>
            {
                ["raiments-of-the-eye"] = bool.FalseString
            });
        Assert.That(noRaimentsMud.BaseRate, Is.EqualTo(98_200m).Within(0m), "Raiments do not alter mud XP/hour");
        Assert.That(Resource(noRaimentsMud.Method.Bands.Last(), 4698).QuantityPerExperience, Is.EqualTo(1.1m / 9.5m).Within(0m), "lantern mud output without Raiments");

        var noOutputBonusesMud = calculator.Calculate(
            definition,
            13_034_431,
            13_132_631,
            emptyPrices,
            methodId: "main-ehp",
            configuration: new Dictionary<string, string>
            {
                ["raiments-of-the-eye"] = bool.FalseString,
                ["abyssal-lantern-magic-logs"] = bool.FalseString
            });
        Assert.That(Resource(noOutputBonusesMud.Method.Bands.Last(), 4698).QuantityPerExperience, Is.EqualTo(1m / 9.5m).Within(0m), "disabled lantern and Raiments preserve base mud output");

        var lavaMethod = definition.ResolveMethod("solo-lava-runes");
        Assert.That(lavaMethod.Bands[1].StartExperience, Is.EqualTo(6_291L), "solo lava unlock XP");
        Assert.That(lavaMethod.Bands[1].ExperiencePerHour, Is.EqualTo(40_000m).Within(0m), "solo lava entry rate");
        var lava85 = lavaMethod.Bands.Single(band => band.StartExperience == 3_258_594);
        Assert.That(lava85.StartExperience, Is.EqualTo(3_258_594L), "solo lava colossal-pouch unlock XP");
        Assert.That(lava85.ExperiencePerHour, Is.EqualTo(102_100m).Within(0m), "solo lava colossal-pouch rate");
        Assert.That(Resource(lava85, 7936).QuantityPerExperience, Is.EqualTo(1m / 10.5m).Within(0m), "lava essence per XP");
        Assert.That(Resource(lava85, 557).QuantityPerExperience, Is.EqualTo(1m / 10.5m).Within(0m), "lava earth runes per XP");
        Assert.That(Resource(lava85, 4699).QuantityPerExperience, Is.EqualTo(105.3m / 661.5m).Within(0m), "Raiments and lantern lava output");
        var lava99 = lavaMethod.Bands.Last();
        Assert.That(lava99.StartExperience, Is.EqualTo(13_034_431L), "solo lava cape threshold");
        Assert.That(lava99.ExperiencePerHour, Is.EqualTo(102_100m).Within(0m), "solo lava cape rate");
        Assert.That(Resource(lava99, 9075).QuantityPerExperience, Is.EqualTo(2m / 661.5m).Within(0m), "lava Magic Imbue astrals");
        Assert.That(lava99.Economics!.Resources.All(resource => resource.ItemId is not 556 and not 564), Is.True, "Runecraft cape removes lava pouch-repair runes");

        var aetherMethod = definition.ResolveMethod("solo-aether-runes");
        var aether90 = aetherMethod.Bands.Single(band => band.StartExperience == 5_346_332);
        Assert.That(aether90.ExperiencePerHour, Is.EqualTo(99_000m).Within(0m), "solo aether level-90 rate");
        Assert.That(Resource(aether90, 7936).QuantityPerExperience, Is.EqualTo(1m / 20m).Within(0m), "aether essence per XP");
        Assert.That(Resource(aether90, 566).QuantityPerExperience, Is.EqualTo(1m / 20m).Within(0m), "aether soul runes per XP");
        Assert.That(Resource(aether90, 30771).QuantityPerExperience, Is.EqualTo(105.3m / 1_260m).Within(0m), "Raiments and lantern catalyst cost");
        Assert.That(Resource(aether90, 30843).QuantityPerExperience, Is.EqualTo(105.3m / 1_260m).Within(0m), "Raiments and lantern aether output");
        Assert.That(Resource(aether90, 2552).QuantityPerExperience, Is.EqualTo(0.125m / 1_260m).Within(0m), "aether rings of dueling per XP");
        var aether99 = aetherMethod.Bands.Last();
        Assert.That(aether99.ExperiencePerHour, Is.EqualTo(102_000m).Within(0m), "solo aether level-99 rate");
        Assert.That(Resource(aether99, 9075).QuantityPerExperience, Is.EqualTo(2m / 1_260m).Within(0m), "aether Magic Imbue astrals");
        Assert.That(aether99.Economics!.Resources.All(resource => resource.ItemId is not 556 and not 564), Is.True, "Runecraft cape removes aether pouch-repair runes");

        var doloAetherMethod = definition.ResolveMethod("dolo-aether-runes");
        var doloAether90 = doloAetherMethod.Bands.Single(band => band.StartExperience == 5_346_332);
        Assert.That(doloAether90.ExperiencePerHour, Is.EqualTo(138_000m).Within(0m), "dolo aether reviewed rate");
        foreach (var soloResource in aether90.Economics!.Resources)
        {
            var doloResource = doloAether90.Economics!.Resources.Single(resource =>
                resource.ItemId == soloResource.ItemId
                && resource.Direction == soloResource.Direction);
            Assert.That(doloResource.QuantityPerExperience, Is.EqualTo(soloResource.QuantityPerExperience).Within(0m), $"dolo aether preserves solo {soloResource.Name} per XP");
        }
        var doloAether99 = doloAetherMethod.Bands.Last();
        Assert.That(doloAether99.ExperiencePerHour, Is.EqualTo(138_000m).Within(0m), "dolo aether cape rate");
        Assert.That(doloAether99.Economics!.Resources.Count, Is.EqualTo(aether99.Economics!.Resources.Count), "dolo aether preserves solo post-99 resource set");

        var daeyaltDoloAether = calculator.Calculate(
            definition,
            5_346_332,
            5_484_332,
            emptyPrices,
            methodId: "dolo-aether-runes",
            configuration: new Dictionary<string, string>
            {
                ["use-daeyalt-essence"] = bool.TrueString,
                ["daeyalt-essence-quantity"] = string.Empty
            });
        Assert.That(daeyaltDoloAether.BaseRate, Is.EqualTo(138_000m * (1m + (63m / 109m * 0.5m))).Within(0m), "Daeyalt boosts only the dolo main-account XP share");
        Assert.That(daeyaltDoloAether.ResourceRequirements.Any(resource => resource.ItemId == 24704), Is.True, "dolo route applies Daeyalt to the main account's tracked essence");
        Assert.That(daeyaltDoloAether.ResourceRequirements.All(resource => resource.ItemId != 7936), Is.True, "unlimited Daeyalt replaces the main account's pure essence");

        var noRaimentsAether = calculator.Calculate(
            definition,
            5_346_332,
            5_445_332,
            emptyPrices,
            methodId: "solo-aether-runes",
            configuration: new Dictionary<string, string>
            {
                ["raiments-of-the-eye"] = bool.FalseString
            });
        var noRaimentsAetherBand = noRaimentsAether.Method.Bands
            .Single(band => band.StartExperience == 5_346_332);
        Assert.That(noRaimentsAether.BaseRate, Is.EqualTo(99_000m).Within(0m), "Raiments do not alter aether XP/hour");
        Assert.That(Resource(noRaimentsAetherBand, 30771).QuantityPerExperience, Is.EqualTo(1.1m / 20m).Within(0m), "lantern catalyst cost");
        Assert.That(Resource(noRaimentsAetherBand, 30843).QuantityPerExperience, Is.EqualTo(1.1m / 20m).Within(0m), "lantern aether output");

        var natureMethod = definition.ResolveMethod("achievement-cape-double-nature-runes");
        var nature91 = natureMethod.Bands.Single(band => band.StartExperience == 5_902_831);
        Assert.That(nature91.ExperiencePerHour, Is.EqualTo(69_120m).Within(0m), "Achievement cape nature rate");
        Assert.That(Resource(nature91, 7936).QuantityPerExperience, Is.EqualTo(1m / 9m).Within(0m), "nature essence per XP");
        Assert.That(Resource(nature91, 9075).QuantityPerExperience, Is.EqualTo(0.125m / 576m).Within(0m), "nature astral runes per XP");
        Assert.That(Resource(nature91, 556).QuantityPerExperience, Is.EqualTo(0.25m / 576m).Within(0m), "nature air runes per XP");
        Assert.That(Resource(nature91, 564).QuantityPerExperience, Is.EqualTo(0.125m / 576m).Within(0m), "nature cosmic runes per XP");
        Assert.That(Resource(nature91, 561).QuantityPerExperience, Is.EqualTo(212.8m / 576m).Within(0m), "Raiments and lantern nature output");
        Assert.That(nature91.Economics!.Resources.All(resource => resource.ItemId != 5521), Is.True, "nature route does not use binding necklaces");
        var nature99 = natureMethod.Bands.Last();
        Assert.That(nature99.StartExperience, Is.EqualTo(13_034_431L), "nature cape threshold");
        Assert.That(nature99.Economics!.Resources.All(resource => resource.ItemId is not 9075 and not 556 and not 564), Is.True, "Runecraft cape removes every nature pouch-repair rune");

        var naturePrices = new Dictionary<int, ItemPrice>
        {
            [7936] = Quote(7936, 1),
            [9075] = Quote(9075, 115),
            [556] = Quote(556, 5),
            [564] = Quote(564, 126),
            [561] = Quote(561, 160)
        };
        var naturePlan = calculator.Calculate(
            definition,
            5_902_831,
            TrainingPlanCalculator.MaximumExperience,
            naturePrices,
            methodId: "achievement-cape-double-nature-runes");
        Assert.That(naturePlan.Hours, Is.EqualTo(2_808.1187644675925925925925926m).Within(0.0000001m), "nature hours to 200m");
        Assert.That(naturePlan.NetGp ?? 0m, Is.EqualTo(11_236_220_146.972916666666666665m).Within(0.01m), "nature GP to 200m");
        Assert.That(naturePlan.ResourceRequirements.Single(item => item.ItemId == 7936).Quantity, Is.EqualTo(21_566_352.111111111111111111111m).Within(0.000001m), "nature essence to 200m");
        Assert.That(naturePlan.ResourceRequirements.Single(item => item.ItemId == 561).Quantity, Is.EqualTo(71_708_120.769444444444444444444m).Within(0.000001m), "Raiments and lantern nature runes to 200m");
        Assert.That(naturePlan.IsFullyPriced, Is.True, "level-91 nature route should be fully priced");

        var noRaimentsNature = calculator.Calculate(
            definition,
            5_902_831,
            TrainingPlanCalculator.MaximumExperience,
            naturePrices,
            methodId: "achievement-cape-double-nature-runes",
            configuration: new Dictionary<string, string>
            {
                ["raiments-of-the-eye"] = bool.FalseString
            });
        Assert.That(noRaimentsNature.BaseRate, Is.EqualTo(69_120m).Within(0m), "Raiments do not alter nature XP/hour");
        Assert.That(Resource(noRaimentsNature.Method.Bands.Last(), 561).QuantityPerExperience, Is.EqualTo(140.8m / 576m).Within(0m), "lantern double-nature output without Raiments");

        var bloodMethod = definition.ResolveMethod("arceuus-blood-runes");
        var blood77 = bloodMethod.Bands.Single(band => band.StartExperience == 1_475_581);
        Assert.That(blood77.ExperiencePerHour, Is.EqualTo(36_000m).Within(0m), "Arceuus blood rate");
        Assert.That(Resource(blood77, 565).QuantityPerExperience, Is.EqualTo(1.7m / 24.425m).Within(0m), "Raiments and lantern blood output per XP");
        Assert.That(blood77.Economics!.Resources.All(resource => resource.Direction == TrainingFlowDirection.Output), Is.True, "gathered dark essence should not create tradeable inputs");

        var soulMethod = definition.ResolveMethod("arceuus-soul-runes");
        var soul90 = soulMethod.Bands.Single(band => band.StartExperience == 5_346_332);
        Assert.That(soul90.ExperiencePerHour, Is.EqualTo(44_000m).Within(0m), "Arceuus soul rate");
        Assert.That(Resource(soul90, 566).QuantityPerExperience, Is.EqualTo(1.7m / 30.325m).Within(0m), "Raiments and lantern soul output per XP");

        var gatheredRunePrices = new Dictionary<int, ItemPrice>
        {
            [565] = Quote(565, 1_000),
            [566] = Quote(566, 1_000)
        };
        var bloodPlan = calculator.Calculate(
            definition,
            1_475_581,
            1_511_581,
            gatheredRunePrices,
            methodId: bloodMethod.Id);
        Assert.That(bloodPlan.Hours, Is.EqualTo(1m).Within(0m), "Arceuus blood calculation hours");
        Assert.That(bloodPlan.ResourceRequirements.Single().Quantity, Is.EqualTo(36_000m * 1.7m / 24.425m).Within(0.0000001m), "Raiments and lantern blood runes per hour");
        Assert.That(bloodPlan.IsFullyPriced, Is.True, "Arceuus blood route should be fully priced");

        var soulPlan = calculator.Calculate(
            definition,
            5_346_332,
            5_390_332,
            gatheredRunePrices,
            methodId: soulMethod.Id);
        Assert.That(soulPlan.Hours, Is.EqualTo(1m).Within(0m), "Arceuus soul calculation hours");
        Assert.That(soulPlan.ResourceRequirements.Single().Quantity, Is.EqualTo(44_000m * 1.7m / 30.325m).Within(0.0000001m), "Raiments and lantern soul runes per hour");
        Assert.That(soulPlan.IsFullyPriced, Is.True, "Arceuus soul route should be fully priced");

        var noRaimentsBlood = calculator.Calculate(
            definition,
            1_475_581,
            1_511_581,
            gatheredRunePrices,
            methodId: bloodMethod.Id,
            configuration: new Dictionary<string, string>
            {
                ["raiments-of-the-eye"] = bool.FalseString
            });
        Assert.That(noRaimentsBlood.BaseRate, Is.EqualTo(36_000m).Within(0m), "Raiments do not alter blood XP/hour");
        Assert.That(Resource(noRaimentsBlood.Method.Bands.Last(), 565).QuantityPerExperience, Is.EqualTo(1.1m / 24.425m).Within(0m), "lantern blood output without Raiments");
    }
}
