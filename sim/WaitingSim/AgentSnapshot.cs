using System.Numerics;

namespace WaitingSim;

/// <summary>One agent as the sim last left it.</summary>
/// <param name="Id">The agent.</param>
/// <param name="Position">Where it stands on the floor plane, in meters.</param>
/// <param name="Velocity">How it is moving, in meters per second.</param>
/// <param name="InLane">Whether it is part of the line.</param>
/// <param name="Ahead">The person ahead, if it is in the lane and someone is.</param>
/// <param name="Spacing">Meters along the lane to the person ahead, when there is one.</param>
/// <param name="Zone">The zone that spacing falls in.</param>
/// <param name="GapOpenedAt">The tick its gap opened, while it is in <see cref="WaitingSim.Zone.Gap"/>.</param>
/// <param name="Trait">Its trait's id; null for an agent without patience, like the player.</param>
/// <param name="Patience">Its patience, from full down to 0.</param>
/// <param name="Stage">The stage it shows.</param>
/// <param name="Step">Where it is in the ripple, when the line moves it.</param>
public sealed record AgentSnapshot(
    AgentId Id,
    Vector2 Position,
    Vector2 Velocity,
    bool InLane,
    AgentId? Ahead,
    float? Spacing,
    Zone? Zone,
    long? GapOpenedAt,
    string? Trait,
    float? Patience,
    Stage? Stage,
    LineStep Step);
