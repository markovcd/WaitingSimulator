using Godot;

namespace WaitingSimulator.Game;

/// <summary>The set dressing's meshes, where <see cref="SetDressing"/> places them: counters, the back wall and its sign, posts along a lane and a deli's display.</summary>
internal static class Furniture
{
    /// <summary>How high a counter stands, in meters.</summary>
    private const float CounterHeight = 1.0f;

    /// <summary>How deep a counter is, back from its edge toward the clerk, in meters.</summary>
    private const float CounterDepth = 0.6f;

    private const float WallWidth = 30f;
    private const float WallHeight = 3f;
    private const float WallDepth = 0.2f;
    private const float SignHeight = 2.3f;
    private const float PostHeight = 0.95f;
    private const float GlassHeight = 0.4f;

    /// <summary>The trays in a deli's display, as wide and as far apart.</summary>
    private const float TrayPitch = 0.45f;

    private static readonly Color CounterColor = new(0.42f, 0.33f, 0.26f);
    private static readonly Color WallColor = new(0.72f, 0.7f, 0.64f);
    private static readonly Color SignColor = new(0.16f, 0.22f, 0.36f);
    private static readonly Color PostColor = new(0.3f, 0.31f, 0.33f);
    private static readonly Color[] FoodColors = [new(0.86f, 0.55f, 0.55f), new(0.93f, 0.8f, 0.4f), new(0.7f, 0.26f, 0.22f), new(0.5f, 0.62f, 0.32f), new(0.92f, 0.9f, 0.82f)];

    /// <summary>A pale pane, drawn see-through and unlit; the one surface the toon shader leaves alone.</summary>
    private static readonly StandardMaterial3D Glass = new()
    {
        AlbedoColor = new Color(0.8f, 0.92f, 0.95f, 0.25f),
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
    };

    /// <summary>A counter: a block behind the edge from <paramref name="counter"/>, on the clerk's side.</summary>
    public static void Counter(Node parent, WaitingSim.Counter counter)
    {
        var middle = ((counter.From + counter.To) / 2f) - (counter.TowardCrowd * (CounterDepth / 2f));
        parent.AddChild(new MeshInstance3D
        {
            Name = "Counter",
            Mesh = new BoxMesh { Size = new Vector3(counter.Length, CounterHeight, CounterDepth) },
            MaterialOverride = Toon.Of(CounterColor, Edging.Square),
            Position = Floor.ToWorld(middle, CounterHeight / 2f),
            Rotation = new Vector3(0f, Floor.YawFacing(counter.TowardCrowd), 0f),
        });
    }

    /// <summary>A deli's display on <paramref name="counter"/>: a row of trays of food behind a glass guard along the crowd's edge.</summary>
    public static void Display(Node parent, WaitingSim.Counter counter)
    {
        var display = new Node3D
        {
            Name = "Display",
            Position = Floor.ToWorld((counter.From + counter.To) / 2f, CounterHeight),
            Rotation = new Vector3(0f, Floor.YawFacing(counter.TowardCrowd), 0f),
        };
        parent.AddChild(display);
        var trays = (int)(counter.Length / TrayPitch);
        for (var i = 0; i < trays; i++)
        {
            display.AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(TrayPitch * 0.8f, 0.06f, CounterDepth * 0.5f) },
                MaterialOverride = Toon.Of(FoodColors[i % FoodColors.Length], Edging.Square),
                Position = new Vector3((i - ((trays - 1) / 2f)) * TrayPitch, 0.03f, CounterDepth * 0.4f),
            });
        }

        display.AddChild(new MeshInstance3D
        {
            Name = "Glass",
            Mesh = new BoxMesh { Size = new Vector3(counter.Length, GlassHeight, 0.01f) },
            MaterialOverride = Glass,
            Position = new Vector3(0f, GlassHeight / 2f, 0.05f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
    }

    /// <summary>The wall at <paramref name="middle"/>, behind a clerk who faces <paramref name="towardCustomers"/>, with <paramref name="sign"/> on it.</summary>
    public static void BackWall(Node parent, System.Numerics.Vector2 middle, System.Numerics.Vector2 towardCustomers, string sign)
    {
        var wall = new Node3D
        {
            Name = "BackWall",
            Position = Floor.ToWorld(middle),
            Rotation = new Vector3(0f, Floor.YawFacing(-towardCustomers), 0f),
        };
        parent.AddChild(wall);
        wall.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(WallWidth, WallHeight, WallDepth) },
            MaterialOverride = Toon.Of(WallColor, Edging.Square),
            Position = new Vector3(0f, WallHeight / 2f, 0f),
        });

        // The wall's front is its +z side, toward the customers; a label reads from its own +z.
        wall.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3((0.2f * sign.Length) + 0.6f, 0.6f, 0.05f) },
            MaterialOverride = Toon.Of(SignColor, Edging.Square),
            Position = new Vector3(0f, SignHeight, (WallDepth / 2f) + 0.025f),
        });
        wall.AddChild(new Label3D
        {
            Text = sign,
            FontSize = 64,
            OutlineSize = 0,
            Modulate = Colors.White,
            DoubleSided = false,
            Position = new Vector3(0f, SignHeight, (WallDepth / 2f) + 0.06f),
        });
    }

    /// <summary>A post at each of <paramref name="posts"/>: a pole on a round foot, as along a bank's line.</summary>
    public static void Posts(Node parent, IReadOnlyList<System.Numerics.Vector2> posts)
    {
        var pole = new CylinderMesh { TopRadius = 0.025f, BottomRadius = 0.025f, Height = PostHeight, RadialSegments = 8, Rings = 1 };
        var foot = new CylinderMesh { TopRadius = 0.12f, BottomRadius = 0.15f, Height = 0.04f, RadialSegments = 8, Rings = 1 };
        var top = new SphereMesh { Radius = 0.04f, Height = 0.08f, RadialSegments = 8, Rings = 4 };
        var material = Toon.Of(PostColor, Edging.Round);
        var all = new Node3D { Name = "Posts" };
        parent.AddChild(all);
        foreach (var point in posts)
        {
            var post = new Node3D { Position = Floor.ToWorld(point) };
            all.AddChild(post);
            post.AddChild(new MeshInstance3D { Mesh = foot, MaterialOverride = material, Position = new Vector3(0f, 0.02f, 0f) });
            post.AddChild(new MeshInstance3D { Mesh = pole, MaterialOverride = material, Position = new Vector3(0f, PostHeight / 2f, 0f) });
            post.AddChild(new MeshInstance3D { Mesh = top, MaterialOverride = material, Position = new Vector3(0f, PostHeight, 0f) });
        }
    }
}
