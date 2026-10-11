using System.Numerics;

namespace WaitingSim;

/// <summary>An agent's mutable state inside the sim.</summary>
internal sealed class Agent(AgentId id, Vector2 position)
{
    public AgentId Id { get; } = id;

    public Vector2 Position { get; set; } = position;

    public Vector2 Velocity { get; set; }

    public LanePosition Lane { get; set; }

    public bool InLane { get; set; }

    public AgentId? Ahead { get; set; }

    public float? Spacing { get; set; }

    public Zone? Zone { get; set; }

    public long? GapOpenedAt { get; set; }

    public bool GraceExpired { get; set; }

    public AgentSnapshot Snapshot() => new(Id, Position, Velocity, InLane, Ahead, Spacing, Zone, GapOpenedAt);
}
