using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
Console.Title = "PC Speaker — Hardware Beeper Jukebox";

AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown.Cleanup();

return App.Run(args);

internal static class Shutdown
{
    private static int _cleanupCount;

    public static void Cleanup()
    {
        if (Interlocked.CompareExchange(ref _cleanupCount, 1, 0) != 0)
            return;

        PcSpeaker.Stop();
        Terminal.Reset();
    }

    internal static int CleanupCount => Volatile.Read(ref _cleanupCount);
}
