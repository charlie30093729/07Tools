namespace RunescapeTools.Tests.Infrastructure.Persistence;

[TestFixture]
[Category("Persistence")]
public sealed class JsonFavouriteStoreTests
{
    [Test]
    [Property("LegacyScenario", "JsonStoreSeedsSortsAndDeduplicates")]
    [Description("JSON store preserves seed order, appends new favourites, and prevents duplicates")]
    public async Task JsonStorePreservesOrderAndDeduplicates()
    {
        using var temporary = new TemporaryDirectory();
        var directory = temporary.Path;
        var path = Path.Combine(directory, "favourites.json");
        var store = new JsonFavouriteStore(new FavouriteStoreOptions
        {
            FilePath = path,
            SeedJson = "[{\"itemId\":2,\"name\":\"Zulrah scale\",\"addedAt\":\"2026-01-01T00:00:00Z\"},{\"itemId\":1,\"name\":\"Blood shard\",\"addedAt\":\"2026-01-01T00:00:00Z\"}]"
        });

        var seeded = await store.GetAllAsync();
        await store.AddAsync(new FavouriteItem(1, "Duplicate", DateTimeOffset.UtcNow));
        await store.AddAsync(new FavouriteItem(3, "Adamant bar", DateTimeOffset.UtcNow));
        var saved = await store.GetAllAsync();

        Assert.That(seeded.Select(item => item.ItemId), Is.EqualTo(new[] { 2, 1 }), "seed order");
        Assert.That(saved.Count, Is.EqualTo(3), "duplicate prevention");
        Assert.That(saved.Select(item => item.ItemId), Is.EqualTo(new[] { 2, 1, 3 }), "new items append without resorting");
        Assert.That(File.Exists(path), Is.True, "favourites file exists");
        Assert.That(!File.Exists(path + ".tmp"), Is.True, "atomic temporary file is replaced");
    }

    [Test]
    public async Task MovesSurviveRestartAndLaterAddRemoveOperations()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "favourites.json");
        var options = new FavouriteStoreOptions { FilePath = path };
        var store = new JsonFavouriteStore(options);
        var items = new[]
        {
            new FavouriteItem(1, "Adamant bar", DateTimeOffset.UnixEpoch),
            new FavouriteItem(2, "Blood shard", DateTimeOffset.UnixEpoch.AddDays(1)),
            new FavouriteItem(3, "Zulrah scale", DateTimeOffset.UnixEpoch.AddDays(2))
        };
        foreach (var item in items)
            await store.AddAsync(item);

        await store.MoveBeforeAsync(3, 1);
        Assert.That((await new JsonFavouriteStore(options).GetAllAsync()).Select(item => item.ItemId),
            Is.EqualTo(new[] { 3, 1, 2 }));
        await store.MoveBeforeAsync(3, null);
        Assert.That((await store.GetAllAsync()).Select(item => item.ItemId), Is.EqualTo(new[] { 1, 2, 3 }));
        await store.MoveBeforeAsync(1, 3);
        await store.AddAsync(new FavouriteItem(4, "Air rune", DateTimeOffset.UnixEpoch));
        await store.RemoveAsync(3);

        var saved = await new JsonFavouriteStore(options).GetAllAsync();
        Assert.That(saved.Select(item => item.ItemId), Is.EqualTo(new[] { 2, 1, 4 }));
        Assert.That(saved.Take(2), Is.EqualTo(new[] { items[1], items[0] }), "item names and dates survive moves");
        Assert.That(File.Exists(path + ".tmp"), Is.False, "atomic replacement leaves no temporary file");
    }

    [Test]
    public async Task ExistingArrayOrderLoadsWithoutSchemaMigrationOrOverwrite()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "favourites.json");
        const string json = "[{\"itemId\":3,\"name\":\"Zulrah scale\",\"addedAt\":\"2026-01-01T00:00:00Z\"},{\"itemId\":1,\"name\":\"Adamant bar\",\"addedAt\":\"2026-01-01T00:00:00Z\"}]";
        await File.WriteAllTextAsync(path, json);
        var store = new JsonFavouriteStore(new FavouriteStoreOptions
        {
            FilePath = path,
            SeedJson = "[{\"itemId\":9,\"name\":\"Conflicting seed\",\"addedAt\":\"2026-01-01T00:00:00Z\"}]"
        });

        Assert.That((await store.GetAllAsync()).Select(item => item.ItemId), Is.EqualTo(new[] { 3, 1 }));
        Assert.That(await File.ReadAllTextAsync(path), Is.EqualTo(json));
    }

    [Test]
    public async Task InvalidCancelledAndNoOpMovesLeaveSavedFileUntouched()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "favourites.json");
        var store = new JsonFavouriteStore(new FavouriteStoreOptions { FilePath = path });
        await store.AddAsync(new FavouriteItem(1, "A", DateTimeOffset.UnixEpoch));
        await store.AddAsync(new FavouriteItem(2, "B", DateTimeOffset.UnixEpoch));
        var original = await File.ReadAllTextAsync(path);

        await Assert.ThatAsync((Func<Task>)(() => store.MoveBeforeAsync(99, 1)), Throws.InstanceOf<ArgumentException>());
        await Assert.ThatAsync((Func<Task>)(() => store.MoveBeforeAsync(1, 99)), Throws.InstanceOf<ArgumentException>());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThatAsync((Func<Task>)(() => store.MoveBeforeAsync(2, 1, cancellation.Token)),
            Throws.InstanceOf<OperationCanceledException>());
        await store.MoveBeforeAsync(1, 1);
        await store.MoveBeforeAsync(1, 2);
        await store.MoveBeforeAsync(2, null);

        Assert.That(await File.ReadAllTextAsync(path), Is.EqualTo(original));
        Assert.That(File.Exists(path + ".tmp"), Is.False);
    }

    [Test]
    public async Task ConcurrentMoveAndAppendPreserveEveryFavourite()
    {
        using var temporary = new TemporaryDirectory();
        var store = new JsonFavouriteStore(new FavouriteStoreOptions
        {
            FilePath = Path.Combine(temporary.Path, "favourites.json")
        });
        await store.AddAsync(new FavouriteItem(1, "A", DateTimeOffset.UnixEpoch));
        await store.AddAsync(new FavouriteItem(2, "B", DateTimeOffset.UnixEpoch));

        await Task.WhenAll(
            store.MoveBeforeAsync(2, 1),
            store.AddAsync(new FavouriteItem(3, "C", DateTimeOffset.UnixEpoch)),
            store.AddAsync(new FavouriteItem(4, "D", DateTimeOffset.UnixEpoch)));

        var saved = await store.GetAllAsync();
        Assert.That(saved.Select(item => item.ItemId), Is.EquivalentTo(new[] { 1, 2, 3, 4 }));
        Assert.That(saved.Take(2).Select(item => item.ItemId), Is.EqualTo(new[] { 2, 1 }));
    }
}
