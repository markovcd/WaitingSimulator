namespace WaitingSim.Runner;

/// <summary>One trait against one bot, averaged over every swept line.</summary>
internal sealed record SweepRow(
    string Trait,
    string Bot,
    double MeanFinalPatience,
    double RanOutShare,
    double MeanGraceExpiries,
    IReadOnlyList<double> MeanPatienceCurve);
