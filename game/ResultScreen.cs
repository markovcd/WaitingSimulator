using System.Globalization;
using System.Text;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>The screen shown once the player reaches the counter: the clerk's line, the time it took and every mistake counted.</summary>
internal static class ResultScreen
{
    public static string Text(AgentSnapshot player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (player.ReachedCounterAt is not { } reached)
        {
            throw new ArgumentException("The player has not reached the counter.", nameof(player));
        }

        var tally = player.Tally;
        var seconds = reached / TickRate.PerSecond;
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"CLERK: \"{ClerkLines.For(tally.Worst)}\"\n\n");
        text.Append("SERVED\n");
        text.Append(CultureInfo.InvariantCulture, $"{"TIME IN LINE",-24}{seconds / 60:00}:{seconds % 60:00}\n");
        Row(text, "BUMPS", tally.Bumps);
        Row(text, "GAPS LEFT OPEN", tally.GapsLeftOpen);
        Row(text, "PLACES LOST", tally.PlacesLost);
        Row(text, "TIMES FAILED TO CUT IN", tally.MissedCutIns);
        Row(text, "TIMES CUT IN", tally.CutIns);
        text.Append("\nPRESS R OR START TO LINE UP AGAIN");
        return text.ToString();
    }

    private static void Row(StringBuilder text, string label, int count) =>
        text.Append(CultureInfo.InvariantCulture, $"{label,-24}{count,5}\n");
}
