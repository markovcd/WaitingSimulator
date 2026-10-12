using System.Numerics;

namespace WaitingSimulator.Game;

/// <summary>One straight stretch of a <see cref="LaneMarking"/>, along its line from <paramref name="From"/> to <paramref name="To"/>.</summary>
internal readonly record struct LaneStrip(Vector2 From, Vector2 To)
{
    public Vector2 Center => (From + To) / 2f;

    public float Length => Vector2.Distance(From, To);
}
