using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class HeadTests
{
    [Fact]
    public void The_head_tilts_no_further_than_short_of_straight_up_or_down()
    {
        var head = new Head(0f);

        head.Turn(0f, 10f);
        Assert.Equal(Head.MaxPitch, head.Pitch);

        head.Turn(0f, -20f);
        Assert.Equal(-Head.MaxPitch, head.Pitch);
    }

    [Fact]
    public void Glancing_back_turns_the_view_round_and_leaves_the_body_facing_the_same_way()
    {
        var head = new Head(0.5f) { GlancingBack = true };

        Assert.Equal(0.5f, head.Yaw);
        Assert.Equal(0.5f - MathF.PI, head.ViewYaw, 0.0001f);
    }

    [Theory]
    [InlineData(7f)]
    [InlineData(-7f)]
    [InlineData(1000f)]
    public void Turning_round_and_round_keeps_the_yaw_within_one_turn(float turn)
    {
        var head = new Head(0f);

        head.Turn(turn, 0f);

        Assert.InRange(head.Yaw, -MathF.PI, MathF.PI);
        Assert.Equal(MathF.Sin(turn), MathF.Sin(head.Yaw), 0.001f);
    }

    [Fact]
    public void A_turn_that_is_not_a_number_is_ignored()
    {
        var head = new Head(0.25f);

        head.Turn(float.NaN, float.PositiveInfinity);

        Assert.Equal(0.25f, head.Yaw);
        Assert.Equal(0f, head.Pitch);
    }
}
