using System.Numerics;

namespace WaitingSim;

/// <summary>
/// The volume the line occupies: a polyline on the floor plane from the front of the line to the back,
/// widened by <see cref="HalfWidth"/> on each side. Spacing is measured along it, so a snaking line measures true.
/// </summary>
public sealed class Lane
{
    private readonly Vector2[] points;
    private readonly float[] startsAlong;

    /// <param name="points">The lane's line, front first. Neighboring points differ.</param>
    /// <param name="halfWidth">How far from the line an agent can stand and still be in the lane, in meters.</param>
    public Lane(IReadOnlyList<Vector2> points, float halfWidth)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentOutOfRangeException.ThrowIfLessThan(points.Count, 2, nameof(points));
        if (!float.IsFinite(halfWidth) || halfWidth <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(halfWidth), halfWidth, "The half width is a positive distance.");
        }

        this.points = [.. points];
        startsAlong = new float[this.points.Length];
        for (var i = 1; i < this.points.Length; i++)
        {
            var length = Vector2.Distance(this.points[i - 1], this.points[i]);
            if (!float.IsFinite(length) || length <= 0f)
            {
                throw new ArgumentException($"Lane points {i - 1} and {i} are the same point or not finite.", nameof(points));
            }

            startsAlong[i] = startsAlong[i - 1] + length;
        }

        HalfWidth = halfWidth;
    }

    /// <summary>How far from the line an agent can stand and still be in the lane, in meters.</summary>
    public float HalfWidth { get; }

    /// <summary>Where <paramref name="point"/> falls against the lane, measured to the nearest point on its line.</summary>
    public LanePosition Measure(Vector2 point)
    {
        var best = new LanePosition(0f, float.PositiveInfinity);
        for (var i = 1; i < points.Length; i++)
        {
            var start = points[i - 1];
            var segment = points[i] - start;
            var length = startsAlong[i] - startsAlong[i - 1];
            var t = Math.Clamp(Vector2.Dot(point - start, segment) / (length * length), 0f, 1f);
            var off = Vector2.Distance(point, start + (segment * t));
            if (off < best.Off)
            {
                best = new(startsAlong[i - 1] + (t * length), off);
            }
        }

        return best;
    }

    /// <summary>Whether a point at <paramref name="position"/> is inside the lane.</summary>
    public bool Holds(LanePosition position) => position.Off <= HalfWidth;
}
