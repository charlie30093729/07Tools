using static RunescapeTools.Tests.TestSupport.Builders.AgilityCourseTestData;
using static RunescapeTools.Tests.TestSupport.Builders.PrayerOfferingTestData;

namespace RunescapeTools.Tests.Wpf.ViewModels;

[TestFixture]
[Category("Unit")]
public sealed class StandaloneAgilityPlannerTests
{
    private static Dictionary<int, ItemPrice> CoursePrices() =>
        TestSupport.Builders.AgilityCourseTestData.Prices();

    [TestCase("prifddinas-agility-course", "Prifddinas Agility Course", 1210421, 75, 54000)]
    [TestCase("ardougne-rooftop-course", "Ardougne Rooftop Course", 5346332, 90, 70000)]
    public async Task CourseLabelsUnlocksOverridesAndPersistence(string id, string label, long unlock, int level, int rate)
    {
        var row = new XpPlannerRowViewModel(Definition(), new TrainingPlanCalculator(), 13034431,
            null, CoursePrices(), () => { });
        row.SelectedMethodOption = row.MethodOptions.Single(option => option.Id == id);
        row.StartExperience = 200000000;
        Assert.That(row.SelectedMethodOption.Name, Is.EqualTo(label));
        row.StartExperience = unlock - 1;
        Assert.That(row.SelectedMethodOption.Name, Is.EqualTo($"{label} — unlocks at {level}"));
        Assert.That(row.HasAvailabilitySummary, Is.True);
        row.StartExperience = unlock;
        Assert.That(row.SelectedMethodOption.Name, Is.EqualTo(label));
        Assert.That(row.PersonalRate, Is.EqualTo(rate));
        row.PersonalRate = 50000m;
        using var temporary = new TemporaryDirectory();
        var store = new JsonTrainingPlanStore(new TrainingPlanOptions { FilePath = Path.Combine(temporary.Path, "plans.json") });
        await store.SaveAsync("Runner", [row.ToPreference()]);
        var restored = new XpPlannerRowViewModel(Definition(), new TrainingPlanCalculator(), unlock,
            (await store.GetAsync("Runner"))["Agility"], CoursePrices(), () => { });
        Assert.That(restored.SelectedMethodOption!.Id, Is.EqualTo(id));
        Assert.That(restored.PersonalRate, Is.EqualTo(50000m));
        restored.ResetSkillCommand.Execute(null);
        Assert.That(restored.SelectedMethodOption.Id, Is.EqualTo(id));
        Assert.That(restored.PersonalRate, Is.EqualTo(rate));
    }

    [TestCase("prifddinas-agility-course", 66000)]
    [TestCase("ardougne-rooftop-course", 70000)]
    public async Task PrayerKeepsItsOwnCalculationWhileAgilityTrainsOnlyRemainingXp(string courseId, int rate)
    {
        var market = new FakeMarketDataService { Latest = CoursePrices() };
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
        var agility = vm.Rows.Single(row => row.Skill == "Agility");
        prayer.StartExperience = 0;
        prayer.TargetExperience = 720000;
        prayer.SelectedMethodOption = prayer.MethodOptions.Single(option => option.Id == "frost-dragon-bones");
        prayer.ApplyConfiguration(Location("offering-at-prif-agility"));
        var prayerBefore = prayer.Result;
        agility.StartExperience = 13034431;
        agility.TargetExperience = 13034431 + 134060 + rate;
        agility.SelectedMethodOption = agility.MethodOptions.Single(option => option.Id == courseId);

        Assert.That(prayer.Result.Hours, Is.EqualTo(prayerBefore.Hours));
        Assert.That(prayer.Result.NetGp, Is.EqualTo(prayerBefore.NetGp));
        Assert.That(prayer.Result.ResourceRequirements, Is.EqualTo(prayerBefore.ResourceRequirements));
        Assert.That(prayer.Result.GeneratedExperience, Is.EqualTo(prayerBefore.GeneratedExperience));
        Assert.That(agility.Result.AppliedExperienceCredit, Is.EqualTo(134060));
        Assert.That(agility.Result.Hours, Is.EqualTo(1m));
        Assert.That(agility.Result.GeneratedExperience, Is.Empty);
        Assert.That(agility.SelectedMethodOption.Id, Is.EqualTo(courseId));
        var independent = new TrainingPlanCalculator().Calculate(Definition(), 13034431 + 134060,
            agility.TargetExperience, CoursePrices(), methodId: courseId);
        Assert.That(agility.Result.ResourceRequirements, Is.EqualTo(independent.ResourceRequirements));
        Assert.That(agility.Result.NetGp, Is.EqualTo(independent.NetGp));
        await vm.RefreshPricesCommand.ExecuteAsync(null);
        Assert.That(market.LatestRequests.Last(), Is.SupersetOf(new[] { 12640, 12695, 23685 }));

        prayer.ApplyConfiguration(Location("offering-at-bank"));
        Assert.That(agility.Result.AppliedExperienceCredit, Is.Zero);
        Assert.That(agility.Result.Hours, Is.EqualTo((134060m + rate) / rate));
        prayer.ApplyConfiguration(Location("offering-at-prif-agility"));
        agility.TargetExperience = agility.StartExperience + 100000;
        Assert.That(agility.Result.Hours, Is.Zero);
        Assert.That(agility.Result.ResourceRequirements, Is.Empty);
        Assert.That(context.CurrentProfile, Is.SameAs(originalProfile));
    }
}
