using System.Numerics;
using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class OverlayTests
{
    private static readonly string Data = Path.Combine(AppContext.BaseDirectory, "data");

    private static readonly GameData Game = GameData.Load(Data, "post-office").Value!;

    [Fact]
    public void The_overlay_heads_with_the_tick_and_the_level_seed()
    {
        var text = Overlay.Text(new Snapshot(42, []), Game.Level, Game.Tuning);

        Assert.Equal($"TICK 42  SEED {Game.Level.Seed}", text);
    }

    [Fact]
    public void The_overlay_shows_each_agent_s_zone_spacing_and_patience_and_names_the_player()
    {
        var snapshot = Game.Level.Start(Game.Tuning).Snapshot();

        var rows = Overlay.Text(snapshot, Game.Level, Game.Tuning).Split('\n');

        Assert.Equal(1 + snapshot.Agents.Count, rows.Length);
        Assert.All(rows, row => Assert.Equal(row.TrimEnd(), row));
        Assert.StartsWith($"#{Game.Level.Player.Value} player ", rows[1 + Game.Level.Player.Value], StringComparison.Ordinal);
        Assert.Contains("zone tight  spacing 0.80 m", rows[5], StringComparison.Ordinal);
        Assert.EndsWith("patience 100.0  stage calm", rows[5], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(100, 100, "1.50 s")]
    [InlineData(100, 160, "0.50 s")]
    [InlineData(100, 190, "expired")]
    public void A_gap_shows_the_grace_left_until_it_runs_out(long opened, long tick, string grace)
    {
        var agent = new AgentSnapshot(new AgentId(0), Vector2.Zero, Vector2.Zero, true, new AgentId(1), 2f, Zone.Gap, opened, null, null, null);

        var text = Overlay.Text(new Snapshot(tick, [agent]), Game.Level, Game.Tuning);

        Assert.Contains($"grace {grace}", text, StringComparison.Ordinal);
    }

    [Fact]
    public void An_agent_out_of_the_lane_says_so_instead_of_a_zone()
    {
        var agent = new AgentSnapshot(new AgentId(0), Vector2.Zero, Vector2.Zero, false, null, null, null, null, null, null, null);

        var text = Overlay.Text(new Snapshot(1, [agent]), Game.Level, Game.Tuning);

        Assert.EndsWith("out of the lane", text, StringComparison.Ordinal);
        Assert.DoesNotContain("zone", text, StringComparison.Ordinal);
    }
}
