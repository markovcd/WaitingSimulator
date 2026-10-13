namespace WaitingSim;

/// <summary>The agent, standing behind <paramref name="Listener"/>, started making small talk with them.</summary>
public sealed record SmallTalkStarted(long Tick, AgentId Agent, AgentId Listener) : SimEvent(Tick, Agent);
