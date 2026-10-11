using System.Globalization;
using System.Text;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The debug overlay's text: the tick and seed, then a row per agent with its zone, spacing, grace left, patience and
/// stage. The smoke run logs it too, so every value it shows can be read without a window.
/// </summary>
internal static class Overlay
{
    public static string Text(Snapshot snapshot, Level level, Tuning tuning)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(level);
        ArgumentNullException.ThrowIfNull(tuning);
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"TICK {snapshot.Tick}  SEED {level.Seed}");
        foreach (var agent in snapshot.Agents)
        {
            text.Append('\n').Append(Row(agent, snapshot.Tick, level, tuning).TrimEnd());
        }

        return text.ToString();
    }

    private static string Row(AgentSnapshot agent, long tick, Level level, Tuning tuning)
    {
        var row = new StringBuilder();
        var who = agent.Id == level.Player ? "player" : agent.Trait ?? "-";
        row.Append(CultureInfo.InvariantCulture, $"#{agent.Id.Value} {who,-18}");
        if (!agent.InLane)
        {
            row.Append("  out of the lane");
        }
        else
        {
            row.Append(CultureInfo.InvariantCulture, $"  zone {Name(agent.Zone),-5}  spacing {Meters(agent.Spacing),-6}  grace {Grace(agent, tick, tuning),-7}");
        }

        if (agent is { Patience: { } patience, Stage: { } stage })
        {
            row.Append(CultureInfo.InvariantCulture, $"  patience {patience,5:0.0}  stage {Name<Stage>(stage)}");
        }

        return row.ToString();
    }

    /// <summary>Seconds left of the grace window while a gap is open, "expired" once it ran out, "-" with no gap.</summary>
    private static string Grace(AgentSnapshot agent, long tick, Tuning tuning)
    {
        if (agent.GapOpenedAt is not { } opened)
        {
            return "-";
        }

        var left = tuning.GraceWindowTicks - (tick - opened);
        return left > 0 ? string.Create(CultureInfo.InvariantCulture, $"{(float)left / TickRate.PerSecond:0.00} s") : "expired";
    }

    private static string Meters(float? spacing) =>
        spacing is { } meters ? string.Create(CultureInfo.InvariantCulture, $"{meters:0.00} m") : "-";

    private static string Name<T>(T? value)
        where T : struct, Enum => value?.ToString().ToLowerInvariant() ?? "-";
}
