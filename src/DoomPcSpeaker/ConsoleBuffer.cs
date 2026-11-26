using System.Runtime.InteropServices;

internal static class ConsoleBuffer
{
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint OpenExisting = 3;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool ReadConsoleOutputW(IntPtr hConsoleOutput, [Out] CHAR_INFO[] lpBuffer, COORD dwBufferSize, COORD dwBufferCoord, ref SMALL_RECT lpReadRegion);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool WriteConsoleOutputW(IntPtr hConsoleOutput, CHAR_INFO[] lpBuffer, COORD dwBufferSize, COORD dwBufferCoord, ref SMALL_RECT lpWriteRegion);

    [StructLayout(LayoutKind.Sequential)]
    internal struct COORD
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
    internal struct CHAR_INFO
    {
        public char UnicodeChar;
        public ushort Attributes;
    }

    private static IntPtr GetConsoleOutputHandle()
    {
        IntPtr handle = CreateFileW("CONOUT$", GenericRead | GenericWrite, FileShareRead | FileShareWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
        return handle == IntPtr.Zero || handle == new IntPtr(-1) ? IntPtr.Zero : handle;
    }

    internal static CHAR_INFO[]? SaveBufferRegion(int left, int top, int width, int height)
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

    internal static void RestoreBufferRegion(int left, int top, int width, int height, CHAR_INFO[]? buffer)
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
