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

    public static void PrintBanner(bool compact, string? availabilityError = null, bool showNfo = true)
    {
        bool online = availabilityError is null;

        Tape("SIGNAL NODE // PCSPKR_HARDWARE_INTERFACE // TACTICAL AUDIO TERMINAL", ConsoleColor.DarkCyan, ConsoleColor.DarkGray);
        Console.WriteLine();

        string[] logo =
        [
            "██████╗  ██████╗███████╗██████╗ ██╗  ██╗██████╗ ",
            "██╔══██╗██╔════╝██╔════╝██╔══██╗██║ ██╔╝██╔══██╗",
            "██████╔╝██║     ███████╗██████╔╝█████╔╝ ██████╔╝",
            "██╔═══╝ ██║     ╚════██║██╔═══╝ ██╔═██╗ ██╔══██╗",
            "██║     ╚██████╗███████║██║     ██║  ██╗██║  ██║",
            "╚═╝      ╚═════╝╚══════╝╚═╝     ╚═╝  ╚═╝╚═╝  ╚═╝",
        ];
        ConsoleColor[] logoColors =
        [
            ConsoleColor.Cyan,
            ConsoleColor.Cyan,
            ConsoleColor.Magenta,
            ConsoleColor.Magenta,
            ConsoleColor.Red,
            ConsoleColor.Red,
        ];

        RoundedTop("NODE:PCSPKR  //  BEEPER CORE  //  SIGNAL INTERFACE", ConsoleColor.Yellow);
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
        Tape($"signal={ (online ? "stable" : "degraded") }  //  payload={Melodies.All.Length:00} modules  //  ui=tactical_terminal", ConsoleColor.Green, ConsoleColor.Cyan);

        if (showNfo)
        {
            Console.WriteLine();
            NfoBox(compact, availabilityError);
        }
        Console.WriteLine();
    }

    private static void NfoBox(bool compact, string? availabilityError = null)
    {
        bool online = availabilityError is null;
        string title = online ? "TACTICAL PANEL // release.nfo" : "TACTICAL PANEL // access.denied";

        DBoxTop(title, ConsoleColor.Cyan);
        DBoxRow("release", "PC speaker hardware jukebox  [terminal cut]", ConsoleColor.White);
        DBoxRow("scene", "standalone signal tool / technical audio interface", ConsoleColor.Magenta);
        DBoxRow("payload", $"{Melodies.All.Length:00} chiptune modules for onboard speaker output", ConsoleColor.Green);
        DBoxRow("target", "windows x64 · net9.0 · inpoutx64 driver", ConsoleColor.Yellow);
        DBoxSep();
        DBoxRow("engine", "i8254 PIT channel 2 @ 0x43/0x42/0x61, square-wave", ConsoleColor.Cyan);
        DBoxRow("access", "administrator rights required for port I/O", ConsoleColor.Yellow);
        DBoxRow("modes", "interactive shell, direct CLI, queue, random", ConsoleColor.Gray);
        if (online)
            DBoxRow("status", "ONLINE :: ready, awaiting operator command", ConsoleColor.Green);
        else
            DBoxRow("status", "ERROR :: " + availabilityError, ConsoleColor.Red);

        if (!compact)
        {
            DBoxSep();
            DBoxRow("input", "id / key / name fragment / alias", ConsoleColor.White);
            DBoxRow("queue", "separate tracks with ',' ';' or space", ConsoleColor.White);
            DBoxRow("cmds", "list | help | cls | stop | 0/q exit", ConsoleColor.DarkGray);
            DBoxRow("abort", "Ctrl+C stops playback/queue (session remains)", ConsoleColor.Red);
        }

        if (!online)
        {
            DBoxSep();
            if (availabilityError!.Contains("inpoutx64.dll", StringComparison.OrdinalIgnoreCase))
                DBoxRow("fix", "place inpoutx64.dll next to exe, run as administrator", ConsoleColor.Yellow);
            else
                DBoxRow("fix", "run as administrator and verify speaker/driver access", ConsoleColor.Yellow);
            DBoxRow("note", "playback input is disabled until access is restored", ConsoleColor.Red);
        }
        DBoxBottom();
    }

    public static void PrintTable()
    {
        int keyWidth = Math.Max(12, Melodies.All.Max(m => m.Key.Length));
        int nameWidth = 36;
        // inner layout: " [00]  " + key + "  ▸ " + name + " " => 7 + keyWidth + 4 + nameWidth + 1
        int inner = 12 + keyWidth + nameWidth;

        DBoxTopCustom("TRACKLIST.DB // DATA MODULE", inner, ConsoleColor.Cyan);

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

                Write(ConsoleColor.DarkGray, Prefix + "║ ");
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

        DBoxSepCustom(inner);
        string footer = Fit($"  {Melodies.All.Length:00} modules armed  //  target: id | key | name | random", inner).PadRight(inner);
        Write(ConsoleColor.DarkGray, Prefix + "║");
        Write(ConsoleColor.Green, footer);
        WriteLine(ConsoleColor.DarkGray, "║");
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
            writer.Row("DoomPcSpeaker.exe", "launch interactive tactical shell");
            writer.Row("DoomPcSpeaker.exe <tracks>", "play track(s) directly and exit");
            writer.Row("DoomPcSpeaker.exe --list", "print full track database");
            writer.Row("DoomPcSpeaker.exe --stop", "mute speaker / clear port state");
            writer.Row("DoomPcSpeaker.exe --help", "show this operator guide");
        });
        Console.WriteLine();

        RoundedPanel("CONTROLS", ConsoleColor.Magenta, writer =>
        {
            writer.Kv("target", "id / key / name fragment / alias");
            writer.Kv("chaos", "random / rnd / rand / shuffle");
            writer.Kv("queue", "comma, semicolon or space separated");
            writer.Kv("list", "show tracklist (interactive: list/l/ls)");
            writer.Kv("help", "show help (interactive: help/h/?)");
            writer.Kv("cls", "return to main screen / clear status");
            writer.Kv("stop", "silence speaker (stop/mute)");
            writer.Kv("abort", "Ctrl+C stops playback/queue");
            writer.Kv("escape", "0 / q / quit / exit");
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
        });
        Console.WriteLine();

        RoundedPanel("REQUIREMENTS", ConsoleColor.Red, writer =>
        {
            writer.Alert("driver", "inpoutx64.dll must be next to executable", ConsoleColor.Yellow);
            writer.Alert("access", "Administrator rights required for port I/O", ConsoleColor.Yellow);
            writer.Tip("no CLI flags needed: run without args and use interactive shell");
        });
    }

    public static void Prompt(bool locked = false)
    {
        Console.WriteLine();
        if (!locked)
        {
            Write(ConsoleColor.DarkGray, Prefix + "╭─[");
            Write(ConsoleColor.Cyan, "NODE@PCSPKR");
            Write(ConsoleColor.DarkGray, "]─[");
            Write(ConsoleColor.Green, "INPUT:READY");
            Write(ConsoleColor.DarkGray, "]─[");
            Write(ConsoleColor.Magenta, "id/key/name/random/queue");
            WriteLine(ConsoleColor.DarkGray, "]");

            Write(ConsoleColor.DarkGray, Prefix + "╰─");
            Write(ConsoleColor.Green, "inject");
            Write(ConsoleColor.DarkGray, " > ");
        }
        else
        {
            Write(ConsoleColor.DarkGray, Prefix + "╭─[");
            Write(ConsoleColor.Red, "NODE@PCSPKR");
            Write(ConsoleColor.DarkGray, "]─[");
            Write(ConsoleColor.Red, "INPUT:LOCKED");
            Write(ConsoleColor.DarkGray, "]─[");
            Write(ConsoleColor.Yellow, "list/help/cls/stop/0");
            WriteLine(ConsoleColor.DarkGray, "]");

            Write(ConsoleColor.DarkGray, Prefix + "╰─");
            Write(ConsoleColor.Red, "denied");
            Write(ConsoleColor.DarkGray, " > ");
        }
        Console.ResetColor();
    }

    public static void PromptSimple(string label = "press Enter to return")
    {
        Console.WriteLine();
        Write(ConsoleColor.DarkGray, Prefix + "╭─[");
        Write(ConsoleColor.Yellow, "AWAIT");
        Write(ConsoleColor.DarkGray, "]─[");
        Write(ConsoleColor.White, Fit(label, 54));
        WriteLine(ConsoleColor.DarkGray, "]");
        Write(ConsoleColor.DarkGray, Prefix + "╰─");
        Write(ConsoleColor.Yellow, "input");
        Write(ConsoleColor.DarkGray, " > ");
        Console.ResetColor();
    }

    public static void NowPlaying(Melodies.Song song, int? index = null, int? total = null)
    {
        string title = "ACTION PANEL // SIGNAL INJECTED";
        if (index.HasValue && total.HasValue && total.Value > 1)
            title += $"  [{index.Value:00}/{total.Value:00}]";

        DBoxTop(title, ConsoleColor.Cyan);
        DBoxRow("track", $"{song.Name}  [{song.Key}]", ConsoleColor.White);
        DBoxRow("route", "port 0x61 > i8254 PIT CH2 > square-wave output", ConsoleColor.Cyan);
        DBoxRow("state", "RUNNING :: playback active", ConsoleColor.Green);
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
        string t = $"[ {title} ]";
        int fill = PanelInner - 1 - t.Length;
        if (fill < 0) { t = Fit(t, PanelInner - 1); fill = 0; }
        Write(ConsoleColor.DarkGray, Prefix + "╭─");
        Write(titleColor, t);
        WriteLine(ConsoleColor.DarkGray, new string('─', fill) + "╮");
    }

    private static void RoundedMid(string label, ConsoleColor labelColor)
    {
        string l = $"[ {label} ]";
        int fill = PanelInner - 1 - l.Length;
        if (fill < 0) { l = Fit(l, PanelInner - 1); fill = 0; }
        Write(ConsoleColor.DarkGray, Prefix + "├─");
        Write(labelColor, l);
        WriteLine(ConsoleColor.DarkGray, new string('─', fill) + "┤");
    }

    private static void RoundedBody(string text, ConsoleColor color)
    {
        string content = Fit(text, PanelInner).PadRight(PanelInner);
        Write(ConsoleColor.DarkGray, Prefix + "│");
        Write(color, content);
        WriteLine(ConsoleColor.DarkGray, "│");
    }

    private static void RoundedRow(string l1, string v1, ConsoleColor c1, string l2, string v2, ConsoleColor c2)
    {
        // Two-column HUD row inside rounded panel
        int half = (PanelInner - 3) / 2; // " │ " separator in middle
        string left = Fit($" {l1,-5}: {v1}", half).PadRight(half);
        string right = Fit($" {l2,-5}: {v2}", PanelInner - 3 - half).PadRight(PanelInner - 3 - half);

        Write(ConsoleColor.DarkGray, Prefix + "│");
        Write(c1, left);
        Write(ConsoleColor.DarkGray, " │ ");
        Write(c2, right);
        WriteLine(ConsoleColor.DarkGray, "│");
    }

    private static void RoundedBottom()
    {
        WriteLine(ConsoleColor.DarkGray, Prefix + "╰" + new string('─', PanelInner) + "╯");
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
            string prefix = "  node@pcspkr:~$ ";
            string content = Fit(prefix + text, _inner).PadRight(_inner);
            Write(ConsoleColor.DarkGray, Prefix + "│");
            // colorize prompt parts
            string p1 = "  node@pcspkr";
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
            WriteLine(ConsoleColor.DarkGray, "│");
        }

        public void Alert(string label, string text, ConsoleColor valueColor)
        {
            Write(ConsoleColor.DarkGray, Prefix + "│");
            Write(ConsoleColor.Red, "  [!] ");
            Write(ConsoleColor.White, $"{Fit(label, 7).PadRight(7)}: ");
            int used = 15; // "  [!] " (6) + label(7) + ": " (2)
            Write(valueColor, Fit(text, _inner - used).PadRight(_inner - used));
            WriteLine(ConsoleColor.DarkGray, "│");
        }

        public void Tip(string text)
        {
            string content = Fit("  [tip] " + text, _inner).PadRight(_inner);
            Write(ConsoleColor.DarkGray, Prefix + "│");
            Write(ConsoleColor.Yellow, "  [tip] ");
            Write(ConsoleColor.Gray, Fit(text, _inner - 8).PadRight(_inner - 8));
            WriteLine(ConsoleColor.DarkGray, "│");
        }

        private void Print(string content, ConsoleColor valueColor, ConsoleColor labelColor, int labelStart, int labelLen)
        {
            content = Fit(content, _inner).PadRight(_inner);
            Write(ConsoleColor.DarkGray, Prefix + "│");
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
            WriteLine(ConsoleColor.DarkGray, "│");
        }
    }

    private static void RoundedPanel(string title, ConsoleColor titleColor, Action<PanelWriter> body)
    {
        RoundedTop(title, titleColor);
        body(new PanelWriter(PanelInner));
        RoundedBottom();
    }

    // --- Double-line tactical panels ---
    private static void DBoxTop(string title, ConsoleColor titleColor)
    {
        DBoxTopCustom(title, PanelInner, titleColor);
    }

    private static void DBoxTopCustom(string title, int inner, ConsoleColor titleColor)
    {
        string t = $"[ {title} ]";
        int fill = inner - 1 - t.Length;
        if (fill < 0) { t = Fit(t, inner - 1); fill = 0; }
        Write(ConsoleColor.DarkGray, Prefix + "╔═");
        Write(titleColor, t);
        WriteLine(ConsoleColor.DarkGray, new string('═', fill) + "╗");
    }

    private static void DBoxSep() => DBoxSepCustom(PanelInner);

    private static void DBoxSepCustom(int inner)
    {
        WriteLine(ConsoleColor.DarkGray, Prefix + "╠" + new string('═', inner) + "╣");
    }

    private static void DBoxSepText(string text, int inner, ConsoleColor color)
    {
        string t = Fit(text, inner);
        int fill = inner - t.Length;
        if (fill < 0) fill = 0;
        Write(ConsoleColor.DarkGray, Prefix + "╠");
        Write(color, t);
        WriteLine(ConsoleColor.DarkGray, new string('═', fill) + "╣");
    }

    private static void DBoxBottom() => DBoxBottomCustom(PanelInner);

    private static void DBoxBottomCustom(int inner)
    {
        WriteLine(ConsoleColor.DarkGray, Prefix + "╚" + new string('═', inner) + "╝");
    }

    private static void DBoxRow(string label, string value, ConsoleColor valueColor)
    {
        string l = $" {label,-10}: ";
        string content = Fit(value, PanelInner - l.Length).PadRight(PanelInner - l.Length);
        Write(ConsoleColor.DarkGray, Prefix + "║");
        Write(ConsoleColor.Cyan, l);
        Write(valueColor, content);
        WriteLine(ConsoleColor.DarkGray, "║");
    }

    private static void Tape(string text, ConsoleColor a, ConsoleColor b)
    {
        const string left = "══[ ";
        const string right = " ]══";
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
