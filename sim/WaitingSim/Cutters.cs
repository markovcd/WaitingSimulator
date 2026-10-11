using System.Numerics;

namespace WaitingSim;

/// <summary>
/// How cutters behave: each waits beside the line for a gap left open <see cref="CutterTuning.AfterGraceTicks"/> past
/// its grace window, walks to it and slides in, and the person behind the one it got ahead of blames them.
/// </summary>
internal sealed class Cutters(CutterTuning cutter, int graceWindowTicks, Lane lane, Line line)
{
    /// <summary>The cutter's command: walk to the middle of the nearest such gap, or stand while there is none.</summary>
    public Command Next(Agent agent, long tick)
    {
        var target = line.Agents
            .Where(other => other is { InLine: true, Zone: Zone.Gap, GapOpenedAt: { } opened } && tick - opened >= graceWindowTicks + cutter.AfterGraceTicks)
            .MinBy(other => (Math.Abs(other.LineAlong - agent.Lane.Along), other.Id.Value));
        if (target is not { Spacing: { } spacing })
        {
            agent.Step = LineStep.Standing;
            return Command.Idle;
        }

        agent.Step = LineStep.Cutting;
        var toward = lane.PointAt(target.LineAlong - (spacing / 2f)) - agent.Position;
        return toward == Vector2.Zero ? Command.Idle : new(Vector2.Normalize(toward));
    }

    /// <summary>
    /// A cutter stepping into the lane slides in wherever it stands. The blame hit adds to <paramref name="cutHits"/>.
    /// </summary>
    public List<SimEvent>? SlideIn(Agent agent, long tick, float[] cutHits)
    {
        agent.Step = LineStep.Standing;
        if (line.Around(agent, agent.Lane.Along).Behind is not { } behind)
        {
            return null;
        }

        if (line.Behind(behind) is { } blaming)
        {
            cutHits[blaming.Id.Value] += cutter.BlameHit;
        }

        return [new CutterEntered(tick, agent.Id, behind.Id)];
    }
}
