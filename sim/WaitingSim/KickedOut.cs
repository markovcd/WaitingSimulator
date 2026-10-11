namespace WaitingSim;

/// <summary>
/// <paramref name="By"/> ran out of patience and pushed past the agent, which loses its place in the line: it is
/// refused one until it steps out, and an agent the line moves walks off.
/// </summary>
public sealed record KickedOut(long Tick, AgentId Agent, AgentId By) : SimEvent(Tick, Agent);
