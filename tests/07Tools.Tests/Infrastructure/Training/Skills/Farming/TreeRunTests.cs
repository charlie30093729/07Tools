namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Farming;

[TestFixture]
[Category("Unit")]
public sealed class TreeRunTests
{
    [Test]
    [Property("LegacyScenario", "FarmingTrainingCalculations")]
    [Description("Farming tree runs price saplings, protection, and clearing fees")]
    public void FarmingTrainingCalculations()
    {
        var definition = new MainEhpCatalogue().Skills.Single(skill => skill.Skill == "Farming");
        var palmMethod = definition.ResolveMethod("magic-palm-tree-runs");
        var palmRates = palmMethod.Bands
            .Where(band => band.StartExperience >= 2_192_818)
            .Select(band => band.ExperiencePerHour)
            .ToArray();
        Assert.That(palmRates[0], Is.EqualTo(1_995_973.1502m).Within(0.0001m), "level-81 palm rate");
        Assert.That(palmRates[1], Is.EqualTo(2_136_088.6575m).Within(0.0001m), "level-85 palm rate");
        Assert.That(palmRates[2], Is.EqualTo(2_194_240.6680m).Within(0.0001m), "level-92 palm rate");

        var palmBand = palmMethod.Bands.Last();
        const decimal palmExperiencePerDay =
            6m * 13_913.8m
            + 6m * 10_260.6m
            + 23_352m
            + 12_225.5m
            + 14_334m
            + 0.225m * 22_680m;
        Assert.That(Resource(palmBand, 5502).QuantityPerExperience, Is.EqualTo(6m / palmExperiencePerDay).Within(0m), "palm saplings per Farming XP");
        Assert.That(Resource(palmBand, 5972).QuantityPerExperience, Is.EqualTo(90m / palmExperiencePerDay).Within(0m), "papaya protection per Farming XP");
        Assert.That(palmBand.Economics!.Resources.All(resource => resource.ItemId != 22866), Is.True, "palm route should not buy dragonfruit saplings");

        var prices = definition.AvailableMethods
            .SelectMany(method => method.Bands)
            .Where(band => band.Economics is not null)
            .SelectMany(band => band.Economics!.Resources)
            .Select(resource => resource.ItemId)
            .Distinct()
            .ToDictionary(itemId => itemId, itemId => Quote(itemId, 100));
        var calculator = new TrainingPlanCalculator();

        var pricedRoute = calculator.Calculate(
            definition,
            32_500,
            TrainingPlanCalculator.MaximumExperience,
            prices);
        Assert.That(pricedRoute.PricedExperience, Is.EqualTo(TrainingPlanCalculator.MaximumExperience - 32_500), "priced Farming XP");
        Assert.That(pricedRoute.IsFullyPriced, Is.True, "every tree-run band should be fully priced");
        Assert.That(!pricedRoute.HasMissingPrice, Is.True, "reviewed Farming inputs should all resolve");
        Assert.That(pricedRoute.NetGp < 0m, Is.True, "tree runs should cost GP");

        var palmRoute = calculator.Calculate(
            definition,
            32_500,
            TrainingPlanCalculator.MaximumExperience,
            prices,
            methodId: palmMethod.Id);
        Assert.That(palmRoute.IsFullyPriced, Is.True, "palm tree-run bands should be fully priced");
        Assert.That(!palmRoute.HasMissingPrice, Is.True, "palm tree-run inputs should all resolve");
        Assert.That(palmRoute.Hours > pricedRoute.Hours, Is.True, "lower-XP palm route should require more active hours");

        var fullRoute = calculator.Calculate(
            definition,
            0,
            TrainingPlanCalculator.MaximumExperience,
            prices);
        Assert.That(!fullRoute.IsFullyPriced, Is.True, "quest XP should remain visibly unpriced");
        Assert.That(fullRoute.ExperienceRemaining - fullRoute.PricedExperience, Is.EqualTo(32_500L), "unpriced quest XP");
    }

    [Test]
    [Property("LegacyScenario", "PhaseThreeMethodCatalogue")]
    public void ReviewedRatesAndItemFlows()
    {
        var catalogue = new MainEhpCatalogue();

        var farming = TrainingBand(catalogue, "Farming", 6_517_253);

        Assert.That(string.Join(
                '|',
                catalogue.Skills.Single(skill => skill.Skill == "Farming")
                    .Bands.Select(band => band.ExperiencePerHour)), Is.EqualTo("16000|364000|575000|841000|1222000|1428000|2063000|2475000|2611000|2669000"), "Farming rate progression");

        Assert.That(farming.ExperiencePerHour, Is.EqualTo(2_669_000m).Within(0m), "Farming level-92 rate");

        Assert.That(farming.Method, Is.EqualTo("Efficient tree runs - magic + dragonfruit"), "Farming method");

        Assert.That(farming.Economics is { IsComplete: true }, Is.True, "Farming economics should be complete");

        const decimal rosewoodTreesPerDay = 1m;

        const decimal redwoodTreesPerDay = 0.225m;

        const decimal farmingExperiencePerDay =
            6m * 13_913.8m
            + 6m * 17_475m
            + rosewoodTreesPerDay * 23_352m
            + 12_225.5m
            + 14_334m
            + redwoodTreesPerDay * 22_680m;

        Assert.That(Resource(farming, 5374).QuantityPerExperience, Is.EqualTo(6m / farmingExperiencePerDay).Within(0m), "Magic saplings per Farming XP");

        Assert.That(Resource(farming, 5974).QuantityPerExperience, Is.EqualTo(240m / farmingExperiencePerDay).Within(0m), "coconuts per Farming XP");

        Assert.That(Resource(farming, 22929).QuantityPerExperience, Is.EqualTo((8m * rosewoodTreesPerDay + 6m * redwoodTreesPerDay) / farmingExperiencePerDay).Within(0m), "dragonfruit protection per Farming XP");

    }
}
