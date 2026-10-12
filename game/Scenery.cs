using Godot;

namespace WaitingSimulator.Game;

/// <summary>
/// The placeholder world, built in code: light, a floor with the lane marked on it, a crude humanoid <see cref="Figure"/> for each agent and the
/// clerk, the voices cues play through and the debug overlay.
/// </summary>
internal static class Scenery
{
    private const float FloorSize = 30f;

    /// <summary>How high the lane's fill and tape sit off the floor, so neither flickers into it.</summary>
    private const float FillHeight = 0.004f;
    private const float TapeHeight = 0.006f;
    private const float TapeWidth = 0.05f;

    private static readonly Color Background = new(0.62f, 0.64f, 0.66f);
    private static readonly Color FloorColor = new(0.55f, 0.53f, 0.5f);

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
                AmbientLightColor = Background,
                AmbientLightEnergy = 0.6f,
            },
        });
        parent.AddChild(new DirectionalLight3D
        {
            Name = "Light",
            RotationDegrees = new Vector3(-55f, 30f, 0f),
            ShadowEnabled = true,
        });
        parent.AddChild(new MeshInstance3D
        {
            Name = "Floor",
            Mesh = new PlaneMesh { Size = new Vector2(FloorSize, FloorSize) },
            MaterialOverride = Flat(FloorColor),
            Position = center with { Y = 0f },
        });
    }

    /// <summary>
    /// Paints <paramref name="marking"/> on the floor: a fill over every strip and cap, and tape along the strips' edges.
    /// Returns the fill's material, which the caller tints to show where the player stands.
    /// </summary>
    public static StandardMaterial3D Lane(Node parent, LaneMarking marking)
    {
        var fill = Flat(ToColor(LaneMarking.Fill(LaneState.InLine)));
        var tape = Flat(ToColor(LaneMarking.Tape));
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
        foreach (var part in Game.Figure.Parts)
        {
            var mesh = new MeshInstance3D
            {
                Name = part.Name,
                Mesh = Mesh(part),
                MaterialOverride = Flat(Color(part.Swatch, look)),
                Position = ToGodot(part.Center),
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

    /// <summary>The result screen, hidden until the player reaches the counter: a label in the middle of the view.</summary>
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

    private static Mesh Mesh(FigurePart part) => part.Shape switch
    {
        PartShape.Box => new BoxMesh { Size = ToGodot(part.Size) },
        PartShape.Capsule => new CapsuleMesh { Radius = part.Size.X / 2f, Height = part.Size.Y },
        _ => new SphereMesh { Radius = part.Size.X / 2f, Height = part.Size.X },
    };

    private static Color Color(Swatch swatch, Look look)
    {
        var paint = swatch switch
        {
            Swatch.Skin => look.Skin,
            Swatch.Shirt => look.Shirt,
            Swatch.Trousers => look.Trousers,
            Swatch.Shoes => Game.Figure.Shoes,
            _ => Game.Figure.Features,
        };
        return ToColor(paint);
    }

    private static Vector3 ToGodot(System.Numerics.Vector3 vector) => new(vector.X, vector.Y, vector.Z);

    private static StandardMaterial3D Flat(Color color) => new() { AlbedoColor = color };
}
