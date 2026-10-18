using System.Globalization;
using System.Text;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The screen shown once the level ends, at the counter, turned away there without a ticket or kicked out of the line:
/// the clerk's line or the push past, the time in line and every mistake counted; in a crowd, served or moved out.
/// </summary>
internal static class ResultScreen
{
    private const string AgainLine = "\nPRESS R OR START TO LINE UP AGAIN";

    public static string Text(AgentSnapshot player)
    {
        ArgumentNullException.ThrowIfNull(player);
        var kicked = player.KickedOutAt is { } kickedAt && !(player.ReachedCounterAt < kickedAt);
        var turned = !kicked && player.TurnedAwayAt is not null;
        if ((kicked ? player.KickedOutAt : turned ? player.TurnedAwayAt : player.ReachedCounterAt) is not { } ended)
        {
            throw new ArgumentException("The player has neither reached the counter nor been kicked out.", nameof(player));
        }

        var tally = player.Tally;
        var text = new StringBuilder();
        text.Append(kicked
            ? "\"EXCUSE ME.\" THE PERSON BEHIND PUSHES PAST YOU.\n\nKICKED OUT\n"
            : string.Create(CultureInfo.InvariantCulture, $"CLERK: \"{ClerkLines.For(tally.Worst)}\"\n\n{(turned ? "NO TICKET" : "SERVED")}\n"));
        Time(text, "TIME IN LINE", ended);
        Row(text, "BUMPS", tally.Bumps);
        Row(text, "GAPS LEFT OPEN", tally.GapsLeftOpen);
        Row(text, "PLACES LOST", tally.PlacesLost);
        Row(text, "CUTTERS LET IN", tally.CuttersLetIn);
        Row(text, "TIMES FAILED TO CUT IN", tally.MissedCutIns);
        Row(text, "TIMES CUT IN", tally.CutIns);
        text.Append(AgainLine);
        return text.ToString();
    }

    /// <summary>The same for a crowd: served, or moved out by staff someone called on the player, and the crowd's mistakes.</summary>
    public static string CrowdText(AgentSnapshot player)
    {
        ArgumentNullException.ThrowIfNull(player);
        var movedOut = player.ReachedCounterAt is null;
        if ((player.ReachedCounterAt ?? player.KickedOutAt) is not { } ended)
        {
            throw new ArgumentException("The player has neither been served nor moved out.", nameof(player));
        }

        var tally = player.Tally;
        var text = new StringBuilder();
        text.Append(movedOut
            ? "STAFF: \"I'M GOING TO HAVE TO ASK YOU TO LEAVE.\"\n\nMOVED OUT\n"
            : string.Create(CultureInfo.InvariantCulture, $"CLERK: \"{ClerkLines.For(tally.Worst)}\"\n\nSERVED\n"));
        Time(text, "TIME IN THE CROWD", ended);
        Row(text, "SERVED AHEAD OF SOMEONE", tally.ServedAhead);
        Row(text, "FALSE \"I WAS HERE FIRST\"", tally.FalseClaims);
        text.Append(AgainLine);
        return text.ToString();
    }

    private static void Time(StringBuilder text, string label, long ticks)
    {
        var seconds = ticks / TickRate.PerSecond;
        text.Append(CultureInfo.InvariantCulture, $"{label,-24}{seconds / 60:00}:{seconds % 60:00}\n");
    }

    private static void Row(StringBuilder text, string label, int count) =>
        text.Append(CultureInfo.InvariantCulture, $"{label,-24}{count,5}\n");
}
