using System.Numerics;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>The controls and the goal, shown at the start of a level until the player walks off their spot or a few seconds pass.</summary>
internal static class HowToPlay
{
    public const string Text = "WASD TO WALK, MOUSE TO LOOK, HOLD Q TO GLANCE BACK\n"
        + "KEEP UP WITH THE LINE. DON'T LEAVE GAPS, AND DON'T BUMP INTO PEOPLE.";

    public const string CrowdText = "WASD TO WALK, MOUSE TO LOOK, HOLD F TO WAVE, E: \"I WAS HERE FIRST\"\n"
        + "GET TO THE COUNTER AND CATCH THE CLERK'S EYE. REMEMBER WHO WAS HERE BEFORE YOU.";

    /// <summary>How long it shows while the player stands still.</summary>
    public const int ShownTicks = 8 * TickRate.PerSecond;

    /// <summary>How far from their spot the player walks before it hides.</summary>
    private const float WalkedMeters = 0.3f;

    public static bool Shown(Snapshot snapshot, Level level)
    {
        ArgumentNullException.ThrowIfNull(level);
        return Shown(snapshot, level.Player, level.Agents[level.Player.Value].Position);
    }

    public static bool Shown(Snapshot snapshot, CrowdLevel level)
    {
        ArgumentNullException.ThrowIfNull(level);
        return Shown(snapshot, level.Player, level.Agents[level.Player.Value].Position);
    }

    private static bool Shown(Snapshot snapshot, AgentId player, Vector2 start)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot.Tick < ShownTicks && Vector2.Distance(snapshot.Agents[player.Value].Position, start) < WalkedMeters;
    }
}
