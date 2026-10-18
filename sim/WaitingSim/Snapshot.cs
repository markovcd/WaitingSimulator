namespace WaitingSim;

/// <summary>The read-only sim state after <see cref="Tick"/> ticks; agents are in id order. A crowd has its <see cref="Clerk"/>.</summary>
public sealed record Snapshot(long Tick, IReadOnlyList<AgentSnapshot> Agents, ClerkSnapshot? Clerk = null);
