using System.Runtime.InteropServices;

static class Terminal
{
    public static void Reset()
    {
        TryExecute(() =>
        {
            Console.ResetColor();
            Console.Write("\x1b[0m");
        });
    }

    public static void ClearScreen()
    {
        TryExecute(() =>
        {
            Console.ResetColor();
            TryExecute(() => Console.CursorVisible = true);

            // Best effort: clear scrollback, screen and move cursor home.
            TryExecute(() => Console.Write("\x1b[H\x1b[2J\x1b[3J"));

            if (OperatingSystem.IsWindows())
            {
                TryExecute(MatchBufferToWindow);
                TryExecute(ScrollToOrigin);
            }

            TryExecute(() => Console.SetCursorPosition(0, 0));
            TryExecute(Console.Clear);

            // Repeat clears to remove remnants in terminals that keep off-screen buffer content.
            TryExecute(() => Console.Write("\x1b[H\x1b[2J\x1b[3J"));
            TryExecute(() => Console.SetCursorPosition(0, 0));

            if (OperatingSystem.IsWindows())
                TryExecute(ScrollToOrigin);

            TryExecute(() => Console.Write("\x1b[H"));
        });
    }

    public static string? ReadLineWithSlashSuggestions(bool locked = false, Action? redraw = null)
    {
        Suggestion[] commands = Commands.Suggestions(locked);

        int inputLeft = Console.CursorLeft;
        int inputTop = Console.CursorTop;
        var buffer = new System.Text.StringBuilder();

        int popupHeight = 0;
        int popupTop = 0;
        int popupLeft = 0;
        int popupWidth = 0;
        CHAR_INFO[]? savedBuf = null;
        int savedW = 0;
        int savedH = 0;
        int selectedIndex = 0;

        Suggestion[] GetMatches()
        {
            string text = buffer.ToString();
            if (!text.StartsWith('/')) return [];
            string query = text[1..];
            return commands
                .Where(c => c.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        static string Label(Suggestion suggestion) => $"/{suggestion.Name} — {suggestion.Description}";

        static void WriteSuggestion(Suggestion suggestion, int width, ConsoleColor nameColor, ConsoleColor descriptionColor)
        {
            string name = "/" + suggestion.Name + " — ";
            string label = Ui.Fit(Label(suggestion), width).PadRight(width);
            int split = Math.Min(name.Length, label.Length);
            Ui.Write(nameColor, label[..split]);
            if (split < label.Length) Ui.Write(descriptionColor, label[split..]);
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
                RestoreBufferRegion(oldLeft, oldTop, savedW, savedH, savedBuf);
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
                string text = buffer.ToString();
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
            string text = buffer.ToString();
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

            int inner = matches.Max(m => Label(m).Length) + 4; // "▸ /cmd :: description "
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
            Ui.Write(ConsoleColor.Magenta, "╭─");
            Ui.Write(ConsoleColor.Cyan, " commands ");
            Ui.Write(ConsoleColor.Magenta, new string('─', Math.Max(0, inner - " commands ".Length - 1)) + "╮");

            for (int i = 0; i < drawCount; i++)
            {
                bool active = i == selectedIndex;
                Console.SetCursorPosition(left, row++);
                Ui.Write(ConsoleColor.Magenta, "│");
                if (active)
                {
                    Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    Ui.Write(ConsoleColor.White, " ▸ ");
                    WriteSuggestion(matches[i], inner - 3, ConsoleColor.Yellow, ConsoleColor.Gray);
                    Console.ResetColor();
                }
                else
                {
                    Ui.Write(ConsoleColor.DarkGray, "   ");
                    WriteSuggestion(matches[i], inner - 3, ConsoleColor.Gray, ConsoleColor.DarkGray);
                }
                Ui.Write(ConsoleColor.Magenta, "│");
            }

            Console.SetCursorPosition(left, row++);
            Ui.Write(ConsoleColor.Magenta, "╰" + new string('─', inner) + "╯");

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
                    buffer.Clear().Append('/' + matches[Math.Clamp(selectedIndex, 0, matches.Length - 1)].Name);

                string text = buffer.ToString();
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
                    buffer.Clear().Append('/' + matches[Math.Clamp(selectedIndex, 0, matches.Length - 1)].Name);
                    selectedIndex = 0;
                    Render();
                }
                continue;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0) { buffer.Length--; selectedIndex = 0; Render(); }
                continue;
            }

            if (key.Key == ConsoleKey.Escape)
            {
                if (buffer.Length > 0) { buffer.Clear(); selectedIndex = 0; Render(); }
                continue;
            }

            char ch = key.KeyChar;
            if (!char.IsControl(ch))
            {
                buffer.Append(ch);
                selectedIndex = 0;
                Render();
            }
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void MatchBufferToWindow()
    {
        // Reduce scrollback/buffer artifacts by matching buffer to visible window.
        if (Console.BufferWidth != Console.WindowWidth || Console.BufferHeight != Console.WindowHeight)
            Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void ScrollToOrigin()
    {
        Console.WindowLeft = 0;
        Console.WindowTop = 0;
    }

    private static void TryExecute(Action action)
    {
        try
        {
            action();
        }
        catch (Exception)
        {
            // Console redirection, headless sessions and non-Windows terminals may reject
            // cursor or buffer operations. These calls are cosmetic, so rendering continues.
        }
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
        IntPtr handle = CreateFileW("CONOUT$", 0x80000000 | 0x40000000, 1 | 2, IntPtr.Zero, 3, 0, IntPtr.Zero);
        return handle == IntPtr.Zero || handle == new IntPtr(-1) ? IntPtr.Zero : handle;
    }

    private static CHAR_INFO[]? SaveBufferRegion(int left, int top, int width, int height)
    {
        if (!IsRegionValid(left, top, width, height)) return null;
        IntPtr handle = GetConsoleOutputHandle();
        if (handle == IntPtr.Zero) return null;
        try
        {
            var buffer = new CHAR_INFO[width * height];
            var size = new COORD((short)width, (short)height);
            var coord = new COORD(0, 0);
            var rect = CreateRegion(left, top, width, height);
            return ReadConsoleOutputW(handle, buffer, size, coord, ref rect) ? buffer : null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    private static void RestoreBufferRegion(int left, int top, int width, int height, CHAR_INFO[]? buffer)
    {
        if (buffer is null || !IsRegionValid(left, top, width, height)) return;
        IntPtr handle = GetConsoleOutputHandle();
        if (handle == IntPtr.Zero) return;
        try
        {
            var size = new COORD((short)width, (short)height);
            var coord = new COORD(0, 0);
            var rect = CreateRegion(left, top, width, height);
            WriteConsoleOutputW(handle, buffer, size, coord, ref rect);
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    private static bool IsRegionValid(int left, int top, int width, int height) =>
        width > 0 && height > 0 && left >= 0 && top >= 0 &&
        left + width <= Console.BufferWidth && top + height <= Console.BufferHeight;

    private static SMALL_RECT CreateRegion(int left, int top, int width, int height) => new()
    {
        Left = (short)left,
        Top = (short)top,
        Right = (short)(left + width - 1),
        Bottom = (short)(top + height - 1)
    };
}
