using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>A cue for <paramref name="Agent"/> to show; <paramref name="Stage"/> is the stage it shows from then on.</summary>
internal readonly record struct Cue(AgentId Agent, CueKind Kind, Stage? Stage);
