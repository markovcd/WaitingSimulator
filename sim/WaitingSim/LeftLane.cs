namespace WaitingSim;

/// <summary>An agent stepped out of the lane and is no longer part of the line.</summary>
public sealed record LeftLane(long Tick, AgentId Agent) : SimEvent(Tick, Agent);
