using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// How the sim's floor plane sits in Godot's world: sim (x, y) is world (x, z), with y up. Facing yaw 0 is facing
/// world -z, Godot's forward, which is sim -y.
/// </summary>
internal static class Floor
{
    /// <summary>The world point <paramref name="height"/> above sim point <paramref name="point"/>.</summary>
    public static Godot.Vector3 ToWorld(System.Numerics.Vector2 point, float height = 0f) => new(point.X, height, point.Y);

    /// <summary>The yaw that faces sim direction <paramref name="direction"/>.</summary>
    public static float YawFacing(System.Numerics.Vector2 direction) => MathF.Atan2(-direction.X, -direction.Y);

    /// <summary>
    /// The command for a stick (or keys) held at <paramref name="stick"/> by a body facing <paramref name="yaw"/>.
    /// The stick is as Godot reads it: x to the right, y back, at most 1 long.
    /// </summary>
    public static Command Walk(System.Numerics.Vector2 stick, float yaw)
    {
        var (sin, cos) = MathF.SinCos(yaw);
        return new(new((stick.X * cos) + (stick.Y * sin), (stick.Y * cos) - (stick.X * sin)));
    }
}
