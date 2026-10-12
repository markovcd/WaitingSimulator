namespace WaitingSim;

/// <summary>
/// What stepping out of the lane and back in does to an agent's place in the line. Stepping out holds the place for a
/// while; stepping back in ahead of whoever stood behind returns to it. Stepping in anywhere else but the back cuts in,
/// made only into a gap left open past its grace window with room to stand clear of a bump: the person behind takes a
/// hit either way, and a missed cut in leaves the agent refused a place. Everyone it passed,
/// who stood ahead of its place and is behind it now, takes a hit too, and the person behind one more per person
/// passed; without a held place, its place is the back of the line.
/// </summary>
internal sealed class StepOut(StepOutTuning stepOut, ZoneLimits zones, Line line)
{
    /// <summary>
    /// What this tick's step into or out of the lane did to the agent's place, given whether it was in the lane and how
    /// far along it stood a tick ago. Hits for the person behind add to <paramref name="cutHits"/>; the events say what
    /// happened to the place.
    /// </summary>
    public List<SimEvent>? Place(Agent agent, bool wasInLane, float wasAlong, long tick, float[] cutHits)
    {
        if (wasInLane && !agent.InLane)
        {
            if (agent is { Refused: false, Step: not (LineStep.Leaving or LineStep.Done) })
            {
                agent.HeldAlong = wasAlong;
                agent.HoldEndsAt = tick + stepOut.SpotHoldTicks;
                agent.HeldBehind = line.Behind(agent)?.Id;
            }

            agent.Refused = false;
            return null;
        }

        if (wasInLane || !agent.InLane)
        {
            return agent.HeldAlong is not null && tick >= agent.HoldEndsAt ? [LoseSpot(agent, tick)] : null;
        }

        var along = agent.Lane.Along;
        var (ahead, behind) = line.Around(agent, along);
        var holding = agent.HeldAlong is not null;
        if (holding && behind?.Id == agent.HeldBehind)
        {
            agent.HeldAlong = null;
            agent.HeldBehind = null;
            return null;
        }

        var outcomes = new List<SimEvent>();
        if (behind is not null)
        {
            var made = behind is { Zone: Zone.Gap, GraceExpired: true }
                && behind.LineAlong - along >= zones.BumpBelow
                && (ahead is null || along - ahead.LineAlong >= zones.BumpBelow);
            agent.Refused = !made;
            var passed = made ? Passed(agent, along) : [];
            foreach (var other in passed.Where(other => other != behind))
            {
                cutHits[other.Id.Value] += stepOut.PassedHit;
            }

            cutHits[behind.Id.Value] += made ? stepOut.PastGraceHit + (passed.Count * stepOut.PassedHit) : stepOut.MissHit;
            outcomes.Add(made ? new CutIn(tick, agent.Id, behind.Id) : new CutInMissed(tick, agent.Id, behind.Id));
        }

        if (holding)
        {
            outcomes.Add(LoseSpot(agent, tick));
        }

        return outcomes;
    }

    /// <summary>Who in the line an agent cutting in at <paramref name="along"/> gets ahead of that stood ahead of its place.</summary>
    private List<Agent> Passed(Agent agent, float along) =>
        [.. line.Agents.Where(other => other != agent && other.InLine && other.LineAlong > along && other.LineAlong < (agent.HeldAlong ?? float.PositiveInfinity))];

    private static SpotLost LoseSpot(Agent agent, long tick)
    {
        agent.HeldAlong = null;
        agent.HeldBehind = null;
        return new SpotLost(tick, agent.Id);
    }
}
