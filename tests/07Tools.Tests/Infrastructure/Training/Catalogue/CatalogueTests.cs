namespace RunescapeTools.Tests.Infrastructure.Training.Catalogue;

[TestFixture]
[Category("Unit")]
public sealed class CatalogueTests
{
    [Test]
    [Property("LegacyScenario", "EhpCatalogueCoverage")]
    [Description("EHP catalogue covers every skill and ordered rate band")]
    public void EhpCatalogueCoverage()
    {
        var catalogue = new MainEhpCatalogue();
        var expectedSkills = OsrsHiscoreSkillOrder.Skills
            .Where(skill => skill is not "Attack" and not "Strength" and not "Hitpoints")
            .ToArray();
        Assert.That(catalogue.Skills.Count, Is.EqualTo(21), "catalogue skill count");
        Assert.That(string.Join('|', catalogue.Skills.Select(skill => skill.Skill)), Is.EqualTo(string.Join('|', expectedSkills)), "catalogue skill order");

        foreach (var skill in catalogue.Skills)
        {
            Assert.That(skill.Bands.Count > 0, Is.True, $"{skill.Skill} should have at least one rate band");
            Assert.That(skill.Bands.All(band => band.ExperiencePerHour > 0), Is.True, $"{skill.Skill} rates should be positive");
            var ordered = skill.Bands.OrderBy(band => band.StartExperience).ToArray();
            Assert.That(ordered.Select(band => band.StartExperience).Distinct().Count(), Is.EqualTo(ordered.Length), $"{skill.Skill} band starts");
            Assert.That(string.Join('|', skill.Bands.Select(band => band.StartExperience)), Is.EqualTo(string.Join('|', ordered.Select(band => band.StartExperience))), $"{skill.Skill} band ordering");
        }
    }

    [Test]
    [Property("LegacyScenario", "CatalogueMarketItemIntegrity")]
    [Description("catalogue market resources keep valid local item identities")]
    public void CatalogueMarketItemIntegrity()
    {
        var resources = new MainEhpCatalogue().Skills
            .SelectMany(skill => skill.AvailableMethods)
            .SelectMany(method => method.Bands)
            .SelectMany(band => band.Economics?.Resources ?? [])
            .ToArray();

        Assert.That(resources.Length > 0, Is.True, "catalogue should expose market resources");
        Assert.That(resources.All(resource => resource.ItemId > 0), Is.True, "catalogue item IDs should be positive");
        Assert.That(resources.All(resource => !string.IsNullOrWhiteSpace(resource.Name)), Is.True, "catalogue items should have display names");

        var idConflicts = resources
            .GroupBy(resource => resource.ItemId)
            .Select(group => new
            {
                ItemId = group.Key,
                Names = group
                    .Select(resource => BaseItemName(resource.Name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Order()
                    .ToArray()
            })
            .Where(group => group.Names.Length > 1)
            .ToArray();
        Assert.That(string.Join("; ", idConflicts.Select(group => $"{group.ItemId}: {string.Join('|', group.Names)}")), Is.EqualTo(string.Empty), "one item ID should not map to conflicting base names");

        var nameConflicts = resources
            .GroupBy(resource => BaseItemName(resource.Name), StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Name = group.Key,
                ItemIds = group.Select(resource => resource.ItemId).Distinct().Order().ToArray()
            })
            .Where(group => group.ItemIds.Length > 1)
            .ToArray();
        Assert.That(string.Join("; ", nameConflicts.Select(group => $"{group.Name}: {string.Join('|', group.ItemIds)}")), Is.EqualTo(string.Empty), "one base item name should not map to conflicting IDs");
    }

    [Test]
    [Property("LegacyScenario", "TrainingMethodSelection")]
    [Description("training definitions support stable default and alternative methods")]
    public void TrainingMethodSelection()
    {
        var main = new TrainingMethodDefinition(
            "main",
            "Main route",
            [new TrainingRateBand(0, 100m, "Main route")]);
        var alternative = new TrainingMethodDefinition(
            "alternative",
            "Alternative route",
            [new TrainingRateBand(0, 200m, "Alternative route")]);
        var definition = new TrainingSkillDefinition(
            "Method test",
            main.Bands,
            Methods: [main, alternative],
            DefaultMethodId: main.Id);
        var calculator = new TrainingPlanCalculator();

        var defaultResult = calculator.Calculate(definition, 0, 1_000, new Dictionary<int, ItemPrice>());
        var alternativeResult = calculator.Calculate(
            definition,
            0,
            1_000,
            new Dictionary<int, ItemPrice>(),
            methodId: alternative.Id);

        Assert.That(defaultResult.Method.Id, Is.EqualTo("main"), "resolved default method");
        Assert.That(defaultResult.Hours, Is.EqualTo(10m).Within(0m), "default method hours");
        Assert.That(alternativeResult.Method.Id, Is.EqualTo("alternative"), "resolved alternative method");
        Assert.That(alternativeResult.Hours, Is.EqualTo(5m).Within(0m), "alternative method hours");
    }

    private static string BaseItemName(string name)
    {
        var contextStart = name.IndexOf(" (", StringComparison.Ordinal);
        return contextStart < 0 ? name : name[..contextStart];
    }

    [Test]
    [Property("LegacyScenario", "DeterministicMethodCatalogue")]
    public void BuyableMethodsHaveCompleteEconomicModels()
    {
        var catalogue = new MainEhpCatalogue();

        var prayer = TrainingBand(catalogue, "Prayer", 0);

        var cooking = TrainingBand(catalogue, "Cooking", 8_771_558);

        var crafting = TrainingBand(catalogue, "Crafting", 2_951_373);

        var smithing = TrainingBand(catalogue, "Smithing", 13_034_431);

        var herblore = TrainingBand(catalogue, "Herblore", 2_192_818);

        var fletching = TrainingBand(catalogue, "Fletching", 5_346_332);

        var firemaking = TrainingBand(catalogue, "Firemaking", 13_034_431);

        foreach (var band in new[] { prayer, cooking, crafting, smithing, herblore, fletching, firemaking })
            Assert.That(band.Economics is { IsComplete: true }, Is.True, $"{band.Method} should be fully modelled");
    }

}
