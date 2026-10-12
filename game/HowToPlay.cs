using System.Numerics;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>The controls and the goal, shown at the start of a level until the player walks off their spot or a few seconds pass.</summary>
internal static class HowToPlay
{
    public const string Text = "WASD TO WALK, MOUSE TO LOOK, HOLD Q TO GLANCE BACK\n"
        + "KEEP UP WITH THE LINE. DON'T LEAVE GAPS, AND DON'T BUMP INTO PEOPLE.";

    /// <summary>How long it shows while the player stands still.</summary>
    public const int ShownTicks = 8 * TickRate.PerSecond;

    /// <summary>How far from their spot the player walks before it hides.</summary>
    private const float WalkedMeters = 0.3f;

    public static bool Shown(Snapshot snapshot, Level level)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(level);
        var player = snapshot.Agents[level.Player.Value];
        return snapshot.Tick < ShownTicks
            && Vector2.Distance(player.Position, level.Agents[level.Player.Value].Position) < WalkedMeters;
    }
}
