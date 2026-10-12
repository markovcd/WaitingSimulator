namespace WaitingSim;

/// <summary>How often newcomers arrive to join the back of the line.</summary>
/// <param name="IntervalTicks">How long after one arrives the next one does, drawn for each.</param>
public sealed record ArrivalTuning(TickRange IntervalTicks);
