using Godot;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The client's root node: loads the level, builds placeholder scenery and a figure per agent for it, and each frame
/// turns input into the player's command, steps the sim, shows where it put everyone and how they stand, and plays the
/// cues its events call for. How to play shows at the start until the player walks off. Once the player reaches the
/// counter or is kicked out, input stops and the result screen shows until they line up again. The HUD shows what the
/// difficulty allows; F2 cycles the difficulty, and <c>-- --difficulty easy</c> starts on one. F5 re-reads the data
/// files and restarts the level on them. F3 toggles the debug overlay. Esc frees the mouse and a click in the window
/// takes it back. Started with <c>-- --smoke</c>, it plays a few frames, logs the overlay and quits, which is how the
/// gate checks the game starts under Godot; a data problem quits with exit code 1.
/// </summary>
public sealed partial class Client : Node3D
{
    internal const string LevelName = "post-office";
    private const int SmokeFrames = 10;

    /// <summary>Ticks between neighbors' strides, so a line stepping up does not walk in step.</summary>
    private const float StrideOffsetTicks = 13f;

    /// <summary>How long a notice stays up, in seconds.</summary>
    private const double NoticeSeconds = 2.0;

    /// <summary>How long the problems that kept a reload stay up, in seconds: long enough to read a field name.</summary>
    private const double ProblemSeconds = 8.0;

    // The difficulty outlives the scene, so lining up again keeps the one the player picked.
    private static Difficulty? difficulty;

    // Data a reload read, and its notice, handed to the scene it restarts.
    private static (GameData Data, string Notice)? reloaded;

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
    private Label? howToPlay;
    private Label? hud;
    private Label? notice;
    private double noticeLeft;
    private readonly List<FigureNode?> bodies = [];
    private FigureNode? clerk;
    private StandardMaterial3D? laneFill;
    private MeshInstance3D? heldMarker;
    private readonly List<AudioStreamPlayer3D> voices = [];

    /// <inheritdoc/>
    public override void _Ready()
    {
        smoke = OS.GetCmdlineUserArgs().Contains("--smoke");
        var data = reloaded is { Data: var fresh } ? new Parsed<GameData>(fresh, []) : GameData.Load(DataFolder(), LevelName);
        if (data.Value is not { } loaded)
        {
            foreach (var problem in data.Problems)
            {
                GD.PrintErr(problem);
            }

            GetTree().Quit(1);
            return;
        }

        var problems = new List<string>();
        difficulty ??= Difficulties.From(OS.GetCmdlineUserArgs(), problems);
        foreach (var problem in problems)
        {
            GD.PrintErr(problem);
        }

        Controls.Register();
        tuning = loaded.Tuning;
        level = loaded.Level;
        driver = new Driver(level.Start(tuning), level.Player);
        motion = new Motion();
        var player = driver.Current.Agents[level.Player.Value].Position;
        head = new Head(Floor.YawFacing(level.Lane.TowardFront(player)));
        Build();
        if (reloaded is { Notice: var said })
        {
            Notify(said);
            reloaded = null;
        }

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
        var ended = Ended();
        var events = driver.Advance(delta, ended ? Command.Idle : Floor.Walk(new(stick.X, stick.Y), head.Yaw));
        AddNewcomers();
        if (!ended && Ended())
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
        howToPlay!.Visible = !Ended() && HowToPlay.Shown(driver.Current, level);
        hud!.Text = Ended() ? "" : Hud.Text(driver.Current, level.Player, tuning, Difficulty);
        noticeLeft -= delta;
        notice!.Visible = noticeLeft > 0;
        if (overlay!.Visible)
        {
            overlay.Text = Overlay.Text(driver.Current, level, tuning);
        }

        if (smoke && ++frames == SmokeFrames)
        {
            GD.Print($"Client ready; sim {typeof(Sim).Assembly.GetName().Version}; level {LevelName}, {frames} frames.\n"
                + Overlay.Text(driver.Current, level, tuning)
                + $"\nHUD ({Difficulties.Name(Difficulty)}): {hud.Text}");
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
        else if (@event.IsActionPressed(Controls.LineUpAgain) && Ended())
        {
            GetTree().ReloadCurrentScene();
        }
        else if (@event.IsActionPressed(Controls.ReloadTuning))
        {
            Reload();
        }
        else if (@event.IsActionPressed(Controls.CycleDifficulty))
        {
            difficulty = Difficulties.Next(Difficulty);
            Notify($"DIFFICULTY: {Difficulties.Name(Difficulty).ToUpperInvariant()}");
        }
        else if (@event.IsActionPressed(Controls.DebugOverlay))
        {
            overlay!.Visible = !overlay.Visible;
        }
        else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && !smoke)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
        }
    }

    private static Difficulty Difficulty => difficulty ?? Difficulty.Normal;

    /// <summary>Shows <paramref name="text"/> under the HUD for <paramref name="seconds"/>.</summary>
    private void Notify(string text, double seconds = NoticeSeconds)
    {
        notice!.Text = text;
        noticeLeft = seconds;
    }

    /// <summary>Re-reads the data files and restarts the level on them; a file with problems keeps the level as it is.</summary>
    private void Reload()
    {
        var (data, text) = TuningReload.Try(DataFolder(), LevelName);
        if (data is null)
        {
            GD.PrintErr(text);
            Notify(text, ProblemSeconds);
            return;
        }

        GD.Print(text);
        reloaded = (data, text);
        GetTree().ReloadCurrentScene();
    }

    /// <summary>Whether the level has ended: the player reached the counter or was kicked out of the line.</summary>
    private bool Ended() => driver is not null && level is not null
        && driver.Current.Agents[level.Player.Value] is { ReachedCounterAt: not null } or { KickedOutAt: not null };

    /// <summary>The repo's <c>data/</c>, beside the Godot project.</summary>
    private static string DataFolder() => System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "data"));

    private void Build()
    {
        var front = level!.Lane.Points[0];
        var back = level.Lane.Points[^1];
        Scenery.AddRoom(this, Floor.ToWorld((front + back) / 2f));
        var marking = LaneMarking.Of(level.Lane);
        laneFill = Scenery.Lane(this, marking);
        heldMarker = Scenery.HeldMarker(this, marking);

        foreach (var tone in Sounds.All())
        {
            sounds[tone] = Scenery.Sound(tone);
        }

        camera = new Camera3D { Name = "Eyes", Current = true };
        AddChild(camera);
        AddNewcomers();

        clerk = Scenery.Figure("Clerk", Figure.Clerk);
        clerk.Root.Position = Floor.ToWorld(level.Clerk);
        clerk.Root.Rotation = new Vector3(0f, Floor.YawFacing(-level.Lane.TowardFront(level.Clerk)), 0f);
        AddChild(clerk.Root);
        overlay = Scenery.Overlay(this);
        result = Scenery.Result(this);
        howToPlay = Scenery.HowToPlay(this);
        hud = Scenery.Hud(this);
        notice = Scenery.Notice(this);
    }

    /// <summary>Builds a figure and a voice for every agent the sim has that the scene does not yet; the player gets the eyes' voice.</summary>
    private void AddNewcomers()
    {
        foreach (var agent in driver!.Current.Agents.Skip(bodies.Count))
        {
            if (agent.Id == level!.Player)
            {
                bodies.Add(null);
                voices.Add(Scenery.Voice(camera!));
                continue;
            }

            var body = Scenery.Figure($"Agent{agent.Id.Value}", Figure.For(agent.Id));
            AddChild(body.Root);
            bodies.Add(body);
            voices.Add(Scenery.Voice(body.Root));
        }
    }

    /// <summary>Puts every body and the eyes where the sim has them this frame, each body facing the front and posed.</summary>
    private void Place()
    {
        var now = driver!.ShownTick;
        foreach (var agent in driver.Current.Agents)
        {
            if (bodies[agent.Id.Value] is { } body)
            {
                body.Root.Visible = agent.Step != LineStep.Gone;
                var position = driver.Position(agent.Id);
                var pose = motion!.PoseOf(agent, now);
                body.Root.Position = Floor.ToWorld(position, pose.Bob);
                body.Root.Rotation = new Vector3(-pose.Lean, Floor.YawFacing(level!.Lane.TowardFront(position)), pose.Sway);
                body.Swing(Figure.Swing(agent.Velocity.Length(), tuning!.WalkSpeed, now + (agent.Id.Value * StrideOffsetTicks)));
                body.Show(Face.For(agent.Stage), Face.Blinking(agent.Id.Value, now));
            }
        }

        clerk!.Show(Face.Bored, Face.Blinking(-1, now));
        var you = driver.Current.Agents[level!.Player.Value];
        laneFill!.AlbedoColor = Scenery.ToColor(LaneMarking.Fill(LaneMarking.StateOf(you)));
        Scenery.Show(heldMarker!, LaneMarking.Held(you, level.Lane, driver.Current.Tick, tuning!.StepOut?.SpotHoldTicks ?? 0));

        // The player's eyes only stumble; a swaying camera would read as the game, not the person.
        var player = driver.Current.Agents[level!.Player.Value];
        camera!.Position = Floor.ToWorld(driver.Position(player.Id), Figure.EyeHeight);
        camera.Rotation = new Vector3(head!.Pitch - motion!.PoseOf(player, now).Lean, head.ViewYaw, 0f);
    }
}
