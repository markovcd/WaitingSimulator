using System.Globalization;
using System.Text;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The debug overlay's text in a crowd: the tick and seed, whom the clerk asks and serves, then a row per agent with
/// when it arrived, where the crowd has it, patience and stage. The smoke run logs it too.
/// </summary>
internal static class CrowdOverlay
{
    /// <summary>Room for an id up to 999 and the longest trait name, so the columns after it line up.</summary>
    private const int IdAndWhoWidth = 24;

    public static string Text(Snapshot snapshot, CrowdLevel level)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(level);
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"TICK {snapshot.Tick}  SEED {level.Seed}  ASKING {Id(snapshot.Clerk?.Picked)}  SERVING {Id(snapshot.Clerk?.Serving)}");
        foreach (var agent in snapshot.Agents)
        {
            var who = agent.Id == level.Player ? "player" : agent.Trait ?? "-";
            text.Append('\n')
                .Append(string.Create(CultureInfo.InvariantCulture, $"#{agent.Id.Value} {who}").PadRight(IdAndWhoWidth))
                .Append(CultureInfo.InvariantCulture, $"  arrived {(float)agent.ArrivedAt / TickRate.PerSecond,7:0.00} s  step {agent.Step.ToString().ToLowerInvariant(),-12}");
            if (agent is { Patience: { } patience, Stage: { } stage })
            {
                text.Append(CultureInfo.InvariantCulture, $"  patience {patience,5:0.0}  stage {stage.ToString().ToLowerInvariant()}");
            }
        }

        return text.ToString();
    }

    private static string Id(AgentId? agent) => agent is { } id ? string.Create(CultureInfo.InvariantCulture, $"#{id.Value}") : "-";
}
