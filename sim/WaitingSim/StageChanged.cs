namespace WaitingSim;

/// <summary>The stage an agent shows changed.</summary>
public sealed record StageChanged(long Tick, AgentId Agent, Stage From, Stage To) : SimEvent(Tick, Agent);
