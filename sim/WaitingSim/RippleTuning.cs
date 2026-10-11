namespace WaitingSim;

/// <summary>How the line moves on its own: the front is served and leaves, and each person behind steps up in turn.</summary>
/// <param name="ServiceTicks">How long the clerk serves the person at the counter.</param>
/// <param name="ReactionTicks">How long an agent takes to step up once it notices the person ahead moved off.</param>
/// <param name="SpacingMeters">How far behind the person ahead an agent likes to stand, drawn once per agent.</param>
/// <param name="NoticeMeters">How much farther than that the person ahead must be before the agent notices.</param>
/// <param name="LeaveMeters">How far past the edge of the lane a served agent walks before it stops.</param>
public sealed record RippleTuning(TickRange ServiceTicks, TickRange ReactionTicks, MeterRange SpacingMeters, float NoticeMeters, float LeaveMeters);
