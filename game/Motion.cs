using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// Each agent's pose from the stage it shows and how long ago the person behind walked into it. Time is in ticks, so a
/// replay shows the same; an agent without patience moves like a calm one.
/// </summary>
internal sealed class Motion
{
    /// <summary>How long a stumble lasts.</summary>
    public const float StumbleTicks = 0.6f * TickRate.PerSecond;

    private const float StumbleLean = 0.3f;

    /// <summary>Radians between neighbors' sways, so a line does not sway in step.</summary>
    private const float PhasePerAgent = 1.7f;

    private readonly Dictionary<AgentId, long> bumpedAt = [];

    /// <summary>The motion of each stage. Stiff on purpose; stage hysteresis in the sim keeps it from flickering.</summary>
    public static StageMotion Of(Stage? stage) => stage switch
    {
        Stage.Restless => new(0f, 0.03f, 0.5f, 0f, 0f),
        Stage.Annoyed => new(0.03f, 0.02f, 0.5f, 0.02f, 3f),
        Stage.Confronting => new(0.15f, 0.02f, 0.5f, 0.01f, 2f),
        Stage.FedUp => new(0.25f, 0.04f, 1.2f, 0.02f, 4f),
        _ => new(0f, 0.015f, 0.3f, 0f, 0f),
    };

    /// <summary>Starts a stumble for a <see cref="CueKind.Bumped"/> cue shown at <paramref name="tick"/>.</summary>
    public void Show(Cue cue, long tick)
    {
        if (cue.Kind == CueKind.Bumped)
        {
            bumpedAt[cue.Agent] = tick;
        }
    }

    /// <summary>The pose of <paramref name="agent"/> at <paramref name="now"/>, in ticks with the frame's blend.</summary>
    public Pose PoseOf(AgentSnapshot agent, float now)
    {
        ArgumentNullException.ThrowIfNull(agent);
        var motion = Of(agent.Stage);
        var seconds = now / TickRate.PerSecond;
        var phase = agent.Id.Value * PhasePerAgent;
        var lean = motion.Lean;
        if (bumpedAt.TryGetValue(agent.Id, out var bumped) && now - bumped is var since && since >= 0f && since < StumbleTicks)
        {
            var progress = since / StumbleTicks;
            lean += StumbleLean * MathF.Sin(MathF.PI * progress) * (1f - progress);
        }

        var sway = motion.Sway * MathF.Sin((MathF.Tau * motion.SwayHertz * seconds) + phase);
        var bob = motion.Tap * MathF.Max(0f, MathF.Sin((MathF.Tau * motion.TapHertz * seconds) + phase));
        return new(lean, sway, bob);
    }
}
