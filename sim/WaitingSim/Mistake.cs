namespace WaitingSim;

/// <summary>Something an agent did wrong on its way to the counter, from least to most serious.</summary>
public enum Mistake
{
    /// <summary>Walked into the person ahead.</summary>
    Bump,

    /// <summary>Let a gap stay open past its grace window.</summary>
    GapLeftOpen,

    /// <summary>Stepped out of the lane and lost its place.</summary>
    LostPlace,

    /// <summary>Left a gap open so long a cutter slid into it.</summary>
    LetCutterIn,

    /// <summary>Tried to cut in without room to stand.</summary>
    MissedCutIn,

    /// <summary>Cut in ahead of someone.</summary>
    CutIn,

    /// <summary>Reached the counter without its ticket.</summary>
    NoTicket,
}
