using Godot;

namespace WaitingSimulator.Game;

/// <summary>A built <see cref="Figure"/>: the node that stands and leans, the pivots its limbs swing about and its face.</summary>
internal sealed class FigureNode(Node3D root, IReadOnlyDictionary<Limb, Node3D> limbs, IReadOnlyDictionary<string, Node3D> parts)
{
    /// <summary>How open an eye stays at a full squint, so a squint reads as a glare rather than a blink.</summary>
    private const float SquintOpen = 0.3f;

    private const float BlinkOpen = 0.1f;

    private readonly float browHeight = parts[Figure.LeftBrow].Position.Y;

    public Node3D Root { get; } = root;

    /// <summary>Swings the legs <paramref name="angle"/> radians forward, the left leg first, and the arms against them.</summary>
    public void Swing(float angle)
    {
        limbs[Limb.LeftLeg].Rotation = new Vector3(angle, 0f, 0f);
        limbs[Limb.RightLeg].Rotation = new Vector3(-angle, 0f, 0f);
        limbs[Limb.LeftArm].Rotation = new Vector3(-angle, 0f, 0f);
        limbs[Limb.RightArm].Rotation = new Vector3(angle, 0f, 0f);
    }

    /// <summary>Sets the face to <paramref name="expression"/>, with the eyes shut while <paramref name="blinking"/>.</summary>
    public void Show(Expression expression, bool blinking)
    {
        var open = blinking ? BlinkOpen : 1f - (expression.Squint * (1f - SquintOpen));
        parts[Figure.LeftEye].Scale = new Vector3(1f, open, 1f);
        parts[Figure.RightEye].Scale = new Vector3(1f, open, 1f);

        // Each half of a pair turns the opposite way, so the inner ends of the brows and the corners of the mouth move alike.
        Turn(parts[Figure.LeftBrow], -expression.BrowTilt, browHeight + expression.BrowLift);
        Turn(parts[Figure.RightBrow], expression.BrowTilt, browHeight + expression.BrowLift);
        var mouth = new Vector3(1f, 1f + (expression.MouthOpen / Figure.MouthHeight), 1f);
        parts[Figure.LeftMouth].Rotation = new Vector3(0f, 0f, -expression.MouthCurve);
        parts[Figure.RightMouth].Rotation = new Vector3(0f, 0f, expression.MouthCurve);
        parts[Figure.LeftMouth].Scale = mouth;
        parts[Figure.RightMouth].Scale = mouth;
    }

    private static void Turn(Node3D part, float angle, float height)
    {
        part.Rotation = new Vector3(0f, 0f, angle);
        part.Position = part.Position with { Y = height };
    }
}
