using System.Numerics;
using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class CrowdHudTests
{
    private static readonly AgentId Player = new(1);

    [Fact]
    public void Hard_shows_nothing_even_while_the_clerk_asks_someone_who_came_later()
    {
        Assert.Equal("", CrowdHud.Text(Asking(new AgentId(2)), Player, CrowdTests.Rules, Difficulty.Hard));
    }

    [Fact]
    public void Normal_shows_only_that_the_clerk_asked_someone_who_came_after_the_player()
    {
        Assert.Equal("ASKED SOMEONE AFTER YOU", CrowdHud.Text(Asking(new AgentId(2)), Player, CrowdTests.Rules, Difficulty.Normal));
        Assert.Equal("", CrowdHud.Text(Asking(new AgentId(0)), Player, CrowdTests.Rules, Difficulty.Normal));
        Assert.Equal("", CrowdHud.Text(Asking(null), Player, CrowdTests.Rules, Difficulty.Normal));
    }

    [Fact]
    public void Easy_counts_who_arrived_before_the_player_names_the_lowest_patience_nearby_and_the_key_to_call_out()
    {
        var text = CrowdHud.Text(Asking(new AgentId(2)), Player, CrowdTests.Rules, Difficulty.Easy);

        Assert.Equal("ARRIVED BEFORE YOU: 1\nLOWEST PATIENCE NEARBY: 40%\nCLERK ASKED SOMEONE WHO CAME AFTER YOU: E TO SAY YOU WERE FIRST", text);
    }

    /// <summary>Three agents in arrival order, the player in the middle, with the clerk asking <paramref name="picked"/>.</summary>
    private static Snapshot Asking(AgentId? picked) => new(
        100,
        [Agent(0, new(0, 1), 40f), Agent(1, new(1, 1), null), Agent(2, new(2, 1), 80f)],
        new ClerkSnapshot(Vector2.Zero, picked, null));

    private static AgentSnapshot Agent(int id, Vector2 at, float? patience) =>
        new(new AgentId(id), at, Vector2.Zero, false, null, null, null, null, patience is null ? null : "plain", patience, patience is null ? null : Stage.Calm, LineStep.Standing, null, false, default, null, null);
}
