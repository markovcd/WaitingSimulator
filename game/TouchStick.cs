using Godot;

namespace WaitingSimulator.Game;

/// <summary>The on-screen stick: a finger dragged from where it landed leans it the way a gamepad stick leans.</summary>
internal static class TouchStick
{
    /// <summary>The lean of a finger at <paramref name="finger"/> that landed at <paramref name="origin"/>: along the drag, full at <paramref name="radius"/> and no further.</summary>
    public static Vector2 Lean(Vector2 origin, Vector2 finger, float radius) => ((finger - origin) / radius).LimitLength(1f);

    /// <summary>The strength each move action takes from <paramref name="lean"/>, in the order right, left, back, forward.</summary>
    public static (float Right, float Left, float Back, float Forward) Strengths(Vector2 lean)
        => (Math.Max(lean.X, 0f), Math.Max(-lean.X, 0f), Math.Max(lean.Y, 0f), Math.Max(-lean.Y, 0f));
}
