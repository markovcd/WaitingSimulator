namespace WaitingSim;

/// <summary>A duration drawn at random between <see cref="Min"/> and <see cref="Max"/> ticks, both included.</summary>
public readonly record struct TickRange(int Min, int Max)
{
    /// <summary>A duration from <paramref name="rng"/>, uniform over the range.</summary>
    public int Draw(Rng rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        return Min + rng.NextBelow(Max - Min + 1);
    }
}
