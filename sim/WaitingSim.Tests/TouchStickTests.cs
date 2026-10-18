using Godot;
using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class TouchStickTests
{
    [Fact]
    public void A_finger_dragged_past_the_reach_leans_the_stick_all_the_way_and_no_further()
    {
        var lean = TouchStick.Lean(new Vector2(100f, 100f), new Vector2(400f, 100f), 50f);

        Assert.Equal(new Vector2(1f, 0f), lean);
    }

    [Fact]
    public void A_finger_dragged_half_the_reach_leans_the_stick_half_way()
    {
        var lean = TouchStick.Lean(new Vector2(100f, 100f), new Vector2(100f, 75f), 50f);

        Assert.Equal(-0.5f, lean.Y, 0.0001f);
        Assert.Equal(0f, lean.X, 0.0001f);
    }

    [Theory]
    [InlineData(0f, -1f, 0f, 0f, 0f, 1f)]
    [InlineData(0f, 1f, 0f, 0f, 1f, 0f)]
    [InlineData(-0.5f, 0f, 0f, 0.5f, 0f, 0f)]
    [InlineData(0.6f, -0.8f, 0.6f, 0f, 0f, 0.8f)]
    public void Dragging_up_the_screen_walks_forward_and_sideways_strafes(float x, float y, float right, float left, float back, float forward)
    {
        Assert.Equal((right, left, back, forward), TouchStick.Strengths(new Vector2(x, y)));
    }
}
