namespace WaitingSim;

/// <summary>An agent stepped out of the lane. With a spot hold, the line keeps its place for a while.</summary>
public sealed record LeftLane(long Tick, AgentId Agent) : SimEvent(Tick, Agent);
