namespace WaitingSimulator.Game;

/// <summary>
/// One cross-section of a lofted part: an octagon <paramref name="Height"/> above the part's center,
/// <paramref name="HalfWidth"/> to either side and <paramref name="HalfDepth"/> to front and back, its middle
/// <paramref name="Back"/> toward the back, all in meters.
/// </summary>
internal readonly record struct Ring(float Height, float HalfWidth, float HalfDepth, float Back = 0f);
