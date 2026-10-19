namespace RunescapeTools.Tests.Wpf.ViewModels;

[TestFixture]
[Category("Unit")]
public sealed class ShellViewModelTests
{
    [Test]
    [Property("LegacyScenario", "ShellNavigation")]
    [Description("shell navigation loads the requested page")]
    public async Task ShellNavigation()
    {
        var store = new MemoryFavouriteStore();
        var market = new FakeMarketDataService();
        var dashboard = new DashboardViewModel(
            store,
            market,
            new FakeItemIconService(),
            [new VyrewatchMethod()]);
        var favourites = new FavouritesViewModel(
            store,
            market,
            new FakeItemIconService(),
            TimeProvider.System);
        var moneyMakerSelection = new MoneyMakerSelectionContext();
        var money = new MoneyMakersViewModel(
            [new VyrewatchMethod()],
            new MoneyMakingCalculator(),
            market,
            new MemoryMoneyMakingPreferenceStore(),
            moneyMakerSelection);
        var profileContext = new CurrentProfileContext(
            new FakeHiscoreClient(),
            new HiscoreParser(TimeProvider.System),
            new MemoryProfilePreferenceStore("bottleo"));
        var profile = new ProfileViewModel(profileContext);
        var xpPlanner = new XpPlannerViewModel(
            new MainEhpCatalogue(),
            new TrainingPlanCalculator(),
            new TrainingMoneyMakingCalculator(),
            market,
            new TrainingPlanPersistenceService(new MemoryTrainingPlanStore()),
            profileContext,
            moneyMakerSelection);
        var shell = new ShellViewModel(profile, dashboard, favourites, money, xpPlanner);

        await shell.InitializeAsync();
        Assert.That(shell.CurrentPageKind, Is.EqualTo(PageKind.Dashboard), "startup page remains dashboard");
        await shell.NavigateCommand.ExecuteAsync("Favourites");

        Assert.That(shell.CurrentPageKind, Is.EqualTo(PageKind.Favourites), "selected page");
        Assert.That(ReferenceEquals(favourites, shell.CurrentPage), Is.True, "active page instance");

        await shell.NavigateCommand.ExecuteAsync("XpPlanner");

        Assert.That(shell.CurrentPageKind, Is.EqualTo(PageKind.XpPlanner), "XP Planner selected page");
        Assert.That(ReferenceEquals(xpPlanner, shell.CurrentPage), Is.True, "XP Planner active page instance");
        Assert.That(xpPlanner.Rows.Count > 0, Is.True, "XP Planner rows loaded through shell navigation");
    }
}
