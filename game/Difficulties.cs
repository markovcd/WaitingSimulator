namespace WaitingSimulator.Game;

/// <summary>Picking a <see cref="Difficulty"/>: from the command line, and the next one when the player cycles it.</summary>
internal static class Difficulties
{
    private const string Option = "--difficulty";

    /// <summary>The difficulty named after <c>--difficulty</c> in <paramref name="arguments"/>, or <paramref name="otherwise"/>; a name the game lacks is a problem.</summary>
    public static Difficulty From(IReadOnlyList<string> arguments, Difficulty otherwise, List<string> problems)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(problems);
        var at = arguments.ToList().IndexOf(Option);
        if (at < 0)
        {
            return otherwise;
        }

        if (at + 1 < arguments.Count && Enum.TryParse<Difficulty>(arguments[at + 1], ignoreCase: true, out var named) && Enum.IsDefined(named))
        {
            return named;
        }

        problems.Add($"{Option}: takes easy, normal or hard");
        return otherwise;
    }

    /// <summary>The difficulty's name as the command line takes it.</summary>
    public static string Name(Difficulty difficulty) => difficulty.ToString().ToLowerInvariant();

    /// <summary>The difficulty after <paramref name="difficulty"/>, back to the first after the last.</summary>
    public static Difficulty Next(Difficulty difficulty) => (Difficulty)(((int)difficulty + 1) % Enum.GetValues<Difficulty>().Length);
}
