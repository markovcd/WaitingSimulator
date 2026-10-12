using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The face a figure makes for the stage it shows, each angrier than the one before, the clerk's bored one, and when a
/// figure blinks. Time is in ticks, so a replay blinks the same.
/// </summary>
internal static class Face
{
    /// <summary>The clerk's face: heavy lids and a flat mouth.</summary>
    public static Expression Bored { get; } = new(0f, -0.004f, 0.55f, 0f, 0f);

    private const int BlinkEveryTicks = 4 * TickRate.PerSecond;
    private const int BlinkTicks = 6;

    /// <summary>Ticks between neighbors' blinks, so a line does not blink together.</summary>
    private const int BlinkOffsetTicks = 37;

    /// <summary>The face for the stage an agent shows; an agent without patience looks calm.</summary>
    public static Expression For(Stage? stage) => stage switch
    {
        Stage.Restless => new(0.1f, 0f, 0.1f, -0.05f, 0f),
        Stage.Annoyed => new(0.3f, -0.004f, 0.3f, -0.2f, 0f),
        Stage.Confronting => new(0.5f, -0.008f, 0.4f, -0.3f, 0.012f),
        Stage.FedUp => new(0.7f, -0.01f, 0.5f, -0.45f, 0.025f),
        _ => new(0f, 0f, 0f, 0.1f, 0f),
    };

    /// <summary>Whether figure <paramref name="seed"/> has its eyes shut for a blink at <paramref name="tick"/>.</summary>
    public static bool Blinking(int seed, float tick) => ((tick + (seed * BlinkOffsetTicks)) % BlinkEveryTicks) is var into && into >= 0f && into < BlinkTicks;
}
