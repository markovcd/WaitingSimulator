using Godot;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The client's root node: loads the level, builds placeholder scenery for it, and each frame turns input into the
/// player's command, steps the sim and shows where it put everyone. Started with <c>-- --smoke</c>, it plays a few
/// frames and quits, which is how the gate checks the game starts under Godot; a data problem quits with exit code 1.
/// </summary>
public sealed partial class Client : Node3D
{
    internal const string LevelName = "post-office";
    private const int SmokeFrames = 10;

    private bool smoke;
    private int frames;
    private Level? level;
    private Driver? driver;
    private Head? head;
    private Camera3D? camera;
    private Node3D?[] bodies = [];

    /// <inheritdoc/>
    public override void _Ready()
    {
        smoke = OS.GetCmdlineUserArgs().Contains("--smoke");
        var data = GameData.Load(DataFolder(), LevelName);
        if (data.Value is not { } loaded)
        {
            foreach (var problem in data.Problems)
            {
                GD.PrintErr(problem);
            }

            GetTree().Quit(1);
            return;
        }

        Controls.Register();
        level = loaded.Level;
        driver = new Driver(level.Start(loaded.Tuning), level.Player);
        var player = driver.Current.Agents[level.Player.Value].Position;
        head = new Head(Floor.YawFacing(level.Lane.TowardFront(player)));
        Build();
        Place();
        if (!smoke)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    /// <inheritdoc/>
    public override void _Process(double delta)
    {
        if (driver is null || head is null || level is null)
        {
            return;
        }

        var look = Input.GetVector(Controls.LookLeft, Controls.LookRight, Controls.LookUp, Controls.LookDown);
        var turn = Controls.StickRadiansPerSecond * (float)delta;
        head.Turn(-look.X * turn, -look.Y * turn);
        head.GlancingBack = Input.IsActionPressed(Controls.GlanceBack);
        var stick = Input.GetVector(Controls.MoveLeft, Controls.MoveRight, Controls.MoveForward, Controls.MoveBack);
        driver.Advance(delta, Floor.Walk(new(stick.X, stick.Y), head.Yaw));
        Place();

        if (smoke && ++frames == SmokeFrames)
        {
            GD.Print($"Client ready; sim {typeof(Sim).Assembly.GetName().Version}; level {LevelName}, "
                + $"{driver.Current.Agents.Count} agents, the player is agent {level.Player.Value}; tick {driver.Current.Tick} after {frames} frames.");
            GetTree().Quit();
        }
    }

    /// <inheritdoc/>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            head?.Turn(-motion.ScreenRelative.X * Controls.MouseRadiansPerPixel, -motion.ScreenRelative.Y * Controls.MouseRadiansPerPixel);
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
        }
    }

    /// <summary>The repo's <c>data/</c>, beside the Godot project.</summary>
    private static string DataFolder() => System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "data"));

    private void Build()
    {
        var front = level!.Lane.Points[0];
        var back = level.Lane.Points[^1];
        Scenery.AddRoom(this, Floor.ToWorld((front + back) / 2f));

        bodies = new Node3D?[driver!.Current.Agents.Count];
        foreach (var agent in driver.Current.Agents)
        {
            if (agent.Id != level.Player)
            {
                bodies[agent.Id.Value] = Scenery.Agent($"Agent{agent.Id.Value}");
                AddChild(bodies[agent.Id.Value]);
            }
        }

        var clerk = Scenery.Clerk();
        clerk.Position = Floor.ToWorld(level.Clerk);
        AddChild(clerk);
        camera = new Camera3D { Name = "Eyes", Current = true };
        AddChild(camera);
    }

    /// <summary>Puts every body and the eyes where the sim has them this frame.</summary>
    private void Place()
    {
        foreach (var agent in driver!.Current.Agents)
        {
            if (bodies[agent.Id.Value] is { } body)
            {
                body.Position = Floor.ToWorld(driver.Position(agent.Id));
            }
        }

        camera!.Position = Floor.ToWorld(driver.Position(level!.Player), Scenery.EyeHeight);
        camera.Rotation = new Vector3(head!.Pitch, head.ViewYaw, 0f);
    }
}
