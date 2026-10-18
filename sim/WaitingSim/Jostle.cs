using System.Numerics;

namespace WaitingSim;

/// <summary>
/// Bodies in a crowd: soft, so two that overlap ease apart over a few ticks and a push carries on through whoever is
/// behind, the way a crowd surges. Being shoved drains the one shoved, charged to the shover, and a shover that keeps at
/// the same agent long enough is let through it.
/// </summary>
internal sealed class Jostle(CrowdTuning tuning, float radius)
{
    /// <summary>How close two bodies ever come, as a share of touching distance, however hard they are pressed.</summary>
    private const float Closest = 0.6f;

    /// <summary>The share of a pair's easing apart that the one moving into the other takes; the rest moves the other.</summary>
    private const float MoverShare = 0.75f;

    // Sorted by (pusher, pushed) and by (lower id, higher id), so the hash reads them in one order.
    private readonly SortedDictionary<(int Pusher, int Pushed), long> pushingSince = [];
    private readonly SortedDictionary<(int, int), long> passingUntil = [];

    /// <summary>Whether <paramref name="first"/> and <paramref name="second"/> are being let through each other.</summary>
    public bool Passing(Agent first, Agent second) => passingUntil.ContainsKey(Pair(first, second));

    /// <summary>
    /// Eases every overlapping pair of <paramref name="agents"/> apart, in agent id order, leaving out an agent that takes
    /// up no room and a pair being let through. Returns each shove: one agent moving into another faster than a shove.
    /// </summary>
    public List<(Agent Pusher, Agent Pushed)> Settle(IReadOnlyList<Agent> agents, Func<Agent, bool> takesRoom)
    {
        var shoves = new List<(Agent, Agent)>();
        var reach = radius * 2f;
        for (var i = 0; i < agents.Count; i++)
        {
            for (var j = i + 1; j < agents.Count; j++)
            {
                var (first, second) = (agents[i], agents[j]);
                var offset = second.Position - first.Position;
                var distance = offset.Length();
                if (distance >= reach || !takesRoom(first) || !takesRoom(second) || Passing(first, second))
                {
                    continue;
                }

                // Two agents on the same spot have no direction between them; any fixed one keeps it deterministic.
                var toSecond = distance > 0f ? offset / distance : Vector2.UnitX;
                var firstClosing = Vector2.Dot(first.Velocity, toSecond);
                var secondClosing = -Vector2.Dot(second.Velocity, toSecond);
                var apart = Math.Max((reach - distance) * tuning.Give, (reach * Closest) - distance);
                var firstShare = firstClosing > secondClosing ? MoverShare : firstClosing < secondClosing ? 1f - MoverShare : 0.5f;
                first.Position -= toSecond * (apart * firstShare);
                second.Position += toSecond * (apart * (1f - firstShare));
                if (Math.Max(firstClosing, secondClosing) > tuning.ShoveMetersPerSecond)
                {
                    shoves.Add(firstClosing > secondClosing ? (first, second) : (second, first));
                }
            }
        }

        return shoves;
    }

    /// <summary>Drains everyone shoved this tick, lets through whoever has shoved long enough, and forgets shoves that stopped.</summary>
    public void Update(List<(Agent Pusher, Agent Pushed)> shoves, long tick, float[] changes, Blame blame, List<SimEvent> events)
    {
        foreach (var pair in passingUntil.Where(pair => pair.Value <= tick).Select(pair => pair.Key).ToList())
        {
            passingUntil.Remove(pair);
        }

        var pushing = new HashSet<(int, int)>();
        foreach (var (pusher, pushed) in shoves)
        {
            var key = (pusher.Id.Value, pushed.Id.Value);
            pushing.Add(key);
            var since = pushingSince.TryGetValue(key, out var started) ? started : pushingSince[key] = tick;
            var drain = tuning.SqueezeDrainPerSecond * (pushed.Trait?.DrainScale ?? 0f) / TickRate.PerSecond;
            changes[pushed.Id.Value] -= drain;
            blame.Charge(pushed, pusher, drain);
            if (tick - since >= tuning.LetThroughAfterTicks)
            {
                pushingSince.Remove(key);
                passingUntil[Pair(pusher, pushed)] = tick + tuning.PassTicks;
                events.Add(new LetThrough(tick, pusher.Id, pushed.Id));
            }
        }

        foreach (var stopped in pushingSince.Keys.Where(key => !pushing.Contains(key)).ToList())
        {
            pushingSince.Remove(stopped);
        }
    }

    /// <summary>Adds every shove and pass under way to <paramref name="hash"/>.</summary>
    public void Hash(StateHash hash)
    {
        hash.Add((long)pushingSince.Count);
        foreach (var ((pusher, pushed), since) in pushingSince)
        {
            hash.Add((long)pusher).Add((long)pushed).Add(since);
        }

        hash.Add((long)passingUntil.Count);
        foreach (var ((first, second), until) in passingUntil)
        {
            hash.Add((long)first).Add((long)second).Add(until);
        }
    }

    private static (int, int) Pair(Agent first, Agent second) =>
        first.Id.Value < second.Id.Value ? (first.Id.Value, second.Id.Value) : (second.Id.Value, first.Id.Value);
}
