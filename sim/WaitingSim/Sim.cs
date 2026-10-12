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
    private readonly ulong seed;
    private readonly Rng rng;
    private readonly Line line;
    private readonly Patience patience;
    private readonly Ripple? ripple;
    private readonly StepOut? stepOut;
    private readonly Cutters? cutters;
    private readonly Bodies? bodies;

    /// <summary>A line on <paramref name="lane"/> with no agents yet, at tick 0. Its randomness all comes from <paramref name="seed"/>.</summary>
    public Sim(Tuning tuning, Lane lane, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(lane);
        this.tuning = tuning;
        this.seed = seed;
        rng = new Rng(seed);
        line = new Line(lane, tuning.Zones);
        patience = new Patience(tuning.Patience, line);
        ripple = tuning.Ripple is { } rippleTuning ? new Ripple(rippleTuning, lane, tuning.WalkAcceleration) : null;
        stepOut = tuning.StepOut is { } stepOutTuning ? new StepOut(stepOutTuning, tuning.Zones, line) : null;
        cutters = tuning.Cutter is { } cutterTuning ? new Cutters(cutterTuning, tuning.GraceWindowTicks, lane, line) : null;
        bodies = tuning.Bodies is { } bodyTuning ? new Bodies(bodyTuning, line) : null;
    }

    /// <summary>How many ticks have been stepped.</summary>
    public long Tick { get; private set; }

    /// <summary>
    /// Places a new agent at <paramref name="position"/>, standing still, with full patience if it has a
    /// <paramref name="trait"/>. A <paramref name="cutter"/> waits for a gap left open too long. With a ripple, it draws
    /// the spacing it likes to stand at. Level setup: it reports no events.
    /// </summary>
    public AgentId Spawn(Vector2 position, Trait? trait = null, bool cutter = false)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "An agent spawns at a finite position.");
        }

        var id = new AgentId(line.Agents.Count);
        var spawned = new Agent(id, position, trait, cutter, new Rng(seed, AgentStreams + (ulong)id.Value));
        if (tuning.Ripple is { } rippleTuning)
        {
            spawned.PreferredSpacing = rippleTuning.SpacingMeters.Draw(spawned.Rng);
        }

        line.Add(spawned);
        line.Measure();
        line.Classify();
        foreach (var agent in line.Agents)
        {
            TrackGap(agent, events: null);
        }

        return id;
    }

    /// <summary>
    /// Advances one tick. Each agent acts on its command in <paramref name="commands"/>; without one, the line moves it.
    /// Returns what happened: movement through the lane and zones first, then patience, each in agent id order, then
    /// kick-outs, then reaching the counter.
    /// </summary>
    public IReadOnlyList<SimEvent> Step(IReadOnlyDictionary<AgentId, Command> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        var agents = line.Agents;
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

        bodies?.Separate();

        var before = agents.Select(agent => (agent.InLane, agent.Zone, agent.Lane.Along)).ToList();
        line.Measure();
        var placed = new List<SimEvent>?[agents.Count];
        var cutHits = new float[agents.Count];
        for (var i = 0; i < agents.Count; i++)
        {
            var agent = agents[i];
            if (agent is { Step: LineStep.Cutting, InLane: true } && !before[i].InLane && cutters is not null)
            {
                placed[i] = cutters.SlideIn(agent, Tick, cutHits);
            }
            else if (stepOut is not null)
            {
                placed[i] = stepOut.Place(agent, before[i].InLane, before[i].Along, Tick, cutHits);
            }
        }

        line.Classify();

        var events = new List<SimEvent>();
        var bumped = new bool[agents.Count];
        for (var i = 0; i < agents.Count; i++)
        {
            var agent = agents[i];
            var (wasInLane, oldZone, _) = before[i];
            if (agent.InLane != wasInLane)
            {
                events.Add(agent.InLane ? new ReenteredLane(Tick, agent.Id) : new LeftLane(Tick, agent.Id));
            }

            if (placed[i] is { } outcomes)
            {
                events.AddRange(outcomes);
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
            patience.Wear(agent, bumped[agent.Id.Value], cutHits[agent.Id.Value], Tick, events);
        }

        patience.KickOuts(Tick, events);

        // After the kick-outs: an agent pushed past on the way to the counter is not served.
        foreach (var agent in agents)
        {
            if (agent is { ReachedCounterAt: null, InLane: true, Refused: false, Ahead: null } && agent.Lane.Along < tuning.Zones.BumpBelow)
            {
                agent.ReachedCounterAt = Tick;
                events.Add(new ReachedCounter(Tick, agent.Id));
            }
        }
        foreach (var happened in events)
        {
            var agent = line[happened.Agent];
            agent.Tally = agent.Tally.Counting(happened, agent.Id);
            if (happened is CutterEntered entered)
            {
                line[entered.Behind].Tally = line[entered.Behind].Tally.Counting(happened, entered.Behind);
            }
        }

        return events;
    }

    /// <summary>The state as of the last tick.</summary>
    public Snapshot Snapshot() => new(Tick, [.. line.Agents.Select(agent => agent.Snapshot())]);

    /// <summary>
    /// A hash of everything the next tick depends on. Two sims with the same hash play on identically;
    /// replays record it to catch a change in behavior. It leaves out each agent's tally and when it reached the
    /// counter: a record of what happened, which no movement or patience depends on.
    /// </summary>
    public ulong StateHash()
    {
        var (state, increment) = rng.Position;
        var hash = new StateHash().Add(Tick).Add(state).Add(increment).Add((long)line.Agents.Count);
        foreach (var agent in line.Agents)
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
            var (agentState, agentIncrement) = agent.Rng.Position;
            hash.Add(agentState).Add(agentIncrement).Add(agent.PreferredSpacing).Add((long)agent.Step).Add(agent.StepEndsAt)
                .Add(agent.HeldAlong ?? -1f).Add(agent.HoldEndsAt).Add((long)(agent.HeldBehind?.Value ?? -1)).Add(agent.Refused)
                .Add(agent.Cutter);
        }

        return hash.Value;
    }

    /// <summary>The command for an agent nobody commands: a waiting cutter's, the ripple's, or standing still without either.</summary>
    private Command FollowLine(Agent agent)
    {
        if (agent is { Cutter: true, InLane: false, Step: LineStep.Standing or LineStep.Cutting } && cutters is not null)
        {
            return cutters.Next(agent, Tick);
        }

        return ripple?.Next(agent, Tick) ?? Command.Idle;
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
