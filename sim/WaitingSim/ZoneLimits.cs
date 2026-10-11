namespace WaitingSim;

/// <summary>Where each zone ends, in meters from the person ahead. Each zone includes its lower edge.</summary>
public sealed record ZoneLimits(float BumpBelow, float TightBelow, float LooseBelow)
{
    /// <summary>The zone an agent is in at <paramref name="spacing"/> meters behind the person ahead.</summary>
    public Zone ZoneAt(float spacing) => spacing switch
    {
        _ when spacing < BumpBelow => Zone.Bump,
        _ when spacing < TightBelow => Zone.Tight,
        _ when spacing < LooseBelow => Zone.Loose,
        _ => Zone.Gap,
    };
}
