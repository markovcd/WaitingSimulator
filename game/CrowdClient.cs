using Godot;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The crowd level's root node: loads the crowd, builds placeholder scenery, the counter and a figure per agent, and
/// each frame turns input into the player's command, steps the crowd, shows where it put everyone and how they stand,
/// and plays the cues its events call for. The clerk turns to follow their glance along the counter and says "Yes?" to
/// whom it lands on. Holding F waves; E says "I was here first". Once the player is served or moved out, input stops and
/// the result screen shows until they line up again. F2, F3, F5, F6 and Esc work as in the line. Started with
/// <c>-- --smoke</c>, it plays a few frames, logs the overlay and quits; a data problem quits with exit code 1.
/// </summary>
public sealed partial class CrowdClient : Node3D
{
    private const int SmokeFrames = 10;

    /// <summary>Ticks between neighbors' strides, so a crowd stepping in does not walk in step.</summary>
    private const float StrideOffsetTicks = 13f;

    private const double NoticeSeconds = 2.0;
    private const double ProblemSeconds = 8.0;

    /// <summary>How fast a figure walking out turns to face where it walks, as a share of its walking speed.</summary>
    private const float FacingWalkShare = 0.3f;

    private readonly Dictionary<Tone, AudioStreamWav> sounds = [];
    private readonly List<FigureNode?> bodies = [];
    private readonly List<AudioStreamPlayer3D> voices = [];
    private bool smoke;
    private int frames;
    private string levelName = Levels.Default;
    private Tuning? tuning;
    private CrowdLevel? level;
    private Driver? driver;
    private Head? head;
    private Motion? motion;
    private Camera3D? camera;
    private TouchControls? touch;
    private Label? overlay;
    private Label? result;
    private Label? howToPlay;
    private Label? hud;
    private Label? notice;
    private double noticeLeft;
    private FigureNode? clerk;
    private Label3D? yes;

    /// <inheritdoc/>
    public override void _Ready()
    {
        smoke = OS.GetCmdlineUserArgs().Contains("--smoke");
        levelName = Levels.Playing ?? Levels.Default;
        var data = CrowdData.Load(Client.DataFolder(), levelName);
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
        motion = new Motion();
        head = new Head(Floor.YawFacing(-level.Counter.TowardCrowd));
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
        if (driver is null || head is null || level is null || motion is null || tuning?.Crowd is not { } crowd)
        {
            return;
        }

        var look = Input.GetVector(Controls.LookLeft, Controls.LookRight, Controls.LookUp, Controls.LookDown);
        var turn = Controls.StickRadiansPerSecond * (float)delta;
        head.Turn(-look.X * turn, -look.Y * turn);
        var swipe = (touch?.TakeLook() ?? Vector2.Zero) * Controls.TouchRadiansPerPixel;
        head.Turn(-swipe.X, -swipe.Y);
        head.GlancingBack = Input.IsActionPressed(Controls.GlanceBack);
        var stick = Input.GetVector(Controls.MoveLeft, Controls.MoveRight, Controls.MoveForward, Controls.MoveBack);
        var ended = Ended();
        var command = Floor.Walk(new(stick.X, stick.Y), head.Yaw) with
        {
            GlancingBack = head.GlancingBack,
            Waving = Input.IsActionPressed(Controls.Wave),
            CallOut = Input.IsActionJustPressed(Controls.CallOut),
        };
        var events = driver.Advance(delta, ended ? Command.Idle : command);
        AddNewcomers();
        if (!ended && Ended())
        {
            result!.Text = ResultScreen.CrowdText(driver.Current.Agents[level.Player.Value]);
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
        hud!.Text = Ended() ? "" : CrowdHud.Text(driver.Current, level.Player, crowd, Client.Difficulty);
        noticeLeft -= delta;
        notice!.Visible = noticeLeft > 0;
        if (overlay!.Visible)
        {
            overlay.Text = CrowdOverlay.Text(driver.Current, level);
        }

        if (smoke && ++frames == SmokeFrames)
        {
            GD.Print($"Crowd ready; sim {typeof(Crowd).Assembly.GetName().Version}; level {levelName}, {frames} frames.\n"
                + CrowdOverlay.Text(driver.Current, level)
                + $"\nHUD ({Difficulties.Name(Client.Difficulty)}): {hud.Text}");
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
        else if (@event.IsActionPressed(Controls.NextLevel))
        {
            Client.PlayNext(GetTree());
        }
        else if (@event.IsActionPressed(Controls.CycleDifficulty))
        {
            Notify(Client.CycleDifficulty());
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

    private void Notify(string text, double seconds = NoticeSeconds)
    {
        notice!.Text = text;
        noticeLeft = seconds;
    }

    /// <summary>Re-reads the data files and restarts the crowd on them; a file with problems keeps the crowd as it is.</summary>
    private void Reload()
    {
        var loaded = CrowdData.Load(Client.DataFolder(), levelName);
        if (loaded.Value is null)
        {
            var text = string.Join('\n', loaded.Problems.Prepend("TUNING NOT RELOADED"));
            GD.PrintErr(text);
            Notify(text, ProblemSeconds);
            return;
        }

        GetTree().ReloadCurrentScene();
    }

    /// <summary>Whether the level has ended: the player was served or moved out.</summary>
    private bool Ended() => driver is not null && level is not null
        && driver.Current.Agents[level.Player.Value] is { ReachedCounterAt: not null } or { KickedOutAt: not null };

    private void Build()
    {
        Scenery.AddRoom(this, Floor.ToWorld((level!.Counter.From + level.Counter.To) / 2f));
        Furniture.Counter(this, level.Counter);
        Furniture.Display(this, level.Counter);
        Furniture.BackWall(this, SetDressing.BackWall(level.Clerk, level.Counter.TowardCrowd), level.Counter.TowardCrowd, "DELI");
        yes = Scenery.SpeechBubble(this);
        yes.Text = "YES?";
        yes.Position = Floor.ToWorld(level.Clerk, Scenery.SpeechHeight);
        foreach (var tone in Sounds.All())
        {
            sounds[tone] = Scenery.Sound(tone);
        }

        camera = new Camera3D { Name = "Eyes", Current = true };
        AddChild(camera);
        AddNewcomers();

        clerk = Scenery.Figure("Clerk", Figure.Clerk);
        clerk.Root.Position = Floor.ToWorld(level.Clerk);
        AddChild(clerk.Root);
        overlay = Scenery.Overlay(this);
        result = Scenery.Result(this);
        howToPlay = Scenery.HowToPlay(this);
        howToPlay.Text = HowToPlay.CrowdText;
        hud = Scenery.Hud(this);
        notice = Scenery.Notice(this);
        touch = TouchControls.AddTo(this, TouchButton.Crowd);
    }

    /// <summary>Builds a figure and a voice for every agent the crowd has that the scene does not yet; the player gets the eyes' voice.</summary>
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

    /// <summary>Puts every body and the eyes where the crowd has them this frame, each facing the counter, or where it walks out.</summary>
    private void Place()
    {
        var now = driver!.ShownTick;
        var towardCounter = -level!.Counter.TowardCrowd;
        foreach (var agent in driver.Current.Agents)
        {
            if (bodies[agent.Id.Value] is { } body)
            {
                body.Root.Visible = agent.Step != LineStep.Gone;
                var position = driver.Position(agent.Id);
                var pose = motion!.PoseOf(agent, now);
                var walkingOut = agent.Step == LineStep.Exiting && agent.Velocity.Length() > tuning!.WalkSpeed * FacingWalkShare;
                var facing = walkingOut ? agent.Velocity : towardCounter;
                body.Root.Position = Floor.ToWorld(position, pose.Bob);
                body.Root.Rotation = new Vector3(-pose.Lean, Floor.YawFacing(facing), pose.Sway);
                body.Swing(Figure.Swing(agent.Velocity.Length(), tuning!.WalkSpeed, now + (agent.Id.Value * StrideOffsetTicks)));
                body.Show(Face.For(agent.Stage), Face.Blinking(agent.Id.Value, now));
            }
        }

        // The clerk turns to their glance, or to whom they ask or serve: the cue to read who is next.
        var desk = driver.Current.Clerk;
        var lookingAt = (desk?.Picked ?? desk?.Serving) is { } attended ? driver.Position(attended) : desk?.Glance ?? level.Counter.From;
        clerk!.Root.Rotation = new Vector3(0f, Floor.YawFacing(lookingAt - level.Clerk), 0f);
        clerk.Show(Face.Bored, Face.Blinking(-1, now));
        yes!.Visible = desk?.Picked is not null;

        var player = driver.Current.Agents[level.Player.Value];
        camera!.Position = Floor.ToWorld(driver.Position(player.Id), Figure.EyeHeight);
        camera.Rotation = new Vector3(head!.Pitch - motion!.PoseOf(player, now).Lean, head.ViewYaw, 0f);
    }
}
