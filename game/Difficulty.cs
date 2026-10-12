namespace WaitingSimulator.Game;

/// <summary>How much the HUD tells the player. The sim plays the same on every one.</summary>
internal enum Difficulty
{
    /// <summary>Deadpan numbers: the patience of the person behind and the grace left on a gap.</summary>
    Easy,

    /// <summary>A quiet word when a gap opens in front of the player.</summary>
    Normal,

    /// <summary>No HUD: the line's cues only.</summary>
    Hard,
}
