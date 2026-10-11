namespace WaitingSim;

/// <summary>An NPC's personality as data, read from <c>data/traits.json</c>.</summary>
/// <param name="Id">The trait's name in data files. Persisted: never renamed.</param>
/// <param name="DrainScale">How much faster than the tuning's rate patience drains.</param>
/// <param name="RecoveryScale">How much faster than the tuning's rate patience recovers.</param>
/// <param name="BumpScale">How much harder than the tuning's hit a bump lands.</param>
/// <param name="ShownStages">The stages this trait shows. Calm is always one.</param>
public sealed record Trait(string Id, float DrainScale, float RecoveryScale, float BumpScale, IReadOnlySet<Stage> ShownStages)
{
    /// <summary>The stage the agent shows when it is really at <paramref name="stage"/>: a hidden stage looks like the last shown one before it.</summary>
    public Stage Shown(Stage stage)
    {
        while (stage > Stage.Calm && !ShownStages.Contains(stage))
        {
            stage--;
        }

        return stage;
    }
}
