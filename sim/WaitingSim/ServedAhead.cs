namespace WaitingSim;

/// <summary>The clerk began serving the agent while <paramref name="Over"/>, who was there first, stood near enough to see it.</summary>
public sealed record ServedAhead(long Tick, AgentId Agent, AgentId Over) : SimEvent(Tick, Agent);
