using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>What the clerk says to someone stepping up to the counter: a flat "Next.", or a dig at their worst mistake.</summary>
internal static class ClerkLines
{
    public static string For(Mistake? worst) => worst switch
    {
        null => "Next.",
        Mistake.Bump => "Mind the person in front of you. Next.",
        Mistake.GapLeftOpen => "Were you waiting for an invitation? Next.",
        Mistake.LostPlace => "Back again? Next.",
        Mistake.LetCutterIn => "You were holding that spot for someone? Next.",
        Mistake.MissedCutIn => "I saw that. Next.",
        Mistake.CutIn => "The line starts at the back. Next.",
        Mistake.NoTicket => "No ticket, no service. Next.",
        Mistake.ServedAhead => "I think someone was here before you. Next.",
        Mistake.FalseClaim => "Were you, now. Next.",
        _ => throw new ArgumentOutOfRangeException(nameof(worst), worst, "A mistake the clerk has no line for."),
    };
}
