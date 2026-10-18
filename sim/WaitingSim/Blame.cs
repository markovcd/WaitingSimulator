namespace WaitingSim;

/// <summary>
/// Who cost each agent in a crowd how much patience, so staff called by an agent that ran out move out whoever caused
/// the most of it, not whoever stands nearest.
/// </summary>
internal sealed class Blame
{
    // Sorted, so the hash and every tie read them in one order.
    private readonly SortedDictionary<int, SortedDictionary<int, float>> owed = [];

    /// <summary>Charges <paramref name="cause"/> with <paramref name="amount"/> of <paramref name="victim"/>'s patience.</summary>
    public void Charge(Agent victim, Agent cause, float amount)
    {
        if (victim.Trait is null || victim == cause || amount <= 0f)
        {
            return;
        }

        if (!owed.TryGetValue(victim.Id.Value, out var causes))
        {
            causes = [];
            owed[victim.Id.Value] = causes;
        }

        causes[cause.Id.Value] = causes.GetValueOrDefault(cause.Id.Value) + amount;
    }

    /// <summary>Whom <paramref name="victim"/> blames most among those <paramref name="still"/> holds for, lowest id on a tie; then forgets it all.</summary>
    public AgentId? Settle(Agent victim, Func<AgentId, bool> still)
    {
        if (!owed.Remove(victim.Id.Value, out var causes))
        {
            return null;
        }

        var worst = causes.Where(cause => still(new AgentId(cause.Key))).OrderByDescending(cause => cause.Value).ThenBy(cause => cause.Key).FirstOrDefault();
        return worst.Value > 0f ? new AgentId(worst.Key) : null;
    }

    /// <summary>Adds every charge to <paramref name="hash"/>.</summary>
    public void Hash(StateHash hash)
    {
        hash.Add((long)owed.Count);
        foreach (var (victim, causes) in owed)
        {
            hash.Add((long)victim).Add((long)causes.Count);
            foreach (var (cause, amount) in causes)
            {
                hash.Add((long)cause).Add(amount);
            }
        }
    }
}
