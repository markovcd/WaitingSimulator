using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// Which level the game plays: the one <c>-- --level &lt;name&gt;</c> names, or the post office, and the next one in
/// <c>data/levels/</c> when F6 asks for it. The choice outlives the scene, so a crowd level and the line swap scenes without losing it.
/// </summary>
internal static class Levels
{
    /// <summary>The level played when none is named.</summary>
    public const string Default = "post-office";

    /// <summary>The level being played, once one is chosen.</summary>
    public static string? Playing { get; set; }

    /// <summary>The level <paramref name="args"/> names after <c>--level</c>, or the default; a bad name is added to <paramref name="problems"/>.</summary>
    public static string From(IReadOnlyList<string> args, List<string> problems)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(problems);
        var at = args.ToList().IndexOf("--level");
        if (at < 0)
        {
            return Default;
        }

        if (at + 1 < args.Count && IsName(args[at + 1]))
        {
            return args[at + 1];
        }

        problems.Add($"--level: {(at + 1 < args.Count ? $"\"{args[at + 1]}\" is not a level name" : "names no level")}; playing {Default}");
        return Default;
    }

    /// <summary>The level after <paramref name="current"/> in <paramref name="folder"/>'s <c>levels/</c>, by name, back to the first after the last.</summary>
    public static string Next(string folder, string current)
    {
        var names = Directory.Exists(Path.Combine(folder, "levels"))
            ? Directory.GetFiles(Path.Combine(folder, "levels"), "*.json").Select(Path.GetFileNameWithoutExtension).OfType<string>().Where(IsName).Order(StringComparer.Ordinal).ToList()
            : [];
        return names.Count == 0 ? current : names.FirstOrDefault(name => string.CompareOrdinal(name, current) > 0) ?? names[0];
    }

    /// <summary>Whether level <paramref name="name"/> in <paramref name="folder"/> is a crowd level; one that cannot be read is not, and loading it reports why.</summary>
    public static bool IsCrowd(string folder, string name)
    {
        try
        {
            return Level.IsCrowd(File.ReadAllText(Path.Combine(folder, "levels", name + ".json")));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>A level file's name: lowercase letters, digits and dashes, so it can only name a file in the levels folder.</summary>
    private static bool IsName(string name) =>
        name.Length > 0 && name.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-');
}
