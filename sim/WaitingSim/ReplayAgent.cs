using System.Numerics;

namespace WaitingSim;

/// <summary>An agent a replay spawns, in id order: where it starts, and its trait id if it has patience.</summary>
public sealed record ReplayAgent(Vector2 Position, string? Trait);
