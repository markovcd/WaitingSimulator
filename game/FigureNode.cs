using Godot;

namespace WaitingSimulator.Game;

/// <summary>A built <see cref="Figure"/>: the node that stands and leans, and the pivots its limbs swing about.</summary>
internal sealed class FigureNode(Node3D root, Node3D leftArm, Node3D rightArm, Node3D leftLeg, Node3D rightLeg)
{
    public Node3D Root { get; } = root;

    /// <summary>Swings the legs <paramref name="angle"/> radians forward, the left leg first, and the arms against them.</summary>
    public void Swing(float angle)
    {
        leftLeg.Rotation = new Vector3(angle, 0f, 0f);
        rightLeg.Rotation = new Vector3(-angle, 0f, 0f);
        leftArm.Rotation = new Vector3(-angle, 0f, 0f);
        rightArm.Rotation = new Vector3(angle, 0f, 0f);
    }
}
