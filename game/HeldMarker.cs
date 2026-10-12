using System.Numerics;

namespace WaitingSimulator.Game;

/// <summary>
/// The mark on the lane at the player's held place: at <paramref name="Point"/>, across the lane from
/// <paramref name="TowardFront"/>, with <paramref name="Left"/> of the hold remaining, from 1 down to 0.
/// </summary>
internal readonly record struct HeldMarker(Vector2 Point, Vector2 TowardFront, float Left);
