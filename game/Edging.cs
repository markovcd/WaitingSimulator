namespace WaitingSimulator.Game;

/// <summary>The ink line drawn round a surface, if any, and how it follows the shape's edges.</summary>
internal enum Edging
{
    /// <summary>No line: floors, markings and anything too small to carry one.</summary>
    None,

    /// <summary>A line pushed out along the shape's smooth normals: balls, capsules and cylinders.</summary>
    Round,

    /// <summary>A line pushed out from each corner along every axis, so a box's rim stays whole.</summary>
    Square,
}
