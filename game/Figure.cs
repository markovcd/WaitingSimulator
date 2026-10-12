using System.Numerics;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The crude humanoid every agent and the clerk is drawn as: its parts and where they hang, the colors it comes in, how
/// each agent's look is picked from its id so a replay looks the same, and how far its limbs swing while it walks.
/// Facing -z, it fits front to back inside the sim's body.
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
    private const float ShoulderHalfWidth = 0.22f;

    /// <summary>Ticks for one stride: a swing forward and back.</summary>
    private const float StrideTicks = 0.7f * TickRate.PerSecond;

    private static readonly float[] Heights = [0.95f, 0.975f, 1f, 1.025f, 1.05f];

    private static readonly Paint[] SkinColors = [new(0.86f, 0.71f, 0.6f), new(0.72f, 0.55f, 0.42f), new(0.5f, 0.36f, 0.27f)];

    private static readonly Paint[] ShirtColors =
    [
        new(0.42f, 0.47f, 0.55f), new(0.5f, 0.52f, 0.4f), new(0.58f, 0.42f, 0.4f),
        new(0.66f, 0.58f, 0.38f), new(0.55f, 0.55f, 0.55f), new(0.36f, 0.5f, 0.5f),
    ];

    private static readonly Paint[] TrouserColors = [new(0.25f, 0.26f, 0.28f), new(0.22f, 0.25f, 0.35f), new(0.36f, 0.3f, 0.25f), new(0.55f, 0.5f, 0.4f)];

    /// <summary>The color of every figure's shoes.</summary>
    public static Paint Shoes { get; } = new(0.15f, 0.14f, 0.13f);

    /// <summary>The clerk's look: usual size, in a brown shirt.</summary>
    public static Look Clerk { get; } = new(1f, SkinColors[0], new(0.55f, 0.44f, 0.38f), TrouserColors[0]);

    public static IReadOnlyList<FigurePart> Parts { get; } =
    [
        new("Hips", PartShape.Box, new(0.3f, 0.16f, 0.18f), new(0f, 0.9f, 0f), Limb.Body, Swatch.Trousers),
        new("Torso", PartShape.Box, new(0.36f, 0.5f, 0.2f), new(0f, 1.23f, 0f), Limb.Body, Swatch.Shirt),
        new("Neck", PartShape.Capsule, new(0.08f, 0.1f, 0.08f), new(0f, 1.5f, 0f), Limb.Body, Swatch.Skin),
        new("Head", PartShape.Sphere, new(0.22f, 0.22f, 0.22f), new(0f, 1.6f, 0f), Limb.Body, Swatch.Skin),
        new("Nose", PartShape.Box, new(0.04f, 0.05f, 0.05f), new(0f, 1.59f, -0.11f), Limb.Body, Swatch.Skin),
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
        return new(Heights[(id * 3) % Heights.Length], SkinColors[(id * 2) % SkinColors.Length], ShirtColors[id % ShirtColors.Length], TrouserColors[(id * 3) % TrouserColors.Length]);
    }

    /// <summary>
    /// How far the legs swing forward at <paramref name="tick"/> walking at <paramref name="speed"/>; the arms swing the other
    /// way. A stride at walking speed or faster swings <see cref="MaxSwing"/>; standing still swings nothing.
    /// </summary>
    public static float Swing(float speed, float walkSpeed, float tick) =>
        MaxSwing * Math.Clamp(speed / walkSpeed, 0f, 1f) * MathF.Sin(MathF.Tau * tick / StrideTicks);

    private static FigurePart[] Arm(Limb limb) =>
    [
        new($"{limb}Sleeve", PartShape.Capsule, new(0.09f, 0.5f, 0.09f), new(0f, -0.25f, 0f), limb, Swatch.Shirt),
        new($"{limb}Hand", PartShape.Sphere, new(0.08f, 0.08f, 0.08f), new(0f, -0.54f, 0f), limb, Swatch.Skin),
    ];

    private static FigurePart[] Leg(Limb limb) =>
    [
        new($"{limb}Trouser", PartShape.Capsule, new(0.13f, 0.8f, 0.13f), new(0f, -0.4f, 0f), limb, Swatch.Trousers),
        new($"{limb}Shoe", PartShape.Box, new(0.11f, 0.06f, 0.2f), new(0f, -0.83f, -0.03f), limb, Swatch.Shoes),
    ];
}
