namespace WaitingSim;

/// <summary>A spacing band to the person ahead, measured along the lane.</summary>
public enum Zone
{
    /// <summary>Walked into the person ahead.</summary>
    Bump,

    /// <summary>The ideal spacing.</summary>
    Tight,

    /// <summary>Further than ideal, but not yet a gap.</summary>
    Loose,

    /// <summary>A gap has opened; the grace window runs.</summary>
    Gap,
}
