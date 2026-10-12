namespace WaitingSimulator.Game;

/// <summary>Where the player stands against the line, as the lane marking shows it.</summary>
internal enum LaneState
{
    /// <summary>In the lane, with a place in the line.</summary>
    InLine,

    /// <summary>Out of the lane with the place held.</summary>
    PlaceHeld,

    /// <summary>Out of the line: out of the lane with no place held, or refused one.</summary>
    OutOfLine,
}
