using static RunescapeTools.Tests.TestSupport.Builders.PrayerOfferingTestData;

namespace RunescapeTools.Tests.Wpf.ViewModels;

[TestFixture]
[Category("Unit")]
public sealed class PrayerOfferingPlannerTests
{
    [Test]
    public async Task InitialLoadAndRefreshPriceOfferingOptionsEvenWithOnlyPrayerInCatalogue()
    {
        var market = new FakeMarketDataService { Latest = Prices() };
        var context = new CurrentProfileContext(new FakeHiscoreClient(),
            new HiscoreParser(TimeProvider.System), new MemoryProfilePreferenceStore("bottleo"));
        var vm = new XpPlannerViewModel(new PrayerOnlyCatalogue(), new TrainingPlanCalculator(),
            new TrainingMoneyMakingCalculator(), market, new TrainingPlanPersistenceService(new MemoryTrainingPlanStore()), context,
            new MoneyMakerSelectionContext());
        await vm.LoadAsync();
        var row = vm.Rows.Single();
        row.SelectedMethodOption = row.MethodOptions.Single(option => option.Id == "frost-dragon-bones");
        row.ApplyConfiguration(Location("offering-at-prif-agility"));
        Assert.That(row.Result.HasMissingPrice, Is.False);
        await vm.RefreshPricesCommand.ExecuteAsync(null);
        Assert.That(market.LatestRequests, Has.Count.EqualTo(2));
        foreach (var request in market.LatestRequests)
            Assert.That(request, Is.SupersetOf(new[] { 31729, 565, 21880, 12695, 23685 }));
    }

    [Test]
    public async Task SelectingPrifCreditsOtherSkillsAndRestoringTheAltarRemovesCredit()
    {
        var context = new CurrentProfileContext(new FakeHiscoreClient(),
            new HiscoreParser(TimeProvider.System), new MemoryProfilePreferenceStore("bottleo"));
        var vm = new XpPlannerViewModel(new MainEhpCatalogue(), new TrainingPlanCalculator(),
            new TrainingMoneyMakingCalculator(), new FakeMarketDataService { Latest = Prices() },
            new TrainingPlanPersistenceService(new MemoryTrainingPlanStore()), context, new MoneyMakerSelectionContext());
        await vm.LoadAsync();
        var originalProfile = context.CurrentProfile;
        foreach (var row in vm.Rows)
            row.TargetExperience = row.StartExperience;
        var prayer = vm.Rows.Single(row => row.Skill == "Prayer");
        var magic = vm.Rows.Single(row => row.Skill == "Magic");
        var agility = vm.Rows.Single(row => row.Skill == "Agility");
        prayer.StartExperience = 0;
        prayer.TargetExperience = 720_000;
        magic.TargetExperience = magic.StartExperience + 1_000_000;
        agility.TargetExperience = agility.StartExperience + 1_000_000;
        prayer.SelectedMethodOption = prayer.MethodOptions.Single(option => option.Id == "frost-dragon-bones");
        prayer.ApplyConfiguration(Location("offering-at-prif-agility"));

        Assert.That(prayer.Result.HasMissingPrice, Is.False);
        Assert.That(magic.Result.AppliedExperienceCredit, Is.EqualTo(144_000));
        Assert.That(agility.Result.AppliedExperienceCredit, Is.EqualTo(134_060));
        Assert.That(agility.CreditSummary, Does.Contain("Prayer"));
        Assert.That(context.CurrentProfile, Is.SameAs(originalProfile));

        // Banking still generates Magic XP, but must restore standalone Agility work.
        prayer.ApplyConfiguration(Location("offering-at-bank"));
        Assert.That(magic.Result.AppliedExperienceCredit, Is.EqualTo(144_000));
        Assert.That(agility.Result.AppliedExperienceCredit, Is.Zero);
        prayer.ApplyConfiguration(Location("gilded-altar"));
        Assert.That(magic.Result.AppliedExperienceCredit, Is.Zero);
        Assert.That(agility.Result.AppliedExperienceCredit, Is.Zero);
    }

    [TestCase("offering-at-bank")]
    [TestCase("offering-at-prif-agility")]
    public void RowRestoresOfferingConfigurationAndResetRestoresGildedAltar(string location)
    {
        var row = new XpPlannerRowViewModel(CreatePrayerDefinition(), new TrainingPlanCalculator(), 13_034_431,
            new TrainingSkillPreference("Prayer", 200_000_000, TrainingMethodId: "frost-dragon-bones",
                Configuration: Location(location)), Prices(), () => { });
        Assert.That(row.ConfigurationValues["offering-location"], Is.EqualTo(location));
        Assert.That(row.Result.Method.Bands.Last().Method, Does.Contain("Sinister Offering"));
        Assert.That(row.ToPreference().Configuration!["offering-location"], Is.EqualTo(location));
        row.ResetSkillCommand.Execute(null);
        Assert.That(row.ConfigurationValues["offering-location"], Is.EqualTo("gilded-altar"));
        Assert.That(row.Result.GeneratedExperience, Is.Empty);
    }

    [Test]
    public void PriceDialogShowsShardGainsWithoutTreatingThemAsPurchasedStock()
    {
        var result = new TrainingPlanCalculator().Calculate(CreatePrayerDefinition(), 0, 720_000, Prices(),
            methodId: "frost-dragon-bones", configuration: Location("offering-at-prif-agility"));
        var dialog = new TrainingPriceDialogViewModel("Prayer", result, Prices());
        var shard = dialog.Items.Single(item => item.ItemId == 23962);
        Assert.That(shard.Action, Is.EqualTo("GAIN"));
        Assert.That(shard.IsOutput, Is.True);
        Assert.That(shard.QuantityCaption, Does.Not.Contain("stock"));
        Assert.That(dialog.Items.Single(item => item.ItemId == 565).UnitPrice, Is.EqualTo("400 gp"));
        Assert.That(dialog.Items.Single(item => item.ItemId == 23685).UnitPrice, Is.EqualTo("15,000 gp"));
    }

    private sealed class PrayerOnlyCatalogue : IEhpCatalogue
    {
        public string Version => "Prayer test catalogue";
        public DateOnly VerifiedOn => new(2026, 9, 8);
        public IReadOnlyList<TrainingSkillDefinition> Skills { get; } = [CreatePrayerDefinition()];
    }
}
