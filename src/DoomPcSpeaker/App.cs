static class App
{
    private static CancellationTokenSource? _playbackCts;
    private static readonly object _lock = new();

    public static int Run(string[] args)
    {
        Console.CancelKeyPress += (_, e) =>
        {
            lock (_lock)
            {
                if (_playbackCts is { IsCancellationRequested: false })
                {
                    e.Cancel = true;
                    _playbackCts.Cancel();
                    PcSpeaker.Stop();
                    Ui.Reset();
                    return;
                }
            }

            // No active playback: mute speaker, reset console state, and allow exit.
            PcSpeaker.Stop();
            Ui.Reset();
        };

        try
        {
            // Direct mode: DoomPcSpeaker.exe doom
            if (args.Length > 0)
            {
                if (args[0] == "0") return 0;
                if (IsHelp(args[0])) { Ui.PrintHelp(); return 0; }
                if (IsList(args[0])) { Ui.PrintBanner(compact: true); Ui.PrintTable(); return 0; }
                if (IsStop(args[0]))
                {
                    string? avail = PcSpeaker.CheckAvailability();
                    if (avail is not null) { Ui.Error(avail); return 1; }
                    PcSpeaker.Stop();
                    Ui.Success("speaker muted :: port 0x61 cleared");
                    return 0;
                }

                var queries = QueriesFromArgs(args);
                if (queries.Length == 0)
                {
                    Ui.Error("no tracks specified :: use --list");
                    return 1;
                }

                var (songs, errors) = ResolveAll(queries);
                if (errors.Count > 0)
                {
                    Ui.Error("not found: " + string.Join(", ", errors.Select(e => "'" + e + "'")));
                    Ui.Dim("  :: use --list for tracklist :: keys: " + string.Join(", ", Melodies.All.Select(m => m.Key)) + ", random");
                    return 1;
                }

                if (!EnsureSpeakerReady()) return 1;

                bool aborted = PlayQueue(songs, interactive: false);
                Console.WriteLine();
                if (songs.Count > 1)
                {
                    if (aborted) Ui.Warn($"aborted :: queue stopped ({songs.Count} tracks)");
                    else Ui.Success($"done :: {songs.Count} tracks played");
                }
                else
                {
                    if (aborted) Ui.Warn("aborted :: signal cut");
                    else Ui.Success("done :: playback complete");
                }
                return 0;
            }

            return Interactive();
        }
        finally
        {
            PcSpeaker.Stop();
            Ui.Reset();
        }
    }

    private static int Interactive()
    {
        Ui.ClearScreen();

        string? status = null;
        bool statusOk = true;
        string? availabilityWarn = PcSpeaker.CheckAvailability();

        while (true)
        {
            DrawMenu(status, statusOk, availabilityWarn);
            status = null;
            availabilityWarn = null;

            Ui.Prompt();
            string? line = Console.ReadLine();
            if (line is null) break;

            string input = line.Trim();
            if (input.Length == 0) continue;

            string cmd = input.ToLowerInvariant();
            if (cmd is "0" or "q" or "quit" or "exit") break;
            if (cmd is "l" or "ls" or "list") { ShowListScreen(); continue; }
            if (cmd is "cls" or "clear") { status = null; continue; }
            if (cmd is "h" or "help" or "?") { ShowHelpScreen(); continue; }
            if (cmd is "stop" or "mute") { PcSpeaker.Stop(); status = "speaker muted :: port 0x61 cleared"; statusOk = true; continue; }

            var queries = SmartSplit(input);
            var (songs, errors) = ResolveAll(queries);
            if (errors.Count > 0)
            {
                status = "not found: " + string.Join(", ", errors.Select(e => "'" + e + "'"));
                statusOk = false;
                continue;
            }
            if (songs.Count == 0)
            {
                status = "empty selection";
                statusOk = false;
                continue;
            }

            string? warn = PcSpeaker.CheckAvailability();
            if (warn is not null)
            {
                status = warn;
                statusOk = false;
                continue;
            }

            bool aborted = PlayQueue(songs, interactive: true);
            if (aborted)
            {
                status = songs.Count > 1
                    ? $"aborted :: queue stopped ({songs.Count} tracks)"
                    : $"aborted :: {songs[0].Name}";
                statusOk = false;
            }
            else
            {
                status = songs.Count > 1
                    ? $"done :: {songs.Count} tracks played"
                    : $"done :: {songs[0].Name}";
                statusOk = true;
            }
        }

        PcSpeaker.Stop();
        Ui.ClearScreen();
        Ui.PrintBanner(compact: true);
        Ui.Success("session closed :: speaker muted");
        Console.WriteLine();
        Ui.Reset();
        return 0;
    }

    private static void DrawMenu(string? status, bool statusOk, string? availabilityWarn)
    {
        Ui.ClearScreen();
        Ui.PrintBanner(compact: true);

        if (availabilityWarn is not null)
        {
            Ui.Warn(availabilityWarn);
            if (availabilityWarn.Contains("inpoutx64.dll", StringComparison.OrdinalIgnoreCase))
                Ui.Dim("  :: get InpOutx64, place inpoutx64.dll next to exe, run as Administrator");
            Console.WriteLine();
        }

        if (status is not null)
        {
            if (statusOk) Ui.Success(status); else Ui.Warn(status);
            Console.WriteLine();
        }

        Ui.Dim("  :: playlist hidden  ::  cmd: list  ::  input: id/key/name/random  ::  queue: , ; space  ::  exit: 0/q");
    }

    private static void ShowHelpScreen()
    {
        Ui.ClearScreen();
        Ui.PrintBanner(compact: true);
        Ui.WriteHelpBody();
        Console.WriteLine();
        Ui.Dim("  :: press Enter to return");
        Ui.PromptSimple();
        Console.ReadLine();
        Ui.Reset();
    }

    private static void ShowListScreen()
    {
        Ui.ClearScreen();
        Ui.PrintBanner(compact: true);
        Ui.PrintTable();
        Console.WriteLine();
        Ui.Dim("  :: press Enter to return");
        Ui.PromptSimple();
        Console.ReadLine();
        Ui.Reset();
    }

    private static bool IsRandomKeyword(string s) =>
        s.Equals("random", StringComparison.OrdinalIgnoreCase) ||
        s.Equals("rnd", StringComparison.OrdinalIgnoreCase) ||
        s.Equals("rand", StringComparison.OrdinalIgnoreCase) ||
        s.Equals("shuffle", StringComparison.OrdinalIgnoreCase);

    private static string[] SmartSplit(string input)
    {
        input = input.Trim();
        if (input.Length == 0) return [];

        if (input.IndexOfAny([',', ';']) >= 0)
            return input.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (IsRandomKeyword(input) || Melodies.Resolve(input) is not null)
            return [input];

        var parts = input.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 1 ? parts : [input];
    }

    private static string[] QueriesFromArgs(string[] args)
    {
        var effective = (args.Length > 0 && args[0] is "--play" or "-p") ? args.Skip(1).ToArray() : args;
        if (effective.Length == 0) return [];

        if (effective.Length == 1)
            return SmartSplit(effective[0]);

        if (effective.Any(a => a.IndexOfAny([',', ';']) >= 0))
            return effective
                .SelectMany(a => a.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToArray();

        // Multiple args without delimiters: each arg is a track token from the shell.
        return effective;
    }

    private static (List<Melodies.Song> Songs, List<string> Errors) ResolveAll(string[] queries)
    {
        var songs = new List<Melodies.Song>();
        var errors = new List<string>();
        foreach (var q in queries)
        {
            var s = Melodies.Resolve(q);
            if (s is null) errors.Add(q);
            else songs.Add(s);
        }
        return (songs, errors);
    }

    private static bool EnsureSpeakerReady()
    {
        string? warn = PcSpeaker.CheckAvailability();
        if (warn is null) return true;
        Ui.Error(warn);
        if (warn.Contains("inpoutx64.dll", StringComparison.OrdinalIgnoreCase))
            Ui.Dim("  :: place inpoutx64.dll next to exe + run as Administrator");
        return false;
    }

    private static bool PlayQueue(IReadOnlyList<Melodies.Song> songs, bool interactive)
    {
        var cts = new CancellationTokenSource();
        lock (_lock) _playbackCts = cts;
        try
        {
            for (int i = 0; i < songs.Count; i++)
            {
                if (cts.IsCancellationRequested) break;

                Ui.ClearScreen();
                Ui.PrintBanner(compact: true);
                Console.WriteLine();
                Ui.NowPlaying(songs[i], i + 1, songs.Count);
                Console.WriteLine();

                foreach (var (freq, ms) in songs[i].Notes)
                {
                    if (cts.IsCancellationRequested) break;
                    PcSpeaker.Play(freq, ms, cts.Token);
                }

                if (!cts.IsCancellationRequested && i < songs.Count - 1)
                    Thread.Sleep(350);
            }
            return cts.IsCancellationRequested;
        }
        finally
        {
            PcSpeaker.Stop();
            Ui.Reset();
            lock (_lock) { if (ReferenceEquals(_playbackCts, cts)) _playbackCts = null; }
            cts.Dispose();
        }
    }

    private static bool IsHelp(string a) => a is "-h" or "--help" or "/?" or "help";
    private static bool IsList(string a) => a is "-l" or "--list" or "list";
    private static bool IsStop(string a) => a is "-s" or "--stop" or "stop" or "mute";
}
