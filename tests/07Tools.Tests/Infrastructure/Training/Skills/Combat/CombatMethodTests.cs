namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Combat;

[TestFixture]
[Category("Unit")]
public sealed class CombatMethodTests
{
    [Test]
    [Property("LegacyScenario", "CombatMethodCatalogue")]
    [Description("combat methods expose reviewed rates, zero-time flags, and supplies")]
    public void CombatMethodCatalogue()
    {
        var catalogue = new MainEhpCatalogue();
        Assert.That(catalogue.Skills.All(skill => skill.Skill is not "Attack" and not "Strength" and not "Hitpoints"), Is.True, "zero-time melee and Hitpoints skills should be omitted from the XP Planner catalogue");

        var defenceDefinition = catalogue.Skills.Single(skill => skill.Skill == "Defence");
        var defence = TrainingBand(catalogue, "Defence", 0);
        Assert.That(defence.ExperiencePerHour, Is.EqualTo(405_000m).Within(0m), "Defence rate");
        Assert.That(defence.Method, Is.EqualTo("Black Chinchompas & Cannon - Defensive"), "Defence method");
        Assert.That(Resource(defence, 11959).QuantityPerExperience, Is.EqualTo(1_500m / 405_000m).Within(0m), "Defence chins per XP");
        Assert.That(Resource(defence, 2).QuantityPerExperience, Is.EqualTo(6_000m / 405_000m).Within(0m), "Defence cannonballs per XP");

        var redDefence = defenceDefinition.ResolveMethod("red-chinchompas-cannon-defensive").Bands.Single();
        Assert.That(redDefence.ExperiencePerHour, Is.EqualTo(330_000m).Within(0m), "red-chinchompa Defence rate");
        Assert.That(redDefence.Method, Is.EqualTo("Red Chinchompas & Cannon - Defensive"), "red-chinchompa Defence method");
        Assert.That(Resource(redDefence, 10034).QuantityPerExperience, Is.EqualTo(1_500m / 330_000m).Within(0m), "red-chinchompa Defence chins per XP");
        Assert.That(Resource(redDefence, 2).QuantityPerExperience, Is.EqualTo(6_000m / 330_000m).Within(0m), "red-chinchompa Defence cannonballs per XP");

        var rangedDefinition = catalogue.Skills.Single(skill => skill.Skill == "Ranged");
        var ranged = TrainingBand(catalogue, "Ranged", 0);
        Assert.That(rangedDefinition.IsZeroTime, Is.True, "Ranged should contribute zero active hours");
        Assert.That(ranged.ExperiencePerHour, Is.EqualTo(1_150_000m).Within(0m), "Ranged rate");
        Assert.That(ranged.Method, Is.EqualTo("Black Chinchompas & Cannon"), "Ranged method");
        Assert.That(Resource(ranged, 11959).QuantityPerExperience, Is.EqualTo(1_866m / 1_150_000m).Within(0m), "Ranged chins per XP");
        Assert.That(Resource(ranged, 2).QuantityPerExperience, Is.EqualTo(6_000m / 1_150_000m).Within(0m), "Ranged cannonballs per XP");

        var redRanged = rangedDefinition.ResolveMethod("red-chinchompas-cannon").Bands.Single();
        Assert.That(redRanged.ExperiencePerHour, Is.EqualTo(940_000m).Within(0m), "red-chinchompa Ranged rate");
        Assert.That(redRanged.Method, Is.EqualTo("Red Chinchompas & Cannon"), "red-chinchompa Ranged method");
        Assert.That(Resource(redRanged, 10034).QuantityPerExperience, Is.EqualTo(1_866m / 940_000m).Within(0m), "red-chinchompa Ranged chins per XP");
        Assert.That(Resource(redRanged, 2).QuantityPerExperience, Is.EqualTo(6_000m / 940_000m).Within(0m), "red-chinchompa Ranged cannonballs per XP");

        var magicDefinition = catalogue.Skills.Single(skill => skill.Skill == "Magic");
        var magic = TrainingBand(catalogue, "Magic", 0);
        Assert.That(magicDefinition.IsZeroTime, Is.True, "Magic should contribute zero active hours");
        Assert.That(magic.ExperiencePerHour, Is.EqualTo(330_000m).Within(0m), "Magic reference rate");
        Assert.That(magic.Method, Is.EqualTo("Ice Barrage"), "Magic method");
        Assert.That(Resource(magic, 565).QuantityPerExperience, Is.EqualTo(2m * 0.85m * 1_085m / 330_000m).Within(0m), "blood runes per Magic XP");
        Assert.That(Resource(magic, 560).QuantityPerExperience, Is.EqualTo(4m * 0.85m * 1_085m / 330_000m).Within(0m), "death runes per Magic XP");

        var slayerDefinition = catalogue.Skills.Single(skill => skill.Skill == "Slayer");
        var slayer = TrainingBand(catalogue, "Slayer", 0);
        Assert.That(slayer.ExperiencePerHour, Is.EqualTo(123_040m).Within(0m), "Slayer rate");
        Assert.That(slayer.Economics is { IsComplete: true }, Is.True, "Slayer break-even economics should be explicit");
        Assert.That(slayerDefinition.ExperienceOutputs?.Count ?? 0, Is.EqualTo(1), "Slayer secondary skill count");
        Assert.That(slayerDefinition.ExperienceOutputs![0].Skill, Is.EqualTo("Magic"), "Slayer secondary skill");
        Assert.That(slayerDefinition.ExperienceOutputs[0].QuantityPerPrimaryExperience, Is.EqualTo(163_136_972m / (6_578m * 28_397m)).Within(0m), "Magic XP per Slayer XP");
    }

    [Test]
    [Property("LegacyScenario", "CombatDependencyCalculations")]
    [Description("Slayer credit reduces zero-time Magic cost without changing profile XP")]
    public void CombatDependencyCalculations()
    {
        var catalogue = new MainEhpCatalogue();
        var calculator = new TrainingPlanCalculator();
        var prices = new Dictionary<int, ItemPrice>
        {
            [2] = Quote(2, 200),
            [10034] = Quote(10034, 1_000),
            [11959] = Quote(11959, 3_000),
            [560] = Quote(560, 150),
            [565] = Quote(565, 300)
        };

        var defence = calculator.Calculate(
            catalogue.Skills.Single(skill => skill.Skill == "Defence"),
            0,
            TrainingPlanCalculator.MaximumExperience,
            prices);
        Assert.That(defence.Hours, Is.EqualTo(493.8272m).Within(0.0001m), "Defence 0-200m hours");
        Assert.That(defence.NetGp ?? 0m, Is.EqualTo(-TrainingPlanCalculator.MaximumExperience
            * (1_500m / 405_000m * 3_000m + 6_000m / 405_000m * 200m)).Within(0.01m), "Defence supply cost");

        var redDefence = calculator.Calculate(
            catalogue.Skills.Single(skill => skill.Skill == "Defence"),
            0,
            TrainingPlanCalculator.MaximumExperience,
            prices,
            methodId: "red-chinchompas-cannon-defensive");
        Assert.That(redDefence.Hours, Is.EqualTo(606.0606m).Within(0.0001m), "red-chinchompa Defence 0-200m hours");
        Assert.That(redDefence.NetGp ?? 0m, Is.EqualTo(-TrainingPlanCalculator.MaximumExperience
            * (1_500m / 330_000m * 1_000m + 6_000m / 330_000m * 200m)).Within(0.01m), "red-chinchompa Defence supply cost");

        var ranged = calculator.Calculate(
            catalogue.Skills.Single(skill => skill.Skill == "Ranged"),
            0,
            TrainingPlanCalculator.MaximumExperience,
            prices);
        Assert.That(ranged.Hours, Is.EqualTo(0m).Within(0m), "zero-time Ranged hours");
        Assert.That(ranged.NetGp ?? 0m, Is.EqualTo(-TrainingPlanCalculator.MaximumExperience
            * (1_866m / 1_150_000m * 3_000m + 6_000m / 1_150_000m * 200m)).Within(0.01m), "Ranged supply cost");
        Assert.That(ranged.GpPerExperience < 0m, Is.True, "Ranged should expose GP per XP");

        var redRanged = calculator.Calculate(
            catalogue.Skills.Single(skill => skill.Skill == "Ranged"),
            0,
            TrainingPlanCalculator.MaximumExperience,
            prices,
            methodId: "red-chinchompas-cannon");
        Assert.That(redRanged.Hours, Is.EqualTo(0m).Within(0m), "zero-time red-chinchompa Ranged hours");
        Assert.That(redRanged.NetGp ?? 0m, Is.EqualTo(-TrainingPlanCalculator.MaximumExperience
            * (1_866m / 940_000m * 1_000m + 6_000m / 940_000m * 200m)).Within(0.01m), "red-chinchompa Ranged supply cost");
        Assert.That(redRanged.GpPerExperience < 0m, Is.True, "red-chinchompa Ranged should expose GP per XP");

        const long reviewedSlayerExperience = 6_578L * 28_397L;
        var slayer = calculator.Calculate(
            catalogue.Skills.Single(skill => skill.Skill == "Slayer"),
            0,
            reviewedSlayerExperience,
            prices);
        Assert.That(slayer.NetGp ?? decimal.MinValue, Is.EqualTo(0m).Within(0m), "Slayer GP/XP break-even");
        Assert.That(slayer.IsFullyPriced, Is.True, "Slayer should be explicitly fully priced at break-even");
        Assert.That(slayer.GeneratedExperience["Magic"], Is.EqualTo(163_136_972m).Within(0.0001m), "generated Slayer Magic XP");

        var magic = calculator.Calculate(
            catalogue.Skills.Single(skill => skill.Skill == "Magic"),
            0,
            TrainingPlanCalculator.MaximumExperience,
            prices,
            pendingExperienceCredit: (long)slayer.GeneratedExperience["Magic"]);
        const long expectedMagicRemaining = TrainingPlanCalculator.MaximumExperience - 163_136_972L;
        Assert.That(magic.AppliedExperienceCredit, Is.EqualTo(163_136_972L), "applied Slayer Magic credit");
        Assert.That(magic.ExperienceRemaining, Is.EqualTo(expectedMagicRemaining), "Magic XP left for Ice Barrage");
        Assert.That(magic.StartExperience, Is.EqualTo(0L), "Magic profile start remains unchanged");
        Assert.That(magic.Hours, Is.EqualTo(0m).Within(0m), "zero-time Magic hours");
        Assert.That(magic.NetGp ?? 0m, Is.EqualTo(-expectedMagicRemaining
            * (2m * 0.85m * 1_085m / 330_000m * 300m
               + 4m * 0.85m * 1_085m / 330_000m * 150m)).Within(0.01m), "Ice Barrage residual cost");
    }
}
