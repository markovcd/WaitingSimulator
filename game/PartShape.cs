namespace WaitingSimulator.Game;

/// <summary>The primitive a figure part is made of.</summary>
internal enum PartShape
{
    /// <summary>A box of the part's size.</summary>
    Box,

    /// <summary>An upright capsule as wide as the part's x size and as tall as its y size.</summary>
    Capsule,

    /// <summary>A ball as wide as the part's x size.</summary>
    Sphere,

    /// <summary>The top half of a ball as wide as the part's x size, its flat side on the part's center.</summary>
    Hemisphere,
}
