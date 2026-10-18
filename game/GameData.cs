using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>What the client plays: the tuning and a level, read from a data folder laid out as the repo's <c>data/</c>.</summary>
internal sealed record GameData(Tuning Tuning, Level Level)
{
    /// <summary>Reads the tuning, the traits and <c>levels/<paramref name="level"/>.json</c>. Problems name the file and field.</summary>
    public static Parsed<GameData> Load(string folder, string level)
    {
        var problems = new List<string>();
        var tuning = Read(Path.Combine(folder, "tuning.json"), Tuning.Parse, problems);
        var traits = Read(Path.Combine(folder, "traits.json"), Traits.Parse, problems);
        var read = traits is null ? null : Read(Path.Combine(folder, "levels", level + ".json"), json => Level.Parse(json, traits), problems);
        return tuning is not null && read is not null ? new(new GameData(tuning, read), []) : new(null, problems);
    }

    /// <summary>Reads and parses the file at <paramref name="path"/>, or adds what is wrong with it, naming the file, and returns null.</summary>
    internal static T? Read<T>(string path, Func<string, Parsed<T>> parse, List<string> problems)
        where T : class
    {
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            problems.Add($"{path}: {exception.Message}");
            return null;
        }

        var parsed = parse(json);
        problems.AddRange(parsed.Problems.Select(problem => $"{path}: {problem}"));
        return parsed.Value;
    }
}
