using Godot;

namespace WaitingSimulator.Game;

/// <summary>
/// The placeholder world, built in code: light, a floor, a capsule for each agent and the clerk, the voices cues play
/// through and the debug overlay.
/// </summary>
internal static class Scenery
{
    /// <summary>How high the player's eyes are above the floor, in meters.</summary>
    public const float EyeHeight = 1.6f;

    private const float CapsuleRadius = 0.25f;
    private const float CapsuleHeight = 1.7f;
    private const float FloorSize = 30f;

    private static readonly Color Background = new(0.62f, 0.64f, 0.66f);
    private static readonly Color FloorColor = new(0.55f, 0.53f, 0.5f);
    private static readonly Color AgentColor = new(0.42f, 0.47f, 0.55f);
    private static readonly Color ClerkColor = new(0.55f, 0.44f, 0.38f);

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

    /// <summary>A capsule standing on the floor, for an agent.</summary>
    public static Node3D Agent(string name) => Capsule(name, AgentColor);

    /// <summary>A capsule standing on the floor, for the clerk.</summary>
    public static Node3D Clerk() => Capsule("Clerk", ClerkColor);

    private static Node3D Capsule(string name, Color color)
    {
        var body = new Node3D { Name = name };
        body.AddChild(new MeshInstance3D
        {
            Mesh = new CapsuleMesh { Radius = CapsuleRadius, Height = CapsuleHeight },
            MaterialOverride = Flat(color),
            Position = new Vector3(0f, CapsuleHeight / 2f, 0f),
        });
        return body;
    }

    /// <summary>A voice at <paramref name="parent"/>'s spot, for its cues.</summary>
    public static AudioStreamPlayer3D Voice(Node3D parent)
    {
        var voice = new AudioStreamPlayer3D { Name = "Voice", Position = new Vector3(0f, EyeHeight, 0f) };
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

    private static StandardMaterial3D Flat(Color color) => new() { AlbedoColor = color };
}
