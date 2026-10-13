namespace WaitingSim;

/// <summary>The agent's small talk with <paramref name="Listener"/> ended, <paramref name="Listened"/> to or ignored.</summary>
public sealed record SmallTalkEnded(long Tick, AgentId Agent, AgentId Listener, bool Listened) : SimEvent(Tick, Agent);
