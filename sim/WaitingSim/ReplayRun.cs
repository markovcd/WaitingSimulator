namespace WaitingSim;

/// <summary>What playing a replay gave: its state hashes, and how many of each event happened, by event name.</summary>
public sealed record ReplayRun(IReadOnlyList<ReplayHash> Hashes, IReadOnlyDictionary<string, int> Events);
