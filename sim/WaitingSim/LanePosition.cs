namespace WaitingSim;

/// <summary>
/// A point measured against the lane: <see cref="Along"/> is the distance along the lane from its front,
/// <see cref="Off"/> how far the point is from the lane's line.
/// </summary>
public readonly record struct LanePosition(float Along, float Off);
