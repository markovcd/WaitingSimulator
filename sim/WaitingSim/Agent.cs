using System.Numerics;

namespace WaitingSim;

/// <summary>An agent's mutable state inside the sim.</summary>
internal sealed class Agent(AgentId id, Vector2 position, Trait? trait, Rng rng)
{
    public AgentId Id { get; } = id;

    /// <summary>The agent's own random stream, so what one agent draws never changes another's luck.</summary>
    public Rng Rng { get; } = rng;

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

    /// <summary>How far behind the person ahead the agent likes to stand when the line moves it.</summary>
    public float PreferredSpacing { get; set; }

    public LineStep Step { get; set; }

    /// <summary>The tick a <see cref="LineStep.Noticed"/> or <see cref="LineStep.BeingServed"/> agent moves on.</summary>
    public long StepEndsAt { get; set; }

    public AgentSnapshot Snapshot() => Trait is null
        ? new(Id, Position, Velocity, InLane, Ahead, Spacing, Zone, GapOpenedAt, null, null, null, Step)
        : new(Id, Position, Velocity, InLane, Ahead, Spacing, Zone, GapOpenedAt, Trait.Id, Patience, Trait.Shown(Stage), Step);
}
