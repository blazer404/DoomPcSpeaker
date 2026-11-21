using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
Console.Title = "PC Speaker — Hardware Beeper Jukebox";

AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown.Cleanup();

return App.Run(args);

internal static class Shutdown
{
    public static void Cleanup()
    {
        PcSpeaker.Stop();
        Terminal.Reset();
    }
}
