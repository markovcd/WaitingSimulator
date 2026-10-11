namespace WaitingSimulator.Game;

/// <summary>
/// The body language of one stage: a steady lean toward the front, a side-to-side sway and a foot tap that bobs the body.
/// Leans and sways are in radians, taps in meters, rates in cycles per second.
/// </summary>
internal readonly record struct StageMotion(float Lean, float Sway, float SwayHertz, float Tap, float TapHertz);
