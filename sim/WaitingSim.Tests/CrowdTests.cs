using System.Numerics;
using static WaitingSim.Tests.SimTests;

namespace WaitingSim.Tests;

public sealed class CrowdTests
{
    private const int PickTicks = 60;
    private const int ServiceTicks = 300;
    private const int LetThroughAfterTicks = 120;
    private const float ServedAheadHit = 15;
    private const float FalseClaimHit = 20;
    private const float SqueezeDrainPerSecond = 6;
    private const float Front = 0.18f;

    internal static readonly CrowdTuning Rules = new(
        GlanceMetersPerSecond: 1.5f,
        GlanceReachMeters: 0.35f,
        WaveReachMeters: 1.2f,
        WaveDrainPerSecond: 10,
        FrontMeters: 0.6f,
        PickTicks,
        new TickRange(ServiceTicks, ServiceTicks),
        ClerkMemoryTicks: 1800,
        SightMeters: 2.5f,
        ServedAheadHit,
        FalseClaimHit,
        SqueezeDrainPerSecond,
        LetThroughAfterTicks,
        PassTicks: 60,
        StandOffMeters: 0.15f,
        PressShare: 0.3f,
        SwayShare: 0.15f,
        new TickRange(30, 90),
        ShoveMetersPerSecond: 0.6f,
        Give: 0.25f,
        RecoveryPerSecond: 0.5f,
        new TickRange(600, 600),
        CutterShare: 0.15f);

    internal static readonly Tuning Crowding = Snappy with { WalkSpeed = 1.2f, WalkAcceleration = 8f, Bodies = new BodyTuning(0.18f, 0.5f), Crowd = Rules };

    // The counter along the x axis with the clerk behind it, so the crowd stands on +y; the glance starts at x = 0.
    internal static readonly Counter Bar = Counter.Between(new(-2, 0), new(2, 0), clerk: new(0, -1))!;

    // Patience that drains as the tuning says and never recovers, so what it lost stays lost.
    private static readonly Trait Plain = new("plain", DrainScale: 1, RecoveryScale: 0, BumpScale: 1, new HashSet<Stage>(Enum.GetValues<Stage>()));

    [Fact]
    public void The_glance_lands_on_whoever_it_passes_at_the_front_that_arrived_first()
    {
        var (crowd, ids) = Start((new(0.3f, Front), Plain), (new(0f, Front), Plain));

        var events = Hold(crowd, 1);

        Assert.Equal(ids[0], Assert.Single(events.OfType<Picked>()).Agent);
    }

    [Fact]
    public void The_glance_passes_over_someone_standing_back_from_the_counter()
    {
        var (crowd, _) = Start((new(0f, 1.5f), Plain));

        Assert.Empty(Hold(crowd, 200).OfType<Picked>());
    }

    [Fact]
    public void Waving_catches_the_glance_from_further_along_the_counter()
    {
        var (still, _) = Start((new(1f, Front), null));
        var (waving, ids) = Start((new(1f, Front), null));

        Assert.Empty(Hold(still, 1).OfType<Picked>());
        Assert.Equal(ids[0], Assert.Single(waving.Step(new Dictionary<AgentId, Command> { [ids[0]] = new(Vector2.Zero, Waving: true) }).OfType<Picked>()).Agent);
    }

    [Fact]
    public void Waving_drains_everyone_within_sight_of_the_waver()
    {
        var (crowd, ids) = Start((new(-1.5f, Front), null), (new(-1.5f, 1.5f), Plain), (new(1.5f, 5f), Plain));

        for (var i = 0; i < TickRate.PerSecond; i++)
        {
            crowd.Step(Idle(crowd, ids[0], new Command(Vector2.Zero, Waving: true)));
        }

        var agents = crowd.Snapshot().Agents;
        Assert.Equal(PatienceTuning.Full - Rules.WaveDrainPerSecond, agents[1].Patience!.Value, 0.01f);
        Assert.Equal(PatienceTuning.Full, agents[2].Patience!.Value, 0.0001f);
    }

    [Fact]
    public void The_clerk_serves_whom_the_glance_landed_on_once_they_have_asked()
    {
        var (crowd, ids) = Start((new(0f, Front), Plain));

        var events = Hold(crowd, 1 + PickTicks);

        var served = Assert.Single(events.OfType<ReachedCounter>());
        Assert.Equal((ids[0], 1L + PickTicks), (served.Agent, served.Tick));
        Assert.Equal(ids[0], crowd.Snapshot().Clerk!.Serving);
    }

    [Fact]
    public void Being_served_ahead_of_someone_who_arrived_first_costs_them_if_they_see_it()
    {
        // Arrival order: the near one, the far one, then the one served.
        var (crowd, ids) = Start((new(0f, 1.5f), Plain), (new(0f, 6f), Plain), (new(0f, Front), null));

        var events = Hold(crowd, 1 + PickTicks);

        var agents = crowd.Snapshot().Agents;
        Assert.Equal(new ServedAhead(1 + PickTicks, ids[2], ids[0]), Assert.Single(events.OfType<ServedAhead>()));
        Assert.Equal(PatienceTuning.Full - ServedAheadHit, agents[0].Patience!.Value, 0.0001f);
        Assert.Equal(PatienceTuning.Full, agents[1].Patience!.Value, 0.0001f);
        Assert.Equal(1, agents[2].Tally.ServedAhead);
    }

    [Fact]
    public void Calling_out_while_the_clerk_asks_someone_who_arrived_later_turns_the_clerk_to_you()
    {
        var (crowd, ids) = Start((new(-1.5f, Front), null), (new(0f, Front), Plain));
        Hold(crowd, 1);

        var events = crowd.Step(Idle(crowd, ids[0], new Command(Vector2.Zero, CallOut: true)));

        Assert.Equal(new CalledOut(2, ids[0], ids[1], Honest: true), Assert.Single(events.OfType<CalledOut>()));
        Assert.Equal(ids[0], crowd.Snapshot().Clerk!.Picked);
        Assert.Equal(ids[0], Assert.Single(Hold(crowd, PickTicks).OfType<ReachedCounter>()).Agent);
    }

    [Fact]
    public void Calling_out_without_having_arrived_first_costs_everyone_within_sight_and_the_clerk_stays_put()
    {
        var (crowd, ids) = Start((new(0f, Front), Plain), (new(-0.6f, 1.5f), Plain), (new(-0.6f, Front), null));
        Hold(crowd, 1);

        var events = crowd.Step(Idle(crowd, ids[2], new Command(Vector2.Zero, CallOut: true)));

        var agents = crowd.Snapshot().Agents;
        Assert.False(Assert.Single(events.OfType<CalledOut>()).Honest);
        Assert.Equal(ids[0], crowd.Snapshot().Clerk!.Picked);
        Assert.Equal(PatienceTuning.Full - FalseClaimHit, agents[0].Patience!.Value, 0.01f);
        Assert.Equal(PatienceTuning.Full - FalseClaimHit, agents[1].Patience!.Value, 0.01f);
        Assert.Equal(1, agents[2].Tally.FalseClaims);
    }

    [Fact]
    public void Staff_move_out_whoever_cost_the_agent_that_ran_out_its_patience_not_whoever_stands_nearest()
    {
        var tuning = Crowding with { Crowd = Rules with { FalseClaimHit = 100 } };
        var (crowd, ids) = Start(tuning, (new(0f, Front), Plain), (new(-0.6f, 2.2f), Plain), (new(-0.6f, 1.8f), null), (new(-1.6f, Front), null));
        Hold(crowd, 1);

        var events = crowd.Step(Idle(crowd, ids[3], new Command(Vector2.Zero, CallOut: true)));

        var movedOut = Assert.Single(events.OfType<KickedOut>());
        Assert.Equal(ids[3], movedOut.Agent);
        Assert.Equal(LineStep.Exiting, crowd.Snapshot().Agents[ids[3].Value].Step);
        Assert.Equal(PatienceTuning.Full, crowd.Snapshot().Agents[movedOut.By.Value].Patience!.Value, 0.0001f);
    }

    [Fact]
    public void Pushing_into_someone_drains_them_and_the_crowd_lets_the_pusher_through_after_a_while()
    {
        var (crowd, ids) = Start((new(-1.8f, Front), Plain), (new(-1.8f, 0.7f), null));
        var events = new List<SimEvent>();

        for (var i = 0; i < LetThroughAfterTicks + 30; i++)
        {
            events.AddRange(crowd.Step(Idle(crowd, ids[1], new Command(new(0, -1)))));
        }

        var through = Assert.Single(events.OfType<LetThrough>());
        Assert.Equal((ids[1], ids[0]), (through.Agent, through.Past));
        var drained = PatienceTuning.Full - crowd.Snapshot().Agents[ids[0].Value].Patience!.Value;
        Assert.InRange(drained, SqueezeDrainPerSecond * (LetThroughAfterTicks - 20) / TickRate.PerSecond, SqueezeDrainPerSecond * (LetThroughAfterTicks + 1) / TickRate.PerSecond);
    }

    [Fact]
    public void People_the_crowd_moves_press_toward_the_counter_without_shoving_anyone()
    {
        // Being served ahead costs nothing here, so only pushing could drain anyone.
        var (crowd, _) = Start(Crowding with { Crowd = Rules with { ServedAheadHit = 0 } },
            (new(0f, Front), Plain), (new(0f, 0.8f), Plain), (new(0.3f, 1.4f), Plain), (new(-0.4f, 2f), Plain));

        var events = new List<SimEvent>();
        for (var i = 0; i < 20 * TickRate.PerSecond; i++)
        {
            events.AddRange(crowd.Step(new Dictionary<AgentId, Command>()));
        }

        Assert.Empty(events.OfType<LetThrough>());
        Assert.All(crowd.Snapshot().Agents, agent => Assert.Equal(PatienceTuning.Full, agent.Patience!.Value, 0.0001f));
    }

    [Fact]
    public void A_shove_carries_on_through_whoever_stands_behind_the_one_shoved()
    {
        var (crowd, ids) = Start((new(0f, 1.3f), Plain), (new(0f, 1.65f), Plain), (new(0f, 2.0f), null));

        for (var i = 0; i < TickRate.PerSecond; i++)
        {
            crowd.Step(Idle(crowd, ids[2], new Command(new(0, -1))));
        }

        Assert.True(crowd.Snapshot().Agents[ids[0].Value].Position.Y < 1.2f, "The shove never reached the one in front.");
    }

    [Fact]
    public void Nobody_the_crowd_moves_stands_exactly_still()
    {
        var (crowd, ids) = Start(Crowding with { Crowd = Rules with { GlanceReachMeters = 0.0001f, WaveReachMeters = 0.0001f } }, (new(1.5f, Front), Plain));
        var seen = new HashSet<Vector2>();

        for (var i = 0; i < 10 * TickRate.PerSecond; i++)
        {
            crowd.Step(new Dictionary<AgentId, Command>());
            seen.Add(crowd.Snapshot().Agents[ids[0].Value].Position);
        }

        Assert.True(seen.Count > 5 * TickRate.PerSecond, $"It stood still for most of ten seconds: {seen.Count} positions.");
    }

    [Fact]
    public void A_cutter_pushes_straight_for_the_counter_through_whoever_is_in_its_way()
    {
        var crowd = new Crowd(Crowding, Bar, exit: new(5, 5), seed: 1);
        var held = crowd.Spawn(new(0f, Front), Plain);
        var cutter = crowd.Spawn(new(0f, 0.8f), Plain, cutter: true);

        for (var i = 0; i < LetThroughAfterTicks + 30; i++)
        {
            crowd.Step(new Dictionary<AgentId, Command> { [held] = Command.Idle });
        }

        // Shoved aside or let through, the cutter ends up at the counter.
        Assert.True(crowd.Snapshot().Agents[held.Value].Patience < PatienceTuning.Full);
        Assert.InRange(crowd.Snapshot().Agents[cutter.Value].Position.Y, 0f, Front + Rules.FrontMeters);
    }

    [Fact]
    public void Someone_served_walks_out_and_is_gone_and_the_clerk_glances_on()
    {
        var (crowd, ids) = Start((new(0f, Front), Plain));
        Hold(crowd, 1 + PickTicks + ServiceTicks);
        var events = new List<SimEvent>();

        for (var i = 0; i < 20 * TickRate.PerSecond; i++)
        {
            events.AddRange(crowd.Step(new Dictionary<AgentId, Command>()));
        }

        Assert.Equal(LineStep.Gone, crowd.Snapshot().Agents[ids[0].Value].Step);
        Assert.Null(crowd.Snapshot().Clerk!.Serving);
        Assert.Empty(events.OfType<Picked>());
    }

    [Fact]
    public void Nobody_walks_through_the_counter()
    {
        var (crowd, ids) = Start((new(0f, 1f), null));

        for (var i = 0; i < 3 * TickRate.PerSecond; i++)
        {
            crowd.Step(Idle(crowd, ids[0], new Command(new(0, -1))));
        }

        Assert.Equal(Front, crowd.Snapshot().Agents[ids[0].Value].Position.Y, 0.001f);
    }

    [Fact]
    public void The_same_seed_and_commands_play_the_same_crowd_and_another_seed_does_not()
    {
        Assert.Equal(Hashes(seed: 3), Hashes(seed: 3));
        Assert.NotEqual(Hashes(seed: 3), Hashes(seed: 4));
    }

    [Fact]
    public void A_crowd_without_its_tuning_is_refused() =>
        Assert.Throws<ArgumentException>(() => new Crowd(Snappy, Bar, exit: Vector2.Zero, seed: 1));

    /// <summary>The state hash after every tick of two minutes of a crowd moving itself, with newcomers arriving.</summary>
    private static List<ulong> Hashes(ulong seed)
    {
        var crowd = new Crowd(Crowding, Bar, exit: new(5, 5), seed);
        crowd.Spawn(new(-1f, 1f), Plain);
        crowd.Spawn(new(1f, 2f), Plain);
        crowd.ArriveAt(new(3, 6), [Plain]);
        return [.. Enumerable.Range(0, 120 * TickRate.PerSecond).Select(_ =>
        {
            crowd.Step(new Dictionary<AgentId, Command>());
            return crowd.StateHash();
        })];
    }

    private static (Crowd Crowd, AgentId[] Ids) Start(params (Vector2 At, Trait? Trait)[] agents) => Start(Crowding, agents);

    private static (Crowd Crowd, AgentId[] Ids) Start(Tuning tuning, params (Vector2 At, Trait? Trait)[] agents)
    {
        var crowd = new Crowd(tuning, Bar, exit: new(5, 5), seed: 1);
        return (crowd, [.. agents.Select(agent => crowd.Spawn(agent.At, agent.Trait))]);
    }

    /// <summary>Steps with everyone standing still for <paramref name="ticks"/>, and returns what happened.</summary>
    private static List<SimEvent> Hold(Crowd crowd, int ticks)
    {
        var events = new List<SimEvent>();
        for (var i = 0; i < ticks; i++)
        {
            events.AddRange(crowd.Step(Idle(crowd)));
        }

        return events;
    }

    /// <summary>Everyone standing still, but <paramref name="agent"/> on <paramref name="command"/>.</summary>
    private static Dictionary<AgentId, Command> Idle(Crowd crowd, AgentId? agent = null, Command command = default)
    {
        var commands = crowd.Snapshot().Agents.ToDictionary(other => other.Id, _ => Command.Idle);
        if (agent is { } id)
        {
            commands[id] = command;
        }

        return commands;
    }
}
