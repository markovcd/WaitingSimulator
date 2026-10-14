namespace WaitingSim;

/// <summary><paramref name="By"/>, out of the lane with its place held, waved the agent behind it past to take its place.</summary>
public sealed record WavedPast(long Tick, AgentId Agent, AgentId By) : SimEvent(Tick, Agent);
