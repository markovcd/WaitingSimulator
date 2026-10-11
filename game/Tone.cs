namespace WaitingSimulator.Game;

/// <summary>A placeholder sound: a sine at <paramref name="Hertz"/> that fades out over <paramref name="Seconds"/>.</summary>
internal readonly record struct Tone(float Hertz, float Seconds)
{
    /// <summary>Samples per second of every tone.</summary>
    public const int MixRate = 22050;

    private const float Volume = 0.4f;
    private const float FadeInSeconds = 0.005f;

    /// <summary>The tone as mono 16-bit little-endian samples at <see cref="MixRate"/>, starting and ending silent.</summary>
    public byte[] Pcm16()
    {
        var count = (int)(Seconds * MixRate);
        var bytes = new byte[count * 2];
        for (var i = 0; i < count; i++)
        {
            var time = (float)i / MixRate;
            var envelope = Math.Min(time / FadeInSeconds, 1f) * (1f - ((float)i / count));
            var sample = (short)(MathF.Sin(MathF.Tau * Hertz * time) * envelope * Volume * short.MaxValue);
            bytes[2 * i] = (byte)sample;
            bytes[(2 * i) + 1] = (byte)(sample >> 8);
        }

        return bytes;
    }
}
