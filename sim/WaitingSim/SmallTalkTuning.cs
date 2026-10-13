namespace WaitingSim;

/// <summary>How the person behind makes small talk: how often, for how long, and what listening or ignoring it does.</summary>
/// <param name="IntervalTicks">How long after the last talk ended the next one is due, drawn each time.</param>
/// <param name="TalkTicks">How long a talk lasts, drawn each time.</param>
/// <param name="ListenShare">The share of a talk the listener has to spend glancing back for it to count as listened to, from 0 up to 1.</param>
/// <param name="ListenedRecovery">The patience the talker gets back from a talk listened to.</param>
/// <param name="IgnoredHit">The patience the talker loses over a talk ignored.</param>
public sealed record SmallTalkTuning(TickRange IntervalTicks, TickRange TalkTicks, float ListenShare, float ListenedRecovery, float IgnoredHit);
