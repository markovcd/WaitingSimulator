namespace WaitingSim.Tests;

public sealed class RngTests
{
    [Fact]
    public void A_seed_draws_the_same_numbers_as_the_PCG32_reference()
    {
        // pcg32-demo's first round: pcg32_srandom_r(&rng, 42u, 54u).
        uint[] reference = [0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e];

        Assert.Equal(reference, Drawn(new Rng(42), reference.Length));
    }

    [Fact]
    public void Different_seeds_draw_different_numbers()
    {
        Assert.NotEqual(Drawn(new Rng(1), 8), Drawn(new Rng(2), 8));
    }

    [Fact]
    public void A_bounded_draw_stays_below_its_bound_and_reaches_every_value()
    {
        var rng = new Rng(7);
        var seen = new bool[6];
        for (var i = 0; i < 1000; i++)
        {
            seen[rng.NextBelow(6)] = true;
        }

        Assert.All(seen, Assert.True);
    }

    private static uint[] Drawn(Rng rng, int count) => [.. Enumerable.Range(0, count).Select(_ => rng.NextUInt())];
}
