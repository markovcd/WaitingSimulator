namespace WaitingSim;

/// <summary>A 64-bit FNV-1a hash built up from sim state, value by value. Floats go in by their exact bits.</summary>
internal sealed class StateHash
{
    private const ulong Prime = 1099511628211;

    public ulong Value { get; private set; } = 14695981039346656037;

    public StateHash Add(ulong value)
    {
        for (var shift = 0; shift < 64; shift += 8)
        {
            Value = unchecked((Value ^ ((value >> shift) & 0xff)) * Prime);
        }

        return this;
    }

    public StateHash Add(long value) => Add(unchecked((ulong)value));

    public StateHash Add(float value) => Add((ulong)BitConverter.SingleToUInt32Bits(value));

    public StateHash Add(bool value) => Add(value ? 1UL : 0UL);

    public StateHash Add(string value)
    {
        Add((long)value.Length);
        foreach (var character in value)
        {
            Add((ulong)character);
        }

        return this;
    }
}
