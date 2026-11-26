using System.Text.Json;

static class Melodies
{
    public sealed record Song(string Key, string Name, string Group, (int freq, int ms)[] Notes);
    public sealed record Group(string Name, Song[] Tracks);

    private static readonly string[] KeyProperties = ["key", "alias", "id"];
    private static readonly string[] NameProperties = ["name", "title"];
    private static readonly string[] GroupProperties = ["group", "category"];
    private static readonly string[] NotesProperties = ["notes", "melody"];
    private static readonly string[] FrequencyProperties = ["freq", "f"];
    private static readonly string[] DurationProperties = ["ms", "d", "duration"];

    public static readonly MelodyCatalog Catalog;
    public static readonly Group[] Groups;
    public static readonly Song[] All;
    public static readonly string? LoadError;
    public static readonly string[] BrokenTracks;

    static Melodies()
    {
        var (catalog, error, broken) = Load();
        Catalog = catalog;
        Groups = catalog.Groups;
        All = catalog.All;
        LoadError = error;
        BrokenTracks = broken;
    }

    public static Song? Resolve(string query) => Catalog.Resolve(query);

    internal static bool IsRandom(string query) =>
        query.Equals("random", StringComparison.OrdinalIgnoreCase) ||
        query.Equals("r", StringComparison.OrdinalIgnoreCase);

    internal static (Song? Song, string? Error) ParseJson(string json, string fileName)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return (null, $"invalid JSON in '{fileName}': {ex.Message}");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return (null, $"invalid melody file '{fileName}': root must be a JSON object");

            string? key = GetFirstString(root, KeyProperties);
            string? name = GetFirstString(root, NameProperties);
            string group = GetFirstString(root, GroupProperties) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(key))
                return (null, $"invalid melody file '{fileName}': missing 'key'");
            if (string.IsNullOrWhiteSpace(name))
                return (null, $"invalid melody file '{fileName}': missing 'name'");

            if (!TryGetNotes(root, out var notes, out string? notesError))
                return (null, $"invalid melody file '{fileName}': {notesError}");

            return (new Song(key.Trim(), name.Trim(), group.Trim(), notes), null);
        }
    }

    private static (MelodyCatalog Catalog, string? Error, string[] Broken) Load()
    {
        string? directory = FindMelodyDir();
        if (directory is null)
            return (new MelodyCatalog([]), null, []);

        return LoadDirectory(directory);
    }

    internal static (MelodyCatalog Catalog, string? Error, string[] Broken) LoadDirectory(string directory)
    {
        string[] files;
        try
        {
            files = Directory.GetFiles(directory, "*.dpsm", SearchOption.TopDirectoryOnly)
                .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception ex)
        {
            return (new MelodyCatalog([]), $"failed to read melody directory '{directory}': {ex.Message}", []);
        }

        var songs = new List<Song>();
        var broken = new List<string>();
        foreach (string file in files)
        {
            var (song, error) = ParseFile(file);
            if (song is not null)
                songs.Add(song);
            else if (error is not null)
                broken.Add(error);
        }

        return (new MelodyCatalog(songs), null, broken.ToArray());
    }

    private static string? FindMelodyDir()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "melodies");
        return Directory.Exists(directory) ? directory : null;
    }

    private static (Song? Song, string? Error) ParseFile(string file)
    {
        string fileName = Path.GetFileName(file);
        string json;
        try
        {
            json = File.ReadAllText(file);
        }
        catch (Exception ex)
        {
            return (null, $"failed to read '{fileName}': {ex.Message}");
        }

        return ParseJson(json, fileName);
    }

    private static string? GetFirstString(JsonElement obj, string[] properties)
    {
        foreach (string property in properties)
        {
            string? value = GetString(obj, property);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }
        return null;
    }

    private static string? GetString(JsonElement obj, string property)
    {
        if (!obj.TryGetProperty(property, out var element)) return null;
        if (element.ValueKind != JsonValueKind.String) return null;
        return element.GetString();
    }

    private static bool TryGetNotes(JsonElement root, out (int freq, int ms)[] notes, out string? error)
    {
        notes = [];
        error = null;

        JsonElement notesElement = default;
        bool found = false;
        foreach (string property in NotesProperties)
        {
            if (root.TryGetProperty(property, out notesElement))
            {
                found = true;
                break;
            }
        }
        if (!found)
        {
            error = "missing 'notes'";
            return false;
        }

        if (notesElement.ValueKind != JsonValueKind.Array)
        {
            error = "'notes' must be an array";
            return false;
        }

        var list = new List<(int freq, int ms)>(notesElement.GetArrayLength());
        int index = 0;
        foreach (var item in notesElement.EnumerateArray())
        {
            index++;
            if (item.ValueKind == JsonValueKind.Array)
            {
                if (item.GetArrayLength() < 2)
                {
                    error = $"note #{index}: expected [freq, ms]";
                    return false;
                }

                if (!TryGetInt(item[0], out int frequency) || !TryGetInt(item[1], out int ms))
                {
                    error = $"note #{index}: freq and ms must be integers";
                    return false;
                }

                if (!IsValidNote(frequency, ms, index, out error))
                    return false;

                list.Add((frequency, ms));
            }
            else if (item.ValueKind == JsonValueKind.Object)
            {
                if (!TryGetFirstInt(item, FrequencyProperties, out int frequency) ||
                    !TryGetFirstInt(item, DurationProperties, out int ms))
                {
                    error = $"note #{index}: expected object with freq/ms";
                    return false;
                }

                if (!IsValidNote(frequency, ms, index, out error))
                    return false;

                list.Add((frequency, ms));
            }
            else
            {
                error = $"note #{index}: expected [freq, ms] or object";
                return false;
            }
        }

        if (list.Count == 0)
        {
            error = "'notes' must not be empty";
            return false;
        }

        notes = list.ToArray();
        return true;
    }

    private static bool IsValidNote(int frequency, int ms, int index, out string? error)
    {
        if (frequency < 0)
        {
            error = $"note #{index}: freq must be 0 for a pause or positive for a note";
            return false;
        }
        if (ms <= 0)
        {
            error = $"note #{index}: ms must be positive";
            return false;
        }
        error = null;
        return true;
    }

    private static bool TryGetFirstInt(JsonElement obj, string[] properties, out int value)
    {
        foreach (string property in properties)
        {
            if (TryGetIntProp(obj, property, out value))
                return true;
        }
        value = 0;
        return false;
    }

    private static bool TryGetInt(JsonElement element, out int value)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value))
            return true;
        value = 0;
        return false;
    }

    private static bool TryGetIntProp(JsonElement obj, string property, out int value)
    {
        value = 0;
        return obj.TryGetProperty(property, out var element) && TryGetInt(element, out value);
    }
}

internal sealed class MelodyCatalog
{
    private readonly Dictionary<string, Melodies.Song> _byKey = new(StringComparer.OrdinalIgnoreCase);

    public MelodyCatalog(IEnumerable<Melodies.Song> songs)
    {
        All = songs.ToArray();
        foreach (var song in All)
            _byKey.TryAdd(song.Key, song);

        Groups = All
            .GroupBy(song => song.Group, StringComparer.OrdinalIgnoreCase)
            .Select(group => new Melodies.Group(group.First().Group, group.ToArray()))
            .ToArray();
    }

    public Melodies.Group[] Groups { get; }
    public Melodies.Song[] All { get; }

    public Melodies.Song? Resolve(string query)
    {
        string normalized = query.Trim();
        if (normalized.Length == 0) return null;

        if (Melodies.IsRandom(normalized))
            return All.Length == 0 ? null : All[Random.Shared.Next(All.Length)];

        return _byKey.TryGetValue(normalized, out var song) ? song : null;
    }
}
