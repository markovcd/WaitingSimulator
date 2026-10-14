namespace WaitingSim;

/// <summary>
/// Staring back: an agent glancing back at the person behind for longer than a glance drains their patience until it
/// looks away. Glancing back while they make small talk is listening, and starts the glance over.
/// </summary>
internal sealed class Staring(StaringTuning staring, Line line)
{
    /// <summary>Tracks each agent's glance back this tick and adds the drain on whoever it stares at to <paramref name="hits"/>.</summary>
    public void Update(IReadOnlyList<Agent> agents, IReadOnlyDictionary<AgentId, Command> commands, long tick, float[] hits, List<SimEvent> events)
    {
        foreach (var agent in agents)
        {
            var glancing = commands.TryGetValue(agent.Id, out var command) && command.GlancingBack;
            if (!glancing || agent.SmallTalkEndsAt is not null)
            {
                agent.GlancingSince = glancing ? tick : null;
                agent.Staring = false;
                continue;
            }

            agent.GlancingSince ??= tick;
            if (tick - agent.GlancingSince + 1 < staring.AfterTicks || line.Behind(agent) is not { Trait: { } trait } behind)
            {
                continue;
            }

            if (!agent.Staring)
            {
                agent.Staring = true;
                events.Add(new StaredAt(tick, behind.Id, agent.Id));
            }

            hits[behind.Id.Value] += staring.DrainPerSecond * trait.DrainScale / TickRate.PerSecond;
        }
    }
}
