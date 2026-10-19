using System.Numerics;
using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class SetDressingTests
{
    private static readonly Tuning Shipped = Tuning.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "tuning.json"))).Value!;

    public static TheoryData<string> Lanes() => ["straight", "bent"];

    [Theory]
    [MemberData(nameof(Lanes))]
    public void Every_post_stands_just_outside_the_lane_and_beside_it(string name)
    {
        var lane = Lane(name);
        var posts = SetDressing.Posts(lane);

        Assert.NotEmpty(posts);
        Assert.All(posts, post =>
        {
            var at = lane.Measure(post);
            Assert.InRange(at.Off, lane.HalfWidth + 0.05f, lane.HalfWidth + 0.5f);
            Assert.InRange(at.Along, 0f, lane.Length);
        });
    }

    [Fact]
    public void A_line_s_counter_stands_between_the_clerk_and_the_front_clear_of_the_front_agent()
    {
        var lane = Lane("straight");
        var clerk = new Vector2(0f, -1f);

        var counter = SetDressing.LineCounter(lane, clerk)!;
        var edge = (counter.From + counter.To) / 2f;

        Assert.Equal(new Vector2(0f, 1f), counter.TowardCrowd);
        Assert.InRange(Vector2.Distance(edge, lane.Points[0]), Shipped.Bodies!.Radius, Vector2.Distance(clerk, lane.Points[0]));
        Assert.Equal(SetDressing.LineCounterLength, counter.Length, 0.001f);
    }

    [Fact]
    public void A_clerk_standing_at_the_front_has_no_counter()
    {
        Assert.Null(SetDressing.LineCounter(Lane("straight"), new Vector2(0f, -0.1f)));
    }

    [Fact]
    public void The_back_wall_stands_behind_the_clerk()
    {
        var wall = SetDressing.BackWall(new Vector2(0f, -1f), new Vector2(0f, 3f));

        Assert.True(wall.Y < -1f && wall.X == 0f, $"The wall is at {wall}.");
    }

    private static Lane Lane(string name) => name == "straight"
        ? new Lane([new(0, 0), new(0, 14)], halfWidth: 0.5f)
        : new Lane([new(0, 0), new(0, 4), new(3, 7)], halfWidth: 0.6f);
}
