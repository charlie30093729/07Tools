using static RunescapeTools.Tests.TestSupport.Builders.PrayerOfferingTestData;

namespace RunescapeTools.Tests.Wpf.ViewModels;

[TestFixture]
[Category("Unit")]
public sealed class AshOfferingPlannerTests
{
    private static XpPlannerRowViewModel Row(TrainingSkillPreference? preference = null) =>
        new(CreatePrayerDefinition(), new TrainingPlanCalculator(), 13034431,
            preference, Prices(), () => { });

    [TestCase("infernal-ashes")]
    [TestCase("abyssal-ashes")]
    public void SelectingAshesDefaultsToPrifAndDialogOnlyAllowsOffering(string id)
    {
        var row = Row();
        row.ApplyConfiguration(Location("offering-at-bank"));
        row.SelectedMethodOption = row.MethodOptions.Single(option => option.Id == id);
        Assert.That(row.ConfigurationValues["offering-location"], Is.EqualTo("offering-at-prif-agility"));
        var definition = row.Definition.Configurator!.GetDefinition(id);
        var dialog = new TrainingConfigurationDialogViewModel("Prayer", row.Method,
            definition, row.ConfigurationValues, id);
        Assert.That(dialog.Options.Single().Choices.Select(choice => choice.Value),
            Is.EquivalentTo(new[] { "offering-at-bank", "offering-at-prif-agility" }));
        dialog.Options.Single().SelectedChoice = dialog.Options.Single().Choices.First();
        row.ApplyConfiguration(dialog.ToValues());
        Assert.That(row.ConfigurationValues["offering-location"], Is.EqualTo("offering-at-bank"));
        Assert.That(row.Result.GeneratedExperience.ContainsKey("Agility"), Is.False);
        dialog.ResetDefaultsCommand.Execute(null);
        Assert.That(dialog.ToValues()["offering-location"], Is.EqualTo("offering-at-prif-agility"));
        row.ResetSkillCommand.Execute(null);
        Assert.That(row.ConfigurationValues["offering-location"], Is.EqualTo("offering-at-prif-agility"));
    }

    [TestCase("infernal-ashes")]
    [TestCase("abyssal-ashes")]
    public async Task BankChoiceAndCustomRatePersistUntilAnExplicitNewSelection(string id)
    {
        var row = Row();
        row.SelectedMethodOption = row.MethodOptions.Single(option => option.Id == id);
        row.ApplyConfiguration(Location("offering-at-bank"));
        row.PersonalRate = 400000m;
        using var temporary = new TemporaryDirectory();
        var store = new JsonTrainingPlanStore(new TrainingPlanOptions { FilePath = Path.Combine(temporary.Path, "plans.json") });
        await store.SaveAsync("Test", [row.ToPreference()]);
        var restored = Row((await store.GetAsync("Test"))["Prayer"]);
        Assert.That(restored.ConfigurationValues["offering-location"], Is.EqualTo("offering-at-bank"));
        Assert.That(restored.PersonalRate, Is.EqualTo(400000m));
        restored.StartExperience += 1000;
        restored.UpdatePrices(Prices());
        Assert.That(restored.ConfigurationValues["offering-location"], Is.EqualTo("offering-at-bank"));
        restored.SelectedMethodOption = restored.MethodOptions.Single(option => option.Id == (id == "infernal-ashes" ? "abyssal-ashes" : "infernal-ashes"));
        Assert.That(restored.ConfigurationValues["offering-location"], Is.EqualTo("offering-at-prif-agility"));
        restored.SelectedMethodOption = restored.MethodOptions.Single(option => option.Id == "dragon-bones");
        Assert.That(restored.Definition.Configurator!.GetDefinition("dragon-bones").Options.Single().Choices,
            Has.Count.EqualTo(4));
        restored.ApplyConfiguration(Location("gilded-altar"));
        Assert.That(restored.Result.GeneratedExperience, Is.Empty);
    }

    [Test]
    public void LegacyAshPreferenceWithAltarIsRepairedOnLoad()
    {
        var row = Row(new TrainingSkillPreference("Prayer", 200000000,
            TrainingMethodId: "infernal-ashes", Configuration: Location("chaos-altar")));
        Assert.That(row.ConfigurationValues["offering-location"], Is.EqualTo("offering-at-prif-agility"));
        Assert.That(row.ToPreference().Configuration!["offering-location"], Is.EqualTo("offering-at-prif-agility"));
    }

    [TestCase("infernal-ashes", 25778, 330)]
    [TestCase("abyssal-ashes", 25775, 255)]
    public async Task AshSelectionPricesAndProjectsCreditsAcrossPlanner(string id, int itemId, int experiencePerAsh)
    {
        var prices = Prices();
        prices[itemId] = new(itemId, 3000, 2000, null, null);
        var market = new FakeMarketDataService { Latest = prices };
        var context = new CurrentProfileContext(new FakeHiscoreClient(),
            new HiscoreParser(TimeProvider.System), new MemoryProfilePreferenceStore("bottleo"));
        var vm = new XpPlannerViewModel(new MainEhpCatalogue(), new TrainingPlanCalculator(),
            new TrainingMoneyMakingCalculator(), market, new TrainingPlanPersistenceService(new MemoryTrainingPlanStore()), context,
            new MoneyMakerSelectionContext());
        await vm.LoadAsync();
        var originalProfile = context.CurrentProfile;
        foreach (var row in vm.Rows)
            row.TargetExperience = row.StartExperience;
        var prayer = vm.Rows.Single(row => row.Skill == "Prayer");
        var magic = vm.Rows.Single(row => row.Skill == "Magic");
        var agility = vm.Rows.Single(row => row.Skill == "Agility");
        prayer.StartExperience = 0;
        prayer.TargetExperience = experiencePerAsh * 2400;
        magic.TargetExperience = magic.StartExperience + 1000000;
        agility.TargetExperience = agility.StartExperience + 1000000;
        prayer.SelectedMethodOption = prayer.MethodOptions.Single(option => option.Id == id);

        Assert.That(prayer.Result.HasMissingPrice, Is.False);
        Assert.That(magic.Result.AppliedExperienceCredit, Is.EqualTo(140000));
        Assert.That(agility.Result.AppliedExperienceCredit, Is.EqualTo(134060));
        await vm.RefreshPricesCommand.ExecuteAsync(null);
        foreach (var request in market.LatestRequests)
            Assert.That(request, Does.Contain(itemId));
        prayer.ApplyConfiguration(Location("offering-at-bank"));
        Assert.That(magic.Result.AppliedExperienceCredit, Is.EqualTo(140000));
        Assert.That(agility.Result.AppliedExperienceCredit, Is.Zero);
        Assert.That(context.CurrentProfile, Is.SameAs(originalProfile));
    }
}
