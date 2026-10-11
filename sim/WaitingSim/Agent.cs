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

    /// <summary>Whether the agent stands in the lane, part of the line or not.</summary>
    public bool InLane { get; set; }

    /// <summary>Where along the lane the line holds the agent's place while it is out of the lane.</summary>
    public float? HeldAlong { get; set; }

    public long HoldEndsAt { get; set; }

    /// <summary>Who stood behind the agent when it stepped out: stepping back in ahead of them is returning to its place.</summary>
    public AgentId? HeldBehind { get; set; }

    /// <summary>Whether the line refused the agent a place when it cut in without room. It ends when the agent steps out.</summary>
    public bool Refused { get; set; }

    /// <summary>Whether the agent is part of the line: standing in the lane and not refused, or out of it with its place held.</summary>
    public bool InLine => (InLane && !Refused) || HeldAlong is not null;

    /// <summary>Where along the lane the agent's place in the line is.</summary>
    public float LineAlong => HeldAlong ?? Lane.Along;

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
        ? new(Id, Position, Velocity, InLane, Ahead, Spacing, Zone, GapOpenedAt, null, null, null, Step, SpotHeldUntil, Refused)
        : new(Id, Position, Velocity, InLane, Ahead, Spacing, Zone, GapOpenedAt, Trait.Id, Patience, Trait.Shown(Stage), Step, SpotHeldUntil, Refused);

    private long? SpotHeldUntil => HeldAlong is null ? null : HoldEndsAt;
}
