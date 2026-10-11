namespace WaitingSim.Runner;

/// <summary>How one swept line ended for the person behind the player.</summary>
/// <param name="FinalPatience">The person behind's patience at the end.</param>
/// <param name="RanOut">Whether their patience reached 0.</param>
/// <param name="GraceExpiries">How many times the player let the grace window run out.</param>
/// <param name="PatienceCurve">The person behind's patience at the end of every sample period.</param>
internal sealed record LineResult(float FinalPatience, bool RanOut, int GraceExpiries, IReadOnlyList<float> PatienceCurve);
