using System.Numerics;
using static WaitingSim.Tests.SimTests;

namespace WaitingSim.Tests;

public sealed class KickOutTests
{
    // Patience that runs out a moment after the grace window does.
    private static readonly Trait Short = new("short", DrainScale: 100, RecoveryScale: 1, BumpScale: 1, new HashSet<Stage>(Enum.GetValues<Stage>()));

    [Fact]
    public void Running_out_of_patience_pushes_past_the_person_ahead_who_loses_their_place()
    {
        var sim = new Sim(Snappy, Straight, seed: 1);
        var lead = sim.Spawn(new(5, 0));
        var player = sim.Spawn(new(5.7f, 0));
        var behind = sim.Spawn(new(6.4f, 0), Short);

        var events = Run(sim, 200, snapshot => Commands((lead, snapshot.Agents[lead.Value].Position.X > 3 ? TowardFront : Vector2.Zero)));
        Run(sim, 1, _ => Commands());

        var ranOut = events.OfType<PatienceRanOut>().Single();
        Assert.Equal(new KickedOut(ranOut.Tick, player, behind), events.OfType<KickedOut>().Single());
        Assert.True(Agent(sim, player) is { Refused: true, Ahead: null });
        Assert.Equal(ranOut.Tick, Agent(sim, player).KickedOutAt);
        Assert.Equal(lead, Agent(sim, behind).Ahead);
    }

    [Fact]
    public void A_place_held_out_of_the_lane_is_lost_to_a_kick_out()
    {
        var tuning = Snappy with { StepOut = new StepOutTuning(600, 10, 20, 30, 15) };
        var sim = new Sim(tuning, Straight, seed: 1);
        var lead = sim.Spawn(new(5, 0));
        var player = sim.Spawn(new(5.7f, 0));
        var behind = sim.Spawn(new(6.4f, 0), Short);
        Run(sim, 30, _ => Commands((player, new Vector2(0, 1))));

        var events = Run(sim, 200, snapshot => Commands((lead, snapshot.Agents[lead.Value].Position.X > 3 ? TowardFront : Vector2.Zero)));
        Run(sim, 1, _ => Commands());

        Assert.Contains(events, e => e is KickedOut kicked && kicked.Agent == player);
        Assert.Null(Agent(sim, player).SpotHeldUntil);
        Assert.Equal(lead, Agent(sim, behind).Ahead);
    }

    [Fact]
    public void An_agent_the_line_moves_walks_off_once_kicked_out()
    {
        var tuning = Snappy with { WalkAcceleration = 3f, Ripple = new RippleTuning(new(6000, 6000), new(30, 30), new(0.7f, 0.7f), 0.2f, 1f) };
        var sim = new Sim(tuning, Straight, seed: 1);
        var lead = sim.Spawn(new(5, 0));
        var slow = sim.Spawn(new(5.7f, 0));
        var behind = sim.Spawn(new(6.4f, 0), Short);

        var events = Run(sim, 400, snapshot =>
            snapshot.Agents[slow.Value].KickedOutAt is null
                ? Commands((lead, snapshot.Agents[lead.Value].Position.X > 3 ? TowardFront : Vector2.Zero), (slow, Vector2.Zero), (behind, Vector2.Zero))
                : Commands((lead, Vector2.Zero), (behind, Vector2.Zero)));

        Assert.Contains(events, e => e is KickedOut kicked && kicked.Agent == slow);
        Assert.Equal(LineStep.Done, Agent(sim, slow).Step);
        Assert.False(Agent(sim, slow).InLane);
    }
}
