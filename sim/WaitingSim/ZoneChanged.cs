namespace WaitingSim;

/// <summary>
/// An agent's zone to the person ahead changed. Null is no zone: out of the lane, or nobody ahead.
/// A change into <see cref="Zone.Gap"/> is a gap opening and starts the grace window.
/// </summary>
public sealed record ZoneChanged(long Tick, AgentId Agent, Zone? From, Zone? To) : SimEvent(Tick, Agent);
