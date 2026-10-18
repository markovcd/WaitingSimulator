namespace WaitingSim;

/// <summary>
/// Pushing in a crowd: being pushed into drains the one pushed, charged to the pusher, and a pusher that keeps at the
/// same agent long enough is let through it.
/// </summary>
internal sealed class Jostle(CrowdTuning tuning)
{
    // Sorted by (pusher, pushed) and by (lower id, higher id), so the hash reads them in one order.
    private readonly SortedDictionary<(int Pusher, int Pushed), long> pushingSince = [];
    private readonly SortedDictionary<(int, int), long> passingUntil = [];

    /// <summary>Whether <paramref name="first"/> and <paramref name="second"/> are being let through each other.</summary>
    public bool Passing(Agent first, Agent second) => passingUntil.ContainsKey(Pair(first, second));

    /// <summary>Drains everyone pushed this tick, lets through whoever has pushed long enough, and forgets pushes that stopped.</summary>
    public void Update(List<(Agent Pusher, Agent Pushed)> pushes, long tick, float[] changes, Blame blame, List<SimEvent> events)
    {
        foreach (var pair in passingUntil.Where(pair => pair.Value <= tick).Select(pair => pair.Key).ToList())
        {
            passingUntil.Remove(pair);
        }

        var pushing = new HashSet<(int, int)>();
        foreach (var (pusher, pushed) in pushes)
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

    /// <summary>Adds every push and pass under way to <paramref name="hash"/>.</summary>
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
