namespace WaitingSim;

/// <summary>How agents keep out of each other on the floor.</summary>
/// <param name="Radius">The radius of the circle each agent takes up, in meters.</param>
/// <param name="Rebound">The share of the speed an agent walked into another at that it bounces back with, above 0 and at most 1.</param>
public sealed record BodyTuning(float Radius, float Rebound);
