using System.Numerics;

namespace WaitingSim;

/// <summary>
/// One running crowd: agents pressing against a counter with no line, stepped one tick at a time by their commands.
/// Every agent remembers who arrived before it, the invisible line; the clerk serves whom their glance lands on. Reports
/// what happened as events and shows its state as snapshots; nothing outside writes to it.
/// </summary>
public sealed class Crowd : ISim
{
    // Each agent draws from a stream of its own, numbered from here so none is the crowd's.
    private const ulong AgentStreams = 1UL << 32;

    /// <summary>How near the exit an agent walking out has to come to be gone, in meters.</summary>
    private const float ExitReachMeters = 0.3f;

    /// <summary>How near its spot an agent the crowd moves stops, in meters.</summary>
    private const float ArrivedMeters = 0.05f;

    /// <summary>How square on someone has to stand to be in an agent's way: the cosine of the widest angle off its path.</summary>
    private const float InTheWay = 0.7f;

    /// <summary>How near two agents stand to count as touching, past their bodies, in meters.</summary>
    private const float TouchMeters = 0.05f;

    /// <summary>Eighth turns an agent tries to its side when someone is in its way.</summary>
    private static readonly float SideStep = MathF.Sqrt(0.5f);

    private readonly Tuning tuning;
    private readonly CrowdTuning crowd;
    private readonly BodyTuning body;
    private readonly Counter counter;
    private readonly Vector2 exit;
    private readonly ulong seed;
    private readonly Rng rng;
    private readonly List<Agent> agents = [];
    private readonly Bodies bodies;
    private readonly Jostle jostle;
    private readonly Clerk clerk;
    private readonly Blame blame = new();
    private Vector2? entrance;
    private IReadOnlyList<Trait> traits = [];
    private long nextArrivalAt = -1;

    /// <summary>A crowd at <paramref name="counter"/> with no agents yet, at tick 0, walking out to <paramref name="exit"/>. Its randomness all comes from <paramref name="seed"/>.</summary>
    public Crowd(Tuning tuning, Counter counter, Vector2 exit, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(counter);
        if (tuning is not { Crowd: { } crowdTuning, Bodies: { } bodyTuning })
        {
            throw new ArgumentException("A crowd needs the tuning's crowd and bodies.", nameof(tuning));
        }

        this.tuning = tuning;
        crowd = crowdTuning;
        this.counter = counter;
        this.exit = exit;
        this.seed = seed;
        rng = new Rng(seed);

        // Squeezing in is a push that does not bounce.
        body = bodyTuning with { Rebound = 0f };
        bodies = new Bodies(body);
        jostle = new Jostle(crowdTuning);
        clerk = new Clerk(crowdTuning, counter, body.Radius);
    }

    /// <inheritdoc/>
    public long Tick { get; private set; }

    /// <summary>Has newcomers with <paramref name="traits"/> arrive at <paramref name="at"/>. Level setup: it draws when the first is due and reports no events.</summary>
    public void ArriveAt(Vector2 at, IReadOnlyList<Trait> traits)
    {
        ArgumentNullException.ThrowIfNull(traits);
        if (traits.Count == 0)
        {
            return;
        }

        entrance = at;
        this.traits = traits;
        nextArrivalAt = Tick + crowd.ArrivalTicks.Draw(rng);
    }

    /// <summary>
    /// Places a new agent at <paramref name="position"/>, standing still, with full patience if it has a
    /// <paramref name="trait"/>; it arrived now. A <paramref name="cutter"/> pushes to the front instead of waiting for
    /// room. Level setup: it reports no events.
    /// </summary>
    public AgentId Spawn(Vector2 position, Trait? trait = null, bool cutter = false)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "An agent spawns at a finite position.");
        }

        var id = new AgentId(agents.Count);
        var spawned = new Agent(id, position, trait, cutter, new Rng(seed, AgentStreams + (ulong)id.Value)) { ArrivedAt = Tick };
        spawned.TargetAlong = new MeterRange(0f, counter.Length).Draw(spawned.Rng);
        agents.Add(spawned);
        return id;
    }

    /// <summary>
    /// Advances one tick. Each agent acts on its command in <paramref name="commands"/>; without one, the crowd moves it.
    /// Returns what happened: pushing first, then the clerk, then patience in agent id order, then staff moving out
    /// whoever the agents that ran out blame, then arrivals.
    /// </summary>
    public IReadOnlyList<SimEvent> Step(IReadOnlyDictionary<AgentId, Command> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        foreach (var id in commands.Keys)
        {
            if (id.Value < 0 || id.Value >= agents.Count)
            {
                throw new ArgumentException($"There is no agent {id.Value} in this crowd.", nameof(commands));
            }
        }

        Tick++;
        var validated = commands.ToDictionary(pair => pair.Key, pair => pair.Value.Validated());
        foreach (var agent in agents)
        {
            Walk(agent, validated.TryGetValue(agent.Id, out var command) ? command : Next(agent));
            KeepOffCounter(agent);
        }

        var pushes = new List<(Agent Pusher, Agent Pushed)>();
        bodies.Separate(agents, TakesRoom, jostle.Passing, pushes);
        foreach (var agent in agents)
        {
            KeepOffCounter(agent);
        }

        var events = new List<SimEvent>();
        var changes = new float[agents.Count];
        jostle.Update(pushes, Tick, changes, blame, events);

        var waiting = agents.Where(Waiting).ToList();
        var waving = new HashSet<Agent>();
        var callingOut = new List<Agent>();
        foreach (var agent in waiting)
        {
            if (validated.TryGetValue(agent.Id, out var command))
            {
                if (command.Waving)
                {
                    waving.Add(agent);
                    Wave(agent, waiting, changes);
                }

                if (command.CallOut)
                {
                    callingOut.Add(agent);
                }
            }
        }

        clerk.Update(waiting, waving, callingOut, Tick, rng, changes, blame, events);

        foreach (var agent in agents)
        {
            var recovery = crowd.RecoveryPerSecond * (agent.Trait?.RecoveryScale ?? 0f) / TickRate.PerSecond;
            Patience.Shift(tuning.Patience, agent, changes[agent.Id.Value] + recovery, Tick, events);
        }

        CallStaff(events);
        Arrive(events);
        foreach (var happened in events)
        {
            var agent = agents[happened.Agent.Value];
            agent.Tally = agent.Tally.Counting(happened, agent.Id);
        }

        return events;
    }

    /// <inheritdoc/>
    public Snapshot Snapshot() => new(
        Tick,
        [.. agents.Select(agent => agent.Snapshot())],
        new ClerkSnapshot(counter.At(clerk.GlanceAlong, 0f), clerk.Picked?.Id, clerk.Serving?.Id));

    /// <inheritdoc/>
    public ulong StateHash()
    {
        var (state, increment) = rng.Position;
        var hash = new StateHash().Add(Tick).Add(state).Add(increment).Add(nextArrivalAt).Add((long)agents.Count);
        clerk.Hash(hash);
        jostle.Hash(hash);
        blame.Hash(hash);
        foreach (var agent in agents)
        {
            var (agentState, agentIncrement) = agent.Rng.Position;
            hash.Add(agent.Position.X).Add(agent.Position.Y)
                .Add(agent.Velocity.X).Add(agent.Velocity.Y)
                .Add(agent.Trait?.Id ?? "")
                .Add(agent.Patience)
                .Add((long)agent.Stage)
                .Add((long)agent.Step)
                .Add(agent.ArrivedAt)
                .Add(agent.TargetAlong)
                .Add(agent.Cutter)
                .Add(agent.ReachedCounterAt ?? -1)
                .Add(agent.KickedOutAt ?? -1)
                .Add(agentState).Add(agentIncrement);
        }

        return hash.Value;
    }

    /// <summary>Whether the agent is still in the crowd waiting to be served: not served, being served or moved out.</summary>
    private static bool Waiting(Agent agent) => agent is { Step: LineStep.Standing, ReachedCounterAt: null, KickedOutAt: null };

    /// <summary>An agent walking out takes up no room, so it does not jam the crowd it leaves through.</summary>
    private static bool TakesRoom(Agent agent) => agent.Step is not (LineStep.Exiting or LineStep.Gone);

    /// <summary>The command for an agent nobody commands: out once done, still while served, else toward its spot at the counter.</summary>
    private Command Next(Agent agent)
    {
        if (agent.Step is LineStep.Exiting or LineStep.Gone)
        {
            var toward = exit - agent.Position;
            if (agent.Step == LineStep.Gone || toward.Length() <= ExitReachMeters)
            {
                agent.Step = LineStep.Gone;
                return Command.Idle;
            }

            return new(Vector2.Normalize(toward));
        }

        if (agent.Step != LineStep.Standing || clerk.Picked == agent)
        {
            return Command.Idle;
        }

        // A cutter makes for the counter straight ahead of it; everyone else for a spot of its own.
        var spot = counter.At(agent.Cutter ? counter.Measure(agent.Position).Along : agent.TargetAlong, body.Radius);
        var way = spot - agent.Position;
        if (way.Length() <= ArrivedMeters)
        {
            return Command.Idle;
        }

        var ahead = Vector2.Normalize(way);
        if (agent.Cutter || Clear(agent, ahead))
        {
            return new(ahead);
        }

        // Someone in the way: an eighth turn to either side, the side of the spot first, or wait.
        var left = new Vector2((ahead.X * SideStep) - (ahead.Y * SideStep), (ahead.X * SideStep) + (ahead.Y * SideStep));
        var right = new Vector2((ahead.X * SideStep) + (ahead.Y * SideStep), (ahead.Y * SideStep) - (ahead.X * SideStep));
        var spotIsLeft = Vector2.Dot(left - right, way) > 0f;
        foreach (var side in spotIsLeft ? new[] { left, right } : [right, left])
        {
            if (Clear(agent, side))
            {
                return new(side);
            }
        }

        return Command.Idle;
    }

    /// <summary>Whether nobody taking up room stands in <paramref name="agent"/>'s way along <paramref name="direction"/>.</summary>
    private bool Clear(Agent agent, Vector2 direction)
    {
        var room = (body.Radius * 2f) + crowd.StandOffMeters;
        var touching = (body.Radius * 2f) + TouchMeters;
        foreach (var other in agents)
        {
            if (other == agent || !TakesRoom(other))
            {
                continue;
            }

            var offset = other.Position - agent.Position;
            var distance = offset.Length();
            // Someone square on is in the way from the stand-off; anyone touching is, unless walking would take the agent off them.
            var facing = distance == 0f ? 1f : Vector2.Dot(offset / distance, direction);
            if ((distance < room && facing > InTheWay) || (distance < touching && facing > 0f))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Drains everyone waiting within sight of <paramref name="waver"/>, charged to it.</summary>
    private void Wave(Agent waver, IReadOnlyList<Agent> waiting, float[] changes)
    {
        foreach (var near in waiting)
        {
            if (near != waver && Vector2.Distance(near.Position, waver.Position) <= crowd.SightMeters)
            {
                var drain = crowd.WaveDrainPerSecond * (near.Trait?.DrainScale ?? 0f) / TickRate.PerSecond;
                changes[near.Id.Value] -= drain;
                blame.Charge(near, waver, drain);
            }
        }
    }

    /// <summary>For each agent that ran out, staff move out whom it blames most among those still waiting, and it calms down.</summary>
    private void CallStaff(List<SimEvent> events)
    {
        foreach (var ranOut in events.OfType<PatienceRanOut>().ToList())
        {
            var victim = agents[ranOut.Agent.Value];
            if (blame.Settle(victim, id => Waiting(agents[id.Value]) || clerk.Picked == agents[id.Value]) is { } culprit)
            {
                var movedOut = agents[culprit.Value];
                clerk.Forget(movedOut);
                movedOut.KickedOutAt = Tick;
                movedOut.Step = LineStep.Exiting;
                events.Add(new KickedOut(Tick, culprit, victim.Id));
            }

            Patience.Shift(tuning.Patience, victim, PatienceTuning.Full, Tick, events);
        }
    }

    private void Arrive(List<SimEvent> events)
    {
        if (entrance is not { } at || Tick < nextArrivalAt)
        {
            return;
        }

        var trait = traits[rng.NextBelow(traits.Count)];
        var cutter = crowd.CutterShare > 0f && rng.NextBelow(1000) < crowd.CutterShare * 1000f;
        var arrived = Spawn(at, trait, cutter);
        nextArrivalAt = Tick + crowd.ArrivalTicks.Draw(rng);
        events.Add(new Arrived(Tick, arrived, cutter));
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

    /// <summary>Keeps an agent in front of the counter on the crowd's side, stopping what it walked into the counter with.</summary>
    private void KeepOffCounter(Agent agent)
    {
        var (along, @out) = counter.Measure(agent.Position);
        if (along < 0f || along > counter.Length || @out >= body.Radius || @out < -body.Radius)
        {
            return;
        }

        agent.Position += counter.TowardCrowd * (body.Radius - @out);
        var into = -Vector2.Dot(agent.Velocity, counter.TowardCrowd);
        if (into > 0f)
        {
            agent.Velocity += counter.TowardCrowd * into;
        }
    }
}
