using System.Globalization;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The player's HUD text in a crowd, which the difficulty decides: nothing on hard; on normal a word when the clerk asks
/// someone who arrived after the player; on easy how many still waiting arrived before them, the lowest patience within
/// sight, and the key to call out while the clerk asks someone who came later. It reads the snapshot only.
/// </summary>
internal static class CrowdHud
{
    public static string Text(Snapshot snapshot, AgentId player, CrowdTuning tuning, Difficulty difficulty)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(tuning);
        var you = snapshot.Agents[player.Value];
        var waiting = snapshot.Agents.Where(agent => agent.Id != player && agent is { Step: LineStep.Standing, ReachedCounterAt: null, KickedOutAt: null }).ToList();
        var asked = snapshot.Clerk?.Picked is { } picked && picked != player ? snapshot.Agents[picked.Value] : null;
        var askedLater = asked is not null && Before(you, asked);
        switch (difficulty)
        {
            case Difficulty.Normal:
                return askedLater ? "ASKED SOMEONE AFTER YOU" : "";
            case Difficulty.Easy:
                var before = waiting.Count(agent => Before(agent, you));
                var lowest = waiting.Where(agent => System.Numerics.Vector2.Distance(agent.Position, you.Position) <= tuning.SightMeters).Min(agent => agent.Patience);
                return string.Join('\n', new[]
                {
                    string.Create(CultureInfo.InvariantCulture, $"ARRIVED BEFORE YOU: {before}"),
                    lowest is { } shown ? string.Create(CultureInfo.InvariantCulture, $"LOWEST PATIENCE NEARBY: {shown:0}%") : null,
                    askedLater ? "CLERK ASKED SOMEONE WHO CAME AFTER YOU: E TO SAY YOU WERE FIRST" : null,
                }.OfType<string>());
            default:
                return "";
        }
    }

    /// <summary>Whether <paramref name="first"/> arrived before <paramref name="second"/>: the invisible line.</summary>
    private static bool Before(AgentSnapshot first, AgentSnapshot second) =>
        first.ArrivedAt < second.ArrivedAt || (first.ArrivedAt == second.ArrivedAt && first.Id.Value < second.Id.Value);
}
