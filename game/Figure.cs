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

    /// <summary>The middle of the head, which its hair and cap are shaped round.</summary>
    private static readonly Vector3 HeadCenter = new(0f, 1.6f, 0f);

    /// <summary>How tall each half of a shut mouth is, in meters.</summary>
    public const float MouthHeight = 0.008f;

    /// <summary>The color of every figure's shoes.</summary>
    public static Paint Shoes { get; } = new(0.15f, 0.14f, 0.13f);

    /// <summary>The color of every shoe's sole.</summary>
    public static Paint Sole { get; } = new(0.9f, 0.89f, 0.86f);

    /// <summary>The color of every figure's eyes, brows and mouth.</summary>
    public static Paint Features { get; } = new(0.12f, 0.1f, 0.1f);

    /// <summary>The clerk's look: usual size, gray-haired, in a brown shirt.</summary>
    public static Look Clerk { get; } = new(1f, SkinColors[0], new(0.55f, 0.44f, 0.38f), TrouserColors[0], HairColors[^1], GearColors[0], Wear.None);

    public static IReadOnlyList<FigurePart> Parts { get; } =
    [
        Loft("Hips", new(0f, 0.9f, 0f), Limb.Body, Swatch.Trousers, [new(-0.09f, 0.14f, 0.09f), new(0f, 0.155f, 0.1f), new(0.08f, 0.15f, 0.095f)]),
        Loft("Torso", new(0f, 1.22f, 0f), Limb.Body, Swatch.Shirt,
            [new(-0.26f, 0.15f, 0.095f), new(-0.1f, 0.155f, 0.1f), new(0.08f, 0.175f, 0.11f), new(0.2f, 0.19f, 0.105f), new(0.26f, 0.11f, 0.08f)]),
        new("Neck", PartShape.Capsule, new(0.08f, 0.1f, 0.08f), new(0f, 1.5f, 0f), Limb.Body, Swatch.Skin),

        // The head and what covers it are faceted shells round one center: hair and a cap sit outside the head except
        // at the face, where they pass behind it and leave a hairline.
        Loft("Head", HeadCenter, Limb.Body, Swatch.Skin,
            [new(-0.115f, 0.04f, 0.05f, -0.02f), new(-0.075f, 0.07f, 0.085f, -0.01f), new(-0.02f, 0.082f, 0.1f), new(0.04f, 0.088f, 0.105f), new(0.075f, 0.075f, 0.09f), new(0.09f, 0.04f, 0.05f)]),
        new("LeftEar", PartShape.Box, new(0.025f, 0.05f, 0.04f), new(-0.09f, 1.6f, 0.005f), Limb.Body, Swatch.Skin),
        new("RightEar", PartShape.Box, new(0.025f, 0.05f, 0.04f), new(0.09f, 1.6f, 0.005f), Limb.Body, Swatch.Skin),
        Loft("Hair", HeadCenter, Limb.Body, Swatch.Hair,
            [new(-0.03f, 0.092f, 0.095f, 0.03f), new(0.03f, 0.095f, 0.107f, 0.012f), new(0.06f, 0.094f, 0.108f, 0.01f), new(0.08f, 0.09f, 0.11f, 0.004f), new(0.1f, 0.07f, 0.09f), new(0.11f, 0.04f, 0.06f)]),
        Loft("Cap", HeadCenter, Limb.Body, Swatch.Gear,
            [new(0.05f, 0.1f, 0.112f, 0.018f), new(0.075f, 0.098f, 0.118f, 0.006f), new(0.1f, 0.078f, 0.1f), new(0.11f, 0.05f, 0.064f)], Wear.Cap),
        new("Brim", PartShape.Box, new(0.17f, 0.014f, 0.09f), new(0f, 1.672f, -0.125f), Limb.Body, Swatch.Gear, Wear: Wear.Cap),
        Loft("Hood", new(0f, 1.47f, 0.11f), Limb.Body, Swatch.Shirt, [new(-0.05f, 0.1f, 0.03f), new(0f, 0.15f, 0.05f), new(0.05f, 0.13f, 0.04f, -0.01f)], Wear.Hood),
        new("Backpack", PartShape.Box, new(0.28f, 0.36f, 0.08f), new(0f, 1.2f, 0.14f), Limb.Body, Swatch.Gear, Wear: Wear.Backpack),
        new("Nose", PartShape.Box, new(0.028f, 0.04f, 0.03f), new(0f, 1.59f, -0.108f), Limb.Body, Swatch.Skin),
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

    /// <summary>How far each arm hangs out from straight down, in radians, so arms clear the body.</summary>
    public const float ArmSplay = 0.07f;

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

    /// <summary>A lofted part through <paramref name="rings"/>, sized by the box round them.</summary>
    private static FigurePart Loft(string name, Vector3 center, Limb limb, Swatch swatch, Ring[] rings, Wear wear = Wear.None)
    {
        var width = rings.Max(ring => ring.HalfWidth) * 2f;
        var depth = rings.Max(ring => ring.HalfDepth + MathF.Abs(ring.Back)) * 2f;
        return new(name, PartShape.Loft, new(width, rings[^1].Height - rings[0].Height, depth), center, limb, swatch, Wear: wear, Rings: rings);
    }

    private static FigurePart[] Arm(Limb limb) =>
    [
        Loft($"{limb}Sleeve", new(0f, -0.24f, 0f), limb, Swatch.Shirt, [new(-0.26f, 0.04f, 0.04f), new(0f, 0.046f, 0.046f), new(0.2f, 0.052f, 0.052f), new(0.26f, 0.035f, 0.035f)]),
        new($"{limb}Hand", PartShape.Box, new(0.07f, 0.09f, 0.06f), new(0f, -0.53f, 0f), limb, Swatch.Skin, Taper: 0.8f),
    ];

    private static FigurePart[] Leg(Limb limb) =>
    [
        Loft($"{limb}Trouser", new(0f, -0.4f, 0f), limb, Swatch.Trousers, [new(-0.4f, 0.06f, 0.065f), new(-0.36f, 0.055f, 0.06f), new(0f, 0.065f, 0.07f), new(0.4f, 0.075f, 0.08f)]),
        new($"{limb}Shoe", PartShape.Box, new(0.11f, 0.05f, 0.22f), new(0f, -0.81f, -0.035f), limb, Swatch.Shoes, Taper: 1.1f),
        new($"{limb}Sole", PartShape.Box, new(0.125f, 0.025f, 0.26f), new(0f, -0.8475f, -0.04f), limb, Swatch.Sole),
    ];
}
