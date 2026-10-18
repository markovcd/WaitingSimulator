using System.Numerics;

namespace WaitingSim;

/// <summary>
/// The counter a crowd presses against, as the straight edge the crowd stands along: from <see cref="From"/> to
/// <see cref="To"/>, with the crowd on the side away from the clerk.
/// </summary>
public sealed record Counter
{
    private Counter(Vector2 from, Vector2 to, Vector2 towardCrowd)
    {
        From = from;
        To = to;
        Length = Vector2.Distance(from, to);
        Along = (to - from) / Length;
        TowardCrowd = towardCrowd;
    }

    /// <summary>One end of the counter's edge.</summary>
    public Vector2 From { get; }

    /// <summary>The other end of the counter's edge.</summary>
    public Vector2 To { get; }

    /// <summary>The counter's length, in meters.</summary>
    public float Length { get; }

    /// <summary>The direction from <see cref="From"/> to <see cref="To"/>.</summary>
    public Vector2 Along { get; }

    /// <summary>The direction out from the counter into the crowd.</summary>
    public Vector2 TowardCrowd { get; }

    /// <summary>The counter from <paramref name="from"/> to <paramref name="to"/>, faced by a crowd on the side away from <paramref name="clerk"/>; null if it has no length or the clerk stands on its line.</summary>
    public static Counter? Between(Vector2 from, Vector2 to, Vector2 clerk)
    {
        var length = Vector2.Distance(from, to);
        if (!(length > 0f) || !float.IsFinite(length))
        {
            return null;
        }

        var along = (to - from) / length;
        var normal = new Vector2(-along.Y, along.X);
        var clerkSide = Vector2.Dot(clerk - from, normal);
        return clerkSide == 0f ? null : new Counter(from, to, clerkSide > 0f ? -normal : normal);
    }

    /// <summary>How far along the counter from <see cref="From"/> <paramref name="point"/> is, and how far out into the crowd.</summary>
    public (float Along, float Out) Measure(Vector2 point) => (Vector2.Dot(point - From, Along), Vector2.Dot(point - From, TowardCrowd));

    /// <summary>The point <paramref name="out"/> meters out into the crowd from <paramref name="along"/> meters along the counter, kept on it.</summary>
    public Vector2 At(float along, float @out) => From + (Along * Math.Clamp(along, 0f, Length)) + (TowardCrowd * @out);
}
