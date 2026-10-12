using System.Numerics;

namespace WaitingSimulator.Game;

/// <summary>
/// One primitive of a figure, centered at <paramref name="Center"/> from its <paramref name="Limb"/>'s pivot, in meters
/// with y up and the figure facing -z.
/// </summary>
internal sealed record FigurePart(string Name, PartShape Shape, Vector3 Size, Vector3 Center, Limb Limb, Swatch Swatch);
