internal enum Command
{
    Unknown,
    Help,
    List,
    Stop,
    Clear,
    Quit,
    Play,
}

internal sealed record Suggestion(string Name, string Description);

internal static class Commands
{
    private static readonly char[] Delimiters = [',', ';'];
    private static readonly char[] Whitespaces = [' ', '\t'];

    private static readonly (string[] Names, Command Command)[] ArgumentCommands =
    [
        (["-h", "--help"], Command.Help),
        (["-l", "--list"], Command.List),
        (["-s", "--stop"], Command.Stop),
        (["--play", "-p"], Command.Play),
    ];

    private static readonly (string[] Names, Command Command)[] InteractiveCommands =
    [
        (["help", "h"], Command.Help),
        (["list", "l"], Command.List),
        (["stop", "s"], Command.Stop),
        (["clear", "c"], Command.Clear),
        (["quit", "q"], Command.Quit),
    ];

    private static readonly Suggestion[] UnlockedSuggestions =
    [
        new("list", "show tracklist"),
        new("random", "play a random track"),
        new("stop", "silence speaker"),
        new("clear", "clear screen"),
        new("help", "show help"),
        new("quit", "exit app"),
    ];

    private static readonly Suggestion[] LockedSuggestions =
    [
        new("list", "show tracklist"),
        new("stop", "silence speaker"),
        new("clear", "clear screen"),
        new("help", "show help"),
        new("quit", "exit app"),
    ];

    public static Suggestion[] Suggestions(bool locked) => locked ? LockedSuggestions : UnlockedSuggestions;

    public static Suggestion[] TrackSuggestions(string query) =>
        Melodies.All
            .Where(song => song.Key.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .Select(song => new Suggestion(song.Key, song.Name))
            .ToArray();

    public static int CurrentTokenStart(string input)
    {
        for (int i = input.Length - 1; i >= 0; i--)
        {
            if (char.IsWhiteSpace(input[i]) || input[i] is ',' or ';')
                return i + 1;
        }
        return 0;
    }

    public static Command MatchArgument(string arg) => Match(argument: true, arg);

    public static Command MatchInteractive(string input) => Match(argument: false, input);

    public static string[] StripPlayFlag(string[] args) =>
        args.Length > 0 && MatchArgument(args[0]) == Command.Play ? args[1..] : args;

    public static string StripPlayPrefix(string input)
    {
        var parts = input.Split(Whitespaces, 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length > 0 && MatchArgument(parts[0]) == Command.Play)
            return parts.Length > 1 ? parts[1] : string.Empty;
        return input;
    }

    public static string[] QueriesFromArgs(string[] args)
    {
        var effective = StripPlayFlag(args);
        if (effective.Length == 0) return [];
        if (effective.Length == 1) return SmartSplit(effective[0]);

        if (effective.Any(a => a.IndexOfAny(Delimiters) >= 0))
            return effective
                .SelectMany(a => a.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToArray();

        // Several shell arguments can form one multi-word track key (e.g. "prince igor").
        string joined = string.Join(" ", effective).Trim();
        if (joined.Length > 0 && Melodies.Resolve(joined) is not null &&
            !effective.All(a => Melodies.Resolve(a) is not null))
            return [joined];

        return effective;
    }

    public static string[] SmartSplit(string input)
    {
        input = input.Trim();
        if (input.Length == 0) return [];

        if (input.IndexOfAny(Delimiters) >= 0)
            return input.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (Melodies.IsRandom(input) || Melodies.Resolve(input) is not null)
            return [input];

        var parts = input.Split(Whitespaces, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 1 ? parts : [input];
    }

    private static Command Match(bool argument, string value)
    {
        var definitions = argument ? ArgumentCommands : InteractiveCommands;
        foreach (var (names, command) in definitions)
        {
            foreach (string name in names)
            {
                if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
                    return command;
            }
        }
        return Command.Unknown;
    }
}
