using System.Text.Json;

static class Melodies
{
    public sealed record Song(string Key, string Name, string Group, (int freq, int ms)[] Notes);
    public sealed record Group(string Name, Song[] Tracks);

    public static readonly Group[] Groups;
    public static readonly Song[] All;
    public static readonly string? LoadError;

    static Melodies()
    {
        var (groups, all, error) = Load();
        Groups = groups;
        All = all;
        LoadError = error;
    }

    public static Song? Resolve(string query)
    {
        var q = query.Trim();
        if (q.Length == 0) return null;

        if (IsRandom(q))
            return All.Length == 0 ? null : All[Random.Shared.Next(All.Length)];

        return All.FirstOrDefault(s => s.Key.Equals(q, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRandom(string q) =>
        q.Equals("random", StringComparison.OrdinalIgnoreCase) ||
        q.Equals("r", StringComparison.OrdinalIgnoreCase);

    private static (Group[] Groups, Song[] All, string? Error) Load()
    {
        string? dir = FindMelodyDir();
        if (dir is null)
            return ([], [], null);

        string[] files;
        try
        {
            files = Directory.GetFiles(dir, "*.dpsm", SearchOption.TopDirectoryOnly)
                .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception ex)
        {
            return ([], [], $"failed to read melody directory '{dir}': {ex.Message}");
        }

        var songs = new List<Song>();
        foreach (string file in files)
        {
            var (song, _) = ParseFile(file);
            if (song is not null)
                songs.Add(song);
        }

        var groups = songs
            .GroupBy(s => s.Group, StringComparer.OrdinalIgnoreCase)
            .Select(g => new Group(g.First().Group, g.ToArray()))
            .ToArray();

        return (groups, songs.ToArray(), null);
    }

    private static string? FindMelodyDir()
    {
        string baseDir = AppContext.BaseDirectory;
        string dir = Path.Combine(baseDir, "melodies");
        return Directory.Exists(dir) ? dir : null;
    }

    private static (Song? Song, string? Error) ParseFile(string file)
    {
        string json;
        try
        {
            json = File.ReadAllText(file);
        }
        catch (Exception ex)
        {
            return (null, $"failed to read '{Path.GetFileName(file)}': {ex.Message}");
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return (null, $"invalid JSON in '{Path.GetFileName(file)}': {ex.Message}");
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return (null, $"invalid melody file '{Path.GetFileName(file)}': root must be a JSON object");

            string? key = GetString(root, "key") ?? GetString(root, "alias") ?? GetString(root, "id");
            string? name = GetString(root, "name") ?? GetString(root, "title");
            string group = GetString(root, "group") ?? GetString(root, "category") ?? string.Empty;

            if (string.IsNullOrWhiteSpace(key))
                return (null, $"invalid melody file '{Path.GetFileName(file)}': missing 'key'");
            if (string.IsNullOrWhiteSpace(name))
                return (null, $"invalid melody file '{Path.GetFileName(file)}': missing 'name'");

            key = key.Trim();
            name = name.Trim();
            group = group.Trim();

            if (!TryGetNotes(root, out var notes, out string? notesError))
                return (null, $"invalid melody file '{Path.GetFileName(file)}': {notesError}");

            return (new Song(key, name, group, notes), null);
        }
    }

    private static string? GetString(JsonElement obj, string prop)
    {
        if (!obj.TryGetProperty(prop, out var el)) return null;
        if (el.ValueKind != JsonValueKind.String) return null;
        return el.GetString();
    }

    private static bool TryGetNotes(JsonElement root, out (int freq, int ms)[] notes, out string? error)
    {
        notes = [];
        error = null;

        if (!root.TryGetProperty("notes", out var notesEl) && !root.TryGetProperty("melody", out notesEl))
        {
            error = "missing 'notes'";
            return false;
        }

        if (notesEl.ValueKind != JsonValueKind.Array)
        {
            error = "'notes' must be an array";
            return false;
        }

        var list = new List<(int freq, int ms)>(notesEl.GetArrayLength());
        int i = 0;
        foreach (var item in notesEl.EnumerateArray())
        {
            i++;
            if (item.ValueKind == JsonValueKind.Array)
            {
                int len = item.GetArrayLength();
                if (len < 2)
                {
                    error = $"note #{i}: expected [freq, ms]";
                    return false;
                }

                if (!TryGetInt(item[0], out int freq) || !TryGetInt(item[1], out int ms))
                {
                    error = $"note #{i}: freq and ms must be integers";
                    return false;
                }

                list.Add((freq, ms));
            }
            else if (item.ValueKind == JsonValueKind.Object)
            {
                bool hasFreq = TryGetIntProp(item, "freq", out int freq) || TryGetIntProp(item, "f", out freq);
                bool hasMs = TryGetIntProp(item, "ms", out int ms) || TryGetIntProp(item, "d", out ms) || TryGetIntProp(item, "duration", out ms);
                if (!hasFreq || !hasMs)
                {
                    error = $"note #{i}: expected object with freq/ms";
                    return false;
                }
                list.Add((freq, ms));
            }
            else
            {
                error = $"note #{i}: expected [freq, ms] or object";
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

    private static bool TryGetInt(JsonElement el, out int value)
    {
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out value))
            return true;
        value = 0;
        return false;
    }

    private static bool TryGetIntProp(JsonElement obj, string prop, out int value)
    {
        value = 0;
        return obj.TryGetProperty(prop, out var el) && TryGetInt(el, out value);
    }
}
