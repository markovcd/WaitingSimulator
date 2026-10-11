namespace WaitingSim;

/// <summary>
/// Where each stage starts, in patience points: a stage begins below its limit. Recovering into a calmer stage
/// takes <see cref="RecoverMargin"/> points past the limit, so cues do not flicker at the edge.
/// </summary>
public sealed record StageLimits(float RestlessBelow, float AnnoyedBelow, float ConfrontingBelow, float FedUpBelow, float RecoverMargin)
{
    /// <summary>The stage an agent at <paramref name="current"/> moves to with <paramref name="patience"/> points left.</summary>
    public Stage After(Stage current, float patience)
    {
        var stage = current;
        while (stage < Stage.FedUp && patience < Below(stage + 1))
        {
            stage++;
        }

        while (stage > Stage.Calm && patience >= Below(stage) + RecoverMargin)
        {
            stage--;
        }

        return stage;
    }

    private float Below(Stage stage) => stage switch
    {
        Stage.Restless => RestlessBelow,
        Stage.Annoyed => AnnoyedBelow,
        Stage.Confronting => ConfrontingBelow,
        Stage.FedUp => FedUpBelow,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Calm has no lower limit."),
    };
}
