using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class ClerkLinesTests
{
    [Fact]
    public void A_clean_run_hears_a_flat_next()
    {
        Assert.Equal("Next.", ClerkLines.For(null));
    }

    [Fact]
    public void Every_mistake_gets_a_line_of_its_own_that_still_ends_in_next()
    {
        var lines = Enum.GetValues<Mistake>().Select(mistake => ClerkLines.For(mistake)).ToList();

        Assert.Equal(lines.Count, lines.Distinct().Count());
        Assert.All(lines, line => Assert.EndsWith(" Next.", line, StringComparison.Ordinal));
    }
}
