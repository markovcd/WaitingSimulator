using System.Numerics;

namespace WaitingSim.Tests;

public sealed class LaneTests
{
    // Front at the origin, running 4 m along x, then turning 4 m along y.
    private static readonly Lane Bent = new([new(0, 0), new(4, 0), new(4, 4)], halfWidth: 0.5f);

    [Fact]
    public void A_point_beside_the_lane_measures_along_it_from_the_front()
    {
        var position = Bent.Measure(new Vector2(1.5f, 0.3f));

        Assert.Equal(1.5f, position.Along, 0.0001f);
        Assert.Equal(0.3f, position.Off, 0.0001f);
    }

    [Fact]
    public void Distance_past_a_bend_is_measured_around_it_not_across_it()
    {
        var position = Bent.Measure(new Vector2(4f, 3f));

        Assert.Equal(7f, position.Along, 0.0001f);
    }

    [Theory]
    [InlineData(2f, 0.5f, true)]
    [InlineData(2f, 0.51f, false)]
    [InlineData(-0.6f, 0f, false)]
    [InlineData(4f, 4.4f, true)]
    [InlineData(4f, 4.6f, false)]
    public void The_lane_holds_points_within_its_half_width_of_its_line(float x, float y, bool held)
    {
        Assert.Equal(held, Bent.Holds(Bent.Measure(new Vector2(x, y))));
    }

    [Fact]
    public void A_lane_with_a_repeated_point_is_refused()
    {
        Assert.Throws<ArgumentException>(() => new Lane([new(0, 0), new(0, 0), new(1, 0)], halfWidth: 0.5f));
    }
}
