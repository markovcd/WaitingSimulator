using Godot;

namespace WaitingSimulator.Game;

/// <summary>
/// The world, built in code and cel shaded (<see cref="Toon"/>): light, a floor with the lane marked on it, a low-poly
/// <see cref="Figure"/> for each agent and the clerk, the voices cues play through and the debug overlay.
/// </summary>
internal static class Scenery
{
    private const float FloorSize = 30f;
    private const float FloorTile = 0.6f;

    /// <summary>How high the lane's fill and tape sit off the floor, so neither flickers into it.</summary>
    private const float FillHeight = 0.004f;
    private const float TapeHeight = 0.006f;
    private const float TapeWidth = 0.05f;
    private const float MarkerHeight = 0.008f;
    private const float MarkerDepth = 0.12f;

    private static readonly Color Background = new(0.62f, 0.64f, 0.66f);
    /// <summary>The warm fill that lights what the sun does not, so the shaded band reads as shade rather than dark.</summary>
    private static readonly Color Ambient = new(1f, 0.97f, 0.92f);

    private static readonly Color FloorColor = new(0.55f, 0.53f, 0.5f);

    /// <summary>Washes out the scene behind the result screen, so its text never sits on a face.</summary>
    private static readonly Color ResultBackdrop = new(1f, 1f, 1f, 0.7f);

    /// <summary>For text laid out in padded columns, which only line up when every character is as wide.</summary>
    private static readonly SystemFont Monospace = new() { FontNames = ["monospace"] };

    /// <summary>Adds the light, the sky and a floor centered on <paramref name="center"/> to <paramref name="parent"/>.</summary>
    public static void AddRoom(Node parent, Vector3 center)
    {
        parent.AddChild(new WorldEnvironment
        {
            Name = "Environment",
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = Background,
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = Ambient,
                AmbientLightEnergy = 0.55f,
            },
        });
        parent.AddChild(new DirectionalLight3D
        {
            Name = "Light",
            RotationDegrees = new Vector3(-55f, 30f, 0f),
            LightEnergy = 0.75f,
            ShadowEnabled = true,
        });
        parent.AddChild(new MeshInstance3D
        {
            Name = "Floor",
            Mesh = new PlaneMesh { Size = new Vector2(FloorSize, FloorSize) },
            MaterialOverride = Toon.Tiled(FloorColor, FloorTile),
            Position = center with { Y = 0f },
        });
    }

    /// <summary>
    /// Paints <paramref name="marking"/> on the floor: a fill over every strip and cap, and tape along the strips' edges.
    /// Returns the fill's material, which the caller tints to show where the player stands.
    /// </summary>
    public static ShaderMaterial Lane(Node parent, LaneMarking marking)
    {
        var fill = Toon.Tintable(ToColor(LaneMarking.Fill(LaneState.InLine)));
        var tape = Toon.Of(ToColor(LaneMarking.Tape));
        var lane = new Node3D { Name = "Lane" };
        parent.AddChild(lane);
        foreach (var strip in marking.Strips)
        {
            var yaw = Floor.YawFacing(strip.To - strip.From);
            var across = System.Numerics.Vector2.Normalize(new(strip.From.Y - strip.To.Y, strip.To.X - strip.From.X));
            lane.AddChild(new MeshInstance3D
            {
                Mesh = new PlaneMesh { Size = new Vector2(marking.HalfWidth * 2f, strip.Length) },
                MaterialOverride = fill,
                Position = Floor.ToWorld(strip.Center, FillHeight),
                Rotation = new Vector3(0f, yaw, 0f),
            });
            foreach (var side in new[] { -1f, 1f })
            {
                lane.AddChild(new MeshInstance3D
                {
                    Mesh = new BoxMesh { Size = new Vector3(TapeWidth, 0.002f, strip.Length) },
                    MaterialOverride = tape,
                    Position = Floor.ToWorld(strip.Center + (across * side * (marking.HalfWidth - (TapeWidth / 2f))), TapeHeight),
                    Rotation = new Vector3(0f, yaw, 0f),
                });
            }
        }

        // The caps' tops sit just under the strips', so the two never fight where they overlap.
        foreach (var cap in marking.Caps)
        {
            lane.AddChild(new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = marking.HalfWidth, BottomRadius = marking.HalfWidth, Height = FillHeight * 0.75f },
                MaterialOverride = fill,
                Position = Floor.ToWorld(cap, FillHeight * 0.375f),
            });
        }

        return fill;
    }

    /// <summary>The bar painted across the lane at a held place, hidden until there is one.</summary>
    public static MeshInstance3D HeldMarker(Node parent, LaneMarking marking)
    {
        var marker = new MeshInstance3D
        {
            Name = "HeldPlace",
            Mesh = new BoxMesh { Size = new Vector3(marking.HalfWidth * 2f, 0.002f, MarkerDepth) },
            MaterialOverride = Toon.Of(ToColor(LaneMarking.HeldPaint)),
            Visible = false,
        };
        parent.AddChild(marker);
        return marker;
    }

    /// <summary>Shows <paramref name="held"/> on <paramref name="marker"/>, its width shrinking with the hold left, or hides it.</summary>
    public static void Show(MeshInstance3D marker, HeldMarker? held)
    {
        marker.Visible = held is not null;
        if (held is { } place)
        {
            marker.Position = Floor.ToWorld(place.Point, MarkerHeight);
            marker.Rotation = new Vector3(0f, Floor.YawFacing(place.TowardFront), 0f);
            marker.Scale = new Vector3(MathF.Max(place.Left, 0.05f), 1f, 1f);
        }
    }

    /// <summary>How high a dropped ticket's center sits off the floor, in meters.</summary>
    public const float TicketHeight = 0.006f;

    /// <summary>The player's ticket lying on the floor, hidden until it drops: a small white slip.</summary>
    public static MeshInstance3D Ticket(Node parent)
    {
        var ticket = new MeshInstance3D
        {
            Name = "Ticket",
            Mesh = new BoxMesh { Size = new Vector3(0.12f, 0.004f, 0.07f) },
            MaterialOverride = Toon.Of(new Color(0.95f, 0.94f, 0.88f)),
            Rotation = new Vector3(0f, 0.4f, 0f),
            Visible = false,
        };
        parent.AddChild(ticket);
        return ticket;
    }

    /// <summary>How high above the floor a speech bubble floats, in meters: over the head of someone talking.</summary>
    public const float SpeechHeight = 2.0f;

    /// <summary>A "..." over the head of the person making small talk, hidden until they start; it always faces the eyes.</summary>
    public static Label3D SpeechBubble(Node parent)
    {
        var bubble = new Label3D
        {
            Name = "SpeechBubble",
            Text = "...",
            FontSize = 96,
            Modulate = Colors.Black,
            OutlineModulate = Colors.White,
            OutlineSize = 24,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Visible = false,
        };
        parent.AddChild(bubble);
        return bubble;
    }

    /// <summary><paramref name="paint"/> as a Godot color.</summary>
    public static Color ToColor(Paint paint) => new(paint.R, paint.G, paint.B);

    /// <summary>A figure standing on the floor, sized and colored by <paramref name="look"/>.</summary>
    public static FigureNode Figure(string name, Look look)
    {
        var root = new Node3D { Name = name, Scale = Vector3.One * look.HeightScale };
        var limbs = new Dictionary<Limb, Node3D> { [Limb.Body] = root };
        foreach (var limb in Enum.GetValues<Limb>().Where(limb => limb != Limb.Body))
        {
            var pivot = new Node3D { Name = limb.ToString(), Position = ToGodot(Game.Figure.Pivot(limb)) };
            root.AddChild(pivot);
            limbs[limb] = pivot;
        }

        var parts = new Dictionary<string, Node3D>();
        foreach (var part in Game.Figure.Parts.Where(part => look.Wears.HasFlag(part.Wear)))
        {
            var mesh = new MeshInstance3D
            {
                Name = part.Name,
                Mesh = Mesh(part),
                MaterialOverride = Toon.Of(Color(part.Swatch, look), Edging(part)),
                Position = ToGodot(part.Center),
                Rotation = new Vector3(part.Tilt, 0f, 0f),
            };
            limbs[part.Limb].AddChild(mesh);
            parts[part.Name] = mesh;
        }

        return new FigureNode(root, limbs, parts);
    }

    /// <summary>A voice at <paramref name="parent"/>'s spot, for its cues.</summary>
    public static AudioStreamPlayer3D Voice(Node3D parent)
    {
        var voice = new AudioStreamPlayer3D { Name = "Voice", Position = new Vector3(0f, Game.Figure.EyeHeight, 0f) };
        parent.AddChild(voice);
        return voice;
    }

    /// <summary><paramref name="tone"/> as a sound a voice can play.</summary>
    public static AudioStreamWav Sound(Tone tone) => new()
    {
        Format = AudioStreamWav.FormatEnum.Format16Bits,
        MixRate = Tone.MixRate,
        Stereo = false,
        Data = tone.Pcm16(),
    };

    /// <summary>The debug overlay, hidden until toggled: a label in the top left corner, over the 3D view.</summary>
    public static Label Overlay(Node parent)
    {
        var layer = new CanvasLayer { Name = "Overlay" };
        var label = new Label { Name = "Text", Position = new Vector2(12f, 12f), Visible = false };
        label.AddThemeColorOverride("font_color", Colors.Black);
        label.AddThemeFontOverride("font", Monospace);
        layer.AddChild(label);
        parent.AddChild(layer);
        return label;
    }

    /// <summary>The HUD: a label at the top of the view, empty when the difficulty shows nothing.</summary>
    public static Label Hud(Node parent) => TopLabel(parent, "Hud", 12f);

    /// <summary>A short notice, such as the difficulty just picked: a label under the HUD, hidden until shown.</summary>
    public static Label Notice(Node parent)
    {
        var label = TopLabel(parent, "Notice", 64f);
        label.Visible = false;
        return label;
    }

    /// <summary>The result screen, hidden until the player reaches the counter: a label in the middle of the view, over a wash.</summary>
    public static Label Result(Node parent)
    {
        var layer = new CanvasLayer { Name = "Result" };
        var label = new Label
        {
            Name = "Text",
            Visible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        label.AddThemeColorOverride("font_color", Colors.Black);
        label.AddThemeFontOverride("font", Monospace);
        label.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = ResultBackdrop });
        layer.AddChild(label);
        parent.AddChild(layer);
        return label;
    }

    /// <summary>How to play, shown at the start of a level: a label along the bottom of the view.</summary>
    public static Label HowToPlay(Node parent)
    {
        var layer = new CanvasLayer { Name = "HowToPlay" };
        var label = new Label
        {
            Name = "Text",
            Text = Game.HowToPlay.Text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            OffsetBottom = -24f,
        };
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        label.AddThemeColorOverride("font_color", Colors.Black);
        layer.AddChild(label);
        parent.AddChild(layer);
        return label;
    }

    private static Label TopLabel(Node parent, string name, float top)
    {
        var layer = new CanvasLayer { Name = name };
        var label = new Label { Name = "Text", HorizontalAlignment = HorizontalAlignment.Center, OffsetTop = top };
        label.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        label.AddThemeColorOverride("font_color", Colors.Black);
        layer.AddChild(label);
        parent.AddChild(layer);
        return label;
    }

    /// <summary>A part's mesh, with as few sides as still read as its shape, which the toon shader shows as facets.</summary>
    private static Mesh Mesh(FigurePart part) => part.Shape switch
    {
        PartShape.Box when part.Taper == 1f => new BoxMesh { Size = ToGodot(part.Size) },
        PartShape.Box => TaperedBox(ToGodot(part.Size), part.Taper),
        PartShape.Capsule => new CapsuleMesh { Radius = part.Size.X / 2f, Height = part.Size.Y, RadialSegments = 8, Rings = 1 },
        PartShape.Sphere => new SphereMesh { Radius = part.Size.X / 2f, Height = part.Size.X, RadialSegments = 8, Rings = 4 },

        // A hemisphere spends half its rings on its flat bottom.
        _ => new SphereMesh { Radius = part.Size.X / 2f, Height = part.Size.X / 2f, IsHemisphere = true, RadialSegments = 8, Rings = 6 },
    };

    /// <summary>A box <paramref name="size"/> big at its top whose bottom is <paramref name="taper"/> as wide and deep.</summary>
    private static ArrayMesh TaperedBox(Vector3 size, float taper)
    {
        Vector3 Corner(int x, int y, int z)
        {
            var scale = y > 0 ? 0.5f : 0.5f * taper;
            return new Vector3(x * size.X * scale, y * size.Y * 0.5f, z * size.Z * scale);
        }

        var tool = new SurfaceTool();
        tool.Begin(Godot.Mesh.PrimitiveType.Triangles);
        foreach (var axis in new[] { 0, 1, 2 })
        {
            foreach (var side in new[] { -1, 1 })
            {
                int[][] square = [[-1, -1], [1, -1], [1, 1], [-1, 1]];
                var quad = square.Select(uv => axis switch
                {
                    0 => Corner(side, uv[0], uv[1]),
                    1 => Corner(uv[0], side, uv[1]),
                    _ => Corner(uv[0], uv[1], side),
                }).ToArray();
                var outward = (quad[0] + quad[1] + quad[2] + quad[3]) / 4f;
                foreach (var (a, b, c) in new[] { (quad[0], quad[1], quad[2]), (quad[0], quad[2], quad[3]) })
                {
                    // Godot draws a triangle's front where its corners run clockwise, which is where their cross product points away.
                    var flipped = (b - a).Cross(c - a).Dot(outward) > 0f;
                    var normal = (b - a).Cross(c - a).Normalized() * (flipped ? 1f : -1f);
                    Vector3[] corners = flipped ? [a, c, b] : [a, b, c];
                    foreach (var corner in corners)
                    {
                        tool.SetNormal(normal);
                        tool.AddVertex(corner);
                    }
                }
            }
        }

        return tool.Commit();
    }

    /// <summary>The face's marks are too small to carry an ink line; every other part has one that follows its shape.</summary>
    private static Edging Edging(FigurePart part) => part switch
    {
        { Swatch: Swatch.Features } => Game.Edging.None,
        { Shape: PartShape.Box } => Game.Edging.Square,
        _ => Game.Edging.Round,
    };

    private static Color Color(Swatch swatch, Look look)
    {
        var paint = swatch switch
        {
            Swatch.Skin => look.Skin,
            Swatch.Shirt => look.Shirt,
            Swatch.Trousers => look.Trousers,
            Swatch.Shoes => Game.Figure.Shoes,
            Swatch.Hair => look.Hair,
            _ => Game.Figure.Features,
        };
        return ToColor(paint);
    }

    private static Vector3 ToGodot(System.Numerics.Vector3 vector) => new(vector.X, vector.Y, vector.Z);
}
