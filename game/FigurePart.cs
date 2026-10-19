using System.Numerics;

namespace WaitingSimulator.Game;

/// <summary>
/// One primitive of a figure, centered at <paramref name="Center"/> from its <paramref name="Limb"/>'s pivot, in meters
/// with y up and the figure facing -z, tipped <paramref name="Tilt"/> radians about x, its top toward the back. A box's
/// bottom is <paramref name="Taper"/> as wide and deep as its top. A part that names <paramref name="Wear"/> is on only
/// the figures that wear it.
/// </summary>
internal sealed record FigurePart(
    string Name, PartShape Shape, Vector3 Size, Vector3 Center, Limb Limb, Swatch Swatch, float Tilt = 0f, float Taper = 1f, Wear Wear = Wear.None);
