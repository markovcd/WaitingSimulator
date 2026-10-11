namespace WaitingSim;

/// <summary>The read-only sim state after <see cref="Tick"/> ticks; agents are in id order.</summary>
public sealed record Snapshot(long Tick, IReadOnlyList<AgentSnapshot> Agents);
