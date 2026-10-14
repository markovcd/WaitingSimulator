namespace WaitingSim;

/// <summary><paramref name="By"/>, standing ahead of the agent, glanced back for so long it became staring at them.</summary>
public sealed record StaredAt(long Tick, AgentId Agent, AgentId By) : SimEvent(Tick, Agent);
