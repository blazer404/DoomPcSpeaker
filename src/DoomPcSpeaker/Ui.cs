static class Ui
{
    private const int W = 78;

    public static void Reset()
    {
        try
        {
            Console.ResetColor();
            Console.Write("\x1b[0m");
        }
        catch { }
    }

    public static void ClearScreen()
    {
        try
        {
            Console.ResetColor();
            try { Console.CursorVisible = true; } catch { }

            // Best effort: clear scrollback, screen and move cursor home.
            try { Console.Write("\x1b[H\x1b[2J\x1b[3J"); } catch { }

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    // Reduce scrollback/buffer artifacts by matching buffer to visible window.
                    if (Console.BufferWidth != Console.WindowWidth || Console.BufferHeight != Console.WindowHeight)
                        Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
                }
                catch { }

                try { Console.WindowLeft = 0; } catch { }
                try { Console.WindowTop = 0; } catch { }
            }

            try { Console.SetCursorPosition(0, 0); } catch { }
            try { Console.Clear(); } catch { }

            // Repeat clears to remove remnants in terminals that keep off-screen buffer content.
            try { Console.Write("\x1b[H\x1b[2J\x1b[3J"); } catch { }
            try { Console.SetCursorPosition(0, 0); } catch { }

            if (OperatingSystem.IsWindows())
            {
                try { Console.WindowLeft = 0; } catch { }
                try { Console.WindowTop = 0; } catch { }
            }

            try { Console.Write("\x1b[H"); } catch { }
        }
        catch { }
    }

    public static void PrintBanner(bool compact, string? availabilityError = null, bool showNfo = true)
    {
        WriteLine(ConsoleColor.DarkGray,  "  ░▒▓████████████████████████████████████████████████████████████████████▓▒░");
        Write(ConsoleColor.Black == Console.BackgroundColor ? ConsoleColor.DarkCyan : ConsoleColor.Cyan, "  ▒");
        Write(ConsoleColor.Magenta, "▓");
        Write(ConsoleColor.Red, "█");
        Write(ConsoleColor.Yellow, "  DEDSEC AUDIO CREW  ");
        Write(ConsoleColor.DarkGray, "///");
        Write(ConsoleColor.White, "  100% ILLEGAL BEEPS  ");
        Write(ConsoleColor.DarkGray, "///");
        Write(ConsoleColor.Green, "  WATCH_DOGS MODE  ");
        Write(ConsoleColor.Red, "█");
        Write(ConsoleColor.Magenta, "▓");
        WriteLine(ConsoleColor.DarkCyan, "▒");
        WriteLine(ConsoleColor.DarkGray,  "  ░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒░");
        Console.WriteLine();

        WriteLine(ConsoleColor.Cyan,      "    ██▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██");
        WriteLine(ConsoleColor.Cyan,      "    ██                                                              ██");
        WriteLine(ConsoleColor.Cyan,      "    ██   ██████╗  ██████╗███████╗██████╗ ██╗  ██╗██████╗            ██");
        WriteLine(ConsoleColor.Cyan,      "    ██   ██╔══██╗██╔════╝██╔════╝██╔══██╗██║ ██╔╝██╔══██╗           ██");
        WriteLine(ConsoleColor.Magenta,   "    ██   ██████╔╝██║     ███████╗██████╔╝█████╔╝ ██████╔╝           ██");
        WriteLine(ConsoleColor.Magenta,   "    ██   ██╔═══╝ ██║     ╚════██║██╔═══╝ ██╔═██╗ ██╔══██╗           ██");
        WriteLine(ConsoleColor.Red,       "    ██   ██║     ╚██████╗███████║██║     ██║  ██╗██║  ██║           ██");
        WriteLine(ConsoleColor.Red,       "    ██   ╚═╝      ╚═════╝╚══════╝╚═╝     ╚═╝  ╚═╝╚═╝  ╚═╝           ██");
        WriteLine(ConsoleColor.DarkGray,  "    ██                                                              ██");
        Write(ConsoleColor.DarkGray,      "    ██   ");
        Write(ConsoleColor.Yellow,        ">>> KEYGEN BEEPER JUKEBOX <<<");
        Write(ConsoleColor.DarkGray,      "  ");
        Write(ConsoleColor.Green,         "NO SOUND CARD? NO PROBLEM.");
        WriteLine(ConsoleColor.DarkGray,  "  ██");
        Write(ConsoleColor.DarkGray,      "    ██   ");
        Write(ConsoleColor.White,         "YOUR MOTHERBOARD SCREAMS ON COMMAND");
        Write(ConsoleColor.DarkGray,      "   ");
        Write(ConsoleColor.Cyan,          "RING-0 RAVE ENGINE");
        WriteLine(ConsoleColor.DarkGray,  "   ██");
        WriteLine(ConsoleColor.DarkGray,  "    ██                                                              ██");
        WriteLine(ConsoleColor.Cyan,      "    ██▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██");
        Console.WriteLine();

        Tape($"cracked · armed · loud · retro · {Melodies.All.Length:00} beeps loaded", ConsoleColor.Green, ConsoleColor.DarkGray);
        Console.WriteLine();

        if (showNfo)
        {
            NfoBox(compact, availabilityError);
            Console.WriteLine();
        }
    }

    private static void NfoBox(bool compact, string? availabilityError = null)
    {
        string title = availabilityError is null ? "ACCESS GRANTED // release.nfo" : "ACCESS DENIED // release.nfo";
        BoxTop(title, W);
        NfoRow("release", "pcspkr hardware beeper jukebox [final keygen cut]", ConsoleColor.White);
        NfoRow("scene", "dedsec audio division / crackscene crossover", ConsoleColor.Magenta);
        NfoRow("payload", $"{Melodies.All.Length:00} weaponized chiptunes for the onboard speaker", ConsoleColor.Green);
        NfoRow("target", "windows x64 · net9.0 · inpoutx64 driver", ConsoleColor.Yellow);
        BoxSep(W);
        NfoRow("engine", "i8254 pit ch2 @ 0x43/42/61 · raw square-wave chaos", ConsoleColor.Cyan);
        NfoRow("access", "administrator rights required (yes, really)", ConsoleColor.Yellow);
        NfoRow("modes", "interactive shell, direct cli, queue, random", ConsoleColor.Gray);
        if (availabilityError is null)
            NfoRow("status", "locked, loaded, waiting for input...", ConsoleColor.Green);
        else
            NfoRow("status", availabilityError, ConsoleColor.Red);
        if (!compact)
        {
            BoxSep(W);
            NfoRow("input", "id / key / name / random", ConsoleColor.White);
            NfoRow("queue", "separate tracks with ',' ';' or space", ConsoleColor.White);
            NfoRow("cmds", "list | help | cls | stop | 0/q exit", ConsoleColor.DarkGray);
            NfoRow("warn", "ctrl+c aborts playback, not your life choices", ConsoleColor.Red);
        }
        if (availabilityError is not null)
        {
            BoxSep(W);
            if (availabilityError.Contains("inpoutx64.dll", StringComparison.OrdinalIgnoreCase))
                NfoRow("fix", "place inpoutx64.dll next to exe, run as administrator", ConsoleColor.Yellow);
            else
                NfoRow("fix", "run as administrator and check speaker/driver access", ConsoleColor.Yellow);
            NfoRow("note", "playback input is disabled until this is resolved", ConsoleColor.Red);
        }
        BoxBottom(W);
    }

    public static void PrintTable()
    {
        int keyWidth = Math.Max(12, Melodies.All.Max(m => m.Key.Length));
        int nameWidth = 38;
        // row inside borders: " [00]  " + key(keyWidth) + "  ▸ " + name(nameWidth) + " "
        int inner = 12 + keyWidth + nameWidth;

        Write(ConsoleColor.DarkGray, "  ╔═");
        Write(ConsoleColor.Yellow, "█▓▒ ");
        Write(ConsoleColor.Cyan, "TRACKLIST.DATABASE");
        Write(ConsoleColor.Yellow, " ▒▓█");
        int topFill = inner - 27; // "═" + "█▓▒ " + title + " ▒▓█"
        if (topFill < 0) topFill = 0;
        WriteLine(ConsoleColor.DarkGray, new string('═', topFill) + "╗");

        int idx = 1;
        foreach (var group in Melodies.Groups)
        {
            string gText = $" >> {group.Name.ToUpperInvariant()} << ";
            Write(ConsoleColor.DarkGray, "  ╠");
            int gFill = inner - gText.Length;
            if (gFill < 0) { gText = Fit(gText, inner); gFill = 0; }
            Write(ConsoleColor.Magenta, gText);
            WriteLine(ConsoleColor.DarkGray, new string('═', gFill) + "╣");

            foreach (var m in group.Tracks)
            {
                string num = idx.ToString("00");
                string key = m.Key.ToLowerInvariant().PadRight(keyWidth);
                string name = Fit(m.Name, nameWidth);

                Write(ConsoleColor.DarkGray, "  ║ ");
                Write(ConsoleColor.DarkGray, "[");
                Write(ConsoleColor.Yellow, num);
                Write(ConsoleColor.DarkGray, "]  ");
                Write(ConsoleColor.Cyan, key);
                Write(ConsoleColor.DarkGray, "  ▸ ");
                Write(ConsoleColor.White, name);
                WriteLine(ConsoleColor.DarkGray, " ║");
                idx++;
            }
        }

        Write(ConsoleColor.DarkGray, "  ╠");
        WriteLine(ConsoleColor.DarkGray, new string('═', inner) + "╣");
        string footer = $"  {Melodies.All.Length:00} modules armed  //  pick your poison: id, key, name, random";
        Write(ConsoleColor.DarkGray, "  ║");
        Write(ConsoleColor.Green, Fit(footer, inner));
        WriteLine(ConsoleColor.DarkGray, "║");
        Write(ConsoleColor.DarkGray, "  ╚");
        WriteLine(ConsoleColor.DarkGray, new string('═', inner) + "╝");
    }

    public static void PrintHelp(string? availabilityError = null)
    {
        PrintBanner(compact: true, availabilityError);
        WriteHelpBody();
        Console.WriteLine();
    }

    public static void WriteHelpBody()
    {
        Section("usage", ConsoleColor.Cyan);
        HelpRow("DoomPcSpeaker.exe", "drop into the interactive shell");
        HelpRow("DoomPcSpeaker.exe <tracks>", "play track(s) and bail out");
        HelpRow("DoomPcSpeaker.exe --list", "show the full contraband list");
        HelpRow("DoomPcSpeaker.exe --stop", "silence the beeper if it screams");
        HelpRow("DoomPcSpeaker.exe --help", "show this screen");
        Console.WriteLine();

        Section("controls", ConsoleColor.Magenta);
        HelpInfo("target", "id / key / name fragment");
        HelpInfo("chaos", "random / rnd / rand / shuffle");
        HelpInfo("queue", "comma, semicolon or space separated");
        HelpInfo("escape", "0 / q / quit / exit");
        Console.WriteLine();

        Section("examples", ConsoleColor.Yellow);
        Example("DoomPcSpeaker.exe doom");
        Example("DoomPcSpeaker.exe 7,13,18");
        Example("DoomPcSpeaker.exe doom pirates rickroll");
        Example("DoomPcSpeaker.exe \"got, godfather, vampirekiller\"");
        Example("DoomPcSpeaker.exe random");
        Console.WriteLine();

        Write(ConsoleColor.Red, "  [!]");
        Write(ConsoleColor.White, " requires ");
        Write(ConsoleColor.Yellow, "inpoutx64.dll");
        Write(ConsoleColor.White, " next to exe + ");
        WriteLine(ConsoleColor.Yellow, "administrator rights");
    }

    public static void Prompt(bool locked = false)
    {
        Console.WriteLine();
        if (!locked)
        {
            Write(ConsoleColor.DarkGray, "  ┏━━[");
            Write(ConsoleColor.Cyan, "inject command");
            Write(ConsoleColor.DarkGray, "]━━[");
            Write(ConsoleColor.Magenta, "id/key/name/random");
            Write(ConsoleColor.DarkGray, "]━━[");
            Write(ConsoleColor.Green, "0 to bail");
            WriteLine(ConsoleColor.DarkGray, "]━━┓");
            Write(ConsoleColor.Red, "  ┗━");
            Write(ConsoleColor.Yellow, "▶ ");
        }
        else
        {
            Write(ConsoleColor.DarkGray, "  ┏━━[");
            Write(ConsoleColor.Red, "input locked");
            Write(ConsoleColor.DarkGray, "]━━[");
            Write(ConsoleColor.Magenta, "list/help/cls/stop");
            Write(ConsoleColor.DarkGray, "]━━[");
            Write(ConsoleColor.Green, "0 to bail");
            WriteLine(ConsoleColor.DarkGray, "]━━┓");
            Write(ConsoleColor.Red, "  ┗━");
            Write(ConsoleColor.DarkGray, "▶ ");
        }
        Console.ResetColor();
    }

    public static void PromptSimple(string label = "press Enter to return")
    {
        Console.WriteLine();
        Write(ConsoleColor.DarkGray, "  ┏━━[");
        Write(ConsoleColor.Yellow, label);
        WriteLine(ConsoleColor.DarkGray, "]━━┓");
        Write(ConsoleColor.Red, "  ┗━");
        Write(ConsoleColor.Yellow, "▶ ");
        Console.ResetColor();
    }

    public static void NowPlaying(Melodies.Song song, int? index = null, int? total = null)
    {
        string signal = "SIGNAL INJECTED";
        if (index.HasValue && total.HasValue && total.Value > 1)
            signal += $" // {index.Value:00}/{total.Value:00}";

        BoxTop(signal, W);
        NfoRow("track", $"{song.Name}  [{song.Key}]", ConsoleColor.White);
        NfoRow("route", "port 0x61 > i8254 ch2 > square-wave > your ears", ConsoleColor.DarkGray);
        BoxSep(W);
        NfoRow("ctrl+c", "abort mission  //  no mercy for the pc speaker", ConsoleColor.Red);
        BoxBottom(W);
    }

    public static void Success(string msg) => Tag(ConsoleColor.Green, "OK", msg, ConsoleColor.White);
    public static void Warn(string msg) => Tag(ConsoleColor.Yellow, "!!", msg, ConsoleColor.Yellow);
    public static void Error(string msg) => Tag(ConsoleColor.Red, "XX", msg, ConsoleColor.Red);

    public static void Dim(string msg)
    {
        Write(ConsoleColor.DarkGray, "     ¬ ");
        WriteLine(ConsoleColor.DarkGray, msg.TrimStart());
    }

    private static void Tag(ConsoleColor c, string tag, string msg, ConsoleColor msgColor)
    {
        Write(ConsoleColor.DarkGray, "  [");
        Write(c, tag);
        Write(ConsoleColor.DarkGray, "] ");
        WriteLine(msgColor, msg);
    }

    public static void HelpRow(string cmd, string desc)
    {
        const int col = 32;
        Write(ConsoleColor.DarkGray, "    ");
        Write(ConsoleColor.White, cmd.PadRight(col));
        Write(ConsoleColor.Magenta, "» ");
        WriteLine(ConsoleColor.Gray, desc);
    }

    public static void HelpInfo(string label, string desc)
    {
        const int col = 12;
        Write(ConsoleColor.DarkGray, "    ");
        Write(ConsoleColor.Cyan, label.PadRight(col));
        Write(ConsoleColor.DarkGray, ":: ");
        WriteLine(ConsoleColor.Gray, desc);
    }

    public static void Example(string text)
    {
        Write(ConsoleColor.DarkGray, "    ");
        Write(ConsoleColor.Green, "root@pcspkr");
        Write(ConsoleColor.DarkGray, ":");
        Write(ConsoleColor.Cyan, "~");
        Write(ConsoleColor.DarkGray, "# ");
        WriteLine(ConsoleColor.White, text);
    }

    private static void Section(string title, ConsoleColor color)
    {
        Write(ConsoleColor.DarkGray, "  ──┤ ");
        Write(color, title.ToUpperInvariant());
        WriteLine(ConsoleColor.DarkGray, " ├────────────────────────────────────────────");
    }

    private static void Tape(string text, ConsoleColor a, ConsoleColor b)
    {
        string line = $"  ══[ {text} ]";
        if (line.Length < 76) line = line.PadRight(76, '═');
        int mid = Math.Min(line.Length, 38);
        Write(a, line[..mid]);
        WriteLine(b, line[mid..]);
    }

    private static void NfoRow(string label, string value, ConsoleColor valueColor)
    {
        string l = $" {label,-10}: ";
        Write(ConsoleColor.DarkGray, "  ║");
        Write(ConsoleColor.DarkGray, l);
        int remaining = W - l.Length;
        if (remaining < 0) remaining = 0;
        Write(valueColor, Fit(value, remaining));
        WriteLine(ConsoleColor.DarkGray, "║");
    }

    private static void BoxTop(string title, int inner)
    {
        string t = $"[ {title} ]";
        Write(ConsoleColor.DarkGray, "  ╔═");
        Write(ConsoleColor.Cyan, t);
        int rest = inner - 1 - t.Length;
        if (rest < 0) rest = 0;
        Write(ConsoleColor.DarkGray, new string('═', rest));
        WriteLine(ConsoleColor.DarkGray, "╗");
    }

    private static void BoxSep(int inner)
    {
        WriteLine(ConsoleColor.DarkGray, "  ╠" + new string('═', inner) + "╣");
    }

    private static void BoxBottom(int inner)
    {
        WriteLine(ConsoleColor.DarkGray, "  ╚" + new string('═', inner) + "╝");
    }

    public static void Write(ConsoleColor color, string text)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = prev;
    }

    public static void WriteLine(ConsoleColor color, string text)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = prev;
    }

    private static void WriteLine(string text)
    {
        Console.WriteLine(text);
    }

    private static string Fit(string s, int w)
    {
        if (w <= 0) return string.Empty;
        if (s.Length <= w) return s.PadRight(w);
        if (w <= 3) return s[..w];
        return s[..(w - 3)] + "...";
    }
}
