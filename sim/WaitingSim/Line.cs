namespace WaitingSim;

/// <summary>The agents on a lane and who stands where in the line: each measured against the lane, with its person ahead.</summary>
internal sealed class Line(Lane lane, ZoneLimits zones)
{
    private readonly List<Agent> agents = [];

    public IReadOnlyList<Agent> Agents => agents;

    public Agent this[AgentId id] => agents[id.Value];

    public void Add(Agent agent) => agents.Add(agent);

    /// <summary>Measures every agent against the lane.</summary>
    public void Measure()
    {
        foreach (var agent in agents)
        {
            agent.Lane = lane.Measure(agent.Position);
            agent.InLane = lane.Holds(agent.Lane);
        }
    }

    /// <summary>Finds the person ahead of everyone in the line, their spacing and their zone.</summary>
    public void Classify()
    {
        foreach (var agent in agents)
        {
            agent.Ahead = null;
            agent.Spacing = null;
            agent.Zone = null;
        }

        var line = agents.Where(agent => agent.InLine).ToList();
        line.Sort((a, b) => a.LineAlong != b.LineAlong
            ? a.LineAlong.CompareTo(b.LineAlong)
            : a.Id.Value.CompareTo(b.Id.Value));
        for (var i = 1; i < line.Count; i++)
        {
            var agent = line[i];
            var ahead = line[i - 1];
            var spacing = agent.LineAlong - ahead.LineAlong;
            agent.Ahead = ahead.Id;
            agent.Spacing = spacing;
            agent.Zone = zones.ZoneAt(spacing);
        }
    }

    /// <summary>The person behind <paramref name="agent"/>, as the line last stood.</summary>
    public Agent? Behind(Agent agent) => agents.FirstOrDefault(other => other.Ahead == agent.Id);

    /// <summary>
    /// Who stands in the lane refused a place between <paramref name="agent"/> and the person ahead of it, nearest
    /// first, or null for no one. An agent walking off after a kick-out blocks no one.
    /// </summary>
    public Agent? Blocking(Agent agent)
    {
        if (agent is not { InLane: true, Refused: false })
        {
            return null;
        }

        var aheadAlong = agent.Ahead is { } ahead ? this[ahead].LineAlong : float.NegativeInfinity;
        return agents
            .Where(other => other is { InLane: true, Refused: true, Step: not (LineStep.Leaving or LineStep.Done or LineStep.Gone) }
                && other.Lane.Along < agent.Lane.Along
                && other.Lane.Along > aheadAlong)
            .MaxBy(other => (other.Lane.Along, other.Id.Value));
    }

    /// <summary>Who in the line would stand ahead of and behind <paramref name="agent"/> if it took a place at <paramref name="along"/>.</summary>
    public (Agent? Ahead, Agent? Behind) Around(Agent agent, float along)
    {
        var others = agents.Where(other => other != agent && other.InLine).ToList();
        return (
            others.Where(other => !IsBehind(other, along, agent.Id)).MaxBy(other => (other.LineAlong, other.Id.Value)),
            others.Where(other => IsBehind(other, along, agent.Id)).MinBy(other => (other.LineAlong, other.Id.Value)));
    }

    /// <summary>Whether <paramref name="other"/> stands behind a place at <paramref name="along"/> taken by agent <paramref name="id"/>.</summary>
    private static bool IsBehind(Agent other, float along, AgentId id) =>
        other.LineAlong > along || (other.LineAlong == along && other.Id.Value > id.Value);
}
