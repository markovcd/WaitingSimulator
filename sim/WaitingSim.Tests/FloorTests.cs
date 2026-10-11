using System.Numerics;
using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class FloorTests
{
    public static TheoryData<float, float> Directions() => new()
    {
        { 0f, -1f },
        { 0f, 1f },
        { 1f, 0f },
        { -1f, 0f },
        { 0.6f, -0.8f },
    };

    [Theory]
    [MemberData(nameof(Directions))]
    public void Pushing_forward_walks_the_way_the_body_faces(float x, float y)
    {
        var direction = new Vector2(x, y);

        var move = Floor.Walk(new Vector2(0, -1), Floor.YawFacing(direction)).Move;

        Assert.Equal(x, move.X, 0.0001f);
        Assert.Equal(y, move.Y, 0.0001f);
    }

    [Theory]
    [MemberData(nameof(Directions))]
    public void Pushing_right_walks_to_the_right_of_the_way_the_body_faces(float x, float y)
    {
        // Seen from above with y up the screen's way down, the right of (x, y) is (-y, x).
        var move = Floor.Walk(new Vector2(1, 0), Floor.YawFacing(new Vector2(x, y))).Move;

        Assert.Equal(-y, move.X, 0.0001f);
        Assert.Equal(x, move.Y, 0.0001f);
    }

    [Fact]
    public void Facing_yaw_0_faces_the_way_Godot_calls_forward()
    {
        var direction = new Vector2(0, -1);

        Assert.Equal(0f, Floor.YawFacing(direction));
        Assert.Equal(Godot.Vector3.Forward, Floor.ToWorld(direction));
    }
}
