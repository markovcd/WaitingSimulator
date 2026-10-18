using Godot;

namespace WaitingSimulator.Game;

/// <summary>
/// On-screen controls for a touchscreen, shown only where there is one. A finger on the left of the screen is a stick
/// that walks, a finger dragged anywhere else turns the head, and the buttons press their actions while held. The
/// stick and the buttons press the same input actions the keys and the gamepad do, so the clients read them as they
/// read those; the head's turn is taken each frame with <see cref="TakeLook"/>.
/// </summary>
public sealed partial class TouchControls : Control
{
    /// <summary>The share of the screen's width, from the left, where a finger is the stick.</summary>
    private const float StickShare = 0.4f;

    /// <summary>How far a finger drags to lean the stick all the way, as a share of the screen's height.</summary>
    private const float StickReach = 0.12f;

    private static readonly Color Fill = new(1f, 1f, 1f, 0.15f);
    private static readonly Color Held = new(1f, 1f, 1f, 0.35f);
    private static readonly Color Ink = new(1f, 1f, 1f, 0.8f);

    private readonly Dictionary<int, TouchButton> pressing = [];
    private IReadOnlyList<TouchButton> buttons = [];
    private int? stickFinger;
    private Vector2 stickOrigin;
    private Vector2 stickLean;
    private int? lookFinger;
    private Vector2 look;

    /// <summary>Adds the controls with <paramref name="buttons"/> to <paramref name="parent"/>, or nothing where there is no touchscreen.</summary>
    internal static TouchControls? AddTo(Node parent, IReadOnlyList<TouchButton> buttons)
    {
        if (!DisplayServer.IsTouchscreenAvailable())
        {
            return null;
        }

        var layer = new CanvasLayer { Name = "Touch" };
        var controls = new TouchControls { Name = "Controls", buttons = buttons, MouseFilter = MouseFilterEnum.Ignore };
        controls.SetAnchorsPreset(LayoutPreset.FullRect);
        layer.AddChild(controls);
        parent.AddChild(layer);
        return controls;
    }

    /// <summary>How far the fingers turning the head have dragged since the last call, in pixels.</summary>
    internal Vector2 TakeLook()
    {
        var taken = look;
        look = Vector2.Zero;
        return taken;
    }

    /// <inheritdoc/>
    public override void _Input(InputEvent @event)
    {
        var size = Size;
        switch (@event)
        {
            case InputEventScreenTouch { Pressed: true } touch:
                Press(touch.Index, touch.Position, size);
                break;
            case InputEventScreenTouch touch:
                Release(touch.Index);
                break;
            case InputEventScreenDrag drag when drag.Index == stickFinger:
                Lean(TouchStick.Lean(stickOrigin, drag.Position, StickReach * size.Y));
                break;
            case InputEventScreenDrag drag when drag.Index == lookFinger:
                look += drag.Relative;
                break;
            default:
                return;
        }

        GetViewport().SetInputAsHandled();
        QueueRedraw();
    }

    /// <inheritdoc/>
    public override void _Draw()
    {
        var size = Size;
        var font = ThemeDB.FallbackFont;
        foreach (var button in buttons)
        {
            var radius = button.RadiusOn(size);
            DrawCircle(button.At(size), radius, pressing.ContainsValue(button) ? Held : Fill);
            var fontSize = (int)(radius * 0.35f);
            DrawString(font, button.At(size) + new Vector2(-radius, fontSize * 0.35f), button.Caption, HorizontalAlignment.Center, radius * 2f, fontSize, Ink);
        }

        if (stickFinger is not null)
        {
            var reach = StickReach * size.Y;
            DrawCircle(stickOrigin, reach, Fill);
            DrawCircle(stickOrigin + (stickLean * reach), reach * 0.4f, Held);
        }
    }

    private void Press(int finger, Vector2 at, Vector2 size)
    {
        if (buttons.FirstOrDefault(button => button.Holds(at, size)) is { } button)
        {
            pressing[finger] = button;
            Input.ParseInputEvent(new InputEventAction { Action = button.Action, Pressed = true, Strength = 1f });
        }
        else if (at.X < size.X * StickShare && stickFinger is null)
        {
            stickFinger = finger;
            stickOrigin = at;
        }
        else if (lookFinger is null)
        {
            lookFinger = finger;
        }
    }

    private void Release(int finger)
    {
        if (pressing.Remove(finger, out var button))
        {
            Input.ParseInputEvent(new InputEventAction { Action = button.Action, Pressed = false });
        }
        else if (finger == stickFinger)
        {
            stickFinger = null;
            Lean(Vector2.Zero);
        }
        else if (finger == lookFinger)
        {
            lookFinger = null;
        }
    }

    private void Lean(Vector2 lean)
    {
        stickLean = lean;
        var (right, left, back, forward) = TouchStick.Strengths(lean);
        Move(Controls.MoveRight, right);
        Move(Controls.MoveLeft, left);
        Move(Controls.MoveBack, back);
        Move(Controls.MoveForward, forward);
    }

    private static void Move(StringName action, float strength)
    {
        if (strength > 0f)
        {
            Input.ActionPress(action, strength);
        }
        else
        {
            Input.ActionRelease(action);
        }
    }
}
