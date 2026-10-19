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
    public void Hair_and_caps_cover_the_head_and_pass_behind_the_face()
    {
        string[] face = [Figure.LeftEye, Figure.RightEye, Figure.LeftBrow, Figure.RightBrow, Figure.LeftMouth, Figure.RightMouth, "Nose"];
        var head = Figure.Parts.Single(part => part.Name == "Head");

        Assert.All(Figure.Parts.Where(part => part.Name is "Hair" or "Cap"), covering =>
        {
            Assert.True(covering.Size.X > head.Size.X, $"{covering.Name} is no wider than the head it sits on.");
            Assert.Equal(head.Center, covering.Center);
            Assert.All(Figure.Parts.Where(part => face.Contains(part.Name)), part =>
            {
                var height = High(part).Y - covering.Center.Y;
                Assert.True(Front(covering, height) is not { } front || front > Low(part).Z, $"{covering.Name} covers {part.Name}.");
            });
        });
    }

    [Fact]
    public void Some_agents_wear_each_thing_and_some_do_not()
    {
        var looks = Enumerable.Range(0, 30).Select(id => Figure.For(new AgentId(id))).ToList();

        Assert.All(Enum.GetValues<Wear>().Where(wear => wear != Wear.None), wear =>
        {
            Assert.Contains(looks, look => look.Wears.HasFlag(wear));
            Assert.Contains(looks, look => !look.Wears.HasFlag(wear));
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

    private static Vector3 Low(FigurePart part) => part.Rings is { } rings
        ? Figure.Pivot(part.Limb) + part.Center + new Vector3(-rings.Max(ring => ring.HalfWidth), rings[0].Height, rings.Min(ring => ring.Back - ring.HalfDepth))
        : Figure.Pivot(part.Limb) + part.Center - Extent(part);

    private static Vector3 High(FigurePart part) => part.Rings is { } rings
        ? Figure.Pivot(part.Limb) + part.Center + new Vector3(rings.Max(ring => ring.HalfWidth), rings[^1].Height, rings.Max(ring => ring.Back + ring.HalfDepth))
        : Figure.Pivot(part.Limb) + part.Center + Extent(part);

    /// <summary>How far forward a loft's front face reaches at <paramref name="height"/> above its center, or null outside it.</summary>
    private static float? Front(FigurePart loft, float height)
    {
        var rings = loft.Rings!;
        for (var i = 1; i < rings.Count; i++)
        {
            if (height >= rings[i - 1].Height && height <= rings[i].Height)
            {
                var t = (height - rings[i - 1].Height) / (rings[i].Height - rings[i - 1].Height);
                return loft.Center.Z + float.Lerp(rings[i - 1].Back - rings[i - 1].HalfDepth, rings[i].Back - rings[i].HalfDepth, t);
            }
        }

        return null;
    }

    /// <summary>Half the part's size along each axis, as its shape takes up room.</summary>
    private static Vector3 Extent(FigurePart part) => part.Shape switch
    {
        PartShape.Box => part.Size / 2f * new Vector3(MathF.Max(part.Taper, 1f), 1f, MathF.Max(part.Taper, 1f)),
        PartShape.Capsule => new(part.Size.X / 2f, part.Size.Y / 2f, part.Size.X / 2f),
        _ => new(part.Size.X / 2f),
    };
}
