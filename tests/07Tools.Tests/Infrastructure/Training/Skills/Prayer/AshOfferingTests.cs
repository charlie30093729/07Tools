using static RunescapeTools.Tests.TestSupport.Builders.PrayerOfferingTestData;

namespace RunescapeTools.Tests.Infrastructure.Training.Skills.Prayer;

[TestFixture]
[Category("Unit")]
public sealed class AshOfferingTests
{
    private static Dictionary<int, ItemPrice> AshPrices()
    {
        var prices = Prices();
        prices[25778] = new(25778, 3000, 2000, null, null);
        prices[25775] = new(25775, 1500, 1000, null, null);
        return prices;
    }

    [TestCase("infernal-ashes", 25778, 330, 3000)]
    [TestCase("abyssal-ashes", 25775, 255, 1500)]
    public void BankUsesCorrectAshXpAndRequestedSinisterRuneCosts(string method, int itemId, int xp, int price)
    {
        var result = new TrainingPlanCalculator().Calculate(CreatePrayerDefinition(), 0, xp * 1800,
            AshPrices(), methodId: method, configuration: Location("offering-at-bank"));
        Assert.That(result.BaseRate, Is.EqualTo(xp * 1800m));
        Assert.That(result.Hours, Is.EqualTo(1m));
        Assert.That(result.ResourceRequirements.Single(item => item.ItemId == itemId).Quantity,
            Is.EqualTo(1800m).Within(0.000001m));
        foreach (var rune in new[] { 565, 21880 })
            Assert.That(result.ResourceRequirements.Single(item => item.ItemId == rune).Quantity,
                Is.EqualTo(600m).Within(0.000001m));
        Assert.That(result.ResourceRequirements.Select(item => item.ItemId),
            Is.EquivalentTo(new[] { itemId, 565, 21880 }));
        Assert.That(result.NetGp, Is.EqualTo(-(1800m * price + 600m * 900m)).Within(0.01m));
        Assert.That(result.GeneratedExperience["Magic"], Is.EqualTo(105000m).Within(0.000001m));
        Assert.That(result.GeneratedExperience.ContainsKey("Agility"), Is.False);
        Assert.That(result.Method.Bands.Single().Method, Does.Contain("Demonic Offering at a bank"));
        Assert.That(result.IsFullyPriced, Is.True);
    }

    [TestCase("infernal-ashes", 25778, 330)]
    [TestCase("abyssal-ashes", 25775, 255)]
    public void DefaultPrifUsesExistingLapAndShardConversionModel(string method, int itemId, int xp)
    {
        var result = new TrainingPlanCalculator().Calculate(CreatePrayerDefinition(), 0, xp * 2400,
            AshPrices(), methodId: method);
        Assert.That(result.BaseRate, Is.EqualTo(xp * 24m * 3600m / 82m));
        Assert.That(result.Hours, Is.EqualTo(100m * 82m / 3600m).Within(0.000001m));
        Assert.That(result.GeneratedExperience["Magic"], Is.EqualTo(800m * 175m).Within(0.000001m));
        Assert.That(result.GeneratedExperience["Agility"], Is.EqualTo(134060m).Within(0.000001m));
        Assert.That(result.ResourceRequirements.Single(item => item.ItemId == 23962).Quantity,
            Is.EqualTo(94m).Within(0.000001m));
        Assert.That(result.ResourceRequirements.Single(item => item.ItemId == 12695).Quantity,
            Is.EqualTo(235m).Within(0.000001m));
        Assert.That(result.ResourceRequirements.Single(item => item.ItemId == 23685).Quantity,
            Is.EqualTo(235m).Within(0.000001m));
        Assert.That(result.ResourceRequirements.Single(item => item.ItemId == itemId).Quantity,
            Is.EqualTo(2400m).Within(0.000001m));
        Assert.That(result.Method.Bands.Single().Method, Does.Contain("Demonic Offering at Prif agility"));
    }

    [TestCase("infernal-ashes", "gilded-altar")]
    [TestCase("abyssal-ashes", "invalid")]
    public void InvalidSavedLocationsCannotCalculateAshesAtAnAltar(string method, string location)
    {
        var result = new TrainingPlanCalculator().Calculate(CreatePrayerDefinition(), 0, 1000000,
            AshPrices(), methodId: method, configuration: Location(location));
        Assert.That(result.Method.Bands.Single().Method, Does.Contain("Demonic Offering at Prif agility"));
        Assert.That(result.GeneratedExperience.ContainsKey("Agility"), Is.True);
    }

    [TestCase("infernal-ashes", 25778)]
    [TestCase("abyssal-ashes", 25775)]
    public void AshPricesAreDiscoveredAndMissingQuotesStayUnpriced(string method, int itemId)
    {
        var definition = CreatePrayerDefinition();
        Assert.That(definition.MarketItemIds, Does.Contain(itemId));
        var prices = AshPrices();
        prices.Remove(itemId);
        var result = new TrainingPlanCalculator().Calculate(definition, 0, 1000000, prices, methodId: method);
        Assert.That(result.HasMissingPrice, Is.True);
        Assert.That(result.IsFullyPriced, Is.False);
        Assert.That(result.NetGp, Is.Null);
    }
}
