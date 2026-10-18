using Godot;

namespace WaitingSimulator.Game;

/// <summary>
/// The input actions and their default keys, mouse buttons and gamepad inputs, registered in code so the whole map is
/// in one table. Look sensitivity lives here until there are settings.
/// </summary>
internal static class Controls
{
    public static readonly StringName MoveForward = "move_forward";
    public static readonly StringName MoveBack = "move_back";
    public static readonly StringName MoveLeft = "move_left";
    public static readonly StringName MoveRight = "move_right";
    public static readonly StringName LookLeft = "look_left";
    public static readonly StringName LookRight = "look_right";
    public static readonly StringName LookUp = "look_up";
    public static readonly StringName LookDown = "look_down";
    public static readonly StringName GlanceBack = "glance_back";
    public static readonly StringName LetPast = "let_past";
    public static readonly StringName CallOut = "call_out";
    public static readonly StringName Wave = "wave";
    public static readonly StringName NextLevel = "next_level";
    public static readonly StringName DebugOverlay = "debug_overlay";
    public static readonly StringName LineUpAgain = "line_up_again";
    public static readonly StringName CycleDifficulty = "cycle_difficulty";
    public static readonly StringName ReloadTuning = "reload_tuning";

    /// <summary>Radians the head turns per pixel of mouse movement.</summary>
    public const float MouseRadiansPerPixel = 0.0025f;

    /// <summary>Radians the head turns per pixel a finger drags on a touchscreen.</summary>
    public const float TouchRadiansPerPixel = 0.004f;

    /// <summary>Radians per second the head turns with the look stick pushed all the way.</summary>
    public const float StickRadiansPerSecond = 2.5f;

    /// <summary>How far a stick must lean before it counts.</summary>
    private const float StickDeadzone = 0.2f;

    /// <summary>Adds every action not already in the input map, so a binding made in the editor wins.</summary>
    public static void Register()
    {
        Add(MoveForward, Key(Godot.Key.W), Stick(JoyAxis.LeftY, -1f));
        Add(MoveBack, Key(Godot.Key.S), Stick(JoyAxis.LeftY, 1f));
        Add(MoveLeft, Key(Godot.Key.A), Stick(JoyAxis.LeftX, -1f));
        Add(MoveRight, Key(Godot.Key.D), Stick(JoyAxis.LeftX, 1f));
        Add(LookLeft, Stick(JoyAxis.RightX, -1f));
        Add(LookRight, Stick(JoyAxis.RightX, 1f));
        Add(LookUp, Stick(JoyAxis.RightY, -1f));
        Add(LookDown, Stick(JoyAxis.RightY, 1f));
        Add(DebugOverlay, Key(Godot.Key.F3));
        Add(CycleDifficulty, Key(Godot.Key.F2));
        Add(ReloadTuning, Key(Godot.Key.F5));
        Add(LineUpAgain, Key(Godot.Key.R), new InputEventJoypadButton { ButtonIndex = JoyButton.Start });
        Add(LetPast, Key(Godot.Key.E), new InputEventJoypadButton { ButtonIndex = JoyButton.X });

        // A crowd has no line to let anyone past in, so its call out shares the key.
        Add(CallOut, Key(Godot.Key.E), new InputEventJoypadButton { ButtonIndex = JoyButton.X });
        Add(Wave, Key(Godot.Key.F), new InputEventJoypadButton { ButtonIndex = JoyButton.Y });
        Add(NextLevel, Key(Godot.Key.F6));
        Add(GlanceBack, Key(Godot.Key.Q), new InputEventMouseButton { ButtonIndex = MouseButton.Right }, new InputEventJoypadButton { ButtonIndex = JoyButton.LeftShoulder });
    }

    private static void Add(StringName action, params InputEvent[] events)
    {
        if (InputMap.HasAction(action))
        {
            return;
        }

        InputMap.AddAction(action, StickDeadzone);
        foreach (var input in events)
        {
            InputMap.ActionAddEvent(action, input);
        }
    }

    private static InputEventKey Key(Key key) => new() { PhysicalKeycode = key };

    private static InputEventJoypadMotion Stick(JoyAxis axis, float direction) => new() { Axis = axis, AxisValue = direction };
}
