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
    public const float WallBehindClerk = 1.4f;

    /// <summary>How far either side of the clerk the side walls stand, in meters.</summary>
    public const float RoomHalfWidth = 7f;

    /// <summary>How far the side walls run from the back wall, in meters.</summary>
    public const float RoomLength = 26f;

    /// <summary>How far from a side wall the chairs along it stand, and how far apart.</summary>
    private const float ChairFromWall = 0.5f;
    private const float ChairPitch = 0.6f;

    /// <summary>How far the first chair stands in front of the clerk, and how many there are.</summary>
    private const float ChairsFromClerk = 3f;
    private const int ChairCount = 5;

    /// <summary>How far the plants in the back corners stand from either wall.</summary>
    private const float PlantFromWalls = 0.5f;

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

    /// <summary>
    /// Where <paramref name="point"/> is in the room of a clerk at <paramref name="clerk"/> facing <paramref name="towardCustomers"/>:
    /// how far in front of the clerk, and how far to the clerk's right.
    /// </summary>
    public static Vector2 InRoom(Vector2 clerk, Vector2 towardCustomers, Vector2 point)
    {
        var forward = Vector2.Normalize(towardCustomers);
        var offset = point - clerk;
        return new(Vector2.Dot(offset, forward), Vector2.Dot(offset, Right(forward)));
    }

    /// <summary>The chairs along the side wall to the clerk's left, each facing into the room.</summary>
    public static IReadOnlyList<(Vector2 At, Vector2 Facing)> Chairs(Vector2 clerk, Vector2 towardCustomers)
    {
        var forward = Vector2.Normalize(towardCustomers);
        var left = -Right(forward);
        return [.. Enumerable.Range(0, ChairCount).Select(i => (clerk + (left * (RoomHalfWidth - ChairFromWall)) + (forward * (ChairsFromClerk + (i * ChairPitch))), -left))];
    }

    /// <summary>The plants in the room's two back corners.</summary>
    public static IReadOnlyList<Vector2> Plants(Vector2 clerk, Vector2 towardCustomers)
    {
        var forward = Vector2.Normalize(towardCustomers);
        var back = clerk - (forward * (WallBehindClerk - PlantFromWalls));
        var side = Right(forward) * (RoomHalfWidth - PlantFromWalls);
        return [back - side, back + side];
    }

    /// <summary>The direction to the right of someone facing <paramref name="forward"/>, on the sim's floor.</summary>
    private static Vector2 Right(Vector2 forward) => new(-forward.Y, forward.X);
}
