namespace WaitingSim;

/// <summary>Where an agent the line moves is in the ripple. An agent given commands stays <see cref="Standing"/>.</summary>
public enum LineStep
{
    /// <summary>Standing where it wants to be.</summary>
    Standing,

    /// <summary>Has seen the person ahead move off, and steps up once its reaction delay runs out.</summary>
    Noticed,

    /// <summary>Walking up to its spacing behind the person ahead, or to the counter at the front.</summary>
    SteppingUp,

    /// <summary>At the counter, at the front of the line.</summary>
    BeingServed,

    /// <summary>Served, and walking out of the lane to the side.</summary>
    Leaving,

    /// <summary>Served and clear of the line; it stands where it stopped.</summary>
    Done,

    /// <summary>A cutter walking to a gap left open too long, to slide into it.</summary>
    Cutting,
}
