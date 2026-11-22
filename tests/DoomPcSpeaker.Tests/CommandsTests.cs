using NUnit.Framework;

[TestFixture]
internal sealed class CommandsTests
{
    [TestCase("-h", Command.Help)]
    [TestCase("--HELP", Command.Help)]
    [TestCase("-l", Command.List)]
    [TestCase("--list", Command.List)]
    [TestCase("-s", Command.Stop)]
    [TestCase("--stop", Command.Stop)]
    [TestCase("-p", Command.Play)]
    [TestCase("--play", Command.Play)]
    [TestCase("help", Command.Unknown)]
    [TestCase("", Command.Unknown)]
    public void MatchArgument_returns_expected_command(string argument, Command expected)
    {
        Assert.That(Commands.MatchArgument(argument), Is.EqualTo(expected));
    }

    [TestCase("help", Command.Help)]
    [TestCase("H", Command.Help)]
    [TestCase("list", Command.List)]
    [TestCase("l", Command.List)]
    [TestCase("stop", Command.Stop)]
    [TestCase("s", Command.Stop)]
    [TestCase("clear", Command.Clear)]
    [TestCase("c", Command.Clear)]
    [TestCase("quit", Command.Quit)]
    [TestCase("q", Command.Quit)]
    [TestCase("--list", Command.Unknown)]
    [TestCase("random", Command.Unknown)]
    public void MatchInteractive_returns_expected_command(string input, Command expected)
    {
        Assert.That(Commands.MatchInteractive(input), Is.EqualTo(expected));
    }

    [Test]
    public void StripPlayFlag_removes_only_leading_play_flag()
    {
        Assert.That(Commands.StripPlayFlag(["--play", "doom", "tetris"]), Is.EqualTo(new[] { "doom", "tetris" }));
        Assert.That(Commands.StripPlayFlag(["doom", "--play"]), Is.EqualTo(new[] { "doom", "--play" }));
        Assert.That(Commands.StripPlayFlag([]), Is.Empty);
    }

    [Test]
    public void StripPlayPrefix_handles_absent_and_empty_flag()
    {
        Assert.That(Commands.StripPlayPrefix("--play tetris"), Is.EqualTo("tetris"));
        Assert.That(Commands.StripPlayPrefix("-p"), Is.Empty);
        Assert.That(Commands.StripPlayPrefix("tetris"), Is.EqualTo("tetris"));
    }

    [TestCase("", 0)]
    [TestCase("   ", 0)]
    [TestCase("random", 1)]
    [TestCase("R", 1)]
    [TestCase("doom,tetris", 2)]
    [TestCase("doom; tetris , sonic", 3)]
    [TestCase("doom tetris", 2)]
    [TestCase("doom\t tetris", 2)]
    public void SmartSplit_returns_expected_queries(string input, int expectedCount)
    {
        Assert.That(Commands.SmartSplit(input).Length, Is.EqualTo(expectedCount));
    }

    [Test]
    public void QueriesFromArgs_splits_delimiters_across_arguments()
    {
        Assert.That(Commands.QueriesFromArgs(["doom,tetris", "sonic;mario"]),
            Is.EqualTo(new[] { "doom", "tetris", "sonic", "mario" }));
    }

    [Test]
    public void QueriesFromArgs_treats_single_argument_as_smart_split()
    {
        Assert.That(Commands.QueriesFromArgs(["doom,tetris"]), Is.EqualTo(new[] { "doom", "tetris" }));
        Assert.That(Commands.QueriesFromArgs([]), Is.Empty);
    }

    [Test]
    public void Suggestions_match_available_commands_for_each_mode()
    {
        var unlocked = Commands.Suggestions(locked: false);
        var locked = Commands.Suggestions(locked: true);

        Assert.That(unlocked.Select(s => s.Name), Does.Contain("random"));
        Assert.That(locked.Select(s => s.Name), Does.Not.Contain("random"));
        Assert.That(locked.Select(s => s.Name), Does.Contain("quit"));
        Assert.That(unlocked.All(s => !string.IsNullOrWhiteSpace(s.Description)), Is.True);
        Assert.That(locked.All(s => !string.IsNullOrWhiteSpace(s.Description)), Is.True);
    }

    [Test]
    public void TrackSuggestions_finds_single_match_case_insensitively()
    {
        var matches = Commands.TrackSuggestions("NOK");

        Assert.That(matches, Has.Length.EqualTo(1));
        Assert.That(matches[0].Name, Is.EqualTo("nokia"));
        Assert.That(matches[0].Description, Is.EqualTo("Nokia Tune"));
    }

    [Test]
    public void TrackSuggestions_returns_all_prefix_matches()
    {
        var matches = Commands.TrackSuggestions("m");

        Assert.That(matches.Select(s => s.Name), Is.EquivalentTo(new[] { "mario", "mk", "mi" }));
        Assert.That(matches.All(s => !string.IsNullOrWhiteSpace(s.Description)), Is.True);
    }

    [Test]
    public void TrackSuggestions_returns_empty_for_unknown_prefix()
    {
        Assert.That(Commands.TrackSuggestions("zzz"), Is.Empty);
    }

    [TestCase("nok", 0)]
    [TestCase("doom nok", 5)]
    [TestCase("doom, nok", 6)]
    [TestCase("doom;nok", 5)]
    [TestCase("", 0)]
    public void CurrentTokenStart_points_to_last_query(string input, int expected)
    {
        Assert.That(Commands.CurrentTokenStart(input), Is.EqualTo(expected));
    }
}
