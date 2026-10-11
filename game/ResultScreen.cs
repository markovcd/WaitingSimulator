using System.Globalization;
using System.Text;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The screen shown once the level ends, at the counter or kicked out of the line: the clerk's line or the push past,
/// the time in line and every mistake counted.
/// </summary>
internal static class ResultScreen
{
    public static string Text(AgentSnapshot player)
    {
        ArgumentNullException.ThrowIfNull(player);
        var kicked = player.KickedOutAt is { } kickedAt && !(player.ReachedCounterAt < kickedAt);
        if ((kicked ? player.KickedOutAt : player.ReachedCounterAt) is not { } ended)
        {
            throw new ArgumentException("The player has neither reached the counter nor been kicked out.", nameof(player));
        }

        var tally = player.Tally;
        var seconds = ended / TickRate.PerSecond;
        var text = new StringBuilder();
        text.Append(kicked
            ? "\"EXCUSE ME.\" THE PERSON BEHIND PUSHES PAST YOU.\n\nKICKED OUT\n"
            : string.Create(CultureInfo.InvariantCulture, $"CLERK: \"{ClerkLines.For(tally.Worst)}\"\n\nSERVED\n"));
        text.Append(CultureInfo.InvariantCulture, $"{"TIME IN LINE",-24}{seconds / 60:00}:{seconds % 60:00}\n");
        Row(text, "BUMPS", tally.Bumps);
        Row(text, "GAPS LEFT OPEN", tally.GapsLeftOpen);
        Row(text, "PLACES LOST", tally.PlacesLost);
        Row(text, "CUTTERS LET IN", tally.CuttersLetIn);
        Row(text, "TIMES FAILED TO CUT IN", tally.MissedCutIns);
        Row(text, "TIMES CUT IN", tally.CutIns);
        text.Append("\nPRESS R OR START TO LINE UP AGAIN");
        return text.ToString();
    }

    private static void Row(StringBuilder text, string label, int count) =>
        text.Append(CultureInfo.InvariantCulture, $"{label,-24}{count,5}\n");
}
