using System.Numerics;
using static WaitingSim.Tests.SimTests;

namespace WaitingSim.Tests;

public sealed class CounterTests
{
    [Fact]
    public void Stepping_up_to_the_counter_at_the_front_of_the_line_is_reported_once()
    {
        var sim = new Sim(Snappy, Straight, seed: 1);
        var player = sim.Spawn(new(1, 0));

        var events = Run(sim, 120, snapshot => Commands((player, snapshot.Agents[player.Value].Position.X > 0.1f ? TowardFront : Vector2.Zero)));

        var reached = Assert.Single(events.OfType<ReachedCounter>());
        Assert.Equal(player, reached.Agent);
        Assert.Equal(reached.Tick, Agent(sim, player).ReachedCounterAt);
        Assert.True(sim.Snapshot().Agents[player.Value].Position.X < Snappy.Zones.BumpBelow);
    }

    [Fact]
    public void Standing_back_from_the_counter_has_not_reached_it()
    {
        var sim = new Sim(Snappy, Straight, seed: 1);
        var player = sim.Spawn(new(0.5f, 0));

        var events = Run(sim, 60, _ => Commands((player, Vector2.Zero)));

        Assert.DoesNotContain(events, e => e is ReachedCounter);
        Assert.Null(Agent(sim, player).ReachedCounterAt);
    }

    [Fact]
    public void Someone_still_ahead_keeps_the_counter_from_being_reached()
    {
        var sim = new Sim(Snappy, Straight, seed: 1);
        var ahead = sim.Spawn(new(0, 0));
        var player = sim.Spawn(new(0.3f, 0));

        var events = Run(sim, 60, _ => Commands((player, Vector2.Zero)));

        Assert.All(events.OfType<ReachedCounter>(), e => Assert.Equal(ahead, e.Agent));
        Assert.Null(Agent(sim, player).ReachedCounterAt);
    }

    [Fact]
    public void Standing_beside_the_counter_out_of_the_lane_has_not_reached_it()
    {
        var sim = new Sim(Snappy, Straight, seed: 1);
        var player = sim.Spawn(new(0, 1));

        var events = Run(sim, 60, _ => Commands((player, Vector2.Zero)));

        Assert.DoesNotContain(events, e => e is ReachedCounter);
    }

    [Fact]
    public void An_agent_tallies_each_gap_it_lets_run_past_grace_and_each_bump()
    {
        var sim = new Sim(Snappy, Straight, seed: 1);
        var ahead = sim.Spawn(new(5, 0));
        var player = sim.Spawn(new(5.7f, 0));

        Run(sim, 200, _ => Commands((ahead, TowardFront)));
        Run(sim, 260, snapshot => Commands((player, snapshot.Agents[player.Value].Zone == Zone.Bump ? Vector2.Zero : TowardFront)));

        Assert.Equal(new Tally(Bumps: 1, GapsLeftOpen: 1, PlacesLost: 0, CuttersLetIn: 0, MissedCutIns: 0, CutIns: 0), Agent(sim, player).Tally);
        Assert.Equal(default, Agent(sim, ahead).Tally);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0, null)]
    [InlineData(3, 0, 0, 0, 0, 0, Mistake.Bump)]
    [InlineData(3, 1, 0, 0, 0, 0, Mistake.GapLeftOpen)]
    [InlineData(3, 1, 1, 0, 0, 0, Mistake.LostPlace)]
    [InlineData(3, 1, 1, 1, 0, 0, Mistake.LetCutterIn)]
    [InlineData(3, 1, 1, 1, 1, 0, Mistake.MissedCutIn)]
    [InlineData(0, 0, 0, 0, 1, 1, Mistake.CutIn)]
    public void The_worst_mistake_is_the_most_serious_made_at_least_once(int bumps, int gaps, int places, int cutters, int missed, int cutIns, Mistake? worst)
    {
        Assert.Equal(worst, new Tally(bumps, gaps, places, cutters, missed, cutIns).Worst);
    }
}
