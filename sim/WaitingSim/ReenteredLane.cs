namespace WaitingSim;

/// <summary>An agent that had left the lane stepped back into it.</summary>
public sealed record ReenteredLane(long Tick, AgentId Agent) : SimEvent(Tick, Agent);
