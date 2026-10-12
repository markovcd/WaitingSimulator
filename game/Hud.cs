using System.Globalization;
using System.Text;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The player's HUD text, which the difficulty decides: nothing on hard, a word when a gap opens in front of the player on
/// normal, and on easy the patience of the person behind and the grace left on a gap. It reads the snapshot only.
/// </summary>
internal static class Hud
{
    public static string Text(Snapshot snapshot, AgentId player, Tuning tuning, Difficulty difficulty)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(tuning);
        var you = snapshot.Agents[player.Value];
        var gapOpenedAt = you.Zone == Zone.Gap ? you.GapOpenedAt : null;
        switch (difficulty)
        {
            case Difficulty.Normal:
                return gapOpenedAt is null ? "" : "GAP";
            case Difficulty.Easy:
                var text = new StringBuilder();
                if (snapshot.Agents.FirstOrDefault(agent => agent.Ahead == player) is { Patience: { } patience })
                {
                    text.Append(CultureInfo.InvariantCulture, $"PATIENCE OF PERSON BEHIND: {patience:0}%\n");
                }

                if (gapOpenedAt is { } opened)
                {
                    var left = tuning.GraceWindowTicks - (snapshot.Tick - opened);
                    text.Append(left > 0
                        ? string.Create(CultureInfo.InvariantCulture, $"GAP: {(float)left / TickRate.PerSecond:0.00} S OF GRACE LEFT")
                        : "GAP: GRACE RAN OUT");
                }

                return text.ToString().TrimEnd('\n');
            default:
                return "";
        }
    }
}
