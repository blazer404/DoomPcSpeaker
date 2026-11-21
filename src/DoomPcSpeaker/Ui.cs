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
            DBoxRow("input", "id / key / name fragment / alias (type like you mean it)", ConsoleColor.White);
            DBoxRow("queue", "separate tracks with ',' ';' or space (go wild)", ConsoleColor.White);
            DBoxRow("cmds", "list | help | cls | stop | 0/q exit (the usual suspects)", ConsoleColor.DarkGray);
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

        DBoxTopCustom("TRACKLIST", inner, ConsoleColor.Cyan);

        int idx = 1;
        foreach (var group in Melodies.Groups)
        {
            string g = $" ▓▒░ {group.Name.ToUpperInvariant()} ░▒▓ ";
            DBoxSepText(g, inner, ConsoleColor.Magenta);

            foreach (var m in group.Tracks)
            {
                string num = idx.ToString("00");
                string key = Fit(m.Key.ToLowerInvariant(), keyWidth).PadRight(keyWidth);
                string name = Fit(m.Name, nameWidth).PadRight(nameWidth);

                Write(ConsoleColor.Gray, Prefix + "║ ");
                Write(ConsoleColor.Gray, "[");
                Write(ConsoleColor.Yellow, num);
                Write(ConsoleColor.Gray, "]  ");
                Write(ConsoleColor.Cyan, key);
                Write(ConsoleColor.Gray, "  ▸ ");
                Write(ConsoleColor.White, name);
                WriteLine(ConsoleColor.Gray, " ║");
                idx++;
            }
        }

        DBoxSepCustom(inner);
        string footer = Fit($"  {Melodies.All.Length:00} tracks loaded  //  pick: id | key | name | random", inner).PadRight(inner);
        Write(ConsoleColor.Gray, Prefix + "║");
        Write(ConsoleColor.Green, footer);
        WriteLine(ConsoleColor.Gray, "║");
        DBoxBottomCustom(inner);
    }

    public static void PrintHelp(string? availabilityError = null)
    {
        PrintBanner(compact: true, availabilityError);
        WriteHelpBody();
        Console.WriteLine();
    }

    public static void WriteHelpBody()
    {
        RoundedPanel("USAGE", ConsoleColor.Cyan, writer =>
        {
            writer.Row("DoomPcSpeaker.exe", "launch interactive shell");
            writer.Row("DoomPcSpeaker.exe <tracks>", "play track(s) directly and exit");
            writer.Row("DoomPcSpeaker.exe --list", "print full track database");
            writer.Row("DoomPcSpeaker.exe --stop", "mute speaker / clear port state");
            writer.Row("DoomPcSpeaker.exe --help", "show this help");
        });
        Console.WriteLine();

        RoundedPanel("CONTROLS", ConsoleColor.Magenta, writer =>
        {
            writer.Section("ACTIONS", ConsoleColor.DarkYellow);
            writer.Kv("play", "enter track number, key, name fragment, or alias");
            writer.Kv("queue", "separate tracks with comma, semicolon, or space");
            writer.Kv("Ctrl+C", "stop playback/queue; if idle, exit the app");
            writer.Section("COMMANDS", ConsoleColor.DarkCyan);
            writer.Kv("random", "play a random track (aliases: random/rnd/rand/shuffle)");
            writer.Kv("list", "show tracklist (aliases: list / l / ls)");
            writer.Kv("help", "show this help (aliases: help / h / ?)");
            writer.Kv("clear", "clear screen and return to main view (cls / clear)");
            writer.Kv("stop", "silence the speaker (stop / mute)");
            writer.Kv("exit", "quit the app (0 / q / quit / exit)");
        });
        Console.WriteLine();

        RoundedPanel("EXAMPLES", ConsoleColor.Yellow, writer =>
        {
            writer.Cmd("DoomPcSpeaker.exe");
            writer.Cmd("DoomPcSpeaker.exe doom");
            writer.Cmd("DoomPcSpeaker.exe 7,13,18");
            writer.Cmd("DoomPcSpeaker.exe doom pirates rickroll");
            writer.Cmd("DoomPcSpeaker.exe \"got, godfather, vampirekiller\"");
            writer.Cmd("DoomPcSpeaker.exe random");
            writer.Tip("copypaste like a pro");
        });
        Console.WriteLine();

        RoundedPanel("REQUIREMENTS", ConsoleColor.Red, writer =>
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
            Write(ConsoleColor.Magenta, "id/key/name/random/queue");
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
            Write(ConsoleColor.Yellow, "list/help/cls/stop/0");
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

    public static void NowPlaying(Melodies.Song song, int? index = null, int? total = null)
    {
        string title = "NOW PLAYING";
        if (index.HasValue && total.HasValue && total.Value > 1)
            title += $"  [{index.Value:00}/{total.Value:00}]";

        DBoxTop(title, ConsoleColor.Cyan);
        DBoxRow("track", $"{song.Name}  [{song.Key}]", ConsoleColor.White);
        DBoxRow("route", "port 0x61 > i8254 PIT CH2 > square-wave output", ConsoleColor.Cyan);
        DBoxRow("state", "RUNNING :: screaming through the beeper, enjoy", ConsoleColor.Green);
        DBoxSep();
        DBoxRow("ctrl+c", "abort playback/queue (signal cut, session alive)", ConsoleColor.Red);
        DBoxBottom();
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

    // --- Rounded banner/prompt-like blocks ---
    private static void RoundedTop(string title, ConsoleColor titleColor)
    {
        string t = $"░▒ {title} ▒░";
        int fill = PanelInner - 1 - t.Length;
        if (fill < 0) { t = Fit($"░▒ {title}", PanelInner - 1); fill = 0; }
        Write(ConsoleColor.Gray, Prefix + "╭─");
        Write(titleColor, t);
        WriteLine(ConsoleColor.Gray, new string('─', fill) + "╮");
    }

    private static void RoundedMid(string label, ConsoleColor labelColor)
    {
        string l = $"◆ {label} ◆";
        int fill = PanelInner - 1 - l.Length;
        if (fill < 0) { l = Fit($"◆ {label}", PanelInner - 1); fill = 0; }
        Write(ConsoleColor.Gray, Prefix + "├─");
        Write(labelColor, l);
        WriteLine(ConsoleColor.Gray, new string('─', fill) + "┤");
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
        // Two-column HUD row inside rounded panel
        int half = (PanelInner - 3) / 2; // " │ " separator in middle
        string left = Fit($" {l1,-5}▸ {v1}", half).PadRight(half);
        string right = Fit($" {l2,-5}▸ {v2}", PanelInner - 3 - half).PadRight(PanelInner - 3 - half);

        Write(ConsoleColor.Gray, Prefix + "│");
        Write(c1, left);
        Write(ConsoleColor.Gray, " │ ");
        Write(c2, right);
        WriteLine(ConsoleColor.Gray, "│");
    }

    private static void RoundedBottom()
    {
        WriteLine(ConsoleColor.Gray, Prefix + "╰" + new string('─', PanelInner) + "╯");
    }

    // --- Rounded help panels ---
    private sealed class PanelWriter
    {
        private readonly int _inner;
        public PanelWriter(int inner) { _inner = inner; }

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
            string prefix = "  pcspkr:~$ ";
            string content = Fit(prefix + text, _inner).PadRight(_inner);
            Write(ConsoleColor.Gray, Prefix + "│");
            // colorize prompt parts
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
            WriteLine(ConsoleColor.Gray, "│");
        }

        public void Alert(string label, string text, ConsoleColor valueColor)
        {
            Write(ConsoleColor.Gray, Prefix + "│");
            Write(ConsoleColor.Red, "  [!] ");
            Write(ConsoleColor.White, $"{Fit(label, 7).PadRight(7)}: ");
            int used = 15; // "  [!] " (6) + label(7) + ": " (2)
            Write(valueColor, Fit(text, _inner - used).PadRight(_inner - used));
            WriteLine(ConsoleColor.Gray, "│");
        }

        public void Tip(string text)
        {
            string content = Fit("  [tip] " + text, _inner).PadRight(_inner);
            Write(ConsoleColor.Gray, Prefix + "│");
            Write(ConsoleColor.Yellow, "  [tip] ");
            Write(ConsoleColor.Gray, Fit(text, _inner - 8).PadRight(_inner - 8));
            WriteLine(ConsoleColor.Gray, "│");
        }

        public void Section(string label, ConsoleColor color)
        {
            string l = $"◆ {label} ◆";
            int fill = _inner - 1 - l.Length;
            if (fill < 0) { l = Fit($"◆ {label}", _inner - 1); fill = 0; }
            Write(ConsoleColor.Gray, Prefix + "├─");
            Write(color, l);
            WriteLine(ConsoleColor.Gray, new string('─', fill) + "┤");
        }

        private void Print(string content, ConsoleColor valueColor, ConsoleColor labelColor, int labelStart, int labelLen)
        {
            content = Fit(content, _inner).PadRight(_inner);
            Write(ConsoleColor.Gray, Prefix + "│");
            // split: prefix spaces + label + " :: " + rest
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
            WriteLine(ConsoleColor.Gray, "│");
        }
    }

    private static void RoundedPanel(string title, ConsoleColor titleColor, Action<PanelWriter> body)
    {
        RoundedTop(title, titleColor);
        body(new PanelWriter(PanelInner));
        RoundedBottom();
    }

    // --- Double-line panels ---
    private static void DBoxTop(string title, ConsoleColor titleColor)
    {
        DBoxTopCustom(title, PanelInner, titleColor);
    }

    private static void DBoxTopCustom(string title, int inner, ConsoleColor titleColor)
    {
        string t = $"◢ {title} ◣";
        int fill = inner - 1 - t.Length;
        if (fill < 0) { t = Fit($"◢ {title}", inner - 1); fill = 0; }
        Write(ConsoleColor.Gray, Prefix + "╔═");
        Write(titleColor, t);
        WriteLine(ConsoleColor.Gray, new string('═', fill) + "╗");
    }

    private static void DBoxSep() => DBoxSepCustom(PanelInner);

    private static void DBoxSepCustom(int inner)
    {
        if (inner < 3)
        {
            WriteLine(ConsoleColor.Gray, Prefix + "╠" + new string('═', inner) + "╣");
            return;
        }

        char[] fill = new string('═', inner).ToCharArray();
        fill[inner / 2] = '◆';
        WriteLine(ConsoleColor.Gray, Prefix + "╠" + new string(fill) + "╣");
    }

    private static void DBoxSepText(string text, int inner, ConsoleColor color)
    {
        string t = Fit(text, inner);
        int fill = inner - t.Length;
        if (fill < 0) fill = 0;
        Write(ConsoleColor.Gray, Prefix + "╠");
        Write(color, t);
        WriteLine(ConsoleColor.Gray, new string('═', fill) + "╣");
    }

    private static void DBoxBottom() => DBoxBottomCustom(PanelInner);

    private static void DBoxBottomCustom(int inner)
    {
        if (inner < 3)
        {
            WriteLine(ConsoleColor.Gray, Prefix + "╚" + new string('═', inner) + "╝");
            return;
        }

        char[] fill = new string('═', inner).ToCharArray();
        fill[inner / 2] = '◆';
        WriteLine(ConsoleColor.Gray, Prefix + "╚" + new string(fill) + "╝");
    }

    private static void DBoxRow(string label, string value, ConsoleColor valueColor)
    {
        string l = $" {label,-10}▸ ";
        string content = Fit(value, PanelInner - l.Length).PadRight(PanelInner - l.Length);
        Write(ConsoleColor.Gray, Prefix + "║");
        Write(ConsoleColor.Cyan, l);
        Write(valueColor, content);
        WriteLine(ConsoleColor.Gray, "║");
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
}
