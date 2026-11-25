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

    private static readonly string[] NewTrackKeys =
    [
        "boomer", "pain", "brigada", "seryoga", "dozor", "zveri",
    ];

    [Test]
    public void NewTracks_all_parse_with_expected_metadata_and_valid_notes()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "melodies");
        var (catalog, error, broken) = Melodies.LoadDirectory(directory);

        Assert.That(error, Is.Null);
        Assert.That(broken, Is.Empty);

        foreach (string key in NewTrackKeys)
        {
            var song = catalog.All.SingleOrDefault(s => s.Key == key);
            Assert.That(song, Is.Not.Null, $"track '{key}' is missing");
            Assert.That(song!.Name, Is.Not.Empty);
            Assert.That(song.Group, Is.Not.Empty);
            Assert.That(song.Notes, Is.Not.Empty);
            Assert.That(song.Notes.All(n => n.freq >= 0 && n.ms > 0), Is.True,
                $"track '{key}' has invalid notes");
        }

        Assert.That(catalog.All.Select(s => s.Key).Distinct().Count(),
            Is.EqualTo(catalog.All.Length), "track keys must be unique");
    }

    [Test]
    public void TokyoDrift_uses_faster_tempo_without_extra_notes()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "melodies");
        var (catalog, _, broken) = Melodies.LoadDirectory(directory);
        var song = catalog.All.SingleOrDefault(s => s.Key == "tokyodrift");

        Assert.That(broken, Is.Empty);
        Assert.That(song, Is.Not.Null, "track 'tokyodrift' is missing");
        Assert.That(song!.Name, Is.EqualTo("Tokyo Drift — Teriyaki Boyz"));
        Assert.That(song.Group, Is.EqualTo("Movies"));

        var melody = song.Notes.Where(n => n.freq > 0).ToArray();
        var pauses = song.Notes.Where(n => n.freq == 0).ToArray();

        // Only the 39 notes of the source MIDI line at 136 BPM: no bells, no
        // bass and no added notes. Long gaps are shorter to keep it dense.
        Assert.That(melody, Has.Length.EqualTo(39));
        Assert.That(melody.All(n => n.ms is 220 or 221), Is.True);
        Assert.That(melody.Select(n => n.freq).Distinct(),
            Is.EquivalentTo(new[] { 247, 262, 330, 370, 392, 440 }));
        Assert.That(melody.Take(14).Select(n => n.freq), Is.EqualTo(new[]
        {
            247, 247, 247, 247, 247, 262, 330, 247, 247, 247, 262, 330, 247, 247,
        }));
        Assert.That(pauses, Has.Length.EqualTo(31));
        Assert.That(pauses.All(p => p.ms is 110 or 111 or 140 or 141), Is.True);
        Assert.That(song.Notes.Sum(n => n.ms), Is.EqualTo(12524));
    }

    private static readonly string[] RebuiltTrackKeys =
    [
        "igor", "zveri", "got",
    ];

    // Tempo grids of the corrected tracks: ~155, ~136 and 75 BPM.
    private static readonly int[] RebuiltTrackGrids = [193, 110, 200];

    [Test]
    public void RebuiltTracks_keep_source_tempo_grid_without_rests_or_flickers()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "melodies");
        var (catalog, _, broken) = Melodies.LoadDirectory(directory);

        Assert.That(broken, Is.Empty);

        for (int i = 0; i < RebuiltTrackKeys.Length; i++)
        {
            string key = RebuiltTrackKeys[i];
            int grid = RebuiltTrackGrids[i];
            var song = catalog.All.SingleOrDefault(s => s.Key == key);
            Assert.That(song, Is.Not.Null, $"track '{key}' is missing");

            // PcSpeaker.Play already inserts a 3 ms articulation gap and mutes
            // after every note, so [0, ms] entries are extra holes.
            Assert.That(song!.Notes.Any(n => n.freq == 0), Is.False,
                $"track '{key}' contains a rest entry");

            // Off-grid durations shift the perceived BPM of everything after.
            Assert.That(song.Notes.Where(n => n.ms < grid).All(n => n.ms >= grid * 9 / 10),
                Is.True, $"track '{key}' has durations off the tempo grid");
            Assert.That(song.Notes.All(n => n.ms >= grid * 9 / 10), Is.True,
                $"track '{key}' contains a flickering note");

            Assert.That(song.Notes.Sum(n => n.ms), Is.LessThanOrEqualTo(30000),
                $"track '{key}' is longer than 30 seconds");

            Assert.That(song.Notes[^1].ms, Is.GreaterThanOrEqualTo(700),
                $"track '{key}' ends in the middle of a phrase");
        }
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
