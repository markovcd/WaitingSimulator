namespace WaitingSimulator.Game;

/// <summary>Re-reading the data files mid-game: the new data and a notice saying so, or no data and the problems that kept it.</summary>
internal static class TuningReload
{
    public static (GameData? Data, string Notice) Try(string folder, string level)
    {
        var loaded = GameData.Load(folder, level);
        return loaded.Value is { } data
            ? (data, "TUNING RELOADED")
            : (null, string.Join('\n', loaded.Problems.Prepend("TUNING NOT RELOADED")));
    }
}
