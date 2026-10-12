using System.Numerics;

namespace WaitingSim;

/// <summary>One agent as the sim last left it.</summary>
/// <param name="Id">The agent.</param>
/// <param name="Position">Where it stands on the floor plane, in meters.</param>
/// <param name="Velocity">How it is moving, in meters per second.</param>
/// <param name="InLane">Whether it stands in the lane.</param>
/// <param name="Ahead">The person ahead, if it is part of the line and someone is.</param>
/// <param name="Spacing">Meters along the lane to the person ahead, when there is one.</param>
/// <param name="Zone">The zone that spacing falls in.</param>
/// <param name="GapOpenedAt">The tick its gap opened, while it is in <see cref="WaitingSim.Zone.Gap"/>.</param>
/// <param name="Trait">Its trait's id; null for an agent without patience, like the player.</param>
/// <param name="Patience">Its patience, from full down to 0.</param>
/// <param name="Stage">The stage it shows.</param>
/// <param name="Step">Where it is in the ripple, when the line moves it.</param>
/// <param name="SpotHeldUntil">While it is out of the lane with its place held, the tick the hold runs out.</param>
/// <param name="Refused">Whether the line refused it a place for cutting in without room.</param>
/// <param name="Tally">The mistakes it has made.</param>
/// <param name="ReachedCounterAt">The tick it first reached the counter, once it has.</param>
/// <param name="KickedOutAt">The tick it was first kicked out of the line, once it has been.</param>
/// <param name="HeldAlong">While its place is held, how far along the lane from the front the place is, in meters.</param>
/// <param name="TicketAt">Where its dropped ticket lies on the floor, while it does.</param>
/// <param name="TurnedAwayAt">The tick the clerk turned it away for reaching the counter without its ticket, once it has.</param>
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
    LineStep Step,
    long? SpotHeldUntil,
    bool Refused,
    Tally Tally,
    long? ReachedCounterAt,
    long? KickedOutAt,
    float? HeldAlong = null,
    Vector2? TicketAt = null,
    long? TurnedAwayAt = null);
