using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

[TestFixture]
internal sealed class CliSmokeTests
{
    private static readonly string[] TrackKeys =
    [
        "igor", "jingle", "nokia", "rickroll",
        "castlevania", "doom", "mario", "mk", "pacman", "sonic", "tetris",
        "axelf", "father", "got", "mi", "pirates", "starwars", "tokyodrift",
    ];

    [Test]
    public void Help_prints_full_help_and_returns_zero()
    {
        var (exitCode, output, error) = Run("--help");

        Assert.That(exitCode, Is.Zero);
        Assert.That(error, Is.Empty);
        Assert.That(output.Length, Is.GreaterThan(1000));
        Assert.That(output, Does.Contain("BEEPER CORE"));
        Assert.That(output, Does.Contain("USAGE"));
        Assert.That(output, Does.Contain("CONTROLS"));
        Assert.That(output, Does.Contain("EXAMPLES"));
        Assert.That(output, Does.Contain("REQUIREMENTS"));
        Assert.That(output, Does.Contain(AppVersion.Current));
        Assert.That(Regex.IsMatch(output, "\x1b"), Is.False);
    }

    [Test]
    public void List_prints_every_track_and_returns_zero()
    {
        var (exitCode, output, error) = Run("--list");

        Assert.That(exitCode, Is.Zero);
        Assert.That(error, Is.Empty);
        Assert.That(output, Does.Contain("TRACKLIST"));
        Assert.That(output, Does.Not.Contain("BROKEN TRACKS"));
        foreach (string key in TrackKeys)
            Assert.That(Regex.IsMatch(output, @"┃\s+" + Regex.Escape(key) + @"\s+▸"), Is.True, $"missing track '{key}'");
    }

    [TestCase("--help", "help")]
    [TestCase("--list", "list")]
    public void Cli_output_matches_approved_snapshot(string argument, string snapshot)
    {
        var (exitCode, output, error) = Run(argument);

        Assert.That(exitCode, Is.Zero);
        Assert.That(error, Is.Empty);

        string actual = NormalizeNewlines(output);
        string expected = NormalizeNewlines(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "snapshots", snapshot + ".approved.txt")));

        Assert.That(actual, Is.EqualTo(expected),
            $"output of '{argument}' changed — review the diff before re-approving {snapshot}.approved.txt");

        Assert.That(Regex.IsMatch(actual, "\x1b"), Is.False,
            "redirected CLI output must not contain ANSI escape codes");
    }

    [Test]
    public void Interactive_exits_cleanly_on_eof_with_redirected_input()
    {
        var startInfo = new ProcessStartInfo(FindExecutable())
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("cannot start CLI");
        process.StandardInput.Close();
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();

        bool exited = process.WaitForExit(10_000);
        if (!exited) process.Kill();
        Assert.That(exited, Is.True, "interactive CLI hung on EOF");

        string output = outputTask.GetAwaiter().GetResult();
        string error = errorTask.GetAwaiter().GetResult();

        Assert.That(process.ExitCode, Is.Zero);
        Assert.That(error, Is.Empty);
        Assert.That(output, Does.Contain("session closed"));
        Assert.That(Regex.IsMatch(output, "\x1b"), Is.False,
            "redirected interactive output must not contain ANSI escape codes");
    }

    [Test]
    public void Unknown_track_returns_one_and_explains_error()
    {
        var (exitCode, output, error) = Run("nosuchtrack");

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.That(error, Is.Empty);
        Assert.That(output, Does.Contain("not found: 'nosuchtrack'"));
        Assert.That(output, Does.Contain("use --list to see what actually exists"));
        Assert.That(Regex.IsMatch(output, "\x1b"), Is.False);
    }

    [Test]
    public void Empty_play_returns_one_and_explains_error()
    {
        var (exitCode, output, error) = Run("--play");

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.That(error, Is.Empty);
        Assert.That(output, Does.Contain("no tracks specified"));
        Assert.That(output, Does.Contain("try --list"));
        Assert.That(Regex.IsMatch(output, "\x1b"), Is.False);
    }

    private static string NormalizeNewlines(string text) => text.Replace("\r\n", "\n");

    private static (int ExitCode, string Output, string Error) Run(params string[] args)
    {
        var startInfo = new ProcessStartInfo(FindExecutable())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (string arg in args)
            startInfo.ArgumentList.Add(arg);

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("cannot start CLI");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output, error);
    }

    private static string FindExecutable()
    {
        var outputDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        string configuration = outputDirectory.Parent!.Name;
        for (DirectoryInfo? directory = outputDirectory; directory is not null; directory = directory.Parent)
        {
            string candidate = Path.Combine(
                directory.FullName, "src", "DoomPcSpeaker", "bin", configuration, "net9.0", "DoomPcSpeaker.exe");
            if (File.Exists(candidate))
                return candidate;
        }
        throw new FileNotFoundException("DoomPcSpeaker.exe not found in build output");
    }
}
