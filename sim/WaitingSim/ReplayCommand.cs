using System.Numerics;

namespace WaitingSim;

/// <summary>From <see cref="Tick"/> on, <see cref="Agent"/> walks by <see cref="Move"/>, until its next command.</summary>
public sealed record ReplayCommand(int Tick, AgentId Agent, Vector2 Move);
