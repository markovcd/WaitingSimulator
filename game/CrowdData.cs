using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>What the client plays in a crowd: the tuning and a crowd level, read from a data folder laid out as the repo's <c>data/</c>.</summary>
internal sealed record CrowdData(Tuning Tuning, CrowdLevel Level)
{
    /// <summary>Reads the tuning, the traits and <c>levels/<paramref name="level"/>.json</c>. Problems name the file and field.</summary>
    public static Parsed<CrowdData> Load(string folder, string level)
    {
        var problems = new List<string>();
        var tuningPath = Path.Combine(folder, "tuning.json");
        var tuning = GameData.Read(tuningPath, Tuning.Parse, problems);
        var traits = GameData.Read(Path.Combine(folder, "traits.json"), Traits.Parse, problems);
        var read = traits is null ? null : GameData.Read(Path.Combine(folder, "levels", level + ".json"), json => CrowdLevel.Parse(json, traits), problems);
        if (tuning is { Crowd: null } or { Bodies: null })
        {
            problems.Add($"{tuningPath}: a crowd level needs the crowd and bodies sections");
        }

        return tuning is not null && read is not null && problems.Count == 0 ? new(new CrowdData(tuning, read), []) : new(null, problems);
    }
}
