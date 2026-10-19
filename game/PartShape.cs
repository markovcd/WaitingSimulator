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

    /// <summary>Octagonal cross-sections joined bottom to top: the part's rings.</summary>
    Loft,
}
