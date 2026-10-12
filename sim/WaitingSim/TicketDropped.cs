using System.Numerics;

namespace WaitingSim;

/// <summary>The agent's ticket slipped and landed on the floor <paramref name="At"/>, just outside the lane.</summary>
public sealed record TicketDropped(long Tick, AgentId Agent, Vector2 At) : SimEvent(Tick, Agent);
