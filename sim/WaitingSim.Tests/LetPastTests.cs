using System.Numerics;
using static WaitingSim.Tests.SimTests;

namespace WaitingSim.Tests;

public sealed class LetPastTests
{
    private const float Recovery = 10f;

    // Staring that drains one point a tick, for a person behind who has lost some patience to win back.
    private const int StaredTicks = 20;
    private const float StaredPerSecond = TickRate.PerSecond;

    private static readonly Trait Plain = new("plain", 1, 1, 1, new HashSet<Stage>(Enum.GetValues<Stage>()));

    private static readonly Tuning Polite = Snappy with
    {
        StepOut = new StepOutTuning(300, 10, 20, 35, 15),
        LetPast = new LetPastTuning(Recovery),
    };

    [Fact]
    public void Waving_the_person_behind_past_from_a_held_place_puts_them_ahead_and_the_place_behind_them()
    {
        var (sim, lead, player, behind, last) = Line(Polite);
        StepOut(sim, player);

        var events = Run(sim, 1, _ => Wave(player));

        var waved = Assert.Single(events.OfType<WavedPast>());
        Assert.Equal((behind, player), (waved.Agent, waved.By));
        Assert.Equal(lead, Agent(sim, behind).Ahead);
        Assert.Equal(behind, Agent(sim, player).Ahead);
        Assert.Equal(player, Agent(sim, last).Ahead);
    }

    [Fact]
    public void The_person_waved_past_gets_patience_back()
    {
        var (sim, _, player, behind, _) = Line(Polite with { Staring = new StaringTuning(1, StaredPerSecond) });
        Run(sim, StaredTicks, _ => new() { [player] = new Command(Vector2.Zero, GlancingBack: true) });
        StepOut(sim, player);

        Run(sim, 1, _ => Wave(player));

        Assert.Equal(PatienceTuning.Full - StaredTicks + Recovery, Agent(sim, behind).Patience!.Value, 0.001f);
    }

    [Fact]
    public void The_line_walks_the_person_waved_past_up_into_the_place()
    {
        var (sim, lead, player, behind, _) = Line(Polite with { Ripple = new RippleTuning(new(6000, 6000), new(15, 15), new(0.8f, 0.8f), 0.2f, 1f) });
        StepOut(sim, player);
        Run(sim, 1, _ => Wave(player));

        Run(sim, 3 * TickRate.PerSecond, _ => Commands((player, Vector2.Zero)));

        Assert.Equal(lead, Agent(sim, behind).Ahead);
        Assert.InRange(Agent(sim, behind).Spacing!.Value, 0.7f, 0.9f);
    }

    [Fact]
    public void Stepping_back_in_behind_the_person_waved_past_is_returning_to_the_place()
    {
        var (sim, _, player, behind, _) = Line(Polite);
        StepOut(sim, player);
        Run(sim, 1, _ => Wave(player));

        var events = Run(sim, 180, snapshot => Toward(player, snapshot, new(5f, snapshot.Agents[player.Value].Position.X < 4.9f ? 1f : 0f)));

        Assert.DoesNotContain(events, e => e is CutIn or CutInMissed or SpotLost);
        Assert.True(Agent(sim, player).InLane, "The player never stepped back into the lane.");
        Assert.Null(Agent(sim, player).HeldAlong);
        Assert.Equal(behind, Agent(sim, player).Ahead);
    }

    [Fact]
    public void Waving_from_inside_the_lane_does_nothing()
    {
        var (sim, lead, player, behind, _) = Line(Polite);

        var events = Run(sim, 1, _ => Wave(player));

        Assert.DoesNotContain(events, e => e is WavedPast);
        Assert.Equal(player, Agent(sim, behind).Ahead);
        Assert.Equal(lead, Agent(sim, player).Ahead);
    }

    [Fact]
    public void Without_a_let_past_tuning_waving_does_nothing()
    {
        var (sim, _, player, behind, _) = Line(Polite with { LetPast = null });
        StepOut(sim, player);

        var events = Run(sim, 1, _ => Wave(player));

        Assert.DoesNotContain(events, e => e is WavedPast);
        Assert.Equal(player, Agent(sim, behind).Ahead);
    }

    /// <summary>A still line, everyone standing loose: someone ahead, the player, a plain person behind, and one more behind them.</summary>
    private static (Sim Sim, AgentId Lead, AgentId Player, AgentId Behind, AgentId Last) Line(Tuning tuning)
    {
        var sim = new Sim(tuning, Straight, seed: 1);
        var lead = sim.Spawn(new(2, 0));
        var player = sim.Spawn(new(3.2f, 0));
        var behind = sim.Spawn(new(4.4f, 0), Plain);
        var last = sim.Spawn(new(5.6f, 0), Plain);
        return (sim, lead, player, behind, last);
    }

    /// <summary>Walks the player sideways out of the lane, holding their place, and stops them there.</summary>
    private static void StepOut(Sim sim, AgentId player)
    {
        Run(sim, 40, _ => Commands((player, new Vector2(0, 1))));
        Run(sim, 5, _ => Commands((player, Vector2.Zero)));
        Assert.False(Agent(sim, player).InLane);
        Assert.NotNull(Agent(sim, player).HeldAlong);
    }

    private static Dictionary<AgentId, Command> Wave(AgentId player) => new() { [player] = new Command(Vector2.Zero, LetPast: true) };

    /// <summary>Walks the player toward <paramref name="target"/>, standing once there.</summary>
    private static Dictionary<AgentId, Command> Toward(AgentId player, Snapshot snapshot, Vector2 target)
    {
        var toward = target - snapshot.Agents[player.Value].Position;
        return Commands((player, toward.Length() < 0.05f ? Vector2.Zero : Vector2.Normalize(toward)));
    }
}
