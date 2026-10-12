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

    /// <summary>The color of the mark at a held place, a deeper amber than the held fill.</summary>
    public static Paint HeldPaint { get; } = new(0.85f, 0.5f, 0.12f);

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

    /// <summary>The mark at <paramref name="player"/>'s held place at <paramref name="tick"/>, while they are out with one held.</summary>
    public static HeldMarker? Held(AgentSnapshot player, Lane lane, long tick, int spotHoldTicks)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(lane);
        if (StateOf(player) != LaneState.PlaceHeld || player is not { HeldAlong: { } along, SpotHeldUntil: { } until })
        {
            return null;
        }

        var point = lane.PointAt(along);
        var left = spotHoldTicks > 0 ? Math.Clamp((float)(until - tick) / spotHoldTicks, 0f, 1f) : 0f;
        return new HeldMarker(point, lane.TowardFront(point), left);
    }

    /// <summary>The lane's fill while the player is <paramref name="state"/>: pale in the line, amber while the place is held, red out of it.</summary>
    public static Paint Fill(LaneState state) => state switch
    {
        LaneState.InLine => new(0.78f, 0.75f, 0.6f),
        LaneState.PlaceHeld => new(0.8f, 0.62f, 0.32f),
        _ => new(0.72f, 0.42f, 0.38f),
    };
}
