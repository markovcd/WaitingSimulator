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
    public void The_overlay_shows_each_agent_s_zone_spacing_step_and_patience_and_names_the_player()
    {
        var snapshot = Game.Level.Start(Game.Tuning).Snapshot();

        var rows = Overlay.Text(snapshot, Game.Level, Game.Tuning).Split('\n');

        Assert.Equal(1 + snapshot.Agents.Count, rows.Length);
        Assert.All(rows, row => Assert.Equal(row.TrimEnd(), row));
        var player = rows[1 + Game.Level.Player.Value];
        Assert.StartsWith($"#{Game.Level.Player.Value} player ", player, StringComparison.Ordinal);
        Assert.DoesNotContain("step", player, StringComparison.Ordinal);
        var withTrait = rows[1 + snapshot.Agents.First(agent => agent.Trait is not null).Id.Value];
        Assert.Contains("zone tight  spacing 0.80 m", withTrait, StringComparison.Ordinal);
        Assert.Contains("step standing", withTrait, StringComparison.Ordinal);
        Assert.EndsWith("patience 100.0  stage calm", withTrait, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(100, 100, "1.50 s")]
    [InlineData(100, 160, "0.50 s")]
    [InlineData(100, 190, "expired")]
    public void A_gap_shows_the_grace_left_until_it_runs_out(long opened, long tick, string grace)
    {
        var agent = new AgentSnapshot(new AgentId(0), Vector2.Zero, Vector2.Zero, true, new AgentId(1), 2f, Zone.Gap, opened, null, null, null, LineStep.Standing, null, false);

        var text = Overlay.Text(new Snapshot(tick, [agent]), Game.Level, Game.Tuning);

        Assert.Contains($"grace {grace}", text, StringComparison.Ordinal);
    }

    [Fact]
    public void An_agent_out_of_the_lane_with_its_place_held_shows_how_long_the_hold_has_left()
    {
        var agent = new AgentSnapshot(new AgentId(0), Vector2.Zero, Vector2.Zero, false, null, 2f, Zone.Gap, null, null, null, null, LineStep.Standing, 250, false);

        var text = Overlay.Text(new Snapshot(100, [agent]), Game.Level, Game.Tuning);

        Assert.Contains("out of the lane  spot held 2.50 s", text, StringComparison.Ordinal);
        Assert.DoesNotContain("zone", text, StringComparison.Ordinal);
    }

    [Fact]
    public void An_agent_refused_a_place_says_so_instead_of_a_zone()
    {
        var agent = new AgentSnapshot(new AgentId(0), Vector2.Zero, Vector2.Zero, true, null, null, null, null, null, null, null, LineStep.Standing, null, true);

        var text = Overlay.Text(new Snapshot(1, [agent]), Game.Level, Game.Tuning);

        Assert.Contains("refused a place", text, StringComparison.Ordinal);
        Assert.DoesNotContain("zone", text, StringComparison.Ordinal);
    }

    [Fact]
    public void An_agent_out_of_the_lane_says_so_instead_of_a_zone()
    {
        var agent = new AgentSnapshot(new AgentId(0), Vector2.Zero, Vector2.Zero, false, null, null, null, null, null, null, null, LineStep.Standing, null, false);

        var text = Overlay.Text(new Snapshot(1, [agent]), Game.Level, Game.Tuning);

        Assert.EndsWith("out of the lane  step standing", text, StringComparison.Ordinal);
        Assert.DoesNotContain("zone", text, StringComparison.Ordinal);
    }
}
