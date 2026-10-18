using System.Numerics;

namespace WaitingSim;

/// <summary>
/// One running line: agents on a lane, stepped one tick at a time by their commands.
/// Reports what happened as events and shows its state as snapshots; nothing outside writes to it.
/// </summary>
public sealed class Sim : ISim
{
    // Each agent draws from a stream of its own, numbered from here so none is the sim's.
    private const ulong AgentStreams = 1UL << 32;

    /// <summary>How near the exit an agent walking out has to come to be gone, in meters.</summary>
    private const float ExitReachMeters = 0.3f;

    private readonly Tuning tuning;
    private readonly Lane lane;
    private readonly ulong seed;
    private readonly Rng rng;
    private readonly Line line;
    private readonly Patience patience;
    private readonly Ripple? ripple;
    private readonly StepOut? stepOut;
    private readonly Cutters? cutters;
    private Arrivals? arrivals;
    private Vector2? exit;
    private Tickets? tickets;
    private SmallTalk? smallTalk;
    private readonly Staring? staring;
    private readonly LettingPast? lettingPast;
    private readonly Bodies? bodies;

    /// <summary>A line on <paramref name="lane"/> with no agents yet, at tick 0. Its randomness all comes from <paramref name="seed"/>.</summary>
    public Sim(Tuning tuning, Lane lane, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(lane);
        this.tuning = tuning;
        this.lane = lane;
        this.seed = seed;
        rng = new Rng(seed);
        line = new Line(lane, tuning.Zones);
        patience = new Patience(tuning.Patience, line);
        ripple = tuning.Ripple is { } rippleTuning ? new Ripple(rippleTuning, lane, tuning.WalkAcceleration) : null;
        stepOut = tuning.StepOut is { } stepOutTuning ? new StepOut(stepOutTuning, tuning.Zones, line) : null;
        cutters = tuning.Cutter is { } cutterTuning ? new Cutters(cutterTuning, tuning.GraceWindowTicks, lane, line) : null;
        bodies = tuning.Bodies is { } bodyTuning ? new Bodies(bodyTuning) : null;
        staring = tuning.Staring is { } staringTuning ? new Staring(staringTuning, line) : null;
        lettingPast = tuning is { LetPast: { } letPastTuning, StepOut: { } heldTuning } ? new LettingPast(letPastTuning, heldTuning, tuning.Zones, line) : null;
    }

    /// <summary>
    /// Has newcomers with <paramref name="traits"/> arrive at <paramref name="entrance"/>, if the tuning has arrivals.
    /// Level setup: it draws when the first is due and reports no events.
    /// </summary>
    public void ArriveAt(Vector2 entrance, IReadOnlyList<Trait> traits)
    {
        ArgumentNullException.ThrowIfNull(traits);
        if (tuning.Arrivals is not { } arrivalTuning || traits.Count == 0)
        {
            return;
        }

        arrivals = new Arrivals(arrivalTuning, tuning.Zones, lane, line, entrance, traits);
        arrivals.Schedule(Tick, rng);
    }

    /// <summary>Hands <paramref name="holder"/> a ticket that slips now and then, if the tuning drops tickets. Level setup.</summary>
    public void HandTicket(AgentId holder)
    {
        if (tuning.Distractions?.DroppedTicket is not { } ticket)
        {
            return;
        }

        tickets = new Tickets(ticket, lane, line[holder]);
        tickets.Schedule(Tick, rng);
    }

    /// <summary>Has whoever stands behind <paramref name="listener"/> make small talk now and then, if the tuning has small talk. Level setup.</summary>
    public void TalkTo(AgentId listener)
    {
        if (tuning.Distractions?.SmallTalk is not { } talk)
        {
            return;
        }

        smallTalk = new SmallTalk(talk, line, line[listener]);
        smallTalk.Schedule(Tick, rng);
    }

    /// <summary>Has agents done with the line walk out to <paramref name="exit"/> and be gone. Level setup.</summary>
    public void ExitAt(Vector2 exit) => this.exit = exit;

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

        // An agent done with the line, or gone out of the exit, takes up no room.
        bodies?.Separate(agents, agent => agent.Step is not (LineStep.Done or LineStep.Gone));

        var before = agents.Select(agent => (agent.InLane, agent.Zone, agent.Lane.Along)).ToList();
        line.Measure();
        var placed = new List<SimEvent>?[agents.Count];
        var hits = new float[agents.Count];
        for (var i = 0; i < agents.Count; i++)
        {
            var agent = agents[i];
            if (agent is { Step: LineStep.Cutting, InLane: true } && !before[i].InLane && cutters is not null)
            {
                placed[i] = cutters.SlideIn(agent, Tick, hits);
            }
            else if (stepOut is not null)
            {
                placed[i] = stepOut.Place(agent, before[i].InLane, before[i].Along, Tick, hits);
            }
        }

        var waved = lettingPast?.Update(agents, commands, Tick, hits);
        line.Classify();

        var events = new List<SimEvent>();
        if (waved is not null)
        {
            events.AddRange(waved);
        }

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

        if (smallTalk is not null)
        {
            var glancingBack = commands.TryGetValue(smallTalk.Listener.Id, out var listening) && listening.GlancingBack;
            smallTalk.Update(Tick, rng, glancingBack, hits, events);
        }

        staring?.Update(agents, commands, Tick, hits, events);

        foreach (var agent in agents)
        {
            patience.Wear(agent, bumped[agent.Id.Value], hits[agent.Id.Value], Tick, events);
        }

        patience.KickOuts(Tick, events);
        tickets?.Update(Tick, rng, events);

        // After the kick-outs: an agent pushed past on the way to the counter is not served.
        foreach (var agent in agents)
        {
            if (agent is { ReachedCounterAt: null, InLane: true, Refused: false, Ahead: null } && agent.Lane.Along < tuning.Zones.BumpBelow)
            {
                if (agent.TicketAt is not null)
                {
                    // Turned away like a kick-out: refused a place, and walked off if the line moves it.
                    agent.TurnedAwayAt = Tick;
                    agent.Refused = true;
                    agent.Step = LineStep.Leaving;
                    events.Add(new TurnedAway(Tick, agent.Id));
                    continue;
                }

                agent.ReachedCounterAt = Tick;
                events.Add(new ReachedCounter(Tick, agent.Id));
            }
        }
        arrivals?.ForgetJoined();
        if (arrivals is not null && arrivals.Due(Tick))
        {
            var trait = arrivals.Pick(rng);
            var cutter = arrivals.PickCutter(rng);
            var arrived = Spawn(arrivals.Entrance, trait, cutter);
            if (cutter)
            {
                arrivals.CameToCut(line[arrived]);
            }
            else
            {
                line[arrived].Step = LineStep.Arriving;
            }

            arrivals.Schedule(Tick, rng);
            events.Add(new Arrived(Tick, arrived, cutter));
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

        // A sim without a ticket, small talk or arrivals has none to drop, make or due, and leaves them out.
        if (tickets is not null)
        {
            hash.Add(tickets.NextDropAt).Add(tickets.Holder.TicketAt?.X ?? float.NaN).Add(tickets.Holder.TicketAt?.Y ?? float.NaN);
        }

        if (smallTalk is not null)
        {
            hash.Add(smallTalk.NextAt).Add((long)(smallTalk.Talker?.Id.Value ?? -1)).Add(smallTalk.StartedAt)
                .Add(smallTalk.Listener.SmallTalkEndsAt ?? -1).Add((long)smallTalk.ListenedTicks);
        }

        if (arrivals is not null)
        {
            hash.Add(arrivals.NextAt).Add((long)arrivals.Cutters.Count);
            foreach (var cutter in arrivals.Cutters)
            {
                hash.Add((long)cutter.Value);
            }
        }

        // Likewise each agent's glance back, only when glancing back can stare.
        if (staring is not null)
        {
            foreach (var agent in line.Agents)
            {
                hash.Add(agent.GlancingSince ?? -1).Add(agent.Staring);
            }
        }

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
        if (exit is { } door && agent.Step is LineStep.Done or LineStep.Exiting or LineStep.Gone)
        {
            return Leave(agent, door);
        }

        if (arrivals is not null && arrivals.Prowls(agent))
        {
            return arrivals.Prowl(agent, Tick, cutters);
        }

        if (agent.Step == LineStep.Arriving && arrivals is not null)
        {
            return arrivals.Next(agent);
        }

        if (agent is { Cutter: true, InLane: false, Step: LineStep.Standing or LineStep.Cutting } && cutters is not null)
        {
            return cutters.Next(agent, Tick);
        }

        return ripple?.Next(agent, Tick) ?? Command.Idle;
    }

    /// <summary>The command for an agent done with the line: out to <paramref name="door"/>, and gone once it is there.</summary>
    private static Command Leave(Agent agent, Vector2 door)
    {
        var toward = door - agent.Position;
        if (agent.Step == LineStep.Gone || toward.Length() <= ExitReachMeters)
        {
            agent.Step = LineStep.Gone;
            return Command.Idle;
        }

        agent.Step = LineStep.Exiting;
        return new(Vector2.Normalize(toward));
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
