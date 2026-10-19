namespace RunescapeTools.Tests.Wpf.ViewModels;

[TestFixture]
[Category("Unit")]
public sealed class XpPlannerViewModelTests
{
    [Test]
    [Property("LegacyScenario", "XpPlannerViewModelFlow")]
    [Description("XP planner allocates money-maker profit to selected skill hours")]
    public async Task XpPlannerViewModelFlow()
    {
        var client = new FakeHiscoreClient();
        var context = new CurrentProfileContext(
            client,
            new HiscoreParser(TimeProvider.System),
            new MemoryProfilePreferenceStore("bottleo"));
        var market = new FakeMarketDataService
        {
            Latest = new Dictionary<int, ItemPrice> { [8778] = Quote(8778, 431), [8782] = Quote(8782, 1_910) }
        };
        var viewModel = new XpPlannerViewModel(
            new MainEhpCatalogue(),
            new TrainingPlanCalculator(),
            new TrainingMoneyMakingCalculator(),
            market,
            new TrainingPlanPersistenceService(new MemoryTrainingPlanStore()),
            context,
            new MoneyMakerSelectionContext());

        await viewModel.LoadAsync();
        Assert.That(viewModel.Rows.Count, Is.EqualTo(21), "XP planner row count");
        Assert.That(viewModel.Rows.All(row => row.Skill is not "Attack" and not "Strength" and not "Hitpoints"), Is.True, "XP planner omits zero-time melee and Hitpoints rows");
        Assert.That(viewModel.ProfileName, Is.EqualTo("bottleo"), "XP planner profile");
        var construction = viewModel.Rows.Single(row => row.Skill == "Construction");
        construction.StartExperience = 0;
        Assert.That(construction.Hours, Is.EqualTo("142.8"), "Construction displayed hours");
        Assert.That(construction.Result.NetGp is < -2_800_000_000m, Is.True, "Construction live cost");
        Assert.That(construction.EconomicRate.EndsWith(" gp/hr"), Is.True, "method subtitle identifies GP per hour");
        Assert.That(construction.AvailableMethods.Count, Is.EqualTo(3), "Construction exposes all selectable routes");
        Assert.That(construction.SelectedMethodOption?.Id ?? string.Empty, Is.EqualTo("main-ehp"), "Construction defaults to Main EHP");
        construction.PersonalRate = 100_000m;
        Assert.That(construction.Hours != "142.8", Is.True, "personal rate changes displayed hours");
        construction.ResetSkillCommand.Execute(null);
        Assert.That(construction.StartExperience, Is.EqualTo(construction.ProfileExperience), "reset restores profile start XP");
        Assert.That(construction.TargetExperience, Is.EqualTo(TrainingPlanCalculator.MaximumExperience), "reset restores 200m goal");
        Assert.That(construction.PersonalRate, Is.EqualTo(construction.Result.BaseRate).Within(0m), "reset restores the selected method rate at profile XP");

        var moneyMakerSelection = new MoneyMakerSelectionContext();
        var allocatedViewModel = new XpPlannerViewModel(
            new MainEhpCatalogue(),
            new TrainingPlanCalculator(),
            new TrainingMoneyMakingCalculator(),
            market,
            new TrainingPlanPersistenceService(new MemoryTrainingPlanStore()),
            context,
            moneyMakerSelection);
        await allocatedViewModel.LoadAsync();
        var allocatedConstruction = allocatedViewModel.Rows.Single(row => row.Skill == "Construction");
        allocatedConstruction.StartExperience = 0;
        allocatedConstruction.IsMoneyMakingSelected = true;
        moneyMakerSelection.Select("vyrewatch-sentinels", "Vyrewatch Sentinels", 2_400_000m, 3, false);
        Assert.That(allocatedViewModel.SelectedMoneyMakingHours, Is.EqualTo(allocatedConstruction.Result.Hours).Within(0m), "only selected skill hours receive money-maker profit");
        Assert.That(allocatedViewModel.MoneyMakerGpContribution, Is.EqualTo(allocatedConstruction.Result.Hours * 2_400_000m * 3m).Within(0m), "all-account money-maker contribution");
        var zeroTimeRanged = allocatedViewModel.Rows.Single(row => row.Skill == "Ranged");
        zeroTimeRanged.IsMoneyMakingSelected = true;
        Assert.That(allocatedViewModel.SelectedMoneyMakingHours, Is.EqualTo(allocatedConstruction.Result.Hours).Within(0m), "zero-time selected skills do not add money-making hours");
        allocatedViewModel.ResetMoneyMakerCommand.Execute(null);
        Assert.That(moneyMakerSelection.Current is null, Is.True, "XP Planner reset clears shared money maker");
        Assert.That(allocatedViewModel.MoneyMakerGpContribution, Is.EqualTo(0m).Within(0m), "reset removes money-maker contribution");

        var slayer = viewModel.Rows.Single(row => row.Skill == "Slayer");
        var magic = viewModel.Rows.Single(row => row.Skill == "Magic");
        slayer.StartExperience = 0;
        slayer.TargetExperience = 6_578L * 28_397L;
        magic.StartExperience = 0;
        magic.TargetExperience = TrainingPlanCalculator.MaximumExperience;
        Assert.That(magic.Result.AppliedExperienceCredit, Is.EqualTo(163_136_972L), "view-model Slayer credit");
        Assert.That(magic.Hours, Is.EqualTo("0"), "view-model zero-time Magic hours");
        Assert.That(magic.HasExperienceCredit, Is.True, "view-model exposes pending Magic credit");
        Assert.That(magic.CreditSummary.Contains("163,136,972"), Is.True, "view-model formats pending Magic credit");

        var fishing = viewModel.Rows.Single(row => row.Skill == "Fishing");
        var agility = viewModel.Rows.Single(row => row.Skill == "Agility");
        agility.StartExperience = 0;
        agility.TargetExperience = TrainingPlanCalculator.MaximumExperience;
        fishing.SelectedMethodOption = fishing.MethodOptions.Single(
            option => option.Id == "three-tick-barbarian-fishing");
        fishing.StartExperience = 83_014;
        fishing.TargetExperience = 183_014;
        Assert.That(agility.Result.AppliedExperienceCredit, Is.EqualTo(10_000L), "view-model Fishing Agility credit");
        Assert.That(agility.HasExperienceCredit, Is.True, "view-model exposes pending Agility credit");
        Assert.That(agility.CreditSummary.Contains("10,000"), Is.True, "view-model formats pending Agility credit");

        var hunter = viewModel.Rows.Single(row => row.Skill == "Hunter");
        hunter.SelectedMethodOption = hunter.MethodOptions.Single(option => option.Id == "aerial-fishing");
        fishing.StartExperience = 13_034_431;
        fishing.TargetExperience = 13_134_431;
        hunter.StartExperience = 13_034_431;
        hunter.TargetExperience = 13_163_431;
        Assert.That(hunter.Result.Hours, Is.EqualTo(1m).Within(0m), "view-model Aerial Fishing hours counted once");
        Assert.That(fishing.Result.AppliedExperienceCredit, Is.EqualTo(99_500L), "view-model Aerial Fishing credit");
        Assert.That(agility.Result.AppliedExperienceCredit, Is.EqualTo(43L), "linked Fishing-to-Agility credit converges");
        Assert.That(fishing.CreditSummary.Contains("Hunter: Aerial Fishing"), Is.True, "Fishing credit identifies the Aerial Fishing source");
    }

    [Test]
    [Property("LegacyScenario", "XpPlannerPriceFailure")]
    [Description("XP planner remains usable when live prices fail")]
    public async Task XpPlannerPriceFailure()
    {
        var market = new FakeMarketDataService
        {
            Failure = new HttpRequestException("Market unavailable")
        };
        var profileContext = new CurrentProfileContext(
            new FakeHiscoreClient(),
            new HiscoreParser(TimeProvider.System),
            new MemoryProfilePreferenceStore("bottleo"));
        var viewModel = new XpPlannerViewModel(
            new MainEhpCatalogue(),
            new TrainingPlanCalculator(),
            new TrainingMoneyMakingCalculator(),
            market,
            new TrainingPlanPersistenceService(new MemoryTrainingPlanStore()),
            profileContext,
            new MoneyMakerSelectionContext());

        await viewModel.LoadAsync();

        Assert.That(viewModel.Rows.Count > 0, Is.True, "catalogue rows remain available");
        Assert.That(viewModel.ErrorMessage?.Contains("prices", StringComparison.OrdinalIgnoreCase) == true, Is.True, "price warning is shown");
        Assert.That(viewModel.Rows.Any(row => row.TotalGp == "Not priced"), Is.True, "affected economics remain visibly unpriced");
    }
}
