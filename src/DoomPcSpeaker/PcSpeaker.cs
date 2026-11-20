using System.Runtime.InteropServices;

static class PcSpeaker
{
    [DllImport("inpoutx64.dll", EntryPoint = "Out32")]
    public static extern void Out32(int portAddress, int data);

    [DllImport("inpoutx64.dll", EntryPoint = "Inp32")]
    public static extern int Inp32(int portAddress);

    public static string? CheckAvailability()
    {
        try
        {
            _ = Inp32(0x61);
            return null;
        }
        catch (DllNotFoundException)
        {
            return "inpoutx64.dll not found next to exe";
        }
        catch (BadImageFormatException)
        {
            return "inpoutx64.dll wrong architecture (x64 required)";
        }
        catch (Exception ex)
        {
            return "port access denied: " + ex.Message + " (run as Administrator)";
        }
    }

    public static void Play(int frequency, int durationMs, CancellationToken token)
    {
        if (frequency <= 0)
        {
            SleepWithCancel(durationMs, token);
            return;
        }

        int divisor = 1193180 / frequency;
        if (divisor <= 0) divisor = 1;

        // Keep overall tempo: short articulation gap is included in note duration.
        int gapMs = durationMs >= 20 ? 3 : 0;
        int playMs = Math.Max(1, durationMs - gapMs);

        try
        {
            Out32(0x43, 0xB6);
            Out32(0x42, divisor & 0xFF);
            Out32(0x42, (divisor >> 8) & 0xFF);

            int tmp = Inp32(0x61);
            if ((tmp & 0x03) != 0x03)
                Out32(0x61, tmp | 0x03);

            SleepWithCancel(playMs, token);
        }
        finally
        {
            // Always mute after a note to avoid a stuck tone.
            Stop();
        }

        if (gapMs > 0)
            SleepWithCancel(gapMs, token);
    }

    public static void Stop()
    {
        try
        {
            int tmp = Inp32(0x61) & 0xFC;
            Out32(0x61, tmp);
        }
        catch
        {
            // ignore: ProcessExit/DllNotFound/etc.
        }
    }

    private static void SleepWithCancel(int ms, CancellationToken token)
    {
        int remaining = ms;
        while (remaining > 0 && !token.IsCancellationRequested)
        {
            int chunk = Math.Min(20, remaining);
            try { Thread.Sleep(chunk); }
            catch { break; }
            remaining -= chunk;
        }
    }
}
