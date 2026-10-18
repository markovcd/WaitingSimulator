namespace WaitingSim;

/// <summary>
/// The agent told the clerk "I was here first" while the clerk asked <paramref name="Against"/>. It is
/// <paramref name="Honest"/> when the agent did arrive first, and the clerk turns to it; otherwise everyone near takes it badly.
/// </summary>
public sealed record CalledOut(long Tick, AgentId Agent, AgentId Against, bool Honest) : SimEvent(Tick, Agent);
