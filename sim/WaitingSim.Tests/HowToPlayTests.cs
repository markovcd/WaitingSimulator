using System.Numerics;
using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class HowToPlayTests
{
    private static readonly GameData Game = GameData.Load(Path.Combine(AppContext.BaseDirectory, "data"), "post-office").Value!;

    [Fact]
    public void How_to_play_names_the_controls_and_the_goal()
    {
        Assert.Contains("WASD", HowToPlay.Text, StringComparison.Ordinal);
        Assert.Contains("KEEP UP WITH THE LINE", HowToPlay.Text, StringComparison.Ordinal);
        Assert.Contains("GAPS", HowToPlay.Text, StringComparison.Ordinal);
        Assert.Contains("BUMP", HowToPlay.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void How_to_play_shows_at_the_start_of_the_level()
    {
        Assert.True(HowToPlay.Shown(Game.Level.Start(Game.Tuning).Snapshot(), Game.Level));
    }

    [Fact]
    public void How_to_play_hides_once_the_player_walks_off_their_spot()
    {
        var sim = Game.Level.Start(Game.Tuning);
        var back = -Game.Level.Lane.TowardFront(Game.Level.Agents[Game.Level.Player.Value].Position);

        while (HowToPlay.Shown(sim.Snapshot(), Game.Level) && sim.Tick < HowToPlay.ShownTicks)
        {
            sim.Step(new Dictionary<AgentId, Command> { [Game.Level.Player] = new(back) });
        }

        Assert.True(sim.Tick < HowToPlay.ShownTicks, "How to play stayed up while the player walked.");
    }

    [Fact]
    public void How_to_play_hides_after_a_while_standing_still()
    {
        var sim = Game.Level.Start(Game.Tuning);

        while (sim.Tick < HowToPlay.ShownTicks)
        {
            Assert.True(HowToPlay.Shown(sim.Snapshot(), Game.Level), $"How to play hid at tick {sim.Tick}.");
            sim.Step(new Dictionary<AgentId, Command> { [Game.Level.Player] = new(Vector2.Zero) });
        }

        Assert.False(HowToPlay.Shown(sim.Snapshot(), Game.Level));
    }
}
