static class App
{
    private enum View { Main, List, Help }

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
                if (IsHelp(args[0])) { Ui.PrintHelp(PcSpeaker.CheckAvailability()); return 0; }
                if (IsList(args[0])) { Ui.PrintBanner(compact: true, PcSpeaker.CheckAvailability()); Ui.PrintTable(); return 0; }
                if (IsStop(args[0]))
                {
                    string? avail = PcSpeaker.CheckAvailability();
                    PcSpeaker.Stop();
                    if (avail is null)
                    {
                        Ui.Success("speaker muted :: port 0x61 cleared");
                        return 0;
                    }

                    Ui.PrintBanner(compact: true, avail);
                    Console.WriteLine();
                    Ui.Warn("mute requested :: best effort (speaker access unavailable)");
                    Ui.Dim("  :: " + avail);
                    return 1;
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
                    Ui.Dim("  :: use --list to show available track ids, keys, and names");
                    return 1;
                }

                if (!EnsureSpeakerReady(showBanner: true)) return 1;

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
        string? availabilityError = PcSpeaker.CheckAvailability();
        View view = View.Main;

        while (true)
        {
            DrawScreen(view, status, statusOk, availabilityError);
            status = null;

            Ui.Prompt(locked: availabilityError is not null);
            string? line = Console.ReadLine();
            if (line is null) break;

            string input = line.Trim();
            if (input.Length == 0)
            {
                // Common input behavior: empty input just refreshes the current screen.
                continue;
            }

            string cmd = input.ToLowerInvariant();
            if (cmd is "0" or "q" or "quit" or "exit") break;
            if (cmd is "l" or "ls" or "list") { view = View.List; continue; }
            if (cmd is "cls" or "clear") { view = View.Main; status = null; continue; }
            if (cmd is "h" or "help" or "?") { view = View.Help; continue; }

            if (cmd is "stop" or "mute")
            {
                PcSpeaker.Stop();
                view = View.Main;
                status = availabilityError is null
                    ? "speaker muted :: port 0x61 cleared"
                    : "mute requested :: best effort (speaker access unavailable)";
                statusOk = availabilityError is null;
                continue;
            }

            if (availabilityError is not null)
            {
                availabilityError = PcSpeaker.CheckAvailability() ?? availabilityError;
                view = View.Main;
                status = "playback disabled: input rejected while speaker access is unavailable";
                statusOk = false;
                continue;
            }

            var queries = SmartSplit(input);
            var (songs, errors) = ResolveAll(queries);
            if (errors.Count > 0)
            {
                view = View.Main;
                status = "not found: " + string.Join(", ", errors.Select(e => "'" + e + "'")) + "  ::  use 'list' for track ids/keys";
                statusOk = false;
                continue;
            }
            if (songs.Count == 0)
            {
                view = View.Main;
                status = "empty selection";
                statusOk = false;
                continue;
            }

            string? warn = PcSpeaker.CheckAvailability();
            if (warn is not null)
            {
                availabilityError = warn;
                view = View.Main;
                status = null;
                statusOk = false;
                continue;
            }

            bool aborted = PlayQueue(songs, interactive: true);
            view = View.Main;
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
        Ui.PrintBanner(compact: true, availabilityError);
        Ui.Success("session closed :: speaker muted");
        Console.WriteLine();
        Ui.Reset();
        return 0;
    }

    private static void DrawScreen(View view, string? status, bool statusOk, string? availabilityError)
    {
        Ui.ClearScreen();

        switch (view)
        {
            case View.List:
                Ui.PrintBanner(compact: true, availabilityError, showNfo: false);
                Ui.PrintTable();
                break;
            case View.Help:
                Ui.PrintBanner(compact: true, availabilityError, showNfo: false);
                Ui.WriteHelpBody();
                break;
            default:
                Ui.PrintBanner(compact: true, availabilityError);
                // headers/info
                if (availabilityError is null)
                {
                    Ui.Dim("  playlist :: hidden (use 'list')");
                    Ui.Dim("  cmd      :: list, help, cls, stop");
                    Ui.Dim("  input    :: id / key / name / random");
                    Ui.Dim("  queue    :: separate with ',' ';' or space");
                    Ui.Dim("  exit     :: 0 / q");
                }
                else
                {
                    Ui.Dim("  mode     :: playback disabled");
                    Ui.Dim("  allowed  :: list, help, cls, stop, 0/q");
                    Ui.Dim("  fix      :: restore driver/access and restart");
                }
                Console.WriteLine();

                // status/messages after headers
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (statusOk) Ui.Success(status); else Ui.Warn(status);
                    Console.WriteLine();
                }
                break;
        }
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

        // Multiple args without delimiters: treat as separate tokens, but allow
        // a multi-word alias/name passed as separate shell args (e.g. "prince igor").
        string joined = string.Join(" ", effective).Trim();
        if (joined.Length > 0 && Melodies.Resolve(joined) is not null)
        {
            bool allResolveIndividually = effective.All(a => Melodies.Resolve(a) is not null);
            if (!allResolveIndividually)
                return [joined];
        }

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

    private static bool EnsureSpeakerReady(bool showBanner = false)
    {
        string? warn = PcSpeaker.CheckAvailability();
        if (warn is null) return true;

        if (showBanner)
        {
            Ui.PrintBanner(compact: true, warn);
            Console.WriteLine();
        }

        Ui.Error(warn);
        Ui.Dim("  :: playback unavailable: input/command blocked until driver and access are ready");
        if (warn.Contains("inpoutx64.dll", StringComparison.OrdinalIgnoreCase))
            Ui.Dim("  :: place inpoutx64.dll next to exe + run as Administrator");
        else
            Ui.Dim("  :: run as Administrator and check speaker/driver availability");
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
