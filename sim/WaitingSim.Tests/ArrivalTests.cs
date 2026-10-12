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
