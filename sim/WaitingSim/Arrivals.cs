using System.Numerics;

namespace WaitingSim;

/// <summary>
/// Newcomers: one arrives at the entrance an interval after the last, walks to the back of the line and joins it. Nobody
/// arrives while another is on the way, or while the back of the line is too near the end of the lane to join. A share
/// of them come as cutters instead: one walks up beside the line, slides into a gap left open too long wherever it finds
/// one, and gives up at the front to join the back.
/// </summary>
internal sealed class Arrivals(ArrivalTuning arrivals, ZoneLimits zones, Lane lane, Line line, Vector2 entrance, IReadOnlyList<Trait> traits)
{
    /// <summary>How far outside the lane's edge a cutter walks up beside the line, in meters.</summary>
    private const float BesideMeters = 0.4f;

    /// <summary>How far ahead along the lane a cutter heads each tick, in meters.</summary>
    private const float AheadMeters = 1f;

    /// <summary>The newcomers that came as cutters and have not yet slid in or given up, in arrival order.</summary>
    private readonly List<AgentId> cutters = [];

    public Vector2 Entrance { get; } = entrance;

    /// <summary>The tick the next newcomer is due.</summary>
    public long NextAt { get; private set; }

    public IReadOnlyList<AgentId> Cutters => cutters;

    /// <summary>Draws when the next newcomer is due, from <paramref name="tick"/>.</summary>
    public void Schedule(long tick, Rng rng) => NextAt = tick + arrivals.IntervalTicks.Draw(rng);

    /// <summary>Whether a newcomer arrives at <paramref name="tick"/>.</summary>
    public bool Due(long tick) =>
        tick >= NextAt
        && !line.Agents.Any(agent => agent.Step == LineStep.Arriving)
        && BackAlong() + zones.LooseBelow <= lane.Length;

    /// <summary>The newcomer's trait, drawn from <paramref name="rng"/>.</summary>
    public Trait Pick(Rng rng) => traits[rng.NextBelow(traits.Count)];

    /// <summary>Whether the newcomer comes as a cutter: drawn by its share, and never past the most cutters waiting at once.</summary>
    public bool PickCutter(Rng rng)
    {
        if (arrivals.CutterShare <= 0f)
        {
            return false;
        }

        var drawn = rng.NextBelow(1000) < arrivals.CutterShare * 1000f;
        var waiting = line.Agents.Count(agent => agent is { Cutter: true, InLane: false, Step: LineStep.Standing or LineStep.Cutting or LineStep.Prowling });
        return drawn && waiting < arrivals.MaxCutters;
    }

    /// <summary>Marks <paramref name="agent"/> as a newcomer that came as a cutter.</summary>
    public void CameToCut(Agent agent)
    {
        agent.Step = LineStep.Prowling;
        cutters.Add(agent.Id);
    }

    /// <summary>Whether <paramref name="agent"/> is a newcomer cutter still beside the line.</summary>
    public bool Prowls(Agent agent) => agent is { InLane: false, Step: LineStep.Prowling or LineStep.Cutting } && cutters.Contains(agent.Id);

    /// <summary>
    /// A newcomer cutter's command: the cutter rule's, while it has a gap to go for; otherwise up the line beside the lane,
    /// and once at the front with none, off to join the back.
    /// </summary>
    public Command Prowl(Agent agent, long tick, Cutters? rule)
    {
        if (rule?.GapAlong(agent, tick) is { } gap)
        {
            agent.Step = LineStep.Cutting;
            var toGap = gap - agent.Lane.Along;
            return Math.Abs(toGap) <= AheadMeters ? rule.Next(agent, tick) : Beside(agent, agent.Lane.Along + Math.Clamp(toGap, -AheadMeters, AheadMeters));
        }

        if (agent.Lane.Along <= zones.LooseBelow)
        {
            cutters.Remove(agent.Id);
            agent.Step = LineStep.Arriving;
            return Next(agent);
        }

        agent.Step = LineStep.Prowling;
        return Beside(agent, agent.Lane.Along - AheadMeters);
    }

    /// <summary>The command toward the spot just outside the lane at <paramref name="along"/>, on the side the agent is on.</summary>
    private Command Beside(Agent agent, float along)
    {
        var point = lane.PointAt(Math.Clamp(along, 0f, lane.Length));
        var front = lane.TowardFront(point);
        var across = new Vector2(-front.Y, front.X);
        var side = Vector2.Dot(agent.Position - lane.PointAt(agent.Lane.Along), across) < 0f ? -1f : 1f;
        var toward = point + (across * side * (lane.HalfWidth + BesideMeters)) - agent.Position;
        return toward == Vector2.Zero ? Command.Idle : new(Vector2.Normalize(toward));
    }

    /// <summary>Forgets the newcomer cutters that have slid into the line.</summary>
    public void ForgetJoined() => cutters.RemoveAll(id => line[id].InLane);

    /// <summary>
    /// A newcomer's command: beside the lane until level with its spacing behind the back of the line, then into the lane
    /// there, and standing once it is in.
    /// </summary>
    public Command Next(Agent agent)
    {
        if (agent.InLane)
        {
            agent.Step = LineStep.Standing;
            return Command.Idle;
        }

        var spacing = agent.PreferredSpacing > 0f ? agent.PreferredSpacing : zones.TightBelow;
        var target = BackAlong(agent) + spacing;
        var toTarget = target - agent.Lane.Along;
        if (Math.Abs(toTarget) > AheadMeters)
        {
            return Beside(agent, agent.Lane.Along + Math.Clamp(toTarget, -AheadMeters, AheadMeters));
        }

        var toward = lane.PointAt(target) - agent.Position;
        return toward == Vector2.Zero ? Command.Idle : new(Vector2.Normalize(toward));
    }

    /// <summary>How far along the lane the back of the line is, leaving out <paramref name="self"/>; the front with no line.</summary>
    private float BackAlong(Agent? self = null) =>
        line.Agents.Where(agent => agent != self && agent.InLine).Select(agent => agent.LineAlong).DefaultIfEmpty(0f).Max();
}
