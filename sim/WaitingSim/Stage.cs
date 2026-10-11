namespace WaitingSim;

/// <summary>A band of patience with its own visible behavior, from full patience down to none.</summary>
public enum Stage
{
    /// <summary>Neutral, idle shuffling.</summary>
    Calm,

    /// <summary>Sighs, checks the watch.</summary>
    Restless,

    /// <summary>Foot-tapping, muttering.</summary>
    Annoyed,

    /// <summary>"Excuse me?", leans into your space.</summary>
    Confronting,

    /// <summary>Shoulder tap, then pushes past or reports you.</summary>
    FedUp,
}
