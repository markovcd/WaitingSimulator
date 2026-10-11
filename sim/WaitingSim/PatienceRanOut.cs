namespace WaitingSim;

/// <summary>An agent's patience reached 0.</summary>
public sealed record PatienceRanOut(long Tick, AgentId Agent) : SimEvent(Tick, Agent);
