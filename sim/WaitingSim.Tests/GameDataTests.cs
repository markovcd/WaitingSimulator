using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class GameDataTests
{
    private static readonly string Data = Path.Combine(AppContext.BaseDirectory, "data");

    [Fact]
    public void The_level_the_client_plays_loads_from_the_shipped_data()
    {
        var loaded = GameData.Load(Data, Client.LevelName);

        Assert.Empty(loaded.Problems);
        Assert.NotNull(loaded.Value);
    }

    [Fact]
    public void A_missing_level_is_reported_with_its_path()
    {
        var loaded = GameData.Load(Data, "nowhere");

        Assert.Null(loaded.Value);
        Assert.StartsWith(Path.Combine(Data, "levels", "nowhere.json") + ": ", Assert.Single(loaded.Problems), StringComparison.Ordinal);
    }
}
