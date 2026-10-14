namespace WaitingSim;

/// <summary>How the person behind takes being stared at: how long a glance back may last, and how fast staring drains them.</summary>
/// <param name="AfterTicks">How long a glance back lasts before it is staring.</param>
/// <param name="DrainPerSecond">The patience staring drains from the person behind each second, before their trait scales it.</param>
public sealed record StaringTuning(int AfterTicks, float DrainPerSecond);
