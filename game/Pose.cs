namespace WaitingSimulator.Game;

/// <summary>
/// How a body stands this frame, about its feet: <paramref name="Lean"/> tips it toward the front of the line and
/// <paramref name="Sway"/> to its left, in radians; <paramref name="Bob"/> lifts it, in meters.
/// </summary>
internal readonly record struct Pose(float Lean, float Sway, float Bob);
