namespace RunescapeTools.Tests.Wpf.ViewModels;

[TestFixture]
[Category("Unit")]
public sealed class XpPlannerRowViewModelTests
{
    [Test]
    [Property("LegacyScenario", "TrainingMethodAvailabilityLabels")]
    [Description("XP Planner method labels stay distinct while future routes use fallback bands")]
    public void TrainingMethodAvailabilityLabels()
    {
        var catalogue = new MainEhpCatalogue();
        var calculator = new TrainingPlanCalculator();
        var runecraft = catalogue.Skills.Single(skill => skill.Skill == "Runecraft");
        var row = new XpPlannerRowViewModel(
            runecraft,
            calculator,
            1_331_175,
            new TrainingSkillPreference("Runecraft", 32_000_000),
            new Dictionary<int, ItemPrice>(),
            () => { });

        const string expectedRunecraftLabels =
            "Solo mud runes|Solo lava runes|Solo aether runes — unlocks at 90|" +
            "Dolo aether runes — unlocks at 90|" +
            "Double nature runes - Achievement Diary cape — unlocks at 91|Ourania Altar (ZMI)|" +
            "Arceuus blood runes — unlocks at 77|Arceuus soul runes — unlocks at 90";
        Assert.That(string.Join('|', row.MethodOptions.Select(option => option.Name)), Is.EqualTo(expectedRunecraftLabels), "level-76 Runecraft labels");

        row.SelectedMethodOption = row.MethodOptions.Single(option => option.Id == "solo-aether-runes");
        Assert.That(row.Method, Is.EqualTo("Solo mud runes"), "Aether pre-unlock fallback calculation");
        Assert.That(row.Result.BaseRate, Is.EqualTo(74_500m).Within(0m), "Aether pre-unlock fallback rate");
        Assert.That(row.AvailabilitySummary, Is.EqualTo("Using Solo mud runes until level 90."), "Aether fallback disclosure");
        Assert.That(row.HasAvailabilitySummary, Is.True, "Aether fallback disclosure visibility");

        row.TargetExperience = 5_000_000;
        Assert.That(row.AvailabilitySummary, Is.EqualTo("Solo aether runes unlocks at level 90; this goal uses Solo mud runes."), "route beyond the selected goal disclosure");
        row.TargetExperience = 32_000_000;

        row.StartExperience = 5_346_332;
        Assert.That(row.SelectedMethodOption.Name, Is.EqualTo("Solo aether runes"), "Aether label after unlock");
        Assert.That(row.Method, Is.EqualTo("Solo aether runes"), "Aether active calculation after unlock");
        Assert.That(!row.HasAvailabilitySummary, Is.True, "Aether fallback disclosure clears after unlock");

        foreach (var skill in catalogue.Skills)
        {
            var skillRow = new XpPlannerRowViewModel(
                skill,
                calculator,
                0,
                null,
                new Dictionary<int, ItemPrice>(),
                () => { });
            var routeNames = skillRow.MethodOptions.Select(option => option.RouteName).ToArray();
            Assert.That(routeNames.Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(routeNames.Length), $"{skill.Skill} route names remain distinct");

            foreach (var experience in skill.AvailableMethods
                         .SelectMany(method => method.Bands)
                         .Select(band => band.StartExperience)
                         .Append(TrainingPlanCalculator.MaximumExperience)
                         .Distinct())
            {
                skillRow.StartExperience = experience;
                Assert.That(skillRow.MethodOptions.Select(option => option.Name)
                        .Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(skillRow.MethodOptions.Count), $"{skill.Skill} method labels stay distinct at {experience:N0} XP");
                for (var index = 0; index < skillRow.MethodOptions.Count; index++)
                {
                    var option = skillRow.MethodOptions[index];
                    Assert.That(option.Name.StartsWith(routeNames[index], StringComparison.Ordinal), Is.True, $"{skill.Skill} {option.Id} keeps its permanent route name at {experience:N0} XP");
                }
            }
        }
    }

    [Test]
    [Property("LegacyScenario", "XpPlannerRowMethodSelection")]
    [Description("XP Planner rows select and persist training methods")]
    public void XpPlannerRowMethodSelection()
    {
        var main = new TrainingMethodDefinition(
            "main",
            "Main route",
            [new TrainingRateBand(0, 100m, "Main training method")]);
        var alternative = new TrainingMethodDefinition(
            "alternative",
            "Alternative route",
            [new TrainingRateBand(0, 200m, "Alternative training method")]);
        var definition = new TrainingSkillDefinition(
            "Method test",
            main.Bands,
            Methods: [main, alternative],
            DefaultMethodId: main.Id);
        var changes = 0;
        var row = new XpPlannerRowViewModel(
            definition,
            new TrainingPlanCalculator(),
            0,
            null,
            new Dictionary<int, ItemPrice>(),
            () => changes++);

        Assert.That(row.SelectedMethodOption?.Id ?? string.Empty, Is.EqualTo("main"), "row default method");
        Assert.That(row.SelectedMethodOption?.Name ?? string.Empty, Is.EqualTo("Main route"), "stable route label");
        Assert.That(row.PersonalRate, Is.EqualTo(100m).Within(0m), "default method rate");

        row.SelectedMethodOption = row.MethodOptions.Single(option => option.Id == "alternative");

        Assert.That(row.Result.Method.Id, Is.EqualTo("alternative"), "selected calculation method");
        Assert.That(row.Method, Is.EqualTo("Alternative training method"), "selected active band label");
        Assert.That(row.PersonalRate, Is.EqualTo(200m).Within(0m), "selected method resets to its catalogue rate");
        Assert.That(changes, Is.EqualTo(1m).Within(0m), "method selection change notification");
        Assert.That(row.ToPreference().TrainingMethodId ?? string.Empty, Is.EqualTo("alternative"), "selected method preference");

        row.StartExperience = 1_000;
        row.TargetExperience = 10_000;
        row.PersonalRate = 321m;
        row.IsMoneyMakingSelected = true;
        row.ResetSkillCommand.Execute(null);

        Assert.That(row.StartExperience, Is.EqualTo(0L), "skill reset restores profile XP");
        Assert.That(row.TargetExperience, Is.EqualTo(TrainingPlanCalculator.MaximumExperience), "skill reset restores 200m goal");
        Assert.That(row.PersonalRate, Is.EqualTo(200m).Within(0m), "skill reset uses selected method catalogue rate");
        Assert.That(!row.IsMoneyMakingSelected, Is.True, "skill reset clears money-making allocation");
        Assert.That(row.SelectedMethodOption?.Id ?? string.Empty, Is.EqualTo("alternative"), "skill reset preserves selected method");

        var restoredRow = new XpPlannerRowViewModel(
            definition,
            new TrainingPlanCalculator(),
            0,
            new TrainingSkillPreference(
                definition.Skill,
                TrainingPlanCalculator.MaximumExperience,
                TrainingMethodId: alternative.Id),
            new Dictionary<int, ItemPrice>(),
            () => { });
        Assert.That(restoredRow.SelectedMethodOption?.Id ?? string.Empty, Is.EqualTo("alternative"), "saved method selection restores");
    }

    [Test]
    [Property("LegacyScenario", "XpPlannerRowConfiguration")]
    [Description("XP Planner rows persist and reset skill configuration")]
    public void XpPlannerRowConfiguration()
    {
        var definition = new MainEhpCatalogue().Skills
            .Single(skill => skill.Skill == "Fletching");
        var row = new XpPlannerRowViewModel(
            definition,
            new TrainingPlanCalculator(),
            5_346_332,
            null,
            new Dictionary<int, ItemPrice>(),
            () => { });

        Assert.That(row.HasConfiguration, Is.True, "Fletching row exposes configuration");
        row.ApplyConfiguration(new Dictionary<string, string>
        {
            ["include-hours"] = bool.FalseString
        });
        Assert.That(row.Result.Hours, Is.EqualTo(0m).Within(0m), "row applies Fletching zero-time selection");
        Assert.That(row.ToPreference().Configuration!["include-hours"], Is.EqualTo(bool.FalseString), "row persists Fletching configuration");

        row.ResetSkillCommand.Execute(null);
        Assert.That(row.ConfigurationValues["include-hours"], Is.EqualTo(bool.TrueString), "skill reset restores configuration default");
        Assert.That(row.Result.IncludesActiveHours, Is.True, "skill reset restores active hours");

        var restored = new XpPlannerRowViewModel(
            definition,
            new TrainingPlanCalculator(),
            5_346_332,
            new TrainingSkillPreference(
                "Fletching",
                TrainingPlanCalculator.MaximumExperience,
                Configuration: new Dictionary<string, string>
                {
                    ["include-hours"] = bool.FalseString
                }),
            new Dictionary<int, ItemPrice>(),
            () => { });
        Assert.That(!restored.Result.IncludesActiveHours, Is.True, "saved configuration restores");
    }

}
