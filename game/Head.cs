namespace WaitingSimulator.Game;

/// <summary>
/// Where the player looks. <see cref="Yaw"/> is the way the body faces, which walking follows; glancing back turns the
/// view, not the body. Angles are radians in Godot's sense: yaw turns left about the up axis, pitch tilts up.
/// </summary>
internal sealed class Head(float yaw)
{
    /// <summary>How far the head tilts up or down, short of straight up or down.</summary>
    public const float MaxPitch = 1.4f;

    public float Yaw { get; private set; } = yaw;

    public float Pitch { get; private set; }

    public bool GlancingBack { get; set; }

    /// <summary>The way the view faces: the body's way, or behind it while glancing back.</summary>
    public float ViewYaw => GlancingBack ? Wrap(Yaw + MathF.PI) : Yaw;

    /// <summary>Turns the body by <paramref name="yaw"/> and tilts the head by <paramref name="pitch"/>, within <see cref="MaxPitch"/>.</summary>
    public void Turn(float yaw, float pitch)
    {
        if (float.IsFinite(yaw))
        {
            Yaw = Wrap(Yaw + yaw);
        }

        if (float.IsFinite(pitch))
        {
            Pitch = Math.Clamp(Pitch + pitch, -MaxPitch, MaxPitch);
        }
    }

    /// <summary>The same angle, from -π up to π.</summary>
    private static float Wrap(float angle) => angle - (MathF.Tau * MathF.Floor((angle + MathF.PI) / MathF.Tau));
}
