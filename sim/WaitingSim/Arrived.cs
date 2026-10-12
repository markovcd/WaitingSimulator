namespace WaitingSim;

/// <summary>A newcomer arrived at the entrance: on its way to the back of the line, or as a <paramref name="Cutter"/> looking for a gap.</summary>
public sealed record Arrived(long Tick, AgentId Agent, bool Cutter = false) : SimEvent(Tick, Agent);
