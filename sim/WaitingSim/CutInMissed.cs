namespace WaitingSim;

/// <summary>
/// An agent stepped into the lane ahead of <paramref name="Behind"/> with no room to stand there. The line refuses it a
/// place until it steps out again.
/// </summary>
public sealed record CutInMissed(long Tick, AgentId Agent, AgentId Behind) : SimEvent(Tick, Agent);
