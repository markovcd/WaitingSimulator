using System.Numerics;

namespace WaitingSim;

/// <summary>
/// Keeps agents out of each other: each takes up a circle on the floor. An agent that walks into another is pushed back
/// out of it and bounces off, and the one walked into stays where it is; two walking into each other alike share the push.
/// </summary>
internal sealed class Bodies(BodyTuning tuning)
{
    /// <summary>
    /// Pushes every overlapping pair of <paramref name="agents"/> apart, in agent id order, leaving out an agent that
    /// takes up no room and a pair that is <paramref name="passing"/> through each other. Each push of one agent walking
    /// into another is added to <paramref name="pushes"/>, when there is one.
    /// </summary>
    public void Separate(IReadOnlyList<Agent> agents, Func<Agent, bool> takesRoom, Func<Agent, Agent, bool>? passing = null, List<(Agent Pusher, Agent Pushed)>? pushes = null)
    {
        var reach = tuning.Radius * 2f;
        for (var i = 0; i < agents.Count; i++)
        {
            for (var j = i + 1; j < agents.Count; j++)
            {
                var (first, second) = (agents[i], agents[j]);
                var offset = second.Position - first.Position;
                var distance = offset.Length();
                if (distance >= reach || !takesRoom(first) || !takesRoom(second) || passing?.Invoke(first, second) == true)
                {
                    continue;
                }

                // Two agents on the same spot have no direction between them; any fixed one keeps it deterministic.
                var toSecond = distance > 0f ? offset / distance : Vector2.UnitX;
                var firstClosing = Vector2.Dot(first.Velocity, toSecond);
                var secondClosing = -Vector2.Dot(second.Velocity, toSecond);
                var firstShare = firstClosing > secondClosing ? 1f : firstClosing < secondClosing ? 0f : 0.5f;
                var overlap = reach - distance;
                Push(first, -toSecond, overlap * firstShare);
                Push(second, toSecond, overlap * (1f - firstShare));
                if (pushes is not null && firstShare != 0.5f)
                {
                    pushes.Add(firstShare > 0.5f ? (first, second) : (second, first));
                }
            }
        }
    }

    /// <summary>Moves the agent <paramref name="distance"/> along <paramref name="away"/> and bounces back what it walked in with.</summary>
    private void Push(Agent agent, Vector2 away, float distance)
    {
        if (distance <= 0f)
        {
            return;
        }

        agent.Position += away * distance;
        var into = -Vector2.Dot(agent.Velocity, away);
        if (into > 0f)
        {
            agent.Velocity += away * (into * (1f + tuning.Rebound));
        }
    }
}
