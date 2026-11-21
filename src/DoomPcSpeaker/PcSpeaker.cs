using System.Runtime.InteropServices;

static class PcSpeaker
{
    private const string LibraryName = "inpoutx64.dll";
    internal const string DriverMarker = "inpoutx64.dll";

    private const int ControlPort = 0x43;
    private const int CounterPort = 0x42;
    private const int SystemPort = 0x61;
    private const int PitClockHz = 1193180;
    private const int SquareWaveControlWord = 0xB6;
    private const int SpeakerEnableBits = 0x03;
    private const int SpeakerDisableMask = 0xFC;
    private const int LowByteMask = 0xFF;
    private const int ByteShift = 8;
    private const int ArticulationThresholdMs = 20;
    private const int ArticulationGapMs = 3;
    private const int SleepChunkMs = 20;

    private const string DriverMissingMessage = DriverMarker + " not found next to exe";
    private const string DriverArchitectureMessage = DriverMarker + " wrong architecture (x64 required)";

    [DllImport(LibraryName, EntryPoint = "Out32")]
    public static extern void Out32(int portAddress, int data);

    [DllImport(LibraryName, EntryPoint = "Inp32")]
    public static extern int Inp32(int portAddress);

    public static string? CheckAvailability()
    {
        try
        {
            _ = Inp32(SystemPort);
            return null;
        }
        catch (DllNotFoundException)
        {
            return DriverMissingMessage;
        }
        catch (BadImageFormatException)
        {
            return DriverArchitectureMessage;
        }
        catch (Exception ex)
        {
            return "port access denied: " + ex.Message + " (run as Administrator)";
        }
    }

    public static bool NeedsDriverHint(string? availabilityError) =>
        availabilityError is not null &&
        availabilityError.Contains(DriverMarker, StringComparison.OrdinalIgnoreCase);

    public static void Play(int frequency, int durationMs, CancellationToken token)
    {
        if (frequency <= 0)
        {
            SleepWithCancel(durationMs, token);
            return;
        }

        int divisor = Math.Max(1, PitClockHz / frequency);

        // Keep overall tempo: short articulation gap is included in note duration.
        int gapMs = durationMs >= ArticulationThresholdMs ? ArticulationGapMs : 0;
        int playMs = Math.Max(1, durationMs - gapMs);

        try
        {
            Out32(ControlPort, SquareWaveControlWord);
            Out32(CounterPort, divisor & LowByteMask);
            Out32(CounterPort, (divisor >> ByteShift) & LowByteMask);

            int state = Inp32(SystemPort);
            if ((state & SpeakerEnableBits) != SpeakerEnableBits)
                Out32(SystemPort, state | SpeakerEnableBits);

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
            Out32(SystemPort, Inp32(SystemPort) & SpeakerDisableMask);
        }
        catch (Exception)
        {
            // Best-effort mute: also runs at process exit when the driver is unavailable.
        }
    }

    private static void SleepWithCancel(int ms, CancellationToken token)
    {
        int remaining = ms;
        while (remaining > 0 && !token.IsCancellationRequested)
        {
            int chunk = Math.Min(SleepChunkMs, remaining);
            Thread.Sleep(chunk);
            remaining -= chunk;
        }
    }
}
