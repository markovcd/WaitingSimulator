namespace WaitingSim.Runner;

/// <summary>How a scripted player stands in line.</summary>
internal enum PlayerBot
{
    /// <summary>Closes every gap the moment it opens.</summary>
    Attentive,

    /// <summary>Closes every gap, a second and a half late.</summary>
    Slow,

    /// <summary>Attentive, except while looking at the phone for a few seconds at random; blind to a dropped ticket, deaf to small talk.</summary>
    Distracted,
}
