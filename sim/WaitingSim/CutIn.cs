namespace WaitingSim;

/// <summary>An agent stepped into the lane ahead of <paramref name="Behind"/>, somewhere other than its own place.</summary>
public sealed record CutIn(long Tick, AgentId Agent, AgentId Behind) : SimEvent(Tick, Agent);
