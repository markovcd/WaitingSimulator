using System.Numerics;
using System.Text.Json;

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
        if (Problem(points, halfWidth) is { } problem)
        {
            throw new ArgumentException(problem, nameof(points));
        }

        this.points = [.. points];
        startsAlong = new float[this.points.Length];
        for (var i = 1; i < this.points.Length; i++)
        {
            startsAlong[i] = startsAlong[i - 1] + Vector2.Distance(this.points[i - 1], this.points[i]);
        }

        Points = Array.AsReadOnly(this.points);
        HalfWidth = halfWidth;
    }

    /// <summary>The lane's line, front first.</summary>
    public IReadOnlyList<Vector2> Points { get; }

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

    /// <summary>What is wrong with a lane of these <paramref name="points"/> and <paramref name="halfWidth"/>, or null if nothing is.</summary>
    public static string? Problem(IReadOnlyList<Vector2> points, float halfWidth)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2)
        {
            return "A lane needs at least two points.";
        }

        if (!float.IsFinite(halfWidth) || halfWidth <= 0f)
        {
            return "The half width is a positive distance.";
        }

        for (var i = 1; i < points.Count; i++)
        {
            var length = Vector2.Distance(points[i - 1], points[i]);
            if (!float.IsFinite(length) || length <= 0f)
            {
                return $"Lane points {i - 1} and {i} are the same point or not finite.";
            }
        }

        return null;
    }

    /// <summary>Reads the <c>lane</c> object: its points and half width. Null, with problems, when it cannot make a lane.</summary>
    internal static Lane? Read(JsonElement root, List<string> problems)
    {
        var section = JsonFile.Section(root, "lane", problems);
        var items = JsonFile.Items(section, "lane.points", problems);
        var points = items.Select(JsonFile.Point).ToList();
        for (var i = 0; i < points.Count; i++)
        {
            if (points[i] is null)
            {
                problems.Add(JsonFile.Invariant($"lane.points[{i}]: not a point [x, y] of two finite numbers"));
            }
        }

        var halfWidth = JsonFile.Positive(section, "lane.halfWidth", problems);
        if (problems.Count > 0)
        {
            return null;
        }

        var shape = points.Select(point => point!.Value).ToList();
        if (Problem(shape, halfWidth) is { } problem)
        {
            problems.Add($"lane: {problem}");
            return null;
        }

        return new Lane(shape, halfWidth);
    }

    /// <summary>Whether a point at <paramref name="position"/> is inside the lane.</summary>
    public bool Holds(LanePosition position) => position.Off <= HalfWidth;
}
