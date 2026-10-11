using Godot;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The client's root node: loads the level, builds placeholder scenery for it, and each frame turns input into the
/// player's command, steps the sim, shows where it put everyone and how they stand, and plays the cues its events call
/// for. Once the player reaches the counter, input stops and the result screen shows until they line up again. F3
/// toggles the debug overlay. Started with <c>-- --smoke</c>, it plays a few frames, logs the overlay and quits,
/// which is how the gate checks the game starts under Godot; a data problem quits with exit code 1.
/// </summary>
public sealed partial class Client : Node3D
{
    internal const string LevelName = "post-office";
    private const int SmokeFrames = 10;

    private readonly Dictionary<Tone, AudioStreamWav> sounds = [];
    private bool smoke;
    private int frames;
    private Tuning? tuning;
    private Level? level;
    private Driver? driver;
    private Head? head;
    private Motion? motion;
    private Camera3D? camera;
    private Label? overlay;
    private Label? result;
    private Node3D?[] bodies = [];
    private AudioStreamPlayer3D[] voices = [];

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
        tuning = loaded.Tuning;
        level = loaded.Level;
        driver = new Driver(level.Start(tuning), level.Player);
        motion = new Motion(driver.Current.Agents.Count);
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
        if (driver is null || head is null || level is null || motion is null || tuning is null)
        {
            return;
        }

        var look = Input.GetVector(Controls.LookLeft, Controls.LookRight, Controls.LookUp, Controls.LookDown);
        var turn = Controls.StickRadiansPerSecond * (float)delta;
        head.Turn(-look.X * turn, -look.Y * turn);
        head.GlancingBack = Input.IsActionPressed(Controls.GlanceBack);
        var stick = Input.GetVector(Controls.MoveLeft, Controls.MoveRight, Controls.MoveForward, Controls.MoveBack);
        var served = Served();
        var events = driver.Advance(delta, served ? Command.Idle : Floor.Walk(new(stick.X, stick.Y), head.Yaw));
        if (!served && Served())
        {
            result!.Text = ResultScreen.Text(driver.Current.Agents[level.Player.Value]);
            result.Visible = true;
        }

        foreach (var cue in Cues.From(events, driver.Current))
        {
            motion.Show(cue, driver.Current.Tick);
            var voice = voices[cue.Agent.Value];
            voice.Stream = sounds[Sounds.For(cue)];
            voice.Play();
        }

        Place();
        if (overlay!.Visible)
        {
            overlay.Text = Overlay.Text(driver.Current, level, tuning);
        }

        if (smoke && ++frames == SmokeFrames)
        {
            GD.Print($"Client ready; sim {typeof(Sim).Assembly.GetName().Version}; level {LevelName}, {frames} frames.\n"
                + Overlay.Text(driver.Current, level, tuning));
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
        else if (@event.IsActionPressed(Controls.LineUpAgain) && Served())
        {
            GetTree().ReloadCurrentScene();
        }
        else if (@event.IsActionPressed(Controls.DebugOverlay))
        {
            overlay!.Visible = !overlay.Visible;
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
        }
    }

    /// <summary>Whether the player has reached the counter, which ends the level.</summary>
    private bool Served() => driver is not null && level is not null && driver.Current.Agents[level.Player.Value].ReachedCounterAt is not null;

    /// <summary>The repo's <c>data/</c>, beside the Godot project.</summary>
    private static string DataFolder() => System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "data"));

    private void Build()
    {
        var front = level!.Lane.Points[0];
        var back = level.Lane.Points[^1];
        Scenery.AddRoom(this, Floor.ToWorld((front + back) / 2f));

        foreach (var tone in Sounds.All())
        {
            sounds[tone] = Scenery.Sound(tone);
        }

        camera = new Camera3D { Name = "Eyes", Current = true };
        AddChild(camera);
        var count = driver!.Current.Agents.Count;
        bodies = new Node3D?[count];
        voices = new AudioStreamPlayer3D[count];
        foreach (var agent in driver.Current.Agents)
        {
            if (agent.Id == level.Player)
            {
                voices[agent.Id.Value] = Scenery.Voice(camera);
                continue;
            }

            var body = Scenery.Agent($"Agent{agent.Id.Value}");
            AddChild(body);
            bodies[agent.Id.Value] = body;
            voices[agent.Id.Value] = Scenery.Voice(body);
        }

        var clerk = Scenery.Clerk();
        clerk.Position = Floor.ToWorld(level.Clerk);
        clerk.Rotation = new Vector3(0f, Floor.YawFacing(-level.Lane.TowardFront(level.Clerk)), 0f);
        AddChild(clerk);
        overlay = Scenery.Overlay(this);
        result = Scenery.Result(this);
    }

    /// <summary>Puts every body and the eyes where the sim has them this frame, each body facing the front and posed.</summary>
    private void Place()
    {
        var now = driver!.ShownTick;
        foreach (var agent in driver.Current.Agents)
        {
            if (bodies[agent.Id.Value] is { } body)
            {
                var position = driver.Position(agent.Id);
                var pose = motion!.PoseOf(agent, now);
                body.Position = Floor.ToWorld(position, pose.Bob);
                body.Rotation = new Vector3(-pose.Lean, Floor.YawFacing(level!.Lane.TowardFront(position)), pose.Sway);
            }
        }

        // The player's eyes only stumble; a swaying camera would read as the game, not the person.
        var player = driver.Current.Agents[level!.Player.Value];
        camera!.Position = Floor.ToWorld(driver.Position(player.Id), Scenery.EyeHeight);
        camera.Rotation = new Vector3(head!.Pitch - motion!.PoseOf(player, now).Lean, head.ViewYaw, 0f);
    }
}
