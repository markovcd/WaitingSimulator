using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class SoundsTests
{
    [Fact]
    public void Each_worse_stage_sounds_lower_and_lasts_longer()
    {
        var tones = new[] { Stage.Restless, Stage.Annoyed, Stage.Confronting, Stage.FedUp }
            .Select(stage => Sounds.For(new Cue(default, CueKind.Escalated, stage)))
            .ToList();

        List<Tone> lower = [.. tones.OrderByDescending(tone => tone.Hertz)];
        List<Tone> longer = [.. tones.OrderBy(tone => tone.Seconds)];
        Assert.Equal(lower, tones);
        Assert.Equal(longer, tones);
        Assert.Equal(tones.Count, tones.Distinct().Count());
    }

    [Fact]
    public void Every_tone_a_cue_plays_is_among_those_built_up_front()
    {
        var built = Sounds.All().ToHashSet();
        var cues = Enum.GetValues<CueKind>()
            .SelectMany(kind => Enum.GetValues<Stage>().Select(stage => new Cue(default, kind, stage)).Append(new Cue(default, kind, null)));

        Assert.All(cues, cue => Assert.Contains(Sounds.For(cue), built));
    }

    [Fact]
    public void A_tone_is_its_length_of_16_bit_samples_and_starts_and_ends_silent()
    {
        var pcm = new Tone(440f, 0.5f).Pcm16();

        Assert.Equal(Tone.MixRate, pcm.Length);
        Assert.Equal(0, Sample(pcm, 0));
        Assert.InRange(Math.Abs((int)Sample(pcm, (pcm.Length / 2) - 1)), 0, 100);
        Assert.InRange(Enumerable.Range(0, pcm.Length / 2).Max(i => Math.Abs((int)Sample(pcm, i))), 5000, short.MaxValue);
    }

    private static short Sample(byte[] pcm, int index) => (short)(pcm[2 * index] | (pcm[(2 * index) + 1] << 8));
}
