namespace WaitingSim;

/// <summary>The agent reached the counter without its ticket, and the clerk sent it away unserved.</summary>
public sealed record TurnedAway(long Tick, AgentId Agent) : SimEvent(Tick, Agent);
