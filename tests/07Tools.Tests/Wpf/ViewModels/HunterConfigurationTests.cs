namespace RunescapeTools.Tests.Wpf.ViewModels;

[TestFixture]
[Category("Unit")]
public sealed class HunterConfigurationTests
{
    private static XpPlannerRowViewModel Row(TrainingSkillPreference? preference = null) => new(
        new MainEhpCatalogue().Skills.Single(skill => skill.Skill == "Hunter"),
        new TrainingPlanCalculator(), 13034431, preference,
        new Dictionary<int, ItemPrice> { [11959] = Quote(11959, 3000), [10034] = Quote(10034, 1000) },
        () => { });

    [Test]
    public void DialogImmediatelyDisablesAndUnchecksAltUntilThreeTickIsEnabled()
    {
        var row = Row();
        var dialog = new TrainingConfigurationDialogViewModel("Hunter", row.Method,
            row.Definition.Configurator!.Definition, row.ConfigurationValues, "main-ehp");
        var threeTick = dialog.Options.Single(option => option.Key == "three-tick");
        var alt = dialog.Options.Single(option => option.Key == "shooting-alt");
        Assert.That(alt.IsApplicable && alt.ToggleValue, Is.True);
        threeTick.ToggleValue = false;
        Assert.That(alt.IsApplicable, Is.False);
        Assert.That(alt.ToggleValue, Is.False);
        row.ApplyConfiguration(dialog.ToValues());
        Assert.That(row.PersonalRate, Is.EqualTo(200000m));
        threeTick.ToggleValue = true;
        Assert.That(alt.IsApplicable, Is.True);
        Assert.That(alt.ToggleValue, Is.False);
        row.ApplyConfiguration(dialog.ToValues());
        Assert.That(row.PersonalRate, Is.EqualTo(225000m));
        dialog.ResetDefaultsCommand.Execute(null);
        Assert.That(threeTick.ToggleValue && alt.ToggleValue && alt.IsApplicable, Is.True);
        row.ApplyConfiguration(dialog.ToValues());
        Assert.That(row.PersonalRate, Is.EqualTo(265000m));
    }

    [Test]
    public void ShootingAltIsUnavailableOnRedAndOtherMethods()
    {
        var row = Row();
        foreach (var id in new[] { "red-chinchompas", "herbiboar", "aerial-fishing" })
        {
            row.SelectedMethodOption = row.MethodOptions.Single(option => option.Id == id);
            var dialog = new TrainingConfigurationDialogViewModel("Hunter", row.Method,
                row.Definition.Configurator!.Definition, row.ConfigurationValues, id);
            Assert.That(dialog.Options.Single(option => option.Key == "shooting-alt").IsApplicable, Is.False);
            Assert.That(dialog.Options.Single(option => option.Key == "three-tick").IsApplicable,
                Is.EqualTo(id == "red-chinchompas"));
        }
    }

    [TestCase("main-ehp", "Black chinchompas", 265000, 200000)]
    public async Task NamesPersistenceCustomRatesAndReset(string id, string name, int defaultRate, int nonTickRate)
    {
        var row = Row();
        row.SelectedMethodOption = row.MethodOptions.Single(option => option.Id == id);
        Assert.That(row.SelectedMethodOption.Name, Is.EqualTo(name));
        row.PersonalRate = defaultRate / 2m;
        row.ApplyConfiguration(new Dictionary<string, string> { ["three-tick"] = "False", ["shooting-alt"] = "True" });
        Assert.That(row.PersonalRate, Is.EqualTo(nonTickRate / 2m).Within(0.000001m));
        Assert.That(row.ConfigurationValues["shooting-alt"], Is.EqualTo("False"));
        using var temp = new TemporaryDirectory();
        var store = new JsonTrainingPlanStore(new TrainingPlanOptions { FilePath = Path.Combine(temp.Path, "plans.json") });
        await store.SaveAsync("Hunter", [row.ToPreference()]);
        var restored = Row((await store.GetAsync("Hunter"))["Hunter"]);
        Assert.That(restored.SelectedMethodOption!.Id, Is.EqualTo(id));
        Assert.That(restored.SelectedMethodOption.Name, Is.EqualTo(name));
        Assert.That(restored.PersonalRate, Is.EqualTo(nonTickRate / 2m).Within(0.000001m));
        restored.ResetSkillCommand.Execute(null);
        Assert.That(restored.PersonalRate, Is.EqualTo(defaultRate));
        Assert.That(restored.ConfigurationValues["three-tick"], Is.EqualTo("True"));
        Assert.That(restored.SelectedMethodOption.Id, Is.EqualTo(id));
    }
}
