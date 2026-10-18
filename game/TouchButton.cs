using Godot;

namespace WaitingSimulator.Game;

/// <summary>
/// An on-screen button that presses <paramref name="Action"/> while a finger is on it. <paramref name="Center"/> is a
/// share of the screen's width and height, and <paramref name="Radius"/> a share of its height.
/// </summary>
internal sealed record TouchButton(string Caption, StringName Action, Vector2 Center, float Radius)
{
    private const float Large = 0.08f;
    private const float Small = 0.05f;

    public static TouchButton GlanceBack { get; } = new("GLANCE", Controls.GlanceBack, new(0.9f, 0.5f), Large);
    public static TouchButton LetPast { get; } = new("LET PAST", Controls.LetPast, new(0.9f, 0.78f), Large);
    public static TouchButton Wave { get; } = new("WAVE", Controls.Wave, new(0.9f, 0.78f), Large);
    public static TouchButton CallOut { get; } = new("I WAS HERE", Controls.CallOut, new(0.77f, 0.84f), Large);
    public static TouchButton LineUpAgain { get; } = new("AGAIN", Controls.LineUpAgain, new(0.94f, 0.1f), Small);
    public static TouchButton NextLevel { get; } = new("NEXT", Controls.NextLevel, new(0.85f, 0.1f), Small);

    /// <summary>The line's buttons.</summary>
    public static IReadOnlyList<TouchButton> Line { get; } = [GlanceBack, LetPast, LineUpAgain, NextLevel];

    /// <summary>The crowd's buttons.</summary>
    public static IReadOnlyList<TouchButton> Crowd { get; } = [GlanceBack, Wave, CallOut, LineUpAgain, NextLevel];

    /// <summary>Where the button's middle is on a screen of <paramref name="size"/>.</summary>
    public Vector2 At(Vector2 size) => Center * size;

    /// <summary>How big the button is, in pixels, on a screen of <paramref name="size"/>.</summary>
    public float RadiusOn(Vector2 size) => Radius * size.Y;

    /// <summary>Whether a finger at <paramref name="point"/> on a screen of <paramref name="size"/> is on the button.</summary>
    public bool Holds(Vector2 point, Vector2 size) => point.DistanceTo(At(size)) <= RadiusOn(size);
}
