using System.Numerics;

namespace WaitingSim;

/// <summary>
/// Newcomers: one arrives at the entrance an interval after the last, walks to the back of the line and joins it. Nobody
/// arrives while another is still on the way, or while the back of the line is too near the end of the lane to join.
/// </summary>
internal sealed class Arrivals(ArrivalTuning arrivals, ZoneLimits zones, Lane lane, Line line, Vector2 entrance, IReadOnlyList<Trait> traits)
{
    public Vector2 Entrance { get; } = entrance;

    /// <summary>The tick the next newcomer is due.</summary>
    public long NextAt { get; private set; }

    /// <summary>Draws when the first newcomer is due, from <paramref name="tick"/>.</summary>
    public void Schedule(long tick, Rng rng) => NextAt = tick + arrivals.IntervalTicks.Draw(rng);

    /// <summary>Whether a newcomer arrives at <paramref name="tick"/>.</summary>
    public bool Due(long tick) =>
        tick >= NextAt
        && !line.Agents.Any(agent => agent.Step == LineStep.Arriving)
        && BackAlong() + zones.LooseBelow <= lane.Length;

    /// <summary>The newcomer's trait, drawn from <paramref name="rng"/>.</summary>
    public Trait Pick(Rng rng) => traits[rng.NextBelow(traits.Count)];

    /// <summary>A newcomer's command: toward its spacing behind the back of the line, and standing once it is in the lane.</summary>
    public Command Next(Agent agent)
    {
        if (agent.InLane)
        {
            agent.Step = LineStep.Standing;
            return Command.Idle;
        }

        var spacing = agent.PreferredSpacing > 0f ? agent.PreferredSpacing : zones.TightBelow;
        var toward = lane.PointAt(BackAlong(agent) + spacing) - agent.Position;
        return toward == Vector2.Zero ? Command.Idle : new(Vector2.Normalize(toward));
    }

    /// <summary>How far along the lane the back of the line is, leaving out <paramref name="self"/>; the front with no line.</summary>
    private float BackAlong(Agent? self = null) =>
        line.Agents.Where(agent => agent != self && agent.InLine).Select(agent => agent.LineAlong).DefaultIfEmpty(0f).Max();
}
