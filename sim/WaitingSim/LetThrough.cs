namespace WaitingSim;

/// <summary>The agent pushed into <paramref name="Past"/> long enough that the crowd lets it through.</summary>
public sealed record LetThrough(long Tick, AgentId Agent, AgentId Past) : SimEvent(Tick, Agent);
