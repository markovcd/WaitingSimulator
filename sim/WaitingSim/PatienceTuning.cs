namespace WaitingSim;

/// <summary>How patience drains and recovers, before an agent's trait scales it.</summary>
/// <param name="Stages">Where each stage starts.</param>
/// <param name="GapDrainPerSecond">Points lost each second the person ahead holds a gap open past its grace window.</param>
/// <param name="TightRecoveryPerSecond">Points regained each second the person ahead stays tight on theirs.</param>
/// <param name="BumpHit">Points lost when the person behind walks into you.</param>
public sealed record PatienceTuning(StageLimits Stages, float GapDrainPerSecond, float TightRecoveryPerSecond, float BumpHit)
{
    /// <summary>Full patience, where every agent starts. Patience runs from here down to 0.</summary>
    public const float Full = 100f;
}
