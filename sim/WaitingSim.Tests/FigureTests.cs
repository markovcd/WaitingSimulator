using System.Numerics;
using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class FigureTests
{
    private static readonly Tuning Shipped = Tuning.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "tuning.json"))).Value!;

    [Fact]
    public void A_figure_stands_on_the_floor_and_is_as_tall_as_its_height()
    {
        Assert.Equal(0f, Figure.Parts.Min(part => Low(part).Y), 0.001f);
        Assert.Equal(Figure.Height, Figure.Parts.Max(part => High(part).Y), 0.001f);
    }

    [Fact]
    public void A_figure_s_eyes_are_in_its_head()
    {
        var head = Figure.Parts.Single(part => part.Name == "Head");

        Assert.InRange(Figure.EyeHeight, Low(head).Y, High(head).Y);
    }

    [Fact]
    public void Every_part_fits_front_to_back_inside_the_sim_s_body()
    {
        var radius = Shipped.Bodies!.Radius;

        Assert.All(Figure.Parts, part => Assert.InRange(MathF.Max(-Low(part).Z, High(part).Z), 0f, radius));
    }

    [Fact]
    public void The_face_is_on_the_front_of_the_head_where_the_nose_points()
    {
        string[] face = [Figure.LeftEye, Figure.RightEye, Figure.LeftBrow, Figure.RightBrow, Figure.LeftMouth, Figure.RightMouth, "Nose"];
        var head = Figure.Parts.Single(part => part.Name == "Head");

        Assert.All(Figure.Parts.Where(part => face.Contains(part.Name)), part =>
        {
            Assert.True(Low(part).Z < Low(head).Z + 0.02f, $"{part.Name} is not on the front of the head.");
            Assert.InRange(part.Center.Y, Low(head).Y, High(head).Y);
        });
    }

    [Fact]
    public void Every_capsule_is_at_least_as_tall_as_it_is_wide()
    {
        Assert.All(Figure.Parts.Where(part => part.Shape == PartShape.Capsule), part => Assert.True(part.Size.Y >= part.Size.X, part.Name));
    }

    [Fact]
    public void An_agent_looks_the_same_every_time_and_unlike_the_one_ahead()
    {
        Assert.Equal(Figure.For(new AgentId(3)), Figure.For(new AgentId(3)));
        Assert.All(Enumerable.Range(1, 11), id => Assert.NotEqual(Figure.For(new AgentId(id - 1)), Figure.For(new AgentId(id))));
    }

    [Fact]
    public void Heights_vary_by_at_most_five_percent()
    {
        Assert.All(Enumerable.Range(0, 50), id => Assert.InRange(Figure.For(new AgentId(id)).HeightScale, 0.95f, 1.05f));
    }

    [Fact]
    public void Standing_still_swings_no_limbs()
    {
        Assert.All(Enumerable.Range(0, 120), tick => Assert.Equal(0f, Figure.Swing(speed: 0f, Shipped.WalkSpeed, tick)));
    }

    [Fact]
    public void Walking_swings_the_limbs_no_further_than_at_walking_speed()
    {
        var walking = Enumerable.Range(0, 120).Max(tick => MathF.Abs(Figure.Swing(Shipped.WalkSpeed, Shipped.WalkSpeed, tick)));
        var faster = Enumerable.Range(0, 120).Max(tick => MathF.Abs(Figure.Swing(Shipped.WalkSpeed * 3f, Shipped.WalkSpeed, tick)));

        Assert.Equal(Figure.MaxSwing, walking, 0.01f);
        Assert.Equal(walking, faster, 0.0001f);
    }

    private static Vector3 Low(FigurePart part) => Figure.Pivot(part.Limb) + part.Center - Extent(part);

    private static Vector3 High(FigurePart part) => Figure.Pivot(part.Limb) + part.Center + Extent(part);

    /// <summary>Half the part's size along each axis, as its shape takes up room.</summary>
    private static Vector3 Extent(FigurePart part) => part.Shape switch
    {
        PartShape.Box => part.Size / 2f,
        PartShape.Capsule => new(part.Size.X / 2f, part.Size.Y / 2f, part.Size.X / 2f),
        _ => new(part.Size.X / 2f),
    };
}
