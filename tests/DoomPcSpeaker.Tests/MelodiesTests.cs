using NUnit.Framework;

[TestFixture]
internal sealed class MelodiesTests
{
    private const string MinimalMelody = """
        {"key":"doom","name":"DOOM","notes":[[440,100],[0,50]]}
        """;

    [Test]
    public void ParseJson_reads_array_notes()
    {
        var (song, error) = Melodies.ParseJson(MinimalMelody, "test.dpsm");

        Assert.That(error, Is.Null);
        Assert.That(song, Is.Not.Null);
        Assert.That(song!.Key, Is.EqualTo("doom"));
        Assert.That(song.Name, Is.EqualTo("DOOM"));
        Assert.That(song.Group, Is.Empty);
        Assert.That(song.Notes, Is.EqualTo(new[] { (440, 100), (0, 50) }));
    }

    [Test]
    public void ParseJson_accepts_note_boundaries()
    {
        const string json = """
            {"key":"k","name":"n","notes":[[1,1],[0,1]]}
            """;

        var (song, error) = Melodies.ParseJson(json, "test.dpsm");

        Assert.That(error, Is.Null);
        Assert.That(song!.Notes, Is.EqualTo(new[] { (1, 1), (0, 1) }));
    }

    [Test]
    public void ParseJson_reads_property_aliases()
    {
        const string json = """
            {"alias":" key ","title":" Title ","category":"Games","melody":[{"f":220,"d":80}]}
            """;

        var (song, error) = Melodies.ParseJson(json, "test.dpsm");

        Assert.That(error, Is.Null);
        Assert.That(song!.Key, Is.EqualTo("key"));
        Assert.That(song.Name, Is.EqualTo("Title"));
        Assert.That(song.Group, Is.EqualTo("Games"));
        Assert.That(song.Notes, Is.EqualTo(new[] { (220, 80) }));
    }

    [TestCase("{}", "missing 'key'")]
    [TestCase("{\"key\":\"k\"}", "missing 'name'")]
    [TestCase("{\"key\":\"k\",\"name\":\"n\"}", "missing 'notes'")]
    [TestCase("{\"key\":\"k\",\"name\":\"n\",\"notes\":[]}", "'notes' must not be empty")]
    [TestCase("{\"key\":\"k\",\"name\":\"n\",\"notes\":{}}", "'notes' must be an array")]
    [TestCase("{\"key\":\"k\",\"name\":\"n\",\"notes\":[[440]]}", "expected [freq, ms]")]
    [TestCase("{\"key\":\"k\",\"name\":\"n\",\"notes\":[[\"x\",1]]}", "freq and ms must be integers")]
    [TestCase("{\"key\":\"k\",\"name\":\"n\",\"notes\":[{}]}", "expected object with freq/ms")]
    [TestCase("{\"key\":\"k\",\"name\":\"n\",\"notes\":[[440,0]]}", "ms must be positive")]
    [TestCase("{\"key\":\"k\",\"name\":\"n\",\"notes\":[[440,-1]]}", "ms must be positive")]
    [TestCase("{\"key\":\"k\",\"name\":\"n\",\"notes\":[[0,-1]]}", "ms must be positive")]
    [TestCase("{\"key\":\"k\",\"name\":\"n\",\"notes\":[[-1,100]]}", "freq must be 0 for a pause or positive for a note")]
    [TestCase("{\"key\":\"k\",\"name\":\"n\",\"notes\":[{\"f\":440,\"d\":0}]}", "ms must be positive")]
    public void ParseJson_rejects_invalid_melodies(string json, string expectedError)
    {
        var (song, error) = Melodies.ParseJson(json, "test.dpsm");

        Assert.That(song, Is.Null);
        Assert.That(error, Does.Contain(expectedError));
    }

    [Test]
    public void ParseJson_rejects_non_object_root_and_malformed_json()
    {
        Assert.That(Melodies.ParseJson("[]", "test.dpsm").Error, Does.Contain("root must be a JSON object"));
        Assert.That(Melodies.ParseJson("{", "test.dpsm").Error, Does.Contain("invalid JSON"));
    }

    private static MelodyCatalog CreateCatalog() => new([
        new Melodies.Song("doom", "DOOM", "Games", [(440, 100)]),
        new Melodies.Song("tetris", "Tetris", "Games", [(220, 100)]),
        new Melodies.Song("nokia", "Nokia", "Fun", [(330, 100)]),
    ]);

    [Test]
    public void Resolve_is_case_insensitive_and_trims_query()
    {
        Assert.That(CreateCatalog().Resolve("  DOOM ")!.Key, Is.EqualTo("doom"));
    }

    [Test]
    public void Resolve_returns_null_for_unknown_or_empty_query()
    {
        var catalog = CreateCatalog();

        Assert.That(catalog.Resolve("unknown"), Is.Null);
        Assert.That(catalog.Resolve("   "), Is.Null);
    }

    [Test]
    public void Resolve_random_returns_track_from_catalog()
    {
        var catalog = CreateCatalog();

        Assert.That(catalog.All, Does.Contain(catalog.Resolve("random")));
        Assert.That(catalog.All, Does.Contain(catalog.Resolve("R")));
    }

    [Test]
    public void Resolve_random_returns_null_for_empty_catalog()
    {
        Assert.That(new MelodyCatalog([]).Resolve("random"), Is.Null);
    }

    [Test]
    public void Catalog_groups_tracks_by_name()
    {
        var catalog = CreateCatalog();

        Assert.That(catalog.Groups.Length, Is.EqualTo(2));
        Assert.That(catalog.Groups.Single(g => g.Name == "Games").Tracks.Length, Is.EqualTo(2));
    }

    [Test]
    public void LoadDirectory_collects_parse_errors_without_losing_valid_tracks()
    {
        string directory = Path.Combine(Path.GetTempPath(), "doompcspeaker-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "good.dpsm"), MinimalMelody);
            File.WriteAllText(Path.Combine(directory, "bad.dpsm"), "{");

            var (catalog, error, broken) = Melodies.LoadDirectory(directory);

            Assert.That(error, Is.Null);
            Assert.That(catalog.All.Select(s => s.Key), Is.EqualTo(new[] { "doom" }));
            Assert.That(broken, Has.Length.EqualTo(1));
            Assert.That(broken[0], Does.Contain("bad.dpsm").And.Contain("invalid JSON"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
