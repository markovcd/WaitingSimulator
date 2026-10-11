using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>The placeholder tone for each cue, until recorded sighs, mutters and thuds go through the asset pipeline.</summary>
internal static class Sounds
{
    /// <summary>The tone <paramref name="cue"/> plays; the worse the stage, the lower and longer.</summary>
    public static Tone For(Cue cue) => cue switch
    {
        { Kind: CueKind.Bumped } => new(110f, 0.15f),
        { Kind: CueKind.RanOut } => new(147f, 0.8f),
        { Stage: Stage.Restless } => new(330f, 0.3f),
        { Stage: Stage.Annoyed } => new(294f, 0.35f),
        { Stage: Stage.Confronting } => new(262f, 0.4f),
        _ => new(220f, 0.5f),
    };

    /// <summary>Every tone a cue can play, so each can be built once.</summary>
    public static IEnumerable<Tone> All() =>
        new[] { new Cue(default, CueKind.Bumped, null), new Cue(default, CueKind.RanOut, null) }
            .Concat(Enum.GetValues<Stage>().Select(stage => new Cue(default, CueKind.Escalated, stage)))
            .Select(For)
            .Distinct();
}
