namespace WaitingSim;

/// <summary>
/// The sim's seeded random number generator: PCG32 (pcg-random.org, XSH RR 64/32).
/// Every random choice in the sim draws from one of these, so a seed replays exactly.
/// </summary>
public sealed class Rng
{
    private const ulong Multiplier = 6364136223846793005;
    private const ulong DefaultStream = 54;

    private readonly ulong increment;
    private ulong state;

    /// <summary>Starts the generator the way the reference <c>pcg32_srandom_r</c> does. Each stream is an independent sequence.</summary>
    public Rng(ulong seed, ulong stream = DefaultStream)
    {
        increment = (stream << 1) | 1;
        NextUInt();
        state += seed;
        NextUInt();
    }

    /// <summary>Where the generator is in its sequence; the sim's state hash includes it.</summary>
    internal (ulong State, ulong Increment) Position => (state, increment);

    /// <summary>A uniform 32-bit number.</summary>
    public uint NextUInt()
    {
        var old = state;
        state = unchecked((old * Multiplier) + increment);
        var xorShifted = (uint)(((old >> 18) ^ old) >> 27);
        var rotation = (int)(old >> 59);
        return uint.RotateRight(xorShifted, rotation);
    }

    /// <summary>A uniform integer in [0, bound), without modulo bias.</summary>
    public int NextBelow(int bound)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bound);
        var span = (uint)bound;
        var threshold = unchecked(0u - span) % span;
        while (true)
        {
            var value = NextUInt();
            if (value >= threshold)
            {
                return (int)(value % span);
            }
        }
    }
}
