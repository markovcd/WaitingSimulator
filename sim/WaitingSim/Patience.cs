namespace WaitingSim;

/// <summary>
/// How patience wears: it drains while the person ahead holds a gap open past its grace window, recovers while they
/// stay tight on theirs, and takes one-off hits. Running out pushes past the person ahead, who is kicked out.
/// </summary>
internal sealed class Patience(PatienceTuning rates, Line line)
{
    /// <summary>Wears the agent's patience for this tick, with a bump into it and any one-off hits, and reports changes.</summary>
    public void Wear(Agent agent, bool bumped, float hit, long tick, List<SimEvent> events)
    {
        if (agent.Trait is not { } trait)
        {
            return;
        }

        var ahead = agent.Ahead is { } id ? line[id] : null;
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

        change -= hit;

        var before = agent.Patience;
        var shownBefore = trait.Shown(agent.Stage);
        agent.Patience = Math.Clamp(before + change, 0f, PatienceTuning.Full);
        agent.Stage = rates.Stages.After(agent.Stage, agent.Patience);
        var shown = trait.Shown(agent.Stage);
        if (shown != shownBefore)
        {
            events.Add(new StageChanged(tick, agent.Id, shownBefore, shown));
        }

        if (agent.Patience == 0f && before > 0f)
        {
            events.Add(new PatienceRanOut(tick, agent.Id));
        }
    }

    /// <summary>Kicks out the person ahead of each agent whose patience ran out in <paramref name="events"/>, adding a <see cref="KickedOut"/> for each.</summary>
    public void KickOuts(long tick, List<SimEvent> events)
    {
        foreach (var ranOut in events.OfType<PatienceRanOut>().ToList())
        {
            if (line[ranOut.Agent].Ahead is { } ahead)
            {
                events.Add(KickOut(line[ahead], ranOut.Agent, tick));
            }
        }
    }

    /// <summary>Takes the agent's place in the line from it, held or not; an agent the line moves walks off.</summary>
    private static KickedOut KickOut(Agent agent, AgentId by, long tick)
    {
        agent.Refused = agent.InLane;
        agent.HeldAlong = null;
        agent.HeldBehind = null;
        agent.Step = LineStep.Leaving;
        agent.KickedOutAt ??= tick;
        return new KickedOut(tick, agent.Id, by);
    }
}
