namespace WaitingSim;

/// <summary>How often newcomers arrive to join the back of the line, and how many of them come to cut in.</summary>
/// <param name="IntervalTicks">How long after one arrives the next one does, drawn for each.</param>
/// <param name="CutterShare">The share of newcomers, from 0 to 1, that come as cutters looking for a gap to slide into.</param>
/// <param name="MaxCutters">The most cutters waiting beside the line at once; a newcomer past it joins the back.</param>
public sealed record ArrivalTuning(TickRange IntervalTicks, float CutterShare = 0f, int MaxCutters = 1);
