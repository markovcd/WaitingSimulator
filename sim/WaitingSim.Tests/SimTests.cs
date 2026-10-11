using System.Numerics;

namespace WaitingSim.Tests;

public sealed class SimTests
{
    private const int GraceWindowTicks = 90;

    // Walking at 1.5 m/s reached in a single tick: the person ahead moves 0.025 m per tick.
    private static readonly Tuning Snappy = new(GraceWindowTicks, new ZoneLimits(0.4f, 0.9f, 1.5f), WalkSpeed: 1.5f, WalkAcceleration: 90f);

    // Front of the line at the origin, the line running back along +x.
    private static readonly Lane Straight = new([new(0, 0), new(20, 0)], halfWidth: 0.5f);

    private static readonly Vector2 TowardFront = new(-1, 0);

    [Fact]
    public void Walking_speeds_up_by_the_acceleration_each_tick()
    {
        var sim = new Sim(Snappy with { WalkAcceleration = 3f }, Straight);
        var agent = sim.Spawn(new(5, 0));

        sim.Step(Commands((agent, TowardFront)));

        Assert.Equal(-3f / TickRate.PerSecond, Agent(sim, agent).Velocity.X, 0.00001f);
    }

    [Fact]
    public void A_command_longer_than_one_walks_no_faster_than_walking_speed()
    {
        var sim = new Sim(Snappy, Straight);
        var agent = sim.Spawn(new(5, 0));

        for (var i = 0; i < 10; i++)
        {
            sim.Step(Commands((agent, new Vector2(-30, 0))));
        }

        Assert.Equal(1.5f, Agent(sim, agent).Velocity.Length(), 0.0001f);
    }

    [Fact]
    public void A_command_that_is_not_a_number_is_standing_still()
    {
        var sim = new Sim(Snappy, Straight);
        var agent = sim.Spawn(new(5, 0));

        sim.Step(Commands((agent, new Vector2(float.NaN, 0))));

        Assert.Equal(new Vector2(5, 0), Agent(sim, agent).Position);
    }

    [Fact]
    public void A_command_for_an_agent_that_does_not_exist_is_refused()
    {
        var sim = new Sim(Snappy, Straight);

        Assert.Throws<ArgumentException>(() => sim.Step(Commands((new AgentId(3), TowardFront))));
    }

    [Fact]
    public void The_front_of_the_line_has_nobody_ahead_and_no_zone()
    {
        var sim = new Sim(Snappy, Straight);
        var front = sim.Spawn(new(5, 0));
        var behind = sim.Spawn(new(5.7f, 0));

        Assert.Null(Agent(sim, front).Zone);
        Assert.Equal(front, Agent(sim, behind).Ahead);
        Assert.Equal(Zone.Tight, Agent(sim, behind).Zone);
    }

    [Fact]
    public void A_gap_opens_through_the_zones_as_the_person_ahead_walks_off()
    {
        var sim = new Sim(Snappy, Straight);
        var ahead = sim.Spawn(new(5, 0));
        var player = sim.Spawn(new(5.7f, 0));

        var events = Run(sim, 40, _ => Commands((ahead, TowardFront)));

        Assert.Equal(
            [(Zone.Tight, Zone.Loose), (Zone.Loose, Zone.Gap)],
            events.OfType<ZoneChanged>().Where(e => e.Agent == player).Select(e => (e.From, e.To)).ToList());
    }

    [Fact]
    public void The_grace_window_expires_exactly_its_length_after_the_gap_opens_and_only_once()
    {
        var sim = new Sim(Snappy, Straight);
        var ahead = sim.Spawn(new(5, 0));
        var player = sim.Spawn(new(5.7f, 0));

        var events = Run(sim, 200, _ => Commands((ahead, TowardFront)));

        var opened = events.OfType<ZoneChanged>().Single(e => e.Agent == player && e.To == Zone.Gap).Tick;
        var expired = Assert.Single(events.OfType<GraceExpired>());
        Assert.Equal(player, expired.Agent);
        Assert.Equal(opened + GraceWindowTicks, expired.Tick);
    }

    [Fact]
    public void A_gap_closed_within_the_grace_window_never_expires()
    {
        var sim = new Sim(Snappy, Straight);
        var ahead = sim.Spawn(new(5, 0));
        var player = sim.Spawn(new(5.7f, 0));
        var gapSeen = false;

        var events = Run(sim, 200, snapshot =>
        {
            var self = snapshot.Agents[player.Value];
            gapSeen |= self.Zone == Zone.Gap;
            return gapSeen
                ? Commands((player, self.Spacing > 0.7f ? TowardFront : Vector2.Zero))
                : Commands((ahead, TowardFront));
        });

        Assert.Contains(events, e => e is ZoneChanged { To: Zone.Gap });
        Assert.DoesNotContain(events, e => e is GraceExpired);
        Assert.Equal(Zone.Tight, Agent(sim, player).Zone);
    }

    [Fact]
    public void Stepping_out_of_the_lane_leaves_the_line_and_stepping_back_rejoins_it()
    {
        var sim = new Sim(Snappy, Straight);
        sim.Spawn(new(5, 0));
        var player = sim.Spawn(new(5.7f, 0));

        var leaving = Run(sim, 30, _ => Commands((player, new Vector2(0, 1))));
        var outside = Agent(sim, player);
        var returning = Run(sim, 30, _ => Commands((player, new Vector2(0, -1))));

        Assert.Equal([typeof(LeftLane), typeof(ZoneChanged)], leaving.Select(e => e.GetType()).ToList());
        Assert.Equal(new ZoneChanged(leaving[0].Tick, player, Zone.Tight, null), leaving[1]);
        Assert.False(outside.InLane);
        Assert.Null(outside.Zone);
        Assert.Equal([typeof(ReenteredLane), typeof(ZoneChanged)], returning.Select(e => e.GetType()).ToList());
        Assert.True(Agent(sim, player).InLane);
    }

    [Fact]
    public void The_same_commands_play_out_the_same_way()
    {
        var first = Played(seed: 11);
        var second = Played(seed: 11);

        Assert.Equal(first.Events, second.Events);
        Assert.Equal(first.States.Count, second.States.Count);
        for (var i = 0; i < first.States.Count; i++)
        {
            Assert.Equal(first.States[i], second.States[i]);
        }
    }

    /// <summary>Three agents in a line, each walking somewhere random every half second for a minute.</summary>
    private static (List<SimEvent> Events, List<IReadOnlyList<AgentSnapshot>> States) Played(ulong seed)
    {
        var sim = new Sim(Snappy with { WalkAcceleration = 3f }, Straight);
        AgentId[] ids = [sim.Spawn(new(2, 0)), sim.Spawn(new(2.6f, 0)), sim.Spawn(new(3.3f, 0))];
        var rng = new Rng(seed);
        var commands = new Dictionary<AgentId, Command>();
        var events = new List<SimEvent>();
        var states = new List<IReadOnlyList<AgentSnapshot>>();
        for (var tick = 0; tick < 60 * TickRate.PerSecond; tick++)
        {
            if (tick % 30 == 0)
            {
                foreach (var id in ids)
                {
                    commands[id] = new(new Vector2(rng.NextBelow(201) - 100, rng.NextBelow(201) - 100) / 100f);
                }
            }

            events.AddRange(sim.Step(commands));
            states.Add(sim.Snapshot().Agents);
        }

        return (events, states);
    }

    private static List<SimEvent> Run(Sim sim, int ticks, Func<Snapshot, Dictionary<AgentId, Command>> commands)
    {
        var events = new List<SimEvent>();
        for (var i = 0; i < ticks; i++)
        {
            events.AddRange(sim.Step(commands(sim.Snapshot())));
        }

        return events;
    }

    private static Dictionary<AgentId, Command> Commands(params (AgentId Agent, Vector2 Move)[] moves) =>
        moves.ToDictionary(move => move.Agent, move => new Command(move.Move));

    private static AgentSnapshot Agent(Sim sim, AgentId id) => sim.Snapshot().Agents[id.Value];
}
