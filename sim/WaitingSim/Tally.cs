namespace WaitingSim;

/// <summary>How many times an agent made each mistake.</summary>
public readonly record struct Tally(int Bumps, int GapsLeftOpen, int PlacesLost, int CuttersLetIn, int MissedCutIns, int CutIns)
{
    /// <summary>The most serious mistake made at least once, or null for none.</summary>
    public Mistake? Worst => this switch
    {
        { CutIns: > 0 } => Mistake.CutIn,
        { MissedCutIns: > 0 } => Mistake.MissedCutIn,
        { CuttersLetIn: > 0 } => Mistake.LetCutterIn,
        { PlacesLost: > 0 } => Mistake.LostPlace,
        { GapsLeftOpen: > 0 } => Mistake.GapLeftOpen,
        { Bumps: > 0 } => Mistake.Bump,
        _ => null,
    };

    /// <summary>This tally of agent <paramref name="self"/>'s with <paramref name="happened"/> counted, if it was a mistake of theirs.</summary>
    internal Tally Counting(SimEvent happened, AgentId self) => happened switch
    {
        CutterEntered entered when entered.Behind == self => this with { CuttersLetIn = CuttersLetIn + 1 },
        CutterEntered => this,
        ZoneChanged { To: Zone.Bump } => this with { Bumps = Bumps + 1 },
        GraceExpired => this with { GapsLeftOpen = GapsLeftOpen + 1 },
        SpotLost => this with { PlacesLost = PlacesLost + 1 },
        CutInMissed => this with { MissedCutIns = MissedCutIns + 1 },
        CutIn => this with { CutIns = CutIns + 1 },
        _ => this,
    };
}
