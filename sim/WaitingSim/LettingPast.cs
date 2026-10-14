namespace WaitingSim;

/// <summary>
/// Letting someone ahead: an agent out of the lane with its place held waves the person behind past. They get some
/// patience back and the place ahead of them, and the agent's held place moves to just behind them, so stepping back in
/// there is returning to it. The hold starts over.
/// </summary>
internal sealed class LettingPast(LetPastTuning letPast, StepOutTuning stepOut, ZoneLimits zones, Line line)
{
    /// <summary>Waves past for every agent whose command says to and can, adding the recovery to <paramref name="hits"/>.</summary>
    public List<SimEvent>? Update(IReadOnlyList<Agent> agents, IReadOnlyDictionary<AgentId, Command> commands, long tick, float[] hits)
    {
        List<SimEvent>? events = null;
        foreach (var agent in agents)
        {
            if (!commands.TryGetValue(agent.Id, out var command)
                || !command.LetPast
                || agent is not { InLane: false, HeldAlong: not null }
                || line.Behind(agent) is not { InLane: true, Refused: false } behind)
            {
                continue;
            }

            var after = line.Behind(behind);
            agent.HeldAlong = after is null ? behind.LineAlong + zones.TightBelow : (behind.LineAlong + after.LineAlong) / 2f;
            agent.HeldBehind = after?.Id;
            agent.HoldEndsAt = tick + stepOut.SpotHoldTicks;
            hits[behind.Id.Value] -= letPast.Recovery;
            (events ??= []).Add(new WavedPast(tick, behind.Id, agent.Id));
        }

        return events;
    }
}
