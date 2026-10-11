namespace WaitingSim;

/// <summary>An agent's gap stayed open for the whole grace window.</summary>
public sealed record GraceExpired(long Tick, AgentId Agent) : SimEvent(Tick, Agent);
