namespace WaitingSim;

/// <summary>An agent stepped up to the counter at the front of the line, close enough to be served.</summary>
public sealed record ReachedCounter(long Tick, AgentId Agent) : SimEvent(Tick, Agent);
