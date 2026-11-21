static class Ui
{
    public enum View { Main, List, Help }

    private const string Prefix = "  ";
    private const int PanelWidth = 74; // total width of framed panels including border chars
    private const int PanelInner = PanelWidth - 2; // content between left/right border chars

    public static void DrawScreen(View view, string? status, bool statusOk, string? availabilityError, bool playing = false)
    {
        Terminal.ClearScreen();

        switch (view)
        {
            case View.List:
                PrintBanner(compact: true, availabilityError, showNfo: false, playing: playing);
                PrintTable();
                break;
            case View.Help:
                PrintBanner(compact: true, availabilityError, showNfo: false, playing: playing);
                WriteHelpBody();
                break;
            default:
                PrintBanner(compact: true, availabilityError, playing: playing);
                if (availabilityError is null)
                {
                    Dim("  [SYS] type 'help' for commands and usage");
                }
                else
                {
                    Dim("  [SYS] mode   :: playback disabled / read-only, look but no beep");
                    Dim("  [SYS] allow  :: list  help  clear  stop  quit");
                    Dim("  [SYS] action :: fix driver/access and restart (you got this)");
                }
                break;
        }

        Console.WriteLine();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (statusOk) Success(status); else Warn(status);
            Console.WriteLine();
        }
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
            if (PcSpeaker.NeedsDriverHint(availabilityError))
                DBoxRow("fix", "drop inpoutx64.dll next to exe, run as admin (easy mode)", ConsoleColor.Yellow);
            else
                DBoxRow("fix", "run as admin and check speaker/driver (yes, again)", ConsoleColor.Yellow);
            DBoxRow("note", "playback locked until you fix it, no beep for you", ConsoleColor.Red);
        }
        DBoxBottom();
    }

    public static void PrintTable()
    {
        int keyWidth = Melodies.All.Length > 0 ? Math.Max(12, Melodies.All.Max(m => m.Key.Length)) : 12;
        int inner = PanelInner;
        // inner layout: "  " + key + "  ▸ " + name + " " => 7 + keyWidth + nameWidth
        int nameWidth = inner - 7 - keyWidth;
        if (nameWidth < 10) nameWidth = 10;

        var s = HeavyFrame;
        DBoxTopCustom("TRACKLIST", inner, ConsoleColor.Cyan, s);

        if (Melodies.All.Length == 0)
        {
            string msg = Fit("  no tracks found — drop .dpsm files into melodies/", inner).PadRight(inner);
            Write(s.BorderColor, Prefix + s.V);
            Write(ConsoleColor.Yellow, msg);
            WriteLine(s.BorderColor, s.V);
        }
        else
        {
            foreach (var group in Melodies.Groups)
            {
                if (!string.IsNullOrWhiteSpace(group.Name))
                {
                    string g = $" ▓▒░ {group.Name.ToUpperInvariant()} ░▒▓ ";
                    DBoxSepText(g, inner, ConsoleColor.Magenta, s);
                }

                foreach (var m in group.Tracks)
                {
                    string key = Fit(m.Key.ToLowerInvariant(), keyWidth).PadRight(keyWidth);
                    string name = Fit(m.Name, nameWidth).PadRight(nameWidth);

                    Write(s.BorderColor, Prefix + s.V + "  ");
                    Write(ConsoleColor.Cyan, key);
                    Write(ConsoleColor.Gray, "  ▸ ");
                    Write(ConsoleColor.White, name);
                    WriteLine(s.BorderColor, " " + s.V);
                }
            }
        }

        DBoxSepCustom(inner, s);
        string footer = Melodies.All.Length == 0
            ? Fit("  00 tracks loaded  //  add .dpsm files to melodies/", inner).PadRight(inner)
            : Fit($"  {Melodies.All.Length:00} tracks loaded  //  pick: key | random", inner).PadRight(inner);
        Write(s.BorderColor, Prefix + s.V);
        Write(Melodies.All.Length == 0 ? ConsoleColor.DarkGray : ConsoleColor.Green, footer);
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
            writer.Kv("play", "enter track key (from tracklist)");
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
            writer.Cmd("DoomPcSpeaker.exe tetris,mario,sonic");
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
            Write(ConsoleColor.Magenta, "key/random/queue");
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

    internal static string Fit(string s, int w)
    {
        if (w <= 0) return string.Empty;
        if (s.Length <= w) return s;
        if (w <= 3) return s[..w];
        return s[..(w - 3)] + "...";
    }

}
