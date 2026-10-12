namespace WaitingSim;

/// <summary>
/// How the line treats an agent that steps out of the lane and back in. A cut in is made only into a gap left open past
/// its grace window, and costs the person cut in front of <see cref="PastGraceHit"/>; a missed one costs them
/// <see cref="MissHit"/>.
/// Everyone passed, who stood ahead of the agent's place and ends up behind it, takes a hit too, and the person cut in
/// front of takes one for each person passed.
/// </summary>
/// <param name="SpotHoldTicks">How long the line holds the place of an agent that stepped out.</param>
/// <param name="PastGraceHit">Points lost by the person cut in front of, whose gap was past its grace window.</param>
/// <param name="GapHit">Unused: a cut in into a gap still within its grace window is a miss. Read so older files load.</param>
/// <param name="LooseHit">Unused: a cut in where no gap is open is a miss. Read so older files load.</param>
/// <param name="MissHit">Points lost by the person an agent tried to cut in front of without room to.</param>
/// <param name="PassedHit">Points lost by each person passed, and by the person cut in front of for each person passed.</param>
public sealed record StepOutTuning(int SpotHoldTicks, float PastGraceHit, float GapHit, float LooseHit, float MissHit, float PassedHit = 0f);
