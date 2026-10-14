using System.Globalization;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The player's HUD text, which the difficulty decides: nothing on hard, a word when a gap opens in front of the player,
/// their ticket drops or the person behind makes small talk on normal, and on easy the patience of the person behind, the
/// grace left on a gap, a dropped ticket, small talk with the key to listen, staring back, and the key to let the person
/// behind go ahead while the player is out of the lane with their place held. It reads the snapshot only.
/// </summary>
internal static class Hud
{
    public static string Text(Snapshot snapshot, AgentId player, Tuning tuning, Difficulty difficulty)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(tuning);
        var you = snapshot.Agents[player.Value];
        var gapOpenedAt = you.Zone == Zone.Gap ? you.GapOpenedAt : null;
        var dropped = you.TicketAt is not null;
        var talking = you.SmallTalkEndsAt is not null;
        switch (difficulty)
        {
            case Difficulty.Normal:
                return string.Join('\n', new[] { gapOpenedAt is null ? null : "GAP", dropped ? "TICKET" : null, talking ? "TALK" : null }.OfType<string>());
            case Difficulty.Easy:
                var patience = snapshot.Agents.FirstOrDefault(agent => agent.Ahead == player)?.Patience;
                var left = gapOpenedAt is { } opened ? tuning.GraceWindowTicks - (snapshot.Tick - opened) : (long?)null;
                return string.Join('\n', new[]
                {
                    patience is { } shown ? string.Create(CultureInfo.InvariantCulture, $"PATIENCE OF PERSON BEHIND: {shown:0}%") : null,
                    left switch
                    {
                        null => null,
                        > 0 => string.Create(CultureInfo.InvariantCulture, $"GAP: {(float)left / TickRate.PerSecond:0.00} S OF GRACE LEFT"),
                        _ => "GAP: GRACE RAN OUT",
                    },
                    dropped ? "TICKET DROPPED" : null,
                    talking ? "SMALL TALK: LISTEN (Q)" : null,
                    you.Staring ? "STARING AT PERSON BEHIND" : null,
                    you is { InLane: false, HeldAlong: not null } && snapshot.Agents.Any(agent => agent.Ahead == player) ? "E: LET THE PERSON BEHIND GO AHEAD" : null,
                }.OfType<string>());
            default:
                return "";
        }
    }
}
