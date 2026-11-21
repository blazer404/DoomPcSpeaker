static class App
{
    public static int Run(string[] args)
    {
        Console.CancelKeyPress += (_, e) =>
        {
            if (Playback.RequestCancel()) e.Cancel = true;
            Shutdown.Cleanup();
        };

        try
        {
            if (args.Length > 0)
                return RunDirect(args);

            return Interactive();
        }
        finally
        {
            Shutdown.Cleanup();
        }
    }

    private static int RunDirect(string[] args)
    {
        var effectiveArgs = Commands.StripPlayFlag(args);
        if (effectiveArgs.Length == 0)
        {
            Ui.Error("no tracks specified :: give me something to beep, try --list");
            return 1;
        }

        Command command = Commands.MatchArgument(effectiveArgs[0]);
        if (command == Command.Help)
        {
            Ui.PrintHelp(PcSpeaker.CheckAvailability());
            return 0;
        }

        if (command == Command.Stop)
        {
            string? availability = PcSpeaker.CheckAvailability();
            PcSpeaker.Stop();
            if (availability is null)
            {
                Ui.Success("speaker muted :: port 0x61 cleared");
                return 0;
            }

            Ui.PrintBanner(compact: true, availability);
            Console.WriteLine();
            Ui.Warn("mute requested :: best effort (speaker access unavailable)");
            Ui.Dim("  " + availability);
            return 1;
        }

        if (Melodies.LoadError is not null)
        {
            Ui.Error(Melodies.LoadError);
            return 1;
        }

        if (command == Command.List)
        {
            Ui.PrintBanner(compact: true, PcSpeaker.CheckAvailability());
            Ui.PrintTable();
            return 0;
        }

        var queries = Commands.QueriesFromArgs(effectiveArgs);
        var (songs, errors) = ResolveAll(queries);
        if (errors.Count > 0)
        {
            if (Melodies.All.Length == 0 && errors.Any(Melodies.IsRandom))
                Ui.Error("no tracks loaded — add .dpsm files to melodies/");
            else
            {
                Ui.Error("not found: " + string.Join(", ", errors.Select(e => "'" + e + "'")) + "  (nice try)");
                Ui.Dim("  use --list to see what actually exists");
            }
            return 1;
        }

        if (!EnsureSpeakerReady(showBanner: true)) return 1;

        bool aborted = Playback.PlayQueue(songs);
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

    private static int Interactive()
    {
        Terminal.ClearScreen();

        string? loadError = Melodies.LoadError;
        string? status = loadError;
        bool statusOk = loadError is null;
        string? availabilityError = PcSpeaker.CheckAvailability();
        Ui.View view = Ui.View.Main;

        while (true)
        {
            Ui.DrawScreen(view, status, statusOk, availabilityError);
            status = null;

            bool locked = availabilityError is not null;
            Ui.Prompt(locked);
            string? line = Terminal.ReadLineWithSlashSuggestions(
                locked: locked,
                redraw: () => { Ui.DrawScreen(view, status: null, statusOk: true, availabilityError); Ui.Prompt(locked); });
            if (line is null) break;

            string input = line.Trim();
            if (input.StartsWith('/'))
                input = input[1..].TrimStart();

            if (input.Length == 0)
                continue;

            Command interactiveCommand = Commands.MatchInteractive(input);
            if (interactiveCommand == Command.Quit) break;

            switch (interactiveCommand)
            {
                case Command.List:
                    view = Ui.View.List;
                    continue;
                case Command.Clear:
                    view = Ui.View.Main;
                    status = null;
                    continue;
                case Command.Help:
                    view = Ui.View.Help;
                    continue;
                case Command.Stop:
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

            if (loadError is not null)
            {
                status = loadError;
                statusOk = false;
                continue;
            }

            var queries = Commands.SmartSplit(Commands.StripPlayPrefix(input));
            var (songs, errors) = ResolveAll(queries);
            if (errors.Count > 0)
            {
                if (Melodies.All.Length == 0 && errors.Any(Melodies.IsRandom))
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

            bool aborted = Playback.PlayQueue(songs, fromView: view, availabilityError: availabilityError);
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

        Terminal.ClearScreen();
        Ui.PrintBanner(compact: true, availabilityError, showNfo: false);
        Ui.Success("session closed :: speaker muted, go touch some grass");
        Console.WriteLine();
        Terminal.Reset();
        return 0;
    }

    private static (List<Melodies.Song> Songs, List<string> Errors) ResolveAll(string[] queries)
    {
        var songs = new List<Melodies.Song>();
        var errors = new List<string>();
        foreach (string query in queries)
        {
            var song = Melodies.Resolve(query);
            if (song is null) errors.Add(query);
            else songs.Add(song);
        }
        return (songs, errors);
    }

    private static bool EnsureSpeakerReady(bool showBanner = false)
    {
        string? warning = PcSpeaker.CheckAvailability();
        if (warning is null) return true;

        if (showBanner)
        {
            Ui.PrintBanner(compact: true, warning);
            Console.WriteLine();
        }

        Ui.Error(warning);
        Ui.Dim("  playback unavailable: input/command blocked until driver and access are ready");
        if (PcSpeaker.NeedsDriverHint(warning))
            Ui.Dim("  place inpoutx64.dll next to exe + run as Administrator");
        else
            Ui.Dim("  run as Administrator and check speaker/driver availability");
        return false;
    }
}
