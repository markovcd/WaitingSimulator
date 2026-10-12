namespace WaitingSimulator.Game;

/// <summary>The one-off cues an event shows. The stage an agent is in shows continuously, in its pose.</summary>
internal enum CueKind
{
    /// <summary>The person behind walked into the agent: it stumbles.</summary>
    Bumped,

    /// <summary>The agent's shown stage got worse: a sigh, a mutter, an "excuse me?".</summary>
    Escalated,

    /// <summary>The agent's patience ran out.</summary>
    RanOut,

    /// <summary>The agent's ticket slipped to the floor: a paper flutter.</summary>
    TicketDropped,
}
