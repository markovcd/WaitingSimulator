namespace WaitingSim;

/// <summary>When a cutter slides into an open gap, and what it costs.</summary>
/// <param name="AfterGraceTicks">How long past its grace window a gap stays open before a cutter goes for it.</param>
/// <param name="BlameHit">Points lost by the person behind the one a cutter slid in ahead of, who blames them for it.</param>
public sealed record CutterTuning(int AfterGraceTicks, float BlameHit);
