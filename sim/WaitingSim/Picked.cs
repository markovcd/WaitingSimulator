namespace WaitingSim;

/// <summary>The clerk's glance landed on the agent, and the clerk asks it "Yes?" before serving it.</summary>
public sealed record Picked(long Tick, AgentId Agent) : SimEvent(Tick, Agent);
