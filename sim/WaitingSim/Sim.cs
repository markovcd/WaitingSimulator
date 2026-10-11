using System.Numerics;

namespace WaitingSim;

/// <summary>
/// One running line: agents on a lane, stepped one tick at a time by their commands.
/// Reports what happened as events and shows its state as snapshots; nothing outside writes to it.
/// </summary>
public sealed class Sim
{
    private readonly Tuning tuning;
    private readonly Lane lane;
    private readonly List<Agent> agents = [];

    /// <summary>A line on <paramref name="lane"/> with no agents yet, at tick 0.</summary>
    public Sim(Tuning tuning, Lane lane)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(lane);
        this.tuning = tuning;
        this.lane = lane;
    }

    /// <summary>How many ticks have been stepped.</summary>
    public long Tick { get; private set; }

    /// <summary>Places a new agent at <paramref name="position"/>, standing still. Level setup: it reports no events.</summary>
    public AgentId Spawn(Vector2 position)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "An agent spawns at a finite position.");
        }

        var id = new AgentId(agents.Count);
        agents.Add(new Agent(id, position));
        Classify();
        foreach (var agent in agents)
        {
            TrackGap(agent, events: null);
        }

        return id;
    }

    /// <summary>
    /// Advances one tick. Each agent acts on its command in <paramref name="commands"/>, or stands still without one.
    /// Returns what happened, in agent id order.
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
            Walk(agent, commands.TryGetValue(agent.Id, out var command) ? command.Validated() : Command.Idle);
        }

        var before = agents.Select(agent => (agent.InLane, agent.Zone)).ToList();
        Classify();

        var events = new List<SimEvent>();
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
            }

            TrackGap(agent, events);
        }

        return events;
    }

    /// <summary>The state as of the last tick.</summary>
    public Snapshot Snapshot() => new(Tick, [.. agents.Select(agent => agent.Snapshot())]);

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
