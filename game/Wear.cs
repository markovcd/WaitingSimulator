namespace WaitingSimulator.Game;

/// <summary>What a figure may have on besides its clothes; a part worn only by some figures names one.</summary>
[Flags]
internal enum Wear
{
    None = 0,

    /// <summary>A hood hanging down the back of the neck.</summary>
    Hood = 1,

    /// <summary>A cap over the hair, with a brim.</summary>
    Cap = 2,

    /// <summary>A pack on the back.</summary>
    Backpack = 4,
}
