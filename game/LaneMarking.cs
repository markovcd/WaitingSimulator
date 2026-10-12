using System.Numerics;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The lane painted on the floor, the shape the sim counts as in it: a strip along each segment of its line and a disc
/// at each point, all <see cref="HalfWidth"/> wide either side, with tape along the strips' edges. Its fill shows the
/// player where they stand against the line.
/// </summary>
internal sealed record LaneMarking(IReadOnlyList<LaneStrip> Strips, IReadOnlyList<Vector2> Caps, float HalfWidth)
{
    public static LaneMarking Of(Lane lane)
    {
        ArgumentNullException.ThrowIfNull(lane);
        var points = lane.Points;
        return new([.. points.Skip(1).Select((to, i) => new LaneStrip(points[i], to))], points, lane.HalfWidth);
    }

    /// <summary>The color of the tape along the lane's edges.</summary>
    public static Paint Tape { get; } = new(0.2f, 0.19f, 0.17f);

    /// <summary>Where <paramref name="player"/> stands against the line.</summary>
    public static LaneState StateOf(AgentSnapshot player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return player switch
        {
            { InLane: true, Refused: false } => LaneState.InLine,
            { InLane: false, SpotHeldUntil: not null } => LaneState.PlaceHeld,
            _ => LaneState.OutOfLine,
        };
    }

    /// <summary>The lane's fill while the player is <paramref name="state"/>: pale in the line, amber while the place is held, red out of it.</summary>
    public static Paint Fill(LaneState state) => state switch
    {
        LaneState.InLine => new(0.78f, 0.75f, 0.6f),
        LaneState.PlaceHeld => new(0.8f, 0.62f, 0.32f),
        _ => new(0.72f, 0.42f, 0.38f),
    };
}
