namespace WaitingSim;

/// <summary>How the player's ticket slips: how often, and how near it has to be to pick it up.</summary>
/// <param name="IntervalTicks">How long the ticket stays in hand before it slips, drawn each time it is picked up.</param>
/// <param name="PickupMeters">How near the ticket the player has to walk to pick it up, in meters.</param>
public sealed record DroppedTicketTuning(TickRange IntervalTicks, float PickupMeters);
