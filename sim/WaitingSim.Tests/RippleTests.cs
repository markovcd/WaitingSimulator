using System.Numerics;
using static WaitingSim.Tests.SimTests;

namespace WaitingSim.Tests;

public sealed class RippleTests
{
    private const int ServiceTicks = 120;
    private const int ReactionTicks = 30;
    private const float Spacing = 0.8f;
    private const float NoticeMeters = 0.2f;
    private const float LeaveMeters = 1f;

    // Fixed durations and spacing, and walking that speeds up and slows down like the shipped tuning.
    private static readonly Tuning Rippling = Snappy with
    {
        WalkAcceleration = 3f,
        Ripple = new RippleTuning(new(ServiceTicks, ServiceTicks), new(ReactionTicks, ReactionTicks), new(Spacing, Spacing), NoticeMeters, LeaveMeters),
    };

    [Fact]
    public void Without_a_ripple_an_agent_given_no_command_stands_still()
    {
        var sim = new Sim(Snappy, Straight, seed: 1);
        var agent = sim.Spawn(new(1, 0));

        Idle(sim, 10 * TickRate.PerSecond);

        Assert.Equal(new Vector2(1, 0), Agent(sim, agent).Position);
        Assert.Equal(LineStep.Standing, Agent(sim, agent).Step);
    }

    [Fact]
    public void The_front_of_the_line_walks_up_to_the_counter_and_is_served()
    {
        var sim = new Sim(Rippling, Straight, seed: 1);
        var front = sim.Spawn(new(1, 0));

        var steps = Idle(sim, 3 * TickRate.PerSecond).Select(snapshot => snapshot.Agents[front.Value].Step).Distinct();

        Assert.Equal([LineStep.Noticed, LineStep.SteppingUp, LineStep.Standing, LineStep.BeingServed], steps);
        Assert.InRange(Agent(sim, front).Position.X, -NoticeMeters, NoticeMeters);
    }

    [Fact]
    public void A_served_agent_leaves_the_lane_to_the_side_once_its_service_ends()
    {
        var sim = new Sim(Rippling, Straight, seed: 1);
        var front = sim.Spawn(new(0, 0));
        var events = new List<SimEvent>();

        var served = Idle(sim, ServiceTicks + 1, events).Count(snapshot => snapshot.Agents[front.Value].Step == LineStep.BeingServed);
        Idle(sim, 5 * TickRate.PerSecond, events);

        Assert.Equal(ServiceTicks, served);
        Assert.Equal(LineStep.Done, Agent(sim, front).Step);
        Assert.IsType<LeftLane>(Assert.Single(events));
        Assert.True(Straight.Measure(Agent(sim, front).Position).Off >= Straight.HalfWidth + LeaveMeters);
        Assert.Equal(Vector2.Zero, Agent(sim, front).Velocity);
    }

    [Fact]
    public void Each_agent_steps_up_its_reaction_delay_after_it_notices()
    {
        var sim = new Sim(Rippling, Straight, seed: 1);
        var agent = sim.Spawn(new(2, 0));

        var steps = Idle(sim, TickRate.PerSecond).Select(snapshot => snapshot.Agents[agent.Value].Step).ToList();

        Assert.Equal(ReactionTicks, steps.IndexOf(LineStep.SteppingUp) - steps.IndexOf(LineStep.Noticed));
    }

    [Fact]
    public void The_ripple_travels_back_one_person_at_a_time()
    {
        var sim = new Sim(Rippling, Straight, seed: 1);
        var line = Enumerable.Range(0, 5).Select(i => sim.Spawn(new(i * Spacing, 0))).ToList();

        var history = Idle(sim, 10 * TickRate.PerSecond);

        var started = line.Skip(1).Select(id => history.FindIndex(snapshot => snapshot.Agents[id.Value].Step == LineStep.SteppingUp)).ToList();
        Assert.DoesNotContain(-1, started);
        for (var i = 1; i < started.Count; i++)
        {
            Assert.True(started[i] >= started[i - 1] + ReactionTicks, $"agent {i + 1} stepped up at tick {started[i]}, the one ahead at {started[i - 1]}");
        }
    }

    [Fact]
    public void An_agent_that_steps_up_stops_at_the_spacing_it_likes()
    {
        var tuning = Rippling with { Ripple = Rippling.Ripple! with { ServiceTicks = new(600, 600) } };
        var sim = new Sim(tuning, Straight, seed: 1);
        var line = Enumerable.Range(0, 5).Select(i => sim.Spawn(new(1 + (i * Spacing), 0))).ToList();

        Idle(sim, 8 * TickRate.PerSecond);

        Assert.Equal(LineStep.BeingServed, Agent(sim, line[0]).Step);
        Assert.All(line.Skip(1), id =>
        {
            var agent = Agent(sim, id);
            Assert.Equal(LineStep.Standing, agent.Step);
            Assert.InRange(agent.Spacing!.Value, Spacing - 0.05f, Spacing + NoticeMeters);
        });
    }

    [Fact]
    public void A_line_that_moves_up_by_itself_never_bumps()
    {
        var tuning = Rippling with { Ripple = Rippling.Ripple! with { ServiceTicks = new(60, 240), ReactionTicks = new(0, 60), SpacingMeters = new(0.6f, 1.1f) } };
        var sim = new Sim(tuning, Straight, seed: 1);
        var line = Enumerable.Range(0, 8).Select(i => sim.Spawn(new(i * Spacing, 0))).ToList();

        var history = Idle(sim, 60 * TickRate.PerSecond);

        Assert.DoesNotContain(history, snapshot => snapshot.Agents.Any(agent => agent.Zone == Zone.Bump));
        Assert.True(Agent(sim, line[^1]).Position.X < Spacing, "The back of the line has reached the front.");
    }

    [Fact]
    public void An_agent_given_a_command_is_not_moved_by_the_line()
    {
        var sim = new Sim(Rippling, Straight, seed: 1);
        var player = sim.Spawn(new(3, 0));

        for (var tick = 0; tick < 5 * TickRate.PerSecond; tick++)
        {
            sim.Step(Commands((player, Vector2.Zero)));
        }

        Assert.Equal(new Vector2(3, 0), Agent(sim, player).Position);
        Assert.Equal(LineStep.Standing, Agent(sim, player).Step);
    }

    [Fact]
    public void The_person_behind_steps_up_after_the_player_does()
    {
        var sim = new Sim(Rippling, Straight, seed: 1);
        var player = sim.Spawn(new(3, 0));
        var behind = sim.Spawn(new(3 + Spacing, 0));

        Run(sim, 5 * TickRate.PerSecond, snapshot => Commands((player, snapshot.Agents[player.Value].Position.X > 2f ? TowardFront : Vector2.Zero)));

        Assert.InRange(Agent(sim, behind).Spacing!.Value, Spacing - 0.05f, Spacing + NoticeMeters);
    }

    [Fact]
    public void Reaction_delays_and_spacings_vary_within_their_tuned_ranges()
    {
        var tuning = Rippling with { Ripple = Rippling.Ripple! with { ReactionTicks = new(15, 60), SpacingMeters = new(0.6f, 1.1f) } };
        var sim = new Sim(tuning, Straight, seed: 1);
        var line = Enumerable.Range(0, 12).Select(i => sim.Spawn(new(i * 1.1f, 0))).ToList();

        var history = Idle(sim, 20 * TickRate.PerSecond);

        var delays = line.Skip(1).Select(id =>
        {
            var steps = history.Select(snapshot => snapshot.Agents[id.Value].Step).ToList();
            return steps.IndexOf(LineStep.SteppingUp) - steps.IndexOf(LineStep.Noticed);
        }).ToList();
        Assert.All(delays, delay => Assert.InRange(delay, 15, 60));
        Assert.True(delays.Distinct().Count() > 1, "Every agent reacted in the same time.");
    }

    [Fact]
    public void What_one_agent_does_never_changes_the_luck_of_another()
    {
        var tuning = Rippling with { Ripple = Rippling.Ripple! with { ServiceTicks = new(60, 240), ReactionTicks = new(15, 60) } };
        var commanded = Ahead(tuning, commandBehind: true);
        var left = Ahead(tuning, commandBehind: false);

        Assert.Equal(commanded, left);
    }

    [Fact]
    public void The_same_seed_ripples_the_same_way_and_another_seed_does_not()
    {
        var tuning = Rippling with { Ripple = Rippling.Ripple! with { ServiceTicks = new(60, 240), ReactionTicks = new(15, 60), SpacingMeters = new(0.6f, 1.1f) } };

        Assert.Equal(Hashes(tuning, seed: 5), Hashes(tuning, seed: 5));
        Assert.NotEqual(Hashes(tuning, seed: 5)[^1], Hashes(tuning, seed: 6)[^1]);
    }

    /// <summary>
    /// Where the four agents at the front of a line stand each tick for 30 s, with the fifth either walking off
    /// backwards on command or left to the line.
    /// </summary>
    private static List<Vector2> Ahead(Tuning tuning, bool commandBehind)
    {
        var sim = new Sim(tuning, Straight, seed: 3);
        var line = Enumerable.Range(0, 5).Select(i => sim.Spawn(new(i * Spacing, 0))).ToList();
        var positions = new List<Vector2>();
        for (var tick = 0; tick < 30 * TickRate.PerSecond; tick++)
        {
            sim.Step(commandBehind ? Commands((line[^1], -TowardFront)) : Commands());
            positions.AddRange(line.SkipLast(1).Select(id => Agent(sim, id).Position));
        }

        return positions;
    }

    private static List<ulong> Hashes(Tuning tuning, ulong seed)
    {
        var sim = new Sim(tuning, Straight, seed);
        for (var i = 0; i < 6; i++)
        {
            sim.Spawn(new(i * Spacing, 0));
        }

        return [.. Enumerable.Range(0, 20 * TickRate.PerSecond).Select(_ =>
        {
            sim.Step(Commands());
            return sim.StateHash();
        })];
    }

    /// <summary>Steps <paramref name="ticks"/> ticks with nobody commanded, and returns the snapshot after each.</summary>
    private static List<Snapshot> Idle(Sim sim, int ticks, List<SimEvent>? events = null)
    {
        var history = new List<Snapshot>();
        for (var i = 0; i < ticks; i++)
        {
            var happened = sim.Step(Commands());
            events?.AddRange(happened);
            history.Add(sim.Snapshot());
        }

        return history;
    }
}
