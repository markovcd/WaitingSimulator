namespace WaitingSim;

/// <summary>A distance drawn at random between <see cref="Min"/> and <see cref="Max"/> meters, both included.</summary>
public readonly record struct MeterRange(float Min, float Max)
{
    private const int Steps = 100;

    /// <summary>A distance from <paramref name="rng"/>, one of a hundred even steps across the range.</summary>
    public float Draw(Rng rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        return Min + ((Max - Min) * rng.NextBelow(Steps + 1) / Steps);
    }
}
