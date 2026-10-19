using System.Numerics;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The low-poly humanoid every agent and the clerk is drawn as: its parts and where they hang, the colors it comes in, how
/// each agent's look is picked from its id so a replay looks the same, and how far its limbs swing while it walks.
/// Facing -z, with its face on the front of its head, it fits front to back inside the sim's body.
/// </summary>
internal static class Figure
{
    /// <summary>How tall a figure stands at its usual size, in meters.</summary>
    public const float Height = 1.71f;

    /// <summary>How high a figure's eyes are, and the player's camera with them.</summary>
    public const float EyeHeight = 1.6f;

    /// <summary>The furthest a limb swings from hanging straight, in radians.</summary>
    public const float MaxSwing = 0.35f;

    private const float HipHeight = 0.86f;
    private const float HipHalfWidth = 0.09f;
    private const float ShoulderHeight = 1.45f;
    private const float ShoulderHalfWidth = 0.245f;

    /// <summary>Ticks for one stride: a swing forward and back.</summary>
    private const float StrideTicks = 0.7f * TickRate.PerSecond;

    private static readonly float[] Heights = [0.95f, 0.975f, 1f, 1.025f, 1.05f];

    private static readonly Paint[] SkinColors = [new(0.86f, 0.71f, 0.6f), new(0.72f, 0.55f, 0.42f), new(0.5f, 0.36f, 0.27f)];

    private static readonly Paint[] ShirtColors =
    [
        new(0.42f, 0.47f, 0.55f), new(0.5f, 0.52f, 0.4f), new(0.58f, 0.42f, 0.4f),
        new(0.66f, 0.58f, 0.38f), new(0.55f, 0.55f, 0.55f), new(0.36f, 0.5f, 0.5f),
    ];

    private static readonly Paint[] HairColors =
    [
        new(0.16f, 0.12f, 0.1f), new(0.32f, 0.22f, 0.14f), new(0.62f, 0.5f, 0.3f), new(0.45f, 0.22f, 0.13f), new(0.55f, 0.54f, 0.52f),
    ];

    private static readonly Paint[] GearColors = [new(0.2f, 0.22f, 0.26f), new(0.6f, 0.2f, 0.18f), new(0.22f, 0.36f, 0.5f), new(0.3f, 0.4f, 0.26f)];

    private static readonly Paint[] TrouserColors = [new(0.25f, 0.26f, 0.28f), new(0.22f, 0.25f, 0.35f), new(0.36f, 0.3f, 0.25f), new(0.55f, 0.5f, 0.4f)];

    public const string LeftEye = "LeftEye";
    public const string RightEye = "RightEye";
    public const string LeftBrow = "LeftBrow";
    public const string RightBrow = "RightBrow";
    public const string LeftMouth = "LeftMouth";
    public const string RightMouth = "RightMouth";

    /// <summary>How far the hair is tipped back, uncovering the face below its front edge.</summary>
    public const float HairTilt = 0.7f;

    /// <summary>How far a cap is tipped back; it sits lower than hair, so further, to clear the brows.</summary>
    public const float CapTilt = 0.8f;

    /// <summary>How tall each half of a shut mouth is, in meters.</summary>
    public const float MouthHeight = 0.008f;

    /// <summary>The color of every figure's shoes.</summary>
    public static Paint Shoes { get; } = new(0.15f, 0.14f, 0.13f);

    /// <summary>The color of every figure's eyes, brows and mouth.</summary>
    public static Paint Features { get; } = new(0.12f, 0.1f, 0.1f);

    /// <summary>The clerk's look: usual size, gray-haired, in a brown shirt.</summary>
    public static Look Clerk { get; } = new(1f, SkinColors[0], new(0.55f, 0.44f, 0.38f), TrouserColors[0], HairColors[^1], GearColors[0], Wear.None);

    public static IReadOnlyList<FigurePart> Parts { get; } =
    [
        new("Hips", PartShape.Box, new(0.3f, 0.16f, 0.19f), new(0f, 0.9f, 0f), Limb.Body, Swatch.Trousers),
        new("Torso", PartShape.Box, new(0.42f, 0.52f, 0.22f), new(0f, 1.22f, 0f), Limb.Body, Swatch.Shirt, Taper: 0.78f),
        new("Neck", PartShape.Capsule, new(0.08f, 0.1f, 0.08f), new(0f, 1.5f, 0f), Limb.Body, Swatch.Skin),
        new("Head", PartShape.Sphere, new(0.22f, 0.22f, 0.22f), new(0f, 1.6f, 0f), Limb.Body, Swatch.Skin),
        new("LeftEar", PartShape.Box, new(0.03f, 0.05f, 0.04f), new(-0.108f, 1.6f, 0.005f), Limb.Body, Swatch.Skin),
        new("RightEar", PartShape.Box, new(0.03f, 0.05f, 0.04f), new(0.108f, 1.6f, 0.005f), Limb.Body, Swatch.Skin),
        new("Hair", PartShape.Hemisphere, new(0.236f, 0.236f, 0.236f), new(0f, 1.592f, 0f), Limb.Body, Swatch.Hair, HairTilt),
        new("Cap", PartShape.Hemisphere, new(0.25f, 0.25f, 0.25f), new(0f, 1.585f, 0f), Limb.Body, Swatch.Gear, CapTilt, Wear: Wear.Cap),
        new("Brim", PartShape.Box, new(0.17f, 0.014f, 0.09f), new(0f, 1.664f, -0.125f), Limb.Body, Swatch.Gear, Wear: Wear.Cap),
        new("Hood", PartShape.Box, new(0.28f, 0.1f, 0.08f), new(0f, 1.49f, 0.12f), Limb.Body, Swatch.Shirt, Taper: 0.7f, Wear: Wear.Hood),
        new("Backpack", PartShape.Box, new(0.28f, 0.36f, 0.08f), new(0f, 1.2f, 0.14f), Limb.Body, Swatch.Gear, Wear: Wear.Backpack),
        new("Nose", PartShape.Box, new(0.04f, 0.05f, 0.05f), new(0f, 1.59f, -0.11f), Limb.Body, Swatch.Skin),
        new(LeftEye, PartShape.Box, new(0.025f, 0.02f, 0.02f), new(-0.038f, 1.625f, -0.1f), Limb.Body, Swatch.Features),
        new(RightEye, PartShape.Box, new(0.025f, 0.02f, 0.02f), new(0.038f, 1.625f, -0.1f), Limb.Body, Swatch.Features),
        new(LeftBrow, PartShape.Box, new(0.045f, 0.01f, 0.02f), new(-0.04f, 1.655f, -0.095f), Limb.Body, Swatch.Features),
        new(RightBrow, PartShape.Box, new(0.045f, 0.01f, 0.02f), new(0.04f, 1.655f, -0.095f), Limb.Body, Swatch.Features),
        new(LeftMouth, PartShape.Box, new(0.03f, MouthHeight, 0.02f), new(-0.015f, 1.555f, -0.098f), Limb.Body, Swatch.Features),
        new(RightMouth, PartShape.Box, new(0.03f, MouthHeight, 0.02f), new(0.015f, 1.555f, -0.098f), Limb.Body, Swatch.Features),
        .. Arm(Limb.LeftArm),
        .. Arm(Limb.RightArm),
        .. Leg(Limb.LeftLeg),
        .. Leg(Limb.RightLeg),
    ];

    /// <summary>Where <paramref name="limb"/> swings from, from the feet of the figure.</summary>
    public static Vector3 Pivot(Limb limb) => limb switch
    {
        Limb.LeftArm => new(-ShoulderHalfWidth, ShoulderHeight, 0f),
        Limb.RightArm => new(ShoulderHalfWidth, ShoulderHeight, 0f),
        Limb.LeftLeg => new(-HipHalfWidth, HipHeight, 0f),
        Limb.RightLeg => new(HipHalfWidth, HipHeight, 0f),
        _ => Vector3.Zero,
    };

    /// <summary>The look of agent <paramref name="agent"/>, the same every time, and different from its neighbors'.</summary>
    public static Look For(AgentId agent)
    {
        var id = agent.Value;
        return new(
            Heights[(id * 3) % Heights.Length],
            SkinColors[(id * 2) % SkinColors.Length],
            ShirtColors[id % ShirtColors.Length],
            TrouserColors[(id * 3) % TrouserColors.Length],
            HairColors[(id * 2) % HairColors.Length],
            GearColors[id % GearColors.Length],
            (id % 3 == 0 ? Wear.Hood : Wear.None) | (id % 5 == 2 ? Wear.Cap : Wear.None) | (id % 4 == 1 ? Wear.Backpack : Wear.None));
    }

    /// <summary>
    /// How far the legs swing forward at <paramref name="tick"/> walking at <paramref name="speed"/>; the arms swing the other
    /// way. A stride at walking speed or faster swings <see cref="MaxSwing"/>; standing still swings nothing.
    /// </summary>
    public static float Swing(float speed, float walkSpeed, float tick) =>
        MaxSwing * Math.Clamp(speed / walkSpeed, 0f, 1f) * MathF.Sin(MathF.Tau * tick / StrideTicks);

    private static FigurePart[] Arm(Limb limb) =>
    [
        new($"{limb}Sleeve", PartShape.Box, new(0.11f, 0.5f, 0.11f), new(0f, -0.24f, 0f), limb, Swatch.Shirt, Taper: 0.75f),
        new($"{limb}Hand", PartShape.Box, new(0.07f, 0.09f, 0.06f), new(0f, -0.53f, 0f), limb, Swatch.Skin, Taper: 0.8f),
    ];

    private static FigurePart[] Leg(Limb limb) =>
    [
        new($"{limb}Trouser", PartShape.Box, new(0.15f, 0.8f, 0.15f), new(0f, -0.4f, 0f), limb, Swatch.Trousers, Taper: 0.8f),
        new($"{limb}Shoe", PartShape.Box, new(0.12f, 0.07f, 0.24f), new(0f, -0.825f, -0.04f), limb, Swatch.Shoes, Taper: 1.1f),
    ];
}
