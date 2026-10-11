using System.Numerics;

namespace WaitingSim;

/// <summary>
/// One running line: agents on a lane, stepped one tick at a time by their commands.
/// Reports what happened as events and shows its state as snapshots; nothing outside writes to it.
/// </summary>
public sealed class Sim
{
    // Each agent draws from a stream of its own, numbered from here so none is the sim's.
    private const ulong AgentStreams = 1UL << 32;

    private readonly Tuning tuning;
    private readonly Lane lane;
    private readonly ulong seed;
    private readonly Rng rng;
    private readonly List<Agent> agents = [];

    /// <summary>A line on <paramref name="lane"/> with no agents yet, at tick 0. Its randomness all comes from <paramref name="seed"/>.</summary>
    public Sim(Tuning tuning, Lane lane, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(lane);
        this.tuning = tuning;
        this.lane = lane;
        this.seed = seed;
        rng = new Rng(seed);
    }

    /// <summary>How many ticks have been stepped.</summary>
    public long Tick { get; private set; }

    /// <summary>
    /// Places a new agent at <paramref name="position"/>, standing still, with full patience if it has a
    /// <paramref name="trait"/>. With a ripple, it draws the spacing it likes to stand at. Level setup: it reports no events.
    /// </summary>
    public AgentId Spawn(Vector2 position, Trait? trait = null)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "An agent spawns at a finite position.");
        }

        var id = new AgentId(agents.Count);
        var spawned = new Agent(id, position, trait, new Rng(seed, AgentStreams + (ulong)id.Value));
        if (tuning.Ripple is { } ripple)
        {
            spawned.PreferredSpacing = ripple.SpacingMeters.Draw(spawned.Rng);
        }

        agents.Add(spawned);
        Classify();
        foreach (var agent in agents)
        {
            TrackGap(agent, events: null);
        }

        return id;
    }

    /// <summary>
    /// Advances one tick. Each agent acts on its command in <paramref name="commands"/>; without one, the line moves it.
    /// Returns what happened: movement through the lane and zones first, then patience, each in agent id order.
    /// </summary>
    public IReadOnlyList<SimEvent> Step(IReadOnlyDictionary<AgentId, Command> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        foreach (var id in commands.Keys)
        {
            if (id.Value < 0 || id.Value >= agents.Count)
            {
                throw new ArgumentException($"There is no agent {id.Value} in this sim.", nameof(commands));
            }
        }

        Tick++;
        foreach (var agent in agents)
        {
            Walk(agent, commands.TryGetValue(agent.Id, out var command) ? command.Validated() : FollowLine(agent));
        }

        var before = agents.Select(agent => (agent.InLane, agent.Zone)).ToList();
        Classify();

        var events = new List<SimEvent>();
        var bumped = new bool[agents.Count];
        for (var i = 0; i < agents.Count; i++)
        {
            var agent = agents[i];
            var (wasInLane, oldZone) = before[i];
            if (agent.InLane != wasInLane)
            {
                events.Add(agent.InLane ? new ReenteredLane(Tick, agent.Id) : new LeftLane(Tick, agent.Id));
            }

            if (agent.Zone != oldZone)
            {
                events.Add(new ZoneChanged(Tick, agent.Id, oldZone, agent.Zone));
                if (agent is { Zone: Zone.Bump, Ahead: { } ahead })
                {
                    bumped[ahead.Value] = true;
                }
            }

            TrackGap(agent, events);
        }

        foreach (var agent in agents)
        {
            WearPatience(agent, bumped[agent.Id.Value], events);
        }

        return events;
    }

    /// <summary>The state as of the last tick.</summary>
    public Snapshot Snapshot() => new(Tick, [.. agents.Select(agent => agent.Snapshot())]);

    /// <summary>
    /// A hash of everything the next tick depends on. Two sims with the same hash play on identically;
    /// replays record it to catch a change in behavior.
    /// </summary>
    public ulong StateHash()
    {
        var (state, increment) = rng.Position;
        var hash = new StateHash().Add(Tick).Add(state).Add(increment).Add((long)agents.Count);
        foreach (var agent in agents)
        {
            hash.Add(agent.Position.X).Add(agent.Position.Y)
                .Add(agent.Velocity.X).Add(agent.Velocity.Y)
                .Add(agent.InLane)
                .Add((long)(agent.Ahead?.Value ?? -1))
                .Add((long)(agent.Zone is { } zone ? (int)zone : -1))
                .Add(agent.GapOpenedAt ?? -1)
                .Add(agent.GraceExpired)
                .Add(agent.Trait?.Id ?? "")
                .Add(agent.Patience)
                .Add((long)agent.Stage);
            if (tuning.Ripple is not null)
            {
                var (agentState, agentIncrement) = agent.Rng.Position;
                hash.Add(agentState).Add(agentIncrement).Add(agent.PreferredSpacing).Add((long)agent.Step).Add(agent.StepEndsAt);
            }
        }

        return hash.Value;
    }

    /// <summary>
    /// Drains the agent's patience while the person ahead holds a gap open past its grace window, recovers it while
    /// they stay tight on theirs, and takes a hit when the person behind walked into it.
    /// </summary>
    private void WearPatience(Agent agent, bool bumped, List<SimEvent> events)
    {
        if (agent.Trait is not { } trait)
        {
            return;
        }

        var rates = tuning.Patience;
        var ahead = agent.Ahead is { } id ? agents[id.Value] : null;
        var change = ahead switch
        {
            { Zone: Zone.Gap, GraceExpired: true } => -rates.GapDrainPerSecond * trait.DrainScale / TickRate.PerSecond,
            { Zone: Zone.Tight } => rates.TightRecoveryPerSecond * trait.RecoveryScale / TickRate.PerSecond,
            _ => 0f,
        };
        if (bumped)
        {
            change -= rates.BumpHit * trait.BumpScale;
        }

        var before = agent.Patience;
        var shownBefore = trait.Shown(agent.Stage);
        agent.Patience = Math.Clamp(before + change, 0f, PatienceTuning.Full);
        agent.Stage = rates.Stages.After(agent.Stage, agent.Patience);
        var shown = trait.Shown(agent.Stage);
        if (shown != shownBefore)
        {
            events.Add(new StageChanged(Tick, agent.Id, shownBefore, shown));
        }

        if (agent.Patience == 0f && before > 0f)
        {
            events.Add(new PatienceRanOut(Tick, agent.Id));
        }
    }

    /// <summary>
    /// The command the line gives an agent nobody commands. It notices the person ahead move off and steps up after a
    /// reaction delay; at the front it walks up to the counter, is served, and leaves the lane to the side. Without a
    /// ripple it stands still.
    /// </summary>
    private Command FollowLine(Agent agent)
    {
        if (tuning.Ripple is not { } ripple)
        {
            return Command.Idle;
        }

        // How far back the agent is from where it wants to stand: its spacing behind the person ahead, or the counter.
        var toGo = agent.Spacing is { } spacing ? spacing - agent.PreferredSpacing : agent.Lane.Along;
        switch (agent.Step)
        {
            case LineStep.Standing when agent.InLane && toGo > ripple.NoticeMeters:
                Begin(agent, LineStep.Noticed, ripple.ReactionTicks);
                break;
            case LineStep.Standing when agent is { InLane: true, Ahead: null }:
                Begin(agent, LineStep.BeingServed, ripple.ServiceTicks);
                break;
            case LineStep.Noticed when Tick >= agent.StepEndsAt:
                agent.Step = agent.InLane && toGo > 0f ? LineStep.SteppingUp : LineStep.Standing;
                break;
            case LineStep.BeingServed when Tick >= agent.StepEndsAt:
                agent.Step = LineStep.Leaving;
                break;
            case LineStep.Leaving when agent.Lane.Off >= lane.HalfWidth + ripple.LeaveMeters:
                agent.Step = LineStep.Done;
                break;
        }

        // Stops walking once what is left is what it takes to slow down.
        var braking = agent.Velocity.LengthSquared() / (2f * tuning.WalkAcceleration);
        if (agent.Step == LineStep.SteppingUp && (!agent.InLane || toGo <= braking))
        {
            agent.Step = LineStep.Standing;
        }

        return agent.Step switch
        {
            LineStep.SteppingUp => new(lane.TowardFront(agent.Position)),
            LineStep.Leaving => new(Aside(lane.TowardFront(agent.Position))),
            _ => Command.Idle,
        };
    }

    /// <summary>A quarter turn from <paramref name="direction"/>: the way a served agent leaves the lane.</summary>
    private static Vector2 Aside(Vector2 direction) => new(-direction.Y, direction.X);

    /// <summary>Puts the agent at <paramref name="step"/> until a duration drawn from <paramref name="ticks"/> has passed.</summary>
    private void Begin(Agent agent, LineStep step, TickRange ticks)
    {
        agent.Step = step;
        agent.StepEndsAt = Tick + ticks.Draw(agent.Rng);
    }

    /// <summary>Eases the agent's velocity toward its wanted one by at most a tick's acceleration, then moves it.</summary>
    private void Walk(Agent agent, Command command)
    {
        var wanted = command.Move * tuning.WalkSpeed;
        var change = wanted - agent.Velocity;
        var maxChange = tuning.WalkAcceleration / TickRate.PerSecond;
        var length = change.Length();
        agent.Velocity = length > maxChange ? agent.Velocity + (change * (maxChange / length)) : wanted;
        agent.Position += agent.Velocity / TickRate.PerSecond;
    }

    /// <summary>Measures every agent against the lane and finds each one's person ahead, spacing and zone.</summary>
    private void Classify()
    {
        foreach (var agent in agents)
        {
            agent.Lane = lane.Measure(agent.Position);
            agent.InLane = lane.Holds(agent.Lane);
            agent.Ahead = null;
            agent.Spacing = null;
            agent.Zone = null;
        }

        var line = agents.Where(agent => agent.InLane).ToList();
        line.Sort((a, b) => a.Lane.Along != b.Lane.Along
            ? a.Lane.Along.CompareTo(b.Lane.Along)
            : a.Id.Value.CompareTo(b.Id.Value));
        for (var i = 1; i < line.Count; i++)
        {
            var agent = line[i];
            var ahead = line[i - 1];
            var spacing = agent.Lane.Along - ahead.Lane.Along;
            agent.Ahead = ahead.Id;
            agent.Spacing = spacing;
            agent.Zone = tuning.Zones.ZoneAt(spacing);
        }
    }

    /// <summary>Starts, runs out or clears the agent's grace window to match its zone.</summary>
    private void TrackGap(Agent agent, List<SimEvent>? events)
    {
        if (agent.Zone != Zone.Gap)
        {
            agent.GapOpenedAt = null;
            agent.GraceExpired = false;
            return;
        }

        agent.GapOpenedAt ??= Tick;
        if (!agent.GraceExpired && Tick - agent.GapOpenedAt >= tuning.GraceWindowTicks)
        {
            agent.GraceExpired = true;
            events?.Add(new GraceExpired(Tick, agent.Id));
        }
    }
}
