namespace WaitingSim;

/// <summary>A newcomer arrived at the entrance, on its way to the back of the line.</summary>
public sealed record Arrived(long Tick, AgentId Agent) : SimEvent(Tick, Agent);
