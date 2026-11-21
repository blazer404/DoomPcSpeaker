static class Playback
{
    private const int TrackGapMs = 350;

    private static readonly object Lock = new();
    private static CancellationTokenSource? _current;

    public static bool RequestCancel()
    {
        lock (Lock)
        {
            if (_current is { IsCancellationRequested: false })
            {
                _current.Cancel();
                return true;
            }
            return false;
        }
    }

    public static bool PlayQueue(
        IReadOnlyList<Melodies.Song> songs,
        Ui.View fromView = Ui.View.Main,
        string? availabilityError = null)
    {
        using var cts = new CancellationTokenSource();
        lock (Lock) _current = cts;

        try
        {
            for (int i = 0; i < songs.Count; i++)
            {
                if (cts.IsCancellationRequested) break;

                Ui.DrawScreen(fromView, status: null, statusOk: true, availabilityError, playing: true);
                Ui.NowPlaying(songs[i], i + 1, songs.Count);
                Console.WriteLine();

                foreach (var (frequency, ms) in songs[i].Notes)
                {
                    if (cts.IsCancellationRequested) break;
                    PcSpeaker.Play(frequency, ms, cts.Token);
                }

                if (!cts.IsCancellationRequested && i < songs.Count - 1)
                    Thread.Sleep(TrackGapMs);
            }

            return cts.IsCancellationRequested;
        }
        finally
        {
            Shutdown.Cleanup();
            lock (Lock)
            {
                if (ReferenceEquals(_current, cts)) _current = null;
            }
        }
    }
}
