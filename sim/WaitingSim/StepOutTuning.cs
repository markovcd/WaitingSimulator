namespace WaitingSim;

/// <summary>
/// How the line treats an agent that steps out of the lane and back in. Cutting in costs the person cut in front of
/// patience by how open they left the gap taken: least for a gap past its grace window, most for a loose spacing.
/// </summary>
/// <param name="SpotHoldTicks">How long the line holds the place of an agent that stepped out.</param>
/// <param name="PastGraceHit">Points lost by the person cut in front of when their gap was past its grace window.</param>
/// <param name="GapHit">Points lost by the person cut in front of when their gap was still within it.</param>
/// <param name="LooseHit">Points lost by the person cut in front of when they had no gap open.</param>
/// <param name="MissHit">Points lost by the person an agent tried to cut in front of without room to.</param>
public sealed record StepOutTuning(int SpotHoldTicks, float PastGraceHit, float GapHit, float LooseHit, float MissHit);
