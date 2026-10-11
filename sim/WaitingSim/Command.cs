using System.Numerics;

namespace WaitingSim;

/// <summary>
/// One agent's input for one tick. A player's input and an NPC brain both produce this.
/// <see cref="Move"/> is the wanted walking direction on the floor plane, at most 1 long.
/// </summary>
public readonly record struct Command(Vector2 Move)
{
    /// <summary>Standing still.</summary>
    public static Command Idle { get; } = new(Vector2.Zero);

    /// <summary>The command the sim acts on: a non-finite move is standing still, a long one is cut to length 1.</summary>
    internal Command Validated()
    {
        if (!float.IsFinite(Move.X) || !float.IsFinite(Move.Y))
        {
            return Idle;
        }

        return Move.LengthSquared() > 1f ? new(Vector2.Normalize(Move)) : this;
    }
}
