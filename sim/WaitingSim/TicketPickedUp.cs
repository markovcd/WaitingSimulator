namespace WaitingSim;

/// <summary>The agent picked its dropped ticket up off the floor.</summary>
public sealed record TicketPickedUp(long Tick, AgentId Agent) : SimEvent(Tick, Agent);
