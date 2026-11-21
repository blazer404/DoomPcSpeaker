using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
Console.Title = "PC Speaker — Hardware Beeper Jukebox";

AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    PcSpeaker.Stop();
    Ui.Reset();
};

return App.Run(args);
