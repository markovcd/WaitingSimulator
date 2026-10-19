using System.Numerics;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// Where the furniture around a level stands: the counter in front of a line's clerk, the wall behind a clerk and the
/// posts along a lane. It is placed from the level's data and only looked at; the sim never sees it.
/// </summary>
internal static class SetDressing
{
    /// <summary>How long the counter in front of a line's clerk is, in meters.</summary>
    public const float LineCounterLength = 1.6f;

    /// <summary>How far apart the posts along a lane stand, in meters.</summary>
    public const float PostSpacing = 2.5f;

    /// <summary>How far in front of the lane's front its counter's edge stands, clear of the front agent's body.</summary>
    private const float CounterClearance = 0.32f;

    /// <summary>How far outside the lane's edge a post stands, so nobody in the lane stands in one.</summary>
    private const float PostClearance = 0.15f;

    /// <summary>How far behind the clerk the back wall stands, in meters.</summary>
    private const float WallBehindClerk = 1.4f;

    /// <summary>The counter between <paramref name="clerk"/> and the front of <paramref name="lane"/>, with the line on its far side; null if the clerk stands at the front.</summary>
    public static Counter? LineCounter(Lane lane, Vector2 clerk)
    {
        ArgumentNullException.ThrowIfNull(lane);
        var front = lane.Points[0];
        var toward = front - clerk;
        if (!(toward.Length() > CounterClearance))
        {
            return null;
        }

        toward = Vector2.Normalize(toward);
        var middle = front - (toward * CounterClearance);
        var along = new Vector2(-toward.Y, toward.X) * (LineCounterLength / 2f);
        return Counter.Between(middle - along, middle + along, clerk);
    }

    /// <summary>The middle of the wall behind <paramref name="clerk"/>, who faces <paramref name="towardCustomers"/>.</summary>
    public static Vector2 BackWall(Vector2 clerk, Vector2 towardCustomers) => clerk - (Vector2.Normalize(towardCustomers) * WallBehindClerk);

    /// <summary>The posts along both edges of <paramref name="lane"/>, every <see cref="PostSpacing"/> from its front, leaving out any a bend puts in the lane.</summary>
    public static IReadOnlyList<Vector2> Posts(Lane lane)
    {
        ArgumentNullException.ThrowIfNull(lane);
        var posts = new List<Vector2>();
        for (var along = PostSpacing / 2f; along <= lane.Length; along += PostSpacing)
        {
            var point = lane.PointAt(along);
            var front = lane.TowardFront(point);
            var across = new Vector2(-front.Y, front.X) * (lane.HalfWidth + PostClearance);
            posts.AddRange(new[] { point + across, point - across }.Where(post => lane.Measure(post).Off >= lane.HalfWidth + (PostClearance / 2f)));
        }

        return posts;
    }
}
