namespace WaitingSim;

/// <summary>A cutter slid into the lane ahead of <paramref name="Behind"/>, into the gap they left open.</summary>
public sealed record CutterEntered(long Tick, AgentId Agent, AgentId Behind) : SimEvent(Tick, Agent);
