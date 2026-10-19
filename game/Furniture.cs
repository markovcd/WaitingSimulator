using Godot;

namespace WaitingSimulator.Game;

/// <summary>
/// The set dressing's meshes, where <see cref="SetDressing"/> places them: the room's walls, counters, posts along a
/// lane, chairs, plants, a post office window and a deli's display, shelves and lamps.
/// </summary>
internal static class Furniture
{
    /// <summary>How high a counter stands, in meters.</summary>
    private const float CounterHeight = 1.0f;

    /// <summary>How deep a counter is, back from its edge toward the clerk, in meters.</summary>
    private const float CounterDepth = 0.6f;

    private const float WallHeight = 3f;
    private const float WallDepth = 0.2f;
    private const float SignHeight = 2.3f;
    private const float PostHeight = 0.95f;
    private const float GlassHeight = 0.4f;

    /// <summary>The trays in a deli's display, as wide and as far apart.</summary>
    private const float TrayPitch = 0.45f;

    private static readonly Color CounterColor = new(0.42f, 0.33f, 0.26f);
    private static readonly Color WallColor = new(0.66f, 0.64f, 0.6f);
    private static readonly Color SignColor = new(0.16f, 0.22f, 0.36f);
    private static readonly Color PostColor = new(0.3f, 0.31f, 0.33f);
    private static readonly Color WindowColor = new(0.5f, 0.62f, 0.7f);
    private static readonly Color CeilingColor = new(0.86f, 0.85f, 0.82f);
    private static readonly Color PanelColor = new(1f, 0.99f, 0.95f);
    private static readonly Color ChairColor = new(0.26f, 0.31f, 0.4f);
    private static readonly Color PotColor = new(0.66f, 0.4f, 0.28f);
    private static readonly Color LeafColor = new(0.3f, 0.5f, 0.27f);
    private static readonly Color ScreenColor = new(0.12f, 0.13f, 0.15f);
    private static readonly Color ShadeColor = new(0.22f, 0.36f, 0.3f);
    private static readonly Color BulbColor = new(1f, 0.92f, 0.7f);
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
        var display = OnTop(parent, counter, "Display");
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

        display.AddChild(Pane(counter.Length, GlassHeight));
    }

    /// <summary>A node on top of <paramref name="counter"/> at the middle of its edge, its -z toward the crowd and +z toward the clerk.</summary>
    private static Node3D OnTop(Node parent, WaitingSim.Counter counter, string name)
    {
        var node = new Node3D
        {
            Name = name,
            Position = Floor.ToWorld((counter.From + counter.To) / 2f, CounterHeight),
            Rotation = new Vector3(0f, Floor.YawFacing(counter.TowardCrowd), 0f),
        };
        parent.AddChild(node);
        return node;
    }

    /// <summary>A glass pane <paramref name="height"/> tall standing along a counter's edge, <paramref name="length"/> long.</summary>
    private static MeshInstance3D Pane(float length, float height) => new()
    {
        Name = "Glass",
        Mesh = new BoxMesh { Size = new Vector3(length, height, 0.01f) },
        MaterialOverride = Glass,
        Position = new Vector3(0f, height / 2f, 0.05f),
        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
    };

    /// <summary>
    /// The room round a clerk at <paramref name="clerk"/> facing <paramref name="towardCustomers"/>: a back wall behind
    /// the clerk with <paramref name="sign"/> on it, and windowed side walls running forward from it.
    /// </summary>
    public static void Room(Node parent, System.Numerics.Vector2 clerk, System.Numerics.Vector2 towardCustomers, string sign)
    {
        // The room's +z runs from the back wall toward the customers and its +x to the clerk's left.
        var room = new Node3D
        {
            Name = "Room",
            Position = Floor.ToWorld(SetDressing.BackWall(clerk, towardCustomers)),
            Rotation = new Vector3(0f, Floor.YawFacing(-towardCustomers), 0f),
        };
        parent.AddChild(room);
        var wall = Toon.Of(WallColor, Edging.Square);
        var glass = Toon.Glowing(WindowColor, 0.5f);
        var width = (SetDressing.RoomHalfWidth * 2f) + WallDepth;
        room.AddChild(new MeshInstance3D
        {
            Name = "BackWall",
            Mesh = new BoxMesh { Size = new Vector3(width, WallHeight, WallDepth) },
            MaterialOverride = wall,
            Position = new Vector3(0f, WallHeight / 2f, 0f),
        });
        foreach (var side in new[] { -1f, 1f })
        {
            var x = side * SetDressing.RoomHalfWidth;
            room.AddChild(new MeshInstance3D
            {
                Name = side < 0f ? "RightWall" : "LeftWall",
                Mesh = new BoxMesh { Size = new Vector3(WallDepth, WallHeight, SetDressing.RoomLength) },
                MaterialOverride = wall,
                Position = new Vector3(x, WallHeight / 2f, SetDressing.RoomLength / 2f),
            });
            for (var z = 3f; z < SetDressing.RoomLength - 1f; z += 4f)
            {
                room.AddChild(new MeshInstance3D
                {
                    Mesh = new BoxMesh { Size = new Vector3(0.04f, 1.2f, 1.8f) },
                    MaterialOverride = glass,
                    Position = new Vector3(x - (side * ((WallDepth / 2f) + 0.02f)), 1.7f, z),
                });
            }
        }

        // The ceiling casts no shadow, or the sun through it would leave the room in the dark.
        room.AddChild(new MeshInstance3D
        {
            Name = "Ceiling",
            Mesh = new BoxMesh { Size = new Vector3(width, WallDepth, SetDressing.RoomLength) },
            MaterialOverride = Toon.Glowing(CeilingColor, 0.4f),
            Position = new Vector3(0f, WallHeight + (WallDepth / 2f), SetDressing.RoomLength / 2f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
        var panel = new BoxMesh { Size = new Vector3(1.2f, 0.03f, 0.6f) };
        var lit = Toon.Glowing(PanelColor, 1f);
        for (var z = 2f; z < SetDressing.RoomLength; z += 4f)
        {
            foreach (var x in new[] { -3f, 3f })
            {
                room.AddChild(new MeshInstance3D
                {
                    Mesh = panel,
                    MaterialOverride = lit,
                    Position = new Vector3(x, WallHeight - 0.015f, z),
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                });
            }
        }

        // A label reads from its own +z, the room's way in.
        room.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3((0.2f * sign.Length) + 0.6f, 0.6f, 0.05f) },
            MaterialOverride = Toon.Of(SignColor, Edging.Square),
            Position = new Vector3(0f, SignHeight, (WallDepth / 2f) + 0.025f),
        });
        room.AddChild(new Label3D
        {
            Text = sign,
            FontSize = 64,
            OutlineSize = 0,
            Modulate = Colors.White,
            DoubleSided = false,
            Position = new Vector3(0f, SignHeight, (WallDepth / 2f) + 0.06f),
        });
    }

    /// <summary>A plastic chair at each of <paramref name="chairs"/>, facing its way.</summary>
    public static void Chairs(Node parent, IReadOnlyList<(System.Numerics.Vector2 At, System.Numerics.Vector2 Facing)> chairs)
    {
        var seat = Toon.Of(ChairColor, Edging.Square);
        var metal = Toon.Of(PostColor, Edging.Square);
        var leg = new BoxMesh { Size = new Vector3(0.035f, 0.43f, 0.035f) };
        foreach (var (at, facing) in chairs)
        {
            var chair = new Node3D { Name = "Chair", Position = Floor.ToWorld(at), Rotation = new Vector3(0f, Floor.YawFacing(facing), 0f) };
            parent.AddChild(chair);
            chair.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.46f, 0.05f, 0.44f) }, MaterialOverride = seat, Position = new Vector3(0f, 0.45f, 0f) });
            chair.AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.46f, 0.42f, 0.05f) },
                MaterialOverride = seat,
                Position = new Vector3(0f, 0.72f, 0.22f),
                Rotation = new Vector3(-0.12f, 0f, 0f),
            });
            foreach (var (x, z) in new[] { (-0.2f, -0.19f), (0.2f, -0.19f), (-0.2f, 0.19f), (0.2f, 0.19f) })
            {
                chair.AddChild(new MeshInstance3D { Mesh = leg, MaterialOverride = metal, Position = new Vector3(x, 0.215f, z) });
            }
        }
    }

    /// <summary>A potted plant at each of <paramref name="plants"/>: a clay pot with a clump of leaves.</summary>
    public static void Plants(Node parent, IReadOnlyList<System.Numerics.Vector2> plants)
    {
        var leaves = Toon.Of(LeafColor, Edging.Round);
        foreach (var at in plants)
        {
            var plant = new Node3D { Name = "Plant", Position = Floor.ToWorld(at) };
            parent.AddChild(plant);
            plant.AddChild(new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.2f, BottomRadius = 0.15f, Height = 0.4f, RadialSegments = 8, Rings = 1 },
                MaterialOverride = Toon.Of(PotColor, Edging.Round),
                Position = new Vector3(0f, 0.2f, 0f),
            });
            foreach (var (x, y, z, radius) in new[] { (0f, 0.75f, 0f, 0.3f), (0.12f, 1.05f, 0.05f, 0.22f), (-0.1f, 1.2f, -0.06f, 0.18f) })
            {
                plant.AddChild(new MeshInstance3D
                {
                    Mesh = new SphereMesh { Radius = radius, Height = radius * 2.2f, RadialSegments = 6, Rings = 3 },
                    MaterialOverride = leaves,
                    Position = new Vector3(x, y, z),
                });
            }
        }
    }

    /// <summary>A post office window on <paramref name="counter"/>: a tall glass screen along its edge and the clerk's monitor behind it.</summary>
    public static void Window(Node parent, WaitingSim.Counter counter)
    {
        var window = OnTop(parent, counter, "Window");
        window.AddChild(Pane(counter.Length, 0.75f));
        window.AddChild(new MeshInstance3D
        {
            Name = "Monitor",
            Mesh = new BoxMesh { Size = new Vector3(0.42f, 0.28f, 0.03f) },
            MaterialOverride = Toon.Of(ScreenColor, Edging.Square),
            Position = new Vector3(0.3f, 0.3f, CounterDepth * 0.55f),
            Rotation = new Vector3(0.1f, 0.3f, 0f),
        });
        window.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.04f, 0.16f, 0.04f) },
            MaterialOverride = Toon.Of(ScreenColor, Edging.Square),
            Position = new Vector3(0.3f, 0.08f, CounterDepth * 0.58f),
        });
    }

    /// <summary>Shelves of tins on the wall behind a clerk at <paramref name="clerk"/> facing <paramref name="towardCustomers"/>.</summary>
    public static void Shelves(Node parent, System.Numerics.Vector2 clerk, System.Numerics.Vector2 towardCustomers)
    {
        var shelves = new Node3D
        {
            Name = "Shelves",
            Position = Floor.ToWorld(SetDressing.BackWall(clerk, towardCustomers)),
            Rotation = new Vector3(0f, Floor.YawFacing(-towardCustomers), 0f),
        };
        parent.AddChild(shelves);
        var board = Toon.Of(CounterColor, Edging.Square);
        var tin = new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.05f, Height = 0.14f, RadialSegments = 6, Rings = 1 };
        const float Length = 3f;
        foreach (var (height, row) in new[] { (1.3f, 0), (1.75f, 1) })
        {
            shelves.AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(Length, 0.04f, 0.28f) },
                MaterialOverride = board,
                Position = new Vector3(0f, height, (WallDepth / 2f) + 0.14f),
            });
            for (var i = 0; i < 11; i++)
            {
                shelves.AddChild(new MeshInstance3D
                {
                    Mesh = tin,
                    MaterialOverride = Toon.Of(FoodColors[(i + (row * 2)) % FoodColors.Length], Edging.Round),
                    Position = new Vector3((i - 5) * 0.26f, height + 0.09f, (WallDepth / 2f) + 0.14f),
                });
            }
        }
    }

    /// <summary>Lamps hanging over <paramref name="counter"/>, one per meter and a half.</summary>
    public static void Lamps(Node parent, WaitingSim.Counter counter)
    {
        var lamps = OnTop(parent, counter, "Lamps");
        var count = Math.Max(1, (int)(counter.Length / 1.5f));
        for (var i = 0; i < count; i++)
        {
            var x = (i - ((count - 1) / 2f)) * (counter.Length / count);
            lamps.AddChild(new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.008f, BottomRadius = 0.008f, Height = 0.7f, RadialSegments = 4, Rings = 1 },
                MaterialOverride = Toon.Of(ScreenColor),
                Position = new Vector3(x, WallHeight - CounterHeight - 0.35f, CounterDepth / 2f),
            });
            lamps.AddChild(new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.06f, BottomRadius = 0.22f, Height = 0.18f, RadialSegments = 8, Rings = 1 },
                MaterialOverride = Toon.Of(ShadeColor, Edging.Round),
                Position = new Vector3(x, WallHeight - CounterHeight - 0.79f, CounterDepth / 2f),
            });
            lamps.AddChild(new MeshInstance3D
            {
                Mesh = new SphereMesh { Radius = 0.06f, Height = 0.12f, RadialSegments = 6, Rings = 3 },
                MaterialOverride = Toon.Glowing(BulbColor, 1f),
                Position = new Vector3(x, WallHeight - CounterHeight - 0.88f, CounterDepth / 2f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            });
        }
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
