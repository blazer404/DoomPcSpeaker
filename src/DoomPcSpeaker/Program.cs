using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
Console.Title = "PCSPKR // BEEPER JUKEBOX";

AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    PcSpeaker.Stop();
    Ui.Reset();
};

return App.Run(args);
