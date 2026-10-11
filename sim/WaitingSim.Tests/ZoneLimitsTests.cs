namespace WaitingSim.Tests;

public sealed class ZoneLimitsTests
{
    private static readonly ZoneLimits Limits = new(BumpBelow: 0.4f, TightBelow: 0.9f, LooseBelow: 1.5f);

    [Theory]
    [InlineData(0f, Zone.Bump)]
    [InlineData(0.39f, Zone.Bump)]
    [InlineData(0.4f, Zone.Tight)]
    [InlineData(0.89f, Zone.Tight)]
    [InlineData(0.9f, Zone.Loose)]
    [InlineData(1.49f, Zone.Loose)]
    [InlineData(1.5f, Zone.Gap)]
    [InlineData(20f, Zone.Gap)]
    public void Each_zone_starts_at_its_lower_limit(float spacing, Zone zone)
    {
        Assert.Equal(zone, Limits.ZoneAt(spacing));
    }
}
