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
                var effectiveArgs = StripPlayArgPrefix(args);
                if (effectiveArgs.Length == 0)
                {
                    Ui.Error("no tracks specified :: give me something to beep, try --list");
                    return 1;
                }

                if (IsHelpArg(effectiveArgs[0])) { Ui.PrintHelp(PcSpeaker.CheckAvailability()); return 0; }
                if (IsStopArg(effectiveArgs[0]))
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
                    Ui.Dim("  " + avail);
                    return 1;
                }
                if (Melodies.LoadError is not null) { Ui.Error(Melodies.LoadError); return 1; }
                if (IsListArg(effectiveArgs[0])) { Ui.PrintBanner(compact: true, PcSpeaker.CheckAvailability()); Ui.PrintTable(); return 0; }

                var queries = QueriesFromArgs(effectiveArgs);

                var (songs, errors) = ResolveAll(queries);
                if (errors.Count > 0)
                {
                    if (Melodies.All.Length == 0 && errors.Any(e => IsRandomKeyword(e)))
                        Ui.Error("no tracks loaded — add .dpsm files to melodies/");
                    else
                    {
                        Ui.Error("not found: " + string.Join(", ", errors.Select(e => "'" + e + "'")) + "  (nice try)");
                        Ui.Dim("  use --list to see what actually exists");
                    }
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
                    if (aborted) Ui.Warn("aborted :: playback stopped");
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

        string? status = Melodies.LoadError;
        bool statusOk = Melodies.LoadError is null;
        string? availabilityError = PcSpeaker.CheckAvailability();
        string? melodyError = Melodies.LoadError;
        View view = View.Main;

        while (true)
        {
            DrawScreen(view, status, statusOk, availabilityError);
            status = null;

            bool locked = availabilityError is not null;
            Ui.Prompt(locked: locked);
            string? line = Ui.ReadLineWithSlashSuggestions(
                locked: locked,
                redraw: () => { DrawScreen(view, status: null, statusOk: true, availabilityError); Ui.Prompt(locked: locked); });
            if (line is null) break;

            string input = line.Trim();
            if (input.StartsWith('/'))
                input = input[1..].TrimStart();

            if (input.Length == 0)
            {
                // Common input behavior: empty input just refreshes the current screen.
                continue;
            }

            if (IsExitCmd(input)) break;
            if (IsListCmd(input)) { view = View.List; continue; }
            if (IsClearCmd(input)) { view = View.Main; status = null; continue; }
            if (IsHelpCmd(input)) { view = View.Help; continue; }

            if (IsStopCmd(input))
            {
                PcSpeaker.Stop();
                status = availabilityError is null
                    ? "speaker muted :: port 0x61 cleared, sweet silence"
                    : "mute requested :: best effort (speaker access unavailable)";
                statusOk = availabilityError is null;
                continue;
            }

            if (availabilityError is not null)
            {
                availabilityError = PcSpeaker.CheckAvailability() ?? availabilityError;
                status = "playback disabled: speaker access unavailable, no beep for you yet";
                statusOk = false;
                continue;
            }

            if (melodyError is not null)
            {
                status = melodyError;
                statusOk = false;
                continue;
            }

            var playInput = StripPlayPrefix(input);
            var queries = SmartSplit(playInput);
            var (songs, errors) = ResolveAll(queries);
            if (errors.Count > 0)
            {
                if (Melodies.All.Length == 0 && errors.Any(e => IsRandomKeyword(e)))
                    status = "no tracks loaded — add .dpsm files to melodies/";
                else
                    status = "not found: " + string.Join(", ", errors.Select(e => "'" + e + "'")) + "  ::  nice try, use 'help'";
                statusOk = false;
                continue;
            }
            if (songs.Count == 0)
            {
                status = "empty selection :: you gotta pick something, buddy";
                statusOk = false;
                continue;
            }

            string? warn = PcSpeaker.CheckAvailability();
            if (warn is not null)
            {
                availabilityError = warn;
                status = warn;
                statusOk = false;
                continue;
            }

            bool aborted = PlayQueue(songs, interactive: true, fromView: view, availabilityError: availabilityError);
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
        Ui.PrintBanner(compact: true, availabilityError, showNfo: false);
        Ui.Success("session closed :: speaker muted, go touch some grass");
        Console.WriteLine();
        Ui.Reset();
        return 0;
    }

    private static void DrawScreen(View view, string? status, bool statusOk, string? availabilityError, bool playing = false)
    {
        Ui.ClearScreen();

        switch (view)
        {
            case View.List:
                Ui.PrintBanner(compact: true, availabilityError, showNfo: false, playing: playing);
                Ui.PrintTable();
                Console.WriteLine();
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (statusOk) Ui.Success(status); else Ui.Warn(status);
                    Console.WriteLine();
                }
                break;
            case View.Help:
                Ui.PrintBanner(compact: true, availabilityError, showNfo: false, playing: playing);
                Ui.WriteHelpBody();
                Console.WriteLine();
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (statusOk) Ui.Success(status); else Ui.Warn(status);
                    Console.WriteLine();
                }
                break;
            default:
                Ui.PrintBanner(compact: true, availabilityError, playing: playing);
                if (availabilityError is null)
                {
                    Ui.Dim("  [SYS] type 'help' for commands and usage");
                }
                else
                {
                    Ui.Dim("  [SYS] mode   :: playback disabled / read-only, look but no beep");
                    Ui.Dim("  [SYS] allow  :: list  help  clear  stop  quit");
                    Ui.Dim("  [SYS] action :: fix driver/access and restart (you got this)");
                }
                Console.WriteLine();

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
        s.Equals("r", StringComparison.OrdinalIgnoreCase);

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

    private static string StripPlayPrefix(string input)
    {
        var parts = input.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length > 0 && (parts[0].Equals("--play", StringComparison.OrdinalIgnoreCase) || parts[0].Equals("-p", StringComparison.OrdinalIgnoreCase)))
            return parts.Length > 1 ? parts[1] : string.Empty;
        return input;
    }

    private static string[] StripPlayArgPrefix(string[] args)
    {
        if (args.Length > 0 && (args[0].Equals("--play", StringComparison.OrdinalIgnoreCase) || args[0].Equals("-p", StringComparison.OrdinalIgnoreCase)))
            return args.Skip(1).ToArray();
        return args;
    }

    private static string[] QueriesFromArgs(string[] args)
    {
        var effective = (args.Length > 0 && (args[0].Equals("--play", StringComparison.OrdinalIgnoreCase) || args[0].Equals("-p", StringComparison.OrdinalIgnoreCase)))
            ? args.Skip(1).ToArray()
            : args;
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
        Ui.Dim("  playback unavailable: input/command blocked until driver and access are ready");
        if (warn.Contains("inpoutx64.dll", StringComparison.OrdinalIgnoreCase))
            Ui.Dim("  place inpoutx64.dll next to exe + run as Administrator");
        else
            Ui.Dim("  run as Administrator and check speaker/driver availability");
        return false;
    }

    private static bool PlayQueue(IReadOnlyList<Melodies.Song> songs, bool interactive, View fromView = View.Main, string? availabilityError = null)
    {
        var cts = new CancellationTokenSource();
        lock (_lock) _playbackCts = cts;
        try
        {
            for (int i = 0; i < songs.Count; i++)
            {
                if (cts.IsCancellationRequested) break;

                // Keep the originating screen and append playback block.
                DrawScreen(fromView, status: null, statusOk: true, availabilityError: availabilityError, playing: true);
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

    private static bool IsHelpArg(string a)
    {
        string c = a.ToLowerInvariant();
        return c is "-h" or "--help";
    }

    private static bool IsHelpCmd(string a)
    {
        string c = a.ToLowerInvariant();
        return c is "help" or "h";
    }

    private static bool IsListArg(string a)
    {
        string c = a.ToLowerInvariant();
        return c is "-l" or "--list";
    }

    private static bool IsListCmd(string a)
    {
        string c = a.ToLowerInvariant();
        return c is "list" or "l";
    }

    private static bool IsStopArg(string a)
    {
        string c = a.ToLowerInvariant();
        return c is "-s" or "--stop";
    }

    private static bool IsStopCmd(string a)
    {
        string c = a.ToLowerInvariant();
        return c is "stop" or "s";
    }

    private static bool IsExitCmd(string a)
    {
        string c = a.ToLowerInvariant();
        return c is "quit" or "q";
    }

    private static bool IsClearCmd(string a)
    {
        string c = a.ToLowerInvariant();
        return c is "clear" or "c";
    }
}
