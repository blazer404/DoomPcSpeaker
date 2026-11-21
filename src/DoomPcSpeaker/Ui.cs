using System.Runtime.InteropServices;

static class Ui
{
    private const string Prefix = "  ";
    private const int PanelWidth = 74; // total width of framed panels including border chars
    private const int PanelInner = PanelWidth - 2; // content between left/right border chars

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

    public static void PrintBanner(bool compact, string? availabilityError = null, bool showNfo = true, bool playing = false)
    {
        bool online = availabilityError is null;

        string[] logo =
        [
            "▓▓▓▓▓▓╗ ░▓▓▓▓▓▓╗▓▓▓▓▓▓▓╗▓▓▓▓▓▓╗░▓▓╗ ░▓▓╗▓▓▓▓▓▓╗░",
            "▓▓╔══▓▓╗▓▓╔════╝▓▓╔════╝▓▓╔══▓▓╗▓▓║░▓▓╔╝▓▓╔══▓▓╗",
            "▓▓▓▓▓▓╔╝▓▓║░░░░░▓▓▓▓▓▓▓╗▓▓▓▓▓▓╔╝▓▓▓▓▓╔╝░▓▓▓▓▓▓╔╝",
            "▓▓╔═══╝░▓▓║░░░░░╚════▓▓║▓▓╔═══╝░▓▓╔═▓▓╗░▓▓╔══▓▓╗",
            "▓▓║░░░░░╚▓▓▓▓▓▓╗▓▓▓▓▓▓▓║▓▓║░░░░░▓▓║ ░▓▓╗▓▓║░░▓▓║",
            "╚═╝░░░░░░╚═════╝╚══════╝╚═╝░░░░░╚═╝  ╚═╝╚═╝░░╚═╝",
        ];
        ConsoleColor[] logoColors =
        [
            ConsoleColor.Cyan,
            ConsoleColor.DarkCyan,
            ConsoleColor.Magenta,
            ConsoleColor.DarkMagenta,
            ConsoleColor.Red,
            ConsoleColor.DarkRed,
        ];

        RoundedTop("PCSPKR  //  BEEPER CORE  //  HARDWARE INTERFACE", ConsoleColor.Yellow);
        RoundedBody(string.Empty, ConsoleColor.DarkGray);
        for (int i = 0; i < logo.Length; i++)
            RoundedBody("  " + logo[i], logoColors[i]);
        RoundedBody(string.Empty, ConsoleColor.DarkGray);
        RoundedMid("HUD", ConsoleColor.Magenta);
        RoundedRow("SYS", online ? "ARMED" : "LOCKED", online ? ConsoleColor.Green : ConsoleColor.Red,
                   "LINK", online ? "ONLINE" : "DENIED", online ? ConsoleColor.Green : ConsoleColor.Red);
        RoundedRow("DRV", online ? "INPOUTX64/READY" : "INPOUTX64/CHECK", online ? ConsoleColor.Cyan : ConsoleColor.Yellow,
                   "CHAN", "I8254 PIT CH2", ConsoleColor.Magenta);
        RoundedRow("MODE", online ? "INTERACTIVE/CLI" : "READ-ONLY", online ? ConsoleColor.White : ConsoleColor.Yellow,
                   "PORT", "0x43/0x42/0x61", ConsoleColor.Cyan);
        RoundedBottom();

        Console.WriteLine();
        Tape($"status={ (online ? "ready" : "degraded") }  //  tracks={Melodies.All.Length:00} loaded  //  mode=console", ConsoleColor.Green, ConsoleColor.Magenta);

        if (showNfo)
        {
            Console.WriteLine();
            NfoBox(compact, availabilityError, playing);
        }
        Console.WriteLine();
    }

    private static void NfoBox(bool compact, string? availabilityError = null, bool playing = false)
    {
        bool online = availabilityError is null;
        string title = online ? "DEVICE INFO" : "DEVICE INFO // ACCESS DENIED";

        DBoxTop(title, ConsoleColor.Cyan);
        DBoxRow("app", "PC speaker hardware jukebox (yes, that beep thing)", ConsoleColor.White);
        DBoxRow("type", "Standalone hardware audio player / no soundcard, no mercy", ConsoleColor.Magenta);
        DBoxRow("tracks", $"{Melodies.All.Length:00} chiptune tracks ready to annoy everyone nearby", ConsoleColor.Green);
        DBoxRow("target", "windows x64 · net9.0 · inpoutx64 driver", ConsoleColor.Yellow);
        DBoxSep();
        DBoxRow("engine", "i8254 PIT channel 2 @ 0x43/0x42/0x61, square-wave", ConsoleColor.Cyan);
        DBoxRow("access", "administrator rights required for port I/O", ConsoleColor.Yellow);
        DBoxRow("modes", "interactive shell, direct CLI, queue, random chaos", ConsoleColor.Gray);
        if (online)
        {
            if (playing)
                DBoxRow("status", "PLAYING :: playback in progress", ConsoleColor.Green);
            else
                DBoxRow("status", "ONLINE :: ready for input, don't be shy", ConsoleColor.Green);
        }
        else
            DBoxRow("status", "ERROR :: " + availabilityError, ConsoleColor.Red);

        if (!compact)
        {
            DBoxSep();
            DBoxRow("input", "id / key (type like you mean it)", ConsoleColor.White);
            DBoxRow("queue", "separate tracks with ',' ';' or space (go wild)", ConsoleColor.White);
            DBoxRow("cmds", "list, help, clear, stop, quit, random", ConsoleColor.DarkGray);
            DBoxRow("abort", "Ctrl+C stops playback/queue (panic button, session lives)", ConsoleColor.Red);
        }

        if (!online)
        {
            DBoxSep();
            if (availabilityError!.Contains("inpoutx64.dll", StringComparison.OrdinalIgnoreCase))
                DBoxRow("fix", "drop inpoutx64.dll next to exe, run as admin (easy mode)", ConsoleColor.Yellow);
            else
                DBoxRow("fix", "run as admin and check speaker/driver (yes, again)", ConsoleColor.Yellow);
            DBoxRow("note", "playback locked until you fix it, no beep for you", ConsoleColor.Red);
        }
        DBoxBottom();
    }

    public static void PrintTable()
    {
        int keyWidth = Math.Max(12, Melodies.All.Max(m => m.Key.Length));
        int inner = PanelInner;
        // inner layout: " [00]  " + key + "  ▸ " + name + " " => 12 + keyWidth + nameWidth
        int nameWidth = inner - 12 - keyWidth;
        if (nameWidth < 10) nameWidth = 10;

        var s = HeavyFrame;
        DBoxTopCustom("TRACKLIST", inner, ConsoleColor.Cyan, s);

        int idx = 1;
        foreach (var group in Melodies.Groups)
        {
            string g = $" ▓▒░ {group.Name.ToUpperInvariant()} ░▒▓ ";
            DBoxSepText(g, inner, ConsoleColor.Magenta, s);

            foreach (var m in group.Tracks)
            {
                string num = idx.ToString("00");
                string key = Fit(m.Key.ToLowerInvariant(), keyWidth).PadRight(keyWidth);
                string name = Fit(m.Name, nameWidth).PadRight(nameWidth);

                Write(s.BorderColor, Prefix + s.V + " ");
                Write(ConsoleColor.Gray, "[");
                Write(ConsoleColor.Yellow, num);
                Write(ConsoleColor.Gray, "]  ");
                Write(ConsoleColor.Cyan, key);
                Write(ConsoleColor.Gray, "  ▸ ");
                Write(ConsoleColor.White, name);
                WriteLine(s.BorderColor, " " + s.V);
                idx++;
            }
        }

        DBoxSepCustom(inner, s);
        string footer = Fit($"  {Melodies.All.Length:00} tracks loaded  //  pick: id | key | random", inner).PadRight(inner);
        Write(s.BorderColor, Prefix + s.V);
        Write(ConsoleColor.Green, footer);
        WriteLine(s.BorderColor, s.V);
        DBoxBottomCustom(inner, s);
    }

    public static void PrintHelp(string? availabilityError = null)
    {
        PrintBanner(compact: true, availabilityError);
        WriteHelpBody();
        Console.WriteLine();
    }

    public static void WriteHelpBody()
    {
        FramedPanel(HeavyFrame, "USAGE", ConsoleColor.Cyan, writer =>
        {
            writer.Row("DoomPcSpeaker.exe", "launch interactive shell");
            writer.Row("DoomPcSpeaker.exe <tracks>", "play track(s) directly and exit");
            writer.Row("DoomPcSpeaker.exe -l, --list", "print full track database");
            writer.Row("DoomPcSpeaker.exe -s, --stop", "mute speaker / clear port state");
            writer.Row("DoomPcSpeaker.exe -h, --help", "show this help");
        });
        Console.WriteLine();

        FramedPanel(DoubleFrame, "CONTROLS", ConsoleColor.Magenta, writer =>
        {
            writer.Section("ACTIONS", ConsoleColor.DarkYellow);
            writer.Kv("play", "enter track number or key");
            writer.Kv("queue", "separate tracks with comma, semicolon, or space");
            writer.Kv("Ctrl+C", "stop playback/queue; if idle, exit the app");
            writer.Section("COMMANDS", ConsoleColor.DarkCyan);
            writer.Kv("random", "play a random track (aliases: random, r)");
            writer.Kv("list", "show tracklist (aliases: list, l)");
            writer.Kv("help", "show this help (aliases: help, h)");
            writer.Kv("clear", "clear screen (aliases: clear, c)");
            writer.Kv("stop", "silence speaker (aliases: stop, s)");
            writer.Kv("quit", "quit the app (aliases: quit, q)");
        });
        Console.WriteLine();

        FramedPanel(SingleFrame, "EXAMPLES", ConsoleColor.Yellow, writer =>
        {
            writer.Cmd("DoomPcSpeaker.exe");
            writer.Cmd("DoomPcSpeaker.exe doom");
            writer.Cmd("DoomPcSpeaker.exe 7,13,18");
            writer.Cmd("DoomPcSpeaker.exe doom pirates rickroll");
            writer.Cmd("DoomPcSpeaker.exe \"got, father, castlevania\"");
            writer.Cmd("DoomPcSpeaker.exe random");
            writer.Tip("copypaste like a pro");
        });
        Console.WriteLine();

        FramedPanel(MixedFrame, "REQUIREMENTS", ConsoleColor.Red, writer =>
        {
            writer.Alert("driver", "inpoutx64.dll must be next to executable", ConsoleColor.Yellow);
            writer.Alert("access", "Administrator rights required for port I/O", ConsoleColor.Yellow);
        });
    }

    public static void Prompt(bool locked = false)
    {
        Console.WriteLine();
        if (!locked)
        {
            Write(ConsoleColor.Gray, Prefix + "╭─[");
            Write(ConsoleColor.Cyan, "PCSPKR");
            Write(ConsoleColor.Gray, "]─[");
            Write(ConsoleColor.Green, "INPUT:READY");
            Write(ConsoleColor.Gray, "]─[");
            Write(ConsoleColor.Magenta, "id/key/random/queue");
            WriteLine(ConsoleColor.Gray, "]");

            Write(ConsoleColor.Gray, Prefix + "╰─");
            Write(ConsoleColor.Green, "▸ select");
            Write(ConsoleColor.Gray, " > ");
        }
        else
        {
            Write(ConsoleColor.Gray, Prefix + "╭─[");
            Write(ConsoleColor.Red, "PCSPKR");
            Write(ConsoleColor.Gray, "]─[");
            Write(ConsoleColor.Red, "INPUT:LOCKED");
            Write(ConsoleColor.Gray, "]─[");
            Write(ConsoleColor.Yellow, "list/help/clear/stop/quit");
            WriteLine(ConsoleColor.Gray, "]");

            Write(ConsoleColor.Gray, Prefix + "╰─");
            Write(ConsoleColor.Red, "▸ locked");
            Write(ConsoleColor.Gray, " > ");
        }
        Console.ResetColor();
    }

    public static void PromptSimple(string label = "press Enter to return")
    {
        Console.WriteLine();
        Write(ConsoleColor.Gray, Prefix + "╭─[");
        Write(ConsoleColor.Yellow, "AWAIT");
        Write(ConsoleColor.Gray, "]─[");
        Write(ConsoleColor.White, Fit(label, 54));
        WriteLine(ConsoleColor.Gray, "]");
        Write(ConsoleColor.Gray, Prefix + "╰─");
        Write(ConsoleColor.Yellow, "input");
        Write(ConsoleColor.Gray, " > ");
        Console.ResetColor();
    }

    public static string? ReadLineWithSlashSuggestions(bool locked = false, Action? redraw = null)
    {
        string[] commands = locked
            ? ["list", "stop", "clear", "help", "quit"]
            : ["list", "random", "stop", "clear", "help", "quit"];

        int inputLeft = Console.CursorLeft;
        int inputTop = Console.CursorTop;
        var buf = new System.Text.StringBuilder();

        int popupHeight = 0;
        int popupTop = 0;
        int popupLeft = 0;
        int popupWidth = 0;
        CHAR_INFO[]? savedBuf = null;
        int savedW = 0;
        int savedH = 0;
        int selectedIndex = 0;

        string[] GetMatches()
        {
            string text = buf.ToString();
            if (!text.StartsWith('/')) return [];
            string q = text[1..];
            return commands
                .Where(c => c.StartsWith(q, StringComparison.OrdinalIgnoreCase))
                .Select(c => "/" + c)
                .ToArray();
        }

        void RestorePopup()
        {
            if (popupHeight <= 0) return;
            int oldH = popupHeight;
            int oldTop = popupTop;
            int oldLeft = popupLeft;
            int oldW = popupWidth;
            if (savedBuf != null)
            {
                RestoreBufferRegion(popupLeft, popupTop, savedW, savedH, savedBuf);
                savedBuf = null;
                popupHeight = 0;
                return;
            }

            savedBuf = null;
            popupHeight = 0;
            if (redraw != null)
            {
                redraw();
                inputLeft = Console.CursorLeft;
                inputTop = Console.CursorTop;
                string text = buf.ToString();
                Console.Write(text);
                int tail = Math.Max(0, Console.WindowWidth - 1 - (inputLeft + text.Length));
                if (tail > 0) Console.Write(new string(' ', tail));
                Console.SetCursorPosition(inputLeft + text.Length, inputTop);
                return;
            }

            int width = Math.Max(0, Math.Min(oldW, Console.BufferWidth - oldLeft));
            for (int i = 0; i < oldH; i++)
            {
                int row = oldTop + i;
                if (row < 0 || row >= Console.BufferHeight) continue;
                Console.SetCursorPosition(oldLeft, row);
                Console.Write(new string(' ', width));
            }
        }

        void SavePopupArea(int left, int top, int width, int height)
        {
            savedBuf = SaveBufferRegion(left, top, width, height);
            savedW = width;
            savedH = height;
        }

        void Render(bool redrawInput = true)
        {
            string text = buf.ToString();
            var matches = GetMatches();
            if (matches.Length == 0) selectedIndex = 0;
            else if (selectedIndex >= matches.Length) selectedIndex = matches.Length - 1;
            else if (selectedIndex < 0) selectedIndex = 0;

            Console.CursorVisible = false;
            if (redrawInput)
            {
                Console.SetCursorPosition(inputLeft, inputTop);
                Console.Write(text);
                int tail = Math.Max(0, Console.WindowWidth - 1 - (inputLeft + text.Length));
                if (tail > 0) Console.Write(new string(' ', tail));
            }

            if (matches.Length == 0)
            {
                if (popupHeight > 0) RestorePopup();
                Console.SetCursorPosition(inputLeft + text.Length, inputTop);
                Console.ResetColor();
                Console.CursorVisible = true;
                return;
            }

            int inner = matches.Max(m => m.Length) + 4; // "▸ /cmd "
            inner = Math.Max(inner, " commands ".Length + 1);
            int left = Math.Min(inputLeft, Math.Max(0, Console.WindowWidth - 4));
            int maxBoxWidth = Math.Max(14, Console.WindowWidth - left - 1);
            int boxWidth = Math.Min(inner + 2, maxBoxWidth);
            inner = boxWidth - 2;

            int spaceAbove = inputTop;
            int spaceBelow = Math.Max(0, Console.BufferHeight - inputTop - 1);
            int needed = matches.Length + 2;
            bool openUp = spaceAbove >= needed || spaceAbove >= spaceBelow;
            int maxRows = openUp ? spaceAbove : spaceBelow;
            int drawCount = Math.Min(matches.Length, Math.Max(0, maxRows - 2));
            if (drawCount <= 0)
            {
                if (popupHeight > 0) RestorePopup();
                Console.SetCursorPosition(inputLeft + text.Length, inputTop);
                Console.ResetColor();
                Console.CursorVisible = true;
                return;
            }

            int totalHeight = drawCount + 2;
            int newTop = openUp ? inputTop - totalHeight : inputTop + 1;
            bool sameGeometry = popupHeight == totalHeight && popupTop == newTop && popupLeft == left && popupWidth == boxWidth;
            if (popupHeight > 0 && !sameGeometry)
                RestorePopup();

            if (popupHeight == 0)
            {
                popupTop = newTop;
                popupLeft = left;
                popupWidth = boxWidth;
                popupHeight = totalHeight;
                SavePopupArea(popupLeft, popupTop, popupWidth, popupHeight);
            }

            int row = popupTop;
            Console.SetCursorPosition(left, row++);
            Write(ConsoleColor.Magenta, "╭─");
            Write(ConsoleColor.Cyan, " commands ");
            Write(ConsoleColor.Magenta, new string('─', Math.Max(0, inner - " commands ".Length - 1)) + "╮");

            for (int i = 0; i < drawCount; i++)
            {
                bool active = i == selectedIndex;
                Console.SetCursorPosition(left, row++);
                Write(ConsoleColor.Magenta, "│");
                if (active)
                {
                    Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    Write(ConsoleColor.White, " ▸ ");
                    Write(ConsoleColor.Yellow, Fit(matches[i], inner - 3).PadRight(inner - 3));
                    Console.ResetColor();
                }
                else
                {
                    Write(ConsoleColor.DarkGray, "   ");
                    Write(ConsoleColor.Gray, Fit(matches[i], inner - 3).PadRight(inner - 3));
                }
                Write(ConsoleColor.Magenta, "│");
            }

            Console.SetCursorPosition(left, row++);
            Write(ConsoleColor.Magenta, "╰" + new string('─', inner) + "╯");

            Console.SetCursorPosition(inputLeft + text.Length, inputTop);
            Console.ResetColor();
            Console.CursorVisible = true;
        }

        Render();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                var matches = GetMatches();
                if (matches.Length > 0)
                    buf.Clear().Append(matches[Math.Clamp(selectedIndex, 0, matches.Length - 1)]);

                string text = buf.ToString();
                if (popupHeight > 0) RestorePopup();
                Console.SetCursorPosition(inputLeft, inputTop);
                Console.Write(text);
                int tail = Math.Max(0, Console.WindowWidth - 1 - (inputLeft + text.Length));
                if (tail > 0) Console.Write(new string(' ', tail));
                Console.SetCursorPosition(inputLeft + text.Length, inputTop);
                Console.WriteLine();
                Console.ResetColor();
                return text;
            }

            if (key.Key == ConsoleKey.UpArrow)
            {
                var matches = GetMatches();
                if (matches.Length > 0) { selectedIndex = (selectedIndex - 1 + matches.Length) % matches.Length; Render(redrawInput: false); }
                continue;
            }

            if (key.Key == ConsoleKey.DownArrow)
            {
                var matches = GetMatches();
                if (matches.Length > 0) { selectedIndex = (selectedIndex + 1) % matches.Length; Render(redrawInput: false); }
                continue;
            }

            if (key.Key == ConsoleKey.Tab)
            {
                var matches = GetMatches();
                if (matches.Length > 0)
                {
                    buf.Clear().Append(matches[Math.Clamp(selectedIndex, 0, matches.Length - 1)]);
                    selectedIndex = 0;
                    Render();
                }
                continue;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buf.Length > 0) { buf.Length--; selectedIndex = 0; Render(); }
                continue;
            }

            if (key.Key == ConsoleKey.Escape)
            {
                if (buf.Length > 0) { buf.Clear(); selectedIndex = 0; Render(); }
                continue;
            }

            char ch = key.KeyChar;
            if (!char.IsControl(ch))
            {
                buf.Append(ch);
                selectedIndex = 0;
                Render();
            }
        }
    }

    public static void NowPlaying(Melodies.Song song, int? index = null, int? total = null)
    {
        string title = "NOW PLAYING";
        if (index.HasValue && total.HasValue && total.Value > 1)
            title += $"  [{index.Value:00}/{total.Value:00}]";

        var s = SingleDVFrame;
        DBoxTop(title, ConsoleColor.Cyan, s);
        DBoxRow("track", $"{song.Name}  [{song.Key}]", ConsoleColor.White, s);
        DBoxRow("route", "port 0x61 > i8254 PIT CH2 > square-wave output", ConsoleColor.Cyan, s);
        DBoxRow("state", "RUNNING :: screaming through the beeper, enjoy", ConsoleColor.Green, s);
        DBoxSep(s);
        DBoxRow("ctrl+c", "abort playback/queue (signal cut, session alive)", ConsoleColor.Red, s);
        DBoxBottom(s);
    }

    public static void Success(string msg) => Tag("OK", ConsoleColor.Green, msg, ConsoleColor.White);
    public static void Warn(string msg) => Tag("WARN", ConsoleColor.Yellow, msg, ConsoleColor.Yellow);
    public static void Error(string msg) => Tag("ERR", ConsoleColor.Red, msg, ConsoleColor.Red);

    public static void Dim(string msg)
    {
        Write(ConsoleColor.DarkGray, "     :: ");
        WriteLine(ConsoleColor.DarkGray, msg.TrimStart());
    }

    private static void Tag(string tag, ConsoleColor tagColor, string msg, ConsoleColor msgColor)
    {
        Write(ConsoleColor.DarkGray, Prefix + "[");
        Write(tagColor, tag);
        Write(ConsoleColor.DarkGray, "] ");
        WriteLine(msgColor, msg);
    }

    // --- Legacy-compatible helpers kept for internal/external callers ---
    public static void HelpRow(string cmd, string desc)
    {
        const int col = 32;
        Write(ConsoleColor.DarkGray, "    ");
        Write(ConsoleColor.Cyan, Fit(cmd, col).PadRight(col));
        Write(ConsoleColor.DarkGray, " :: ");
        WriteLine(ConsoleColor.Gray, desc);
    }

    public static void HelpInfo(string label, string desc)
    {
        const int col = 12;
        Write(ConsoleColor.DarkGray, "    ");
        Write(ConsoleColor.Green, label.PadRight(col));
        Write(ConsoleColor.DarkGray, " :: ");
        WriteLine(ConsoleColor.Gray, desc);
    }

    public static void Example(string text)
    {
        Write(ConsoleColor.DarkGray, "    ");
        Write(ConsoleColor.Green, "node@pcspkr");
        Write(ConsoleColor.DarkGray, ":");
        Write(ConsoleColor.Cyan, "~");
        Write(ConsoleColor.DarkGray, "$ ");
        WriteLine(ConsoleColor.White, text);
    }

    // --- Frame styles ---
    private readonly record struct FrameStyle(
        string TL, string TR, string BL, string BR,
        string H, string V,
        string SL, string SR, string SF,
        string TitleLeft, string TitleRight,
        ConsoleColor BorderColor)
    {
    }

    private static readonly FrameStyle RoundedFrame = new("╭", "╮", "╰", "╯", "─", "│", "├", "┤", "─", "░▒ ", " ▒░", ConsoleColor.Gray);
    private static readonly FrameStyle HeavyFrame   = new("┏", "┓", "┗", "┛", "━", "┃", "┣", "┫", "━", "◈ ", " ◈", ConsoleColor.DarkCyan);
    private static readonly FrameStyle SingleFrame  = new("┌", "┐", "└", "┘", "─", "│", "├", "┤", "─", "▸ ", " ◂", ConsoleColor.DarkGray);
    private static readonly FrameStyle DoubleFrame  = new("╔", "╗", "╚", "╝", "═", "║", "╠", "╣", "═", "◢ ", " ◣", ConsoleColor.Gray);
    private static readonly FrameStyle MixedFrame   = new("╒", "╕", "╘", "╛", "═", "│", "╞", "╡", "═", "◆ ", " ◆", ConsoleColor.DarkYellow);
    private static readonly FrameStyle SingleDVFrame = new("╓", "╖", "╙", "╜", "─", "║", "╟", "╢", "─", "◇ ", " ◇", ConsoleColor.DarkMagenta);

    // --- Rounded banner/prompt-like blocks ---
    private static void RoundedTop(string title, ConsoleColor titleColor) => FrameTop(RoundedFrame, title, titleColor);
    private static void RoundedMid(string label, ConsoleColor labelColor) => FrameMid(RoundedFrame, label, labelColor);
    private static void RoundedBottom() => FrameBottom(RoundedFrame);

    private static void FrameTop(FrameStyle s, string title, ConsoleColor titleColor, int inner = PanelInner)
    {
        string t = $"{s.TitleLeft}{title}{s.TitleRight}";
        int fill = inner - 1 - t.Length;
        if (fill < 0) { t = Fit($"{s.TitleLeft}{title}", inner - 1); fill = 0; }
        Write(s.BorderColor, Prefix + s.TL + s.H);
        Write(titleColor, t);
        WriteLine(s.BorderColor, new string(s.H[0], fill) + s.TR);
    }

    private static void FrameMid(FrameStyle s, string label, ConsoleColor labelColor, int inner = PanelInner)
    {
        string l = $"◆ {label} ◆";
        int fill = inner - 1 - l.Length;
        if (fill < 0) { l = Fit($"◆ {label}", inner - 1); fill = 0; }
        Write(s.BorderColor, Prefix + s.SL + s.SF);
        Write(labelColor, l);
        WriteLine(s.BorderColor, new string(s.SF[0], fill) + s.SR);
    }

    private static void FrameBottom(FrameStyle s, int inner = PanelInner)
    {
        WriteLine(s.BorderColor, Prefix + s.BL + new string(s.H[0], inner) + s.BR);
    }

    private static void RoundedBody(string text, ConsoleColor color)
    {
        string content = Fit(text, PanelInner).PadRight(PanelInner);
        Write(ConsoleColor.Gray, Prefix + "│");
        Write(color, content);
        WriteLine(ConsoleColor.Gray, "│");
    }

    private static void RoundedRow(string l1, string v1, ConsoleColor c1, string l2, string v2, ConsoleColor c2)
    {
        int half = (PanelInner - 3) / 2;
        string left = Fit($" {l1,-5}▸ {v1}", half).PadRight(half);
        string right = Fit($" {l2,-5}▸ {v2}", PanelInner - 3 - half).PadRight(PanelInner - 3 - half);

        Write(ConsoleColor.Gray, Prefix + "│");
        Write(c1, left);
        Write(ConsoleColor.Gray, " │ ");
        Write(c2, right);
        WriteLine(ConsoleColor.Gray, "│");
    }

    // --- Framed help panels ---
    private sealed class PanelWriter
    {
        private readonly int _inner;
        private readonly FrameStyle _s;
        public PanelWriter(int inner, FrameStyle? style = null) { _inner = inner; _s = style ?? RoundedFrame; }

        public void Row(string cmd, string desc)
        {
            const int col = 30;
            string left = Fit(cmd, col).PadRight(col);
            string right = Fit(desc, _inner - 2 - col - 4);
            Print("  " + left + " :: " + right, ConsoleColor.Gray, ConsoleColor.Cyan, 2, col);
        }

        public void Kv(string label, string desc)
        {
            const int col = 12;
            string l = Fit(label, col).PadRight(col);
            string r = Fit(desc, _inner - 2 - col - 4);
            Print("  " + l + " :: " + r, ConsoleColor.Gray, ConsoleColor.Green, 2, col);
        }

        public void Cmd(string text)
        {
            string content = Fit("  pcspkr:~$ " + text, _inner).PadRight(_inner);
            Write(_s.BorderColor, Prefix + _s.V);
            string p1 = "  pcspkr";
            string p2 = ":";
            string p3 = "~";
            string p4 = "$ ";
            int pos = 0;
            void Seg(string s, ConsoleColor c)
            {
                if (pos >= content.Length) return;
                int take = Math.Min(s.Length, content.Length - pos);
                Write(c, content.Substring(pos, take));
                pos += take;
            }
            Seg(p1, ConsoleColor.Green);
            Seg(p2, ConsoleColor.DarkGray);
            Seg(p3, ConsoleColor.Cyan);
            Seg(p4, ConsoleColor.DarkGray);
            if (pos < content.Length) Write(ConsoleColor.White, content[pos..]);
            WriteLine(_s.BorderColor, _s.V);
        }

        public void Alert(string label, string text, ConsoleColor valueColor)
        {
            Write(_s.BorderColor, Prefix + _s.V);
            Write(ConsoleColor.Red, "  [!] ");
            Write(ConsoleColor.White, $"{Fit(label, 7).PadRight(7)}: ");
            int used = 15;
            Write(valueColor, Fit(text, _inner - used).PadRight(_inner - used));
            WriteLine(_s.BorderColor, _s.V);
        }

        public void Tip(string text)
        {
            Write(_s.BorderColor, Prefix + _s.V);
            Write(ConsoleColor.Yellow, "  [tip] ");
            Write(ConsoleColor.Gray, Fit(text, _inner - 8).PadRight(_inner - 8));
            WriteLine(_s.BorderColor, _s.V);
        }

        public void Section(string label, ConsoleColor color)
        {
            string l = $"◆ {label} ◆";
            int fill = _inner - 1 - l.Length;
            if (fill < 0) { l = Fit($"◆ {label}", _inner - 1); fill = 0; }
            Write(_s.BorderColor, Prefix + _s.SL + _s.SF);
            Write(color, l);
            WriteLine(_s.BorderColor, new string(_s.SF[0], fill) + _s.SR);
        }

        private void Print(string content, ConsoleColor valueColor, ConsoleColor labelColor, int labelStart, int labelLen)
        {
            content = Fit(content, _inner).PadRight(_inner);
            Write(_s.BorderColor, Prefix + _s.V);
            if (labelStart + labelLen <= content.Length)
            {
                Write(ConsoleColor.DarkGray, content[..labelStart]);
                Write(labelColor, content.Substring(labelStart, labelLen));
                int sepStart = labelStart + labelLen;
                int sepEnd = Math.Min(sepStart + 4, content.Length);
                Write(ConsoleColor.DarkGray, content[sepStart..sepEnd]);
                if (sepEnd < content.Length) Write(valueColor, content[sepEnd..]);
            }
            else
            {
                Write(valueColor, content);
            }
            WriteLine(_s.BorderColor, _s.V);
        }
    }

    private static void RoundedPanel(string title, ConsoleColor titleColor, Action<PanelWriter> body)
        => FramedPanel(RoundedFrame, title, titleColor, body);

    private static void FramedPanel(FrameStyle style, string title, ConsoleColor titleColor, Action<PanelWriter> body, int inner = PanelInner)
    {
        FrameTop(style, title, titleColor, inner);
        body(new PanelWriter(inner, style));
        FrameBottom(style, inner);
    }

    // --- Double-line panels (style-aware) ---
    private static void DBoxTop(string title, ConsoleColor titleColor, FrameStyle? style = null)
        => DBoxTopCustom(title, PanelInner, titleColor, style);

    private static void DBoxTopCustom(string title, int inner, ConsoleColor titleColor, FrameStyle? style = null)
    {
        var s = style ?? DoubleFrame;
        string t = $"{s.TitleLeft}{title}{s.TitleRight}";
        int fill = inner - 1 - t.Length;
        if (fill < 0) { t = Fit($"{s.TitleLeft}{title}", inner - 1); fill = 0; }
        Write(s.BorderColor, Prefix + s.TL + s.H);
        Write(titleColor, t);
        WriteLine(s.BorderColor, new string(s.H[0], fill) + s.TR);
    }

    private static void DBoxSep(FrameStyle? style = null) => DBoxSepCustom(PanelInner, style);

    private static void DBoxSepCustom(int inner, FrameStyle? style = null)
    {
        var s = style ?? DoubleFrame;
        if (inner < 3)
        {
            WriteLine(s.BorderColor, Prefix + s.SL + new string(s.SF[0], inner) + s.SR);
            return;
        }

        char[] fill = new string(s.SF[0], inner).ToCharArray();
        fill[inner / 2] = '◆';
        WriteLine(s.BorderColor, Prefix + s.SL + new string(fill) + s.SR);
    }

    private static void DBoxSepText(string text, int inner, ConsoleColor color, FrameStyle? style = null)
    {
        var s = style ?? DoubleFrame;
        string t = Fit(text, inner);
        int fill = inner - t.Length;
        if (fill < 0) fill = 0;
        Write(s.BorderColor, Prefix + s.SL);
        Write(color, t);
        WriteLine(s.BorderColor, new string(s.SF[0], fill) + s.SR);
    }

    private static void DBoxBottom(FrameStyle? style = null) => DBoxBottomCustom(PanelInner, style);

    private static void DBoxBottomCustom(int inner, FrameStyle? style = null)
    {
        var s = style ?? DoubleFrame;
        if (inner < 3)
        {
            WriteLine(s.BorderColor, Prefix + s.BL + new string(s.H[0], inner) + s.BR);
            return;
        }

        char[] fill = new string(s.H[0], inner).ToCharArray();
        fill[inner / 2] = '◆';
        WriteLine(s.BorderColor, Prefix + s.BL + new string(fill) + s.BR);
    }

    private static void DBoxRow(string label, string value, ConsoleColor valueColor, FrameStyle? style = null, ConsoleColor? labelColor = null)
    {
        var s = style ?? DoubleFrame;
        string l = $" {label,-10}▸ ";
        string content = Fit(value, PanelInner - l.Length).PadRight(PanelInner - l.Length);
        Write(s.BorderColor, Prefix + s.V);
        Write(labelColor ?? ConsoleColor.Cyan, l);
        Write(valueColor, content);
        WriteLine(s.BorderColor, s.V);
    }

    private static void Tape(string text, ConsoleColor a, ConsoleColor b)
    {
        const string left = "░▒▓[ ";
        const string right = " ]▓▒░";
        int inner = PanelWidth - left.Length - right.Length;
        if (inner < 8) inner = 8;
        string body = Fit(text, inner);
        string line = Prefix + left + body + right;
        if (line.Length < Prefix.Length + PanelWidth)
            line += new string('═', Prefix.Length + PanelWidth - line.Length);
        else if (line.Length > Prefix.Length + PanelWidth)
            line = line[..(Prefix.Length + PanelWidth)];

        int mid = Math.Min(Prefix.Length + PanelWidth / 2, line.Length);
        Write(a, line[..mid]);
        WriteLine(b, line[mid..]);
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
        if (s.Length <= w) return s;
        if (w <= 3) return s[..w];
        return s[..(w - 3)] + "...";
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool ReadConsoleOutputW(IntPtr hConsoleOutput, [Out] CHAR_INFO[] lpBuffer, COORD dwBufferSize, COORD dwBufferCoord, ref SMALL_RECT lpReadRegion);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool WriteConsoleOutputW(IntPtr hConsoleOutput, CHAR_INFO[] lpBuffer, COORD dwBufferSize, COORD dwBufferCoord, ref SMALL_RECT lpWriteRegion);

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD
    {
        public short X;
        public short Y;
        public COORD(short x, short y) { X = x; Y = y; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SMALL_RECT
    {
        public short Left;
        public short Top;
        public short Right;
        public short Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CHAR_INFO
    {
        public char UnicodeChar;
        public ushort Attributes;
    }

    private static IntPtr GetConsoleOutputHandle()
    {
        IntPtr h = CreateFileW("CONOUT$", 0x80000000 | 0x40000000, 1 | 2, IntPtr.Zero, 3, 0, IntPtr.Zero);
        return (h == IntPtr.Zero || h == new IntPtr(-1)) ? IntPtr.Zero : h;
    }

    private static CHAR_INFO[]? SaveBufferRegion(int left, int top, int width, int height)
    {
        if (width <= 0 || height <= 0) return null;
        if (left < 0 || top < 0) return null;
        if (left + width > Console.BufferWidth || top + height > Console.BufferHeight) return null;
        IntPtr handle = GetConsoleOutputHandle();
        if (handle == IntPtr.Zero) return null;
        try
        {
            var buf = new CHAR_INFO[width * height];
            var size = new COORD((short)width, (short)height);
            var coord = new COORD(0, 0);
            var rect = new SMALL_RECT
            {
                Left = (short)left,
                Top = (short)top,
                Right = (short)(left + width - 1),
                Bottom = (short)(top + height - 1)
            };
            return ReadConsoleOutputW(handle, buf, size, coord, ref rect) ? buf : null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    private static void RestoreBufferRegion(int left, int top, int width, int height, CHAR_INFO[]? buf)
    {
        if (buf is null || width <= 0 || height <= 0) return;
        if (left < 0 || top < 0) return;
        if (left + width > Console.BufferWidth || top + height > Console.BufferHeight) return;
        IntPtr handle = GetConsoleOutputHandle();
        if (handle == IntPtr.Zero) return;
        try
        {
            var size = new COORD((short)width, (short)height);
            var coord = new COORD(0, 0);
            var rect = new SMALL_RECT
            {
                Left = (short)left,
                Top = (short)top,
                Right = (short)(left + width - 1),
                Bottom = (short)(top + height - 1)
            };
            WriteConsoleOutputW(handle, buf, size, coord, ref rect);
        }
        finally
        {
            CloseHandle(handle);
        }
    }
}
