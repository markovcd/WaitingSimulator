using System.Numerics;
using static WaitingSim.Tests.SimTests;

namespace WaitingSim.Tests;

public sealed class ArrivalTests
{
    private const int IntervalTicks = 600;

    private static readonly Trait Calm = new("calm", DrainScale: 0, RecoveryScale: 0, BumpScale: 1, new HashSet<Stage>(Enum.GetValues<Stage>()));

    private static readonly Tuning Arriving = Snappy with
    {
        WalkAcceleration = 8f,
        Ripple = new RippleTuning(new(6000, 6000), new(15, 15), new(0.8f, 0.8f), 0.2f, 1f),
        Arrivals = new ArrivalTuning(new(IntervalTicks, IntervalTicks)),
    };

    // A lane 12 m long running back along +x, with the entrance off to its side beyond the back.
    private static readonly Lane Long = new([new(0, 0), new(12, 0)], halfWidth: 0.5f);

    private static readonly Vector2 Entrance = new(13, 2);

    [Fact]
    public void Without_an_entrance_nobody_arrives()
    {
        var sim = new Sim(Arriving, Long, seed: 1);
        sim.Spawn(new(0, 0), Calm);

        var events = Run(sim, 10 * IntervalTicks, _ => Commands());

        Assert.DoesNotContain(events, e => e is Arrived);
        Assert.Single(sim.Snapshot().Agents);
    }

    [Fact]
    public void Newcomers_arrive_at_the_entrance_every_interval_and_join_the_back_of_the_line()
    {
        var sim = Line(seed: 1);

        var events = Run(sim, (3 * IntervalTicks) + 500, _ => Commands());

        var arrived = events.OfType<Arrived>().ToList();
        Assert.Equal(3, arrived.Count);
        Assert.Equal([IntervalTicks, 2 * IntervalTicks, 3 * IntervalTicks], arrived.Select(e => e.Tick));
        Assert.DoesNotContain(events, e => e is CutIn or CutInMissed);
        var agents = sim.Snapshot().Agents;
        Assert.All(arrived, e => Assert.True(agents[e.Agent.Value] is { InLane: true, Refused: false, Step: LineStep.Standing }, $"{e.Agent} did not join."));
        Assert.Equal(arrived[1].Agent, agents[arrived[2].Agent.Value].Ahead);
        Assert.Equal(arrived[0].Agent, agents[arrived[1].Agent.Value].Ahead);
    }

    [Fact]
    public void Newcomers_follow_the_seed()
    {
        Assert.Equal(Arrivals(seed: 3), Arrivals(seed: 3));
    }

    [Fact]
    public void A_newcomer_is_drawn_from_the_traits_and_the_interval_range()
    {
        var tuning = Arriving with { Arrivals = new ArrivalTuning(new(60, 600)) };
        var ticks = Enumerable.Range(1, 5).Select(seed => Arrivals((ulong)seed, tuning).First().Tick).ToList();

        Assert.All(ticks, tick => Assert.InRange(tick, 60, 600));
        Assert.True(ticks.Distinct().Count() > 1, "Every seed drew the same interval.");
    }

    [Fact]
    public void Nobody_arrives_while_the_back_of_the_line_is_near_the_end_of_the_lane()
    {
        var events = Run(FullLine(Arriving with { Ripple = null }), 5 * IntervalTicks, _ => Commands());

        Assert.DoesNotContain(events, e => e is Arrived);
    }

    [Fact]
    public void Newcomers_arrive_again_once_the_line_moves_up()
    {
        var events = Run(FullLine(Arriving), 5 * IntervalTicks, _ => Commands());

        Assert.Contains(events, e => e is Arrived);
    }

    /// <summary>A line spread out to the end of <see cref="Long"/>, with no room left to join behind it.</summary>
    private static Sim FullLine(Tuning tuning)
    {
        var sim = new Sim(tuning, Long, seed: 1);
        foreach (var x in new[] { 0f, 1.5f, 3f, 4.5f, 6f, 7.5f, 9f, 11f })
        {
            sim.Spawn(new(x, 0), Calm);
        }

        sim.ArriveAt(Entrance, [Calm]);
        return sim;
    }

    [Fact]
    public void Every_newcomer_is_a_cutter_at_a_share_of_one_until_the_cap_and_none_at_zero()
    {
        var all = Arrivals(seed: 1, Arriving with { Arrivals = new ArrivalTuning(new(60, 60), CutterShare: 1f, MaxCutters: 1) });
        var none = Arrivals(seed: 1, Arriving with { Arrivals = new ArrivalTuning(new(60, 60), CutterShare: 0f, MaxCutters: 1) });

        Assert.True(all[0].Cutter);
        Assert.Contains(all, e => !e.Cutter);
        Assert.DoesNotContain(none, e => e.Cutter);
    }

    [Fact]
    public void Arriving_cutters_come_at_their_share_and_follow_the_seed()
    {
        var tuning = Arriving with { Arrivals = new ArrivalTuning(new(IntervalTicks, IntervalTicks), CutterShare: 0.5f, MaxCutters: 10) };
        var cutters = Enumerable.Range(1, 20).Select(seed => Arrivals((ulong)seed, tuning).First().Cutter).ToList();

        Assert.InRange(cutters.Count(cutter => cutter), 4, 16);
        Assert.Equal(Arrivals(seed: 7, tuning), Arrivals(seed: 7, tuning));
    }

    [Fact]
    public void An_arriving_cutter_walks_up_beside_the_line_and_with_no_gap_gives_up_and_joins_the_back()
    {
        // One newcomer before the run ends.
        var sim = Line(seed: 1, Arriving with { Arrivals = new ArrivalTuning(new(1500, 1500), CutterShare: 1f, MaxCutters: 1), Cutter = new CutterTuning(60, 40) });
        var prowling = new List<AgentSnapshot>();

        var events = Run(sim, 2900, snapshot =>
        {
            prowling.AddRange(snapshot.Agents.Where(agent => agent.Step == LineStep.Prowling));
            return Commands();
        });

        var cutter = Assert.Single(events.OfType<Arrived>()).Agent;
        Assert.NotEmpty(prowling);
        Assert.All(prowling, agent => Assert.False(agent.InLane, $"The cutter stepped into the lane at {agent.Position}."));
        Assert.True(prowling.Min(agent => agent.Position.X) < 2f, "The cutter never walked up to the front.");
        Assert.DoesNotContain(events, e => e is CutterEntered);
        Assert.True(Agent(sim, cutter) is { InLane: true, Refused: false, Step: LineStep.Standing }, "The cutter never joined the back.");
        Assert.Equal(new AgentId(2), Agent(sim, cutter).Ahead);
    }

    [Fact]
    public void An_arriving_cutter_slides_into_a_gap_left_open_too_long()
    {
        var tuning = Arriving with
        {
            Arrivals = new ArrivalTuning(new(300, 300), CutterShare: 1f, MaxCutters: 1),
            Cutter = new CutterTuning(60, 40),
            Ripple = Arriving.Ripple! with { ReactionTicks = new(6000, 6000) },
        };
        var sim = new Sim(tuning, Long, seed: 1);
        sim.Spawn(new(0, 0), Calm);
        var behindGap = sim.Spawn(new(3, 0), Calm);
        sim.Spawn(new(3.8f, 0), Calm);
        sim.ArriveAt(Entrance, [Calm]);

        var events = Run(sim, 1800, _ => Commands());

        var cutter = events.OfType<Arrived>().First().Agent;
        Assert.Contains(events, e => e is CutterEntered entered && entered.Agent == cutter && entered.Behind == behindGap);
    }

    /// <summary>A line of three standing at the front of <see cref="Long"/>, newcomers coming every interval.</summary>
    private static Sim Line(ulong seed, Tuning? tuning = null)
    {
        var sim = new Sim(tuning ?? Arriving, Long, seed);
        sim.Spawn(new(0, 0), Calm);
        sim.Spawn(new(0.8f, 0), Calm);
        sim.Spawn(new(1.6f, 0), Calm);
        sim.ArriveAt(Entrance, [Calm]);
        return sim;
    }

    private static List<Arrived> Arrivals(ulong seed, Tuning? tuning = null) =>
        [.. Run(Line(seed, tuning), 1200, _ => Commands()).OfType<Arrived>()];
}
