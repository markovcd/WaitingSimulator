namespace WaitingSim;

/// <summary>The place the line held for an agent that stepped out is gone: the hold ran out, or it stepped back in elsewhere.</summary>
public sealed record SpotLost(long Tick, AgentId Agent) : SimEvent(Tick, Agent);
