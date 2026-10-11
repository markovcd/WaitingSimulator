using System.Numerics;
using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class ResultScreenTests
{
    [Fact]
    public void The_result_screen_shows_the_clerk_s_line_the_time_in_line_and_every_mistake_counted()
    {
        var tally = new Tally(Bumps: 2, GapsLeftOpen: 1, PlacesLost: 0, MissedCutIns: 0, CutIns: 0);

        var text = ResultScreen.Text(Player(tally, reachedAt: 83 * TickRate.PerSecond));

        Assert.StartsWith($"CLERK: \"{ClerkLines.For(Mistake.GapLeftOpen)}\"", text, StringComparison.Ordinal);
        Assert.Contains("TIME IN LINE            01:23", text, StringComparison.Ordinal);
        Assert.Contains("BUMPS                       2", text, StringComparison.Ordinal);
        Assert.Contains("GAPS LEFT OPEN              1", text, StringComparison.Ordinal);
        Assert.Contains("TIMES CUT IN                0", text, StringComparison.Ordinal);
    }

    [Fact]
    public void There_is_no_result_before_the_counter()
    {
        Assert.Throws<ArgumentException>(() => ResultScreen.Text(Player(default, reachedAt: null)));
    }

    private static AgentSnapshot Player(Tally tally, long? reachedAt) =>
        new(new AgentId(0), Vector2.Zero, Vector2.Zero, true, null, null, null, null, null, null, null, LineStep.Standing, null, false, tally, reachedAt);
}
