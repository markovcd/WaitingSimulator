namespace WaitingSim;

/// <summary>How a crowd at a counter behaves: the clerk's glance, the invisible line and the jostling.</summary>
/// <param name="GlanceMetersPerSecond">How fast the clerk's glance sweeps along the counter, back and forth.</param>
/// <param name="GlanceReachMeters">How near along the counter the glance has to pass an agent at the front to land on it.</param>
/// <param name="WaveReachMeters">How near it has to pass an agent waving.</param>
/// <param name="WaveDrainPerSecond">How fast waving drains everyone within sight of the waver.</param>
/// <param name="FrontMeters">How far from the counter an agent stands and still counts as at the front.</param>
/// <param name="PickTicks">How long the clerk asks "Yes?" before serving whom the glance landed on; the time to call out.</param>
/// <param name="ServiceTicks">How long the clerk serves someone, drawn for each.</param>
/// <param name="ClerkMemoryTicks">How much earlier than whom the glance landed on someone at the front must have arrived for the clerk to serve them instead.</param>
/// <param name="SightMeters">How near an agent has to stand to see what happens to someone and take it personally.</param>
/// <param name="ServedAheadHit">What being served ahead of someone who was there first costs them, if they see it.</param>
/// <param name="FalseClaimHit">What saying "I was here first" without being first costs everyone within sight.</param>
/// <param name="SqueezeDrainPerSecond">How fast being shoved drains the one shoved.</param>
/// <param name="LetThroughAfterTicks">How long one agent shoves into another before the crowd lets it through.</param>
/// <param name="PassTicks">How long two agents let through each other take to pass, overlapping.</param>
/// <param name="StandOffMeters">How near someone has to stand in an agent's way for it to stop walking and only press.</param>
/// <param name="PressShare">How hard, as a share of walking speed, an agent the crowd moves presses on toward the counter with someone in its way.</param>
/// <param name="SwayShare">How hard, as a share of walking speed, everyone the crowd moves sways about.</param>
/// <param name="SwayTicks">How long each sway holds its direction before it picks another, drawn for each.</param>
/// <param name="ShoveMetersPerSecond">How fast one agent has to be moving into another for it to be a shove, which costs the one shoved.</param>
/// <param name="Give">The share of the overlap between two bodies that pushes them apart each tick, above 0 and at most 1; less is a softer, more fluid crowd.</param>
/// <param name="RecoveryPerSecond">How fast patience comes back in a crowd.</param>
/// <param name="ArrivalTicks">How long after one newcomer arrives the next one does, drawn for each.</param>
/// <param name="CutterShare">The share of newcomers, from 0 to 1, that push to the front instead of waiting for room.</param>
public sealed record CrowdTuning(
    float GlanceMetersPerSecond,
    float GlanceReachMeters,
    float WaveReachMeters,
    float WaveDrainPerSecond,
    float FrontMeters,
    int PickTicks,
    TickRange ServiceTicks,
    int ClerkMemoryTicks,
    float SightMeters,
    float ServedAheadHit,
    float FalseClaimHit,
    float SqueezeDrainPerSecond,
    int LetThroughAfterTicks,
    int PassTicks,
    float StandOffMeters,
    float PressShare,
    float SwayShare,
    TickRange SwayTicks,
    float ShoveMetersPerSecond,
    float Give,
    float RecoveryPerSecond,
    TickRange ArrivalTicks,
    float CutterShare);
