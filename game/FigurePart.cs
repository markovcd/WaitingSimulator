using System.Numerics;

namespace WaitingSimulator.Game;

/// <summary>
/// One primitive of a figure, centered at <paramref name="Center"/> from its <paramref name="Limb"/>'s pivot, in meters
/// with y up and the figure facing -z. A box's bottom is <paramref name="Taper"/> as wide and deep as its top; a loft is
/// its <paramref name="Rings"/>, bottom first, and its size is the box round them. A part that names
/// <paramref name="Wear"/> is on only the figures that wear it.
/// </summary>
internal sealed record FigurePart(
    string Name,
    PartShape Shape,
    Vector3 Size,
    Vector3 Center,
    Limb Limb,
    Swatch Swatch,
    float Taper = 1f,
    Wear Wear = Wear.None,
    IReadOnlyList<Ring>? Rings = null);
