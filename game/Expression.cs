namespace WaitingSimulator.Game;

/// <summary>
/// How a face is set: <paramref name="BrowTilt"/> drops the brows' inner ends and <paramref name="MouthCurve"/> lifts the
/// mouth's corners, in radians; <paramref name="BrowLift"/> raises the brows and <paramref name="MouthOpen"/> opens the
/// mouth, in meters; <paramref name="Squint"/> closes the eyes, from 0 open to 1 shut.
/// </summary>
internal readonly record struct Expression(float BrowTilt, float BrowLift, float Squint, float MouthCurve, float MouthOpen);
