using System.Numerics;
using static WaitingSim.Tests.SimTests;

namespace WaitingSim.Tests;

public sealed class StepOutTests
{
    private const int HoldTicks = 120;
    private const float PastGraceHit = 10;
    private const float GapHit = 20;
    private const float LooseHit = 30;
    private const float MissHit = 15;

    private const float PassedHit = 25;

    private static readonly Tuning SteppingOut = Snappy with { StepOut = new StepOutTuning(HoldTicks, PastGraceHit, GapHit, LooseHit, MissHit) };

    private static readonly Tuning Passing = Snappy with { StepOut = new StepOutTuning(HoldTicks, PastGraceHit, GapHit, LooseHit, MissHit, PassedHit) };

    private static readonly Vector2 Aside = new(0, 1);

    // Patience that neither drains nor recovers, so only one-off hits move it.
    private static readonly Trait Still = new("still", DrainScale: 0, RecoveryScale: 0, BumpScale: 1, new HashSet<Stage>(Enum.GetValues<Stage>()));

    [Fact]
    public void Without_a_step_out_tuning_stepping_out_leaves_the_line_at_once()
    {
        var sim = new Sim(Snappy, Straight, seed: 1);
        sim.Spawn(new(5, 0));
        var player = sim.Spawn(new(5.8f, 0));
        var behind = sim.Spawn(new(6.6f, 0));

        StepOut(sim, player);

        Assert.NotEqual(player, Agent(sim, behind).Ahead);
    }

    [Fact]
    public void Stepping_out_holds_the_place_and_the_person_behind_stays_behind_it()
    {
        var sim = new Sim(SteppingOut, Straight, seed: 1);
        sim.Spawn(new(5, 0));
        var player = sim.Spawn(new(5.8f, 0));
        var behind = sim.Spawn(new(6.6f, 0));

        var left = StepOut(sim, player).OfType<LeftLane>().Single().Tick;

        Assert.False(Agent(sim, player).InLane);
        Assert.Equal(left + HoldTicks, Agent(sim, player).SpotHeldUntil);
        Assert.Equal(player, Agent(sim, behind).Ahead);
        Assert.Equal(0.8f, Agent(sim, behind).Spacing!.Value, 0.0001f);
    }

    [Fact]
    public void The_snapshot_says_where_the_held_place_is_until_the_hold_ends()
    {
        var sim = new Sim(SteppingOut, Straight, seed: 1);
        sim.Spawn(new(5, 0));
        var player = sim.Spawn(new(5.8f, 0));
        sim.Spawn(new(6.6f, 0));

        StepOut(sim, player);
        Assert.Equal(5.8f, Agent(sim, player).HeldAlong!.Value, 0.05f);

        Run(sim, HoldTicks, _ => Commands((player, Vector2.Zero)));
        Assert.Null(Agent(sim, player).HeldAlong);
    }

    [Fact]
    public void Stepping_back_in_ahead_of_the_same_person_returns_to_the_place()
    {
        var sim = new Sim(SteppingOut, Straight, seed: 1);
        sim.Spawn(new(5, 0));
        var player = sim.Spawn(new(5.8f, 0));
        var behind = sim.Spawn(new(6.6f, 0), Still);
        StepOut(sim, player);

        var events = StepIn(sim, player);

        Assert.Equal([typeof(ReenteredLane)], events.Where(e => e.Agent == player).Select(e => e.GetType()).ToList());
        Assert.Null(Agent(sim, player).SpotHeldUntil);
        Assert.Equal(player, Agent(sim, behind).Ahead);
        Assert.Equal(PatienceTuning.Full, Agent(sim, behind).Patience);
    }

    [Fact]
    public void A_place_held_for_the_whole_hold_is_lost()
    {
        var sim = new Sim(SteppingOut, Straight, seed: 1);
        var ahead = sim.Spawn(new(5, 0));
        var player = sim.Spawn(new(5.8f, 0));
        var behind = sim.Spawn(new(6.6f, 0));

        var events = StepOut(sim, player);
        events.AddRange(Run(sim, HoldTicks + 10, _ => Commands()));

        var left = events.OfType<LeftLane>().Single().Tick;
        Assert.Equal(left + HoldTicks, events.OfType<SpotLost>().Single(e => e.Agent == player).Tick);
        Assert.Null(Agent(sim, player).SpotHeldUntil);
        Assert.Equal(ahead, Agent(sim, behind).Ahead);
    }

    [Fact]
    public void A_gap_opening_at_a_held_place_drains_the_person_behind_past_its_grace_window()
    {
        var sim = new Sim(SteppingOut with { StepOut = SteppingOut.StepOut! with { SpotHoldTicks = 600 } }, Straight, seed: 1);
        var ahead = sim.Spawn(new(5, 0));
        var player = sim.Spawn(new(5.8f, 0));
        var behind = sim.Spawn(new(6.6f, 0), Still with { DrainScale = 1 });
        StepOut(sim, player);

        var events = Run(sim, 200, snapshot => Commands((ahead, snapshot.Agents[ahead.Value].Position.X > 3 ? TowardFront : Vector2.Zero)));

        Assert.Equal(Zone.Gap, Agent(sim, player).Zone);
        Assert.Contains(events, e => e is GraceExpired expired && expired.Agent == player);
        Assert.True(Agent(sim, behind).Patience < PatienceTuning.Full);
    }

    [Theory]
    [InlineData(2.4f, 0, GapHit)]
    [InlineData(2.4f, 100, PastGraceHit)]
    [InlineData(1.2f, 0, LooseHit)]
    public void Cutting_in_costs_the_person_behind_by_how_open_they_left_their_gap(float gap, int waitTicks, float hit)
    {
        var sim = new Sim(SteppingOut, Straight, seed: 1);
        var ahead = sim.Spawn(new(2, 0));
        var target = sim.Spawn(new(2 + gap, 0), Still);
        var player = sim.Spawn(new(2 + (gap / 2), 1));
        Run(sim, waitTicks, _ => Commands());

        var events = StepIn(sim, player);

        Assert.Equal(new CutIn(events.OfType<ReenteredLane>().Single().Tick, player, target), events.OfType<CutIn>().Single());
        Assert.Equal(player, Agent(sim, target).Ahead);
        Assert.Equal(ahead, Agent(sim, player).Ahead);
        Assert.Equal(PatienceTuning.Full - hit, Agent(sim, target).Patience!.Value, 0.0001f);
    }

    [Fact]
    public void Cutting_in_without_room_to_stand_is_refused_a_place_until_stepping_out()
    {
        var sim = new Sim(SteppingOut, Straight, seed: 1);
        var ahead = sim.Spawn(new(2, 0));
        var target = sim.Spawn(new(2.6f, 0), Still);
        var player = sim.Spawn(new(2.3f, 1));

        var events = StepIn(sim, player);
        var refused = Agent(sim, player);
        StepOut(sim, player);

        Assert.Single(events.OfType<CutInMissed>(), e => e.Agent == player && e.Behind == target);
        Assert.True(refused is { InLane: true, Refused: true, Ahead: null, Zone: null });
        Assert.Equal(ahead, Agent(sim, target).Ahead);
        Assert.Equal(PatienceTuning.Full - MissHit, Agent(sim, target).Patience!.Value, 0.0001f);
        Assert.False(Agent(sim, player).Refused);
        Assert.Null(Agent(sim, player).SpotHeldUntil);
    }

    [Fact]
    public void Stepping_in_at_the_back_of_the_line_is_not_cutting_in()
    {
        var sim = new Sim(SteppingOut, Straight, seed: 1);
        sim.Spawn(new(2, 0));
        var last = sim.Spawn(new(2.8f, 0));
        var player = sim.Spawn(new(4, 1));

        var events = StepIn(sim, player);

        Assert.DoesNotContain(events, e => e is CutIn or CutInMissed or SpotLost);
        Assert.Equal(last, Agent(sim, player).Ahead);
    }

    [Fact]
    public void Cutting_in_elsewhere_loses_the_held_place()
    {
        var sim = new Sim(SteppingOut, Straight, seed: 1);
        var front = sim.Spawn(new(2, 0));
        var target = sim.Spawn(new(4.4f, 0), Still);
        var player = sim.Spawn(new(5.2f, 0));
        var behind = sim.Spawn(new(6, 0));
        StepOut(sim, player);
        Run(sim, 90, snapshot => Commands((player, snapshot.Agents[player.Value].Position.X > 3.2f ? TowardFront : Vector2.Zero)));

        var events = StepIn(sim, player);

        Assert.Equal([typeof(ReenteredLane), typeof(CutIn), typeof(SpotLost)], events.Where(e => e.Agent == player && e is not ZoneChanged).Select(e => e.GetType()).ToList());
        Assert.Equal(front, Agent(sim, player).Ahead);
        Assert.Equal(target, Agent(sim, behind).Ahead);
    }

    [Fact]
    public void A_served_agent_walking_off_holds_no_place()
    {
        var tuning = SteppingOut with { Ripple = new RippleTuning(new(60, 60), new(15, 15), new(0.8f, 0.8f), 0.2f, 1f) };
        var sim = new Sim(tuning, Straight, seed: 1);
        var front = sim.Spawn(new(0, 0));

        var events = Run(sim, 4 * TickRate.PerSecond, _ => Commands());

        Assert.Contains(events, e => e is LeftLane && e.Agent == front);
        Assert.Null(Agent(sim, front).SpotHeldUntil);
        Assert.DoesNotContain(events, e => e is SpotLost);
    }

    [Fact]
    public void Walking_beside_the_line_and_cutting_in_at_the_front_gets_the_player_pushed_past()
    {
        var (sim, line, player) = LineOfFour(Passing);
        StepOut(sim, player);
        WalkUntil(sim, player, TowardFront, self => self.Position.X <= 0.05f);

        var events = StepIn(sim, player);

        Assert.Contains(events, e => e is KickedOut kicked && kicked.Agent == player && kicked.By == line[0]);
        Assert.DoesNotContain(events, e => e is ReachedCounter);
        Assert.All(line[1..], passed => Assert.Equal(PatienceTuning.Full - PassedHit, Agent(sim, passed).Patience!.Value, 0.0001f));
    }

    [Fact]
    public void Cutting_in_one_place_ahead_costs_the_person_passed_a_hit_they_survive()
    {
        var (sim, line, player) = LineOfFour(Passing);
        StepOut(sim, player);
        WalkUntil(sim, player, TowardFront, self => self.Position.X <= 3f);

        var events = StepIn(sim, player);

        Assert.Contains(events, e => e is CutIn cut && cut.Agent == player && cut.Behind == line[3]);
        Assert.DoesNotContain(events, e => e is KickedOut);
        Assert.Equal(PatienceTuning.Full - LooseHit - PassedHit, Agent(sim, line[3]).Patience!.Value, 0.0001f);
        Assert.Equal(PatienceTuning.Full, Agent(sim, line[2]).Patience!.Value, 0.0001f);
    }

    [Fact]
    public void Cutting_in_behind_the_held_place_passes_no_one()
    {
        var sim = new Sim(Passing, Straight, seed: 1);
        sim.Spawn(new(5, 0), Still);
        var player = sim.Spawn(new(6, 0));
        var behind = sim.Spawn(new(7, 0), Still);
        var further = sim.Spawn(new(8, 0), Still);
        StepOut(sim, player);
        WalkUntil(sim, player, -TowardFront, self => self.Position.X >= 7.5f);

        var events = StepIn(sim, player);

        Assert.Contains(events, e => e is CutIn cut && cut.Behind == further);
        Assert.Equal(PatienceTuning.Full - LooseHit, Agent(sim, further).Patience!.Value, 0.0001f);
        Assert.Equal(PatienceTuning.Full, Agent(sim, behind).Patience!.Value, 0.0001f);
    }

    /// <summary>Four people a meter apart from the counter back, and the player a meter behind them.</summary>
    private static (Sim Sim, AgentId[] Line, AgentId Player) LineOfFour(Tuning tuning)
    {
        var sim = new Sim(tuning, Straight, seed: 1);
        AgentId[] line = [.. Enumerable.Range(0, 4).Select(i => sim.Spawn(new(0.5f + i, 0), Still))];
        return (sim, line, sim.Spawn(new(4.5f, 0)));
    }

    /// <summary>Walks <paramref name="agent"/> aside until it is out of the lane, then stops it.</summary>
    private static List<SimEvent> StepOut(Sim sim, AgentId agent) => WalkUntil(sim, agent, Aside, self => !self.InLane);

    /// <summary>Walks <paramref name="agent"/> back toward the lane's line until it is in the lane, then stops it.</summary>
    private static List<SimEvent> StepIn(Sim sim, AgentId agent) => WalkUntil(sim, agent, -Aside, self => self.InLane);

    private static List<SimEvent> WalkUntil(Sim sim, AgentId agent, Vector2 move, Func<AgentSnapshot, bool> done)
    {
        var events = new List<SimEvent>();
        for (var tick = 0; tick < 10 * TickRate.PerSecond && !done(Agent(sim, agent)); tick++)
        {
            events.AddRange(sim.Step(Commands((agent, move))));
        }

        Assert.True(done(Agent(sim, agent)), "The agent never got there.");
        events.AddRange(sim.Step(Commands((agent, Vector2.Zero))));
        return events;
    }
}
