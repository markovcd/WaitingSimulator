namespace WaitingSim;

/// <summary>The distractions that compete with watching the line; each one a tuning may leave out.</summary>
/// <param name="DroppedTicket">The player's ticket slipping to the floor beside the lane.</param>
public sealed record DistractionTuning(DroppedTicketTuning? DroppedTicket = null);
