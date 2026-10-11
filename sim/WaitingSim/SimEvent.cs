namespace WaitingSim;

/// <summary>Something the sim reports happened to an agent on a tick, for the client to show.</summary>
public abstract record SimEvent(long Tick, AgentId Agent);
