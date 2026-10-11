namespace WaitingSim;

/// <summary>The sim's state hash after <see cref="Tick"/>.</summary>
public sealed record ReplayHash(int Tick, ulong Hash);
