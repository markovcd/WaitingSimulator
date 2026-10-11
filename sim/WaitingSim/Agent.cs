using System.Numerics;

namespace WaitingSim;

/// <summary>An agent's mutable state inside the sim.</summary>
internal sealed class Agent(AgentId id, Vector2 position, Trait? trait)
{
    public AgentId Id { get; } = id;

    /// <summary>The agent's personality; an agent without one, like the player, has no patience.</summary>
    public Trait? Trait { get; } = trait;

    public Vector2 Position { get; set; } = position;

    public Vector2 Velocity { get; set; }

    public LanePosition Lane { get; set; }

    public bool InLane { get; set; }

    public AgentId? Ahead { get; set; }

    public float? Spacing { get; set; }

    public Zone? Zone { get; set; }

    public long? GapOpenedAt { get; set; }

    public bool GraceExpired { get; set; }

    public float Patience { get; set; } = PatienceTuning.Full;

    /// <summary>The stage the agent's patience is really in; its trait decides what it shows.</summary>
    public Stage Stage { get; set; }

    public AgentSnapshot Snapshot() => Trait is null
        ? new(Id, Position, Velocity, InLane, Ahead, Spacing, Zone, GapOpenedAt, null, null, null)
        : new(Id, Position, Velocity, InLane, Ahead, Spacing, Zone, GapOpenedAt, Trait.Id, Patience, Trait.Shown(Stage));
}
