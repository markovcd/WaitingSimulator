using System.Numerics;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// Steps the sim at its tick rate from the client's frame times, and keeps the last two snapshots so a frame can show
/// the agents part way between ticks. The player's command holds from one frame to the next; the line moves everyone else.
/// </summary>
internal sealed class Driver
{
    /// <summary>The most ticks one frame steps. A longer frame drops the rest, so a hitch slows the game rather than stalling it.</summary>
    public const int MaxTicksPerFrame = 6;

    private readonly Sim sim;
    private readonly AgentId player;
    private readonly Dictionary<AgentId, Command> commands = [];
    private double owed;

    public Driver(Sim sim, AgentId player)
    {
        ArgumentNullException.ThrowIfNull(sim);
        this.sim = sim;
        this.player = player;
        Previous = Current = sim.Snapshot();
    }

    /// <summary>The state one tick before <see cref="Current"/>.</summary>
    public Snapshot Previous { get; private set; }

    /// <summary>The state after the last tick stepped.</summary>
    public Snapshot Current { get; private set; }

    /// <summary>How far the frame has come from <see cref="Previous"/> toward <see cref="Current"/>, from 0 up to 1.</summary>
    public float Blend => (float)owed;

    /// <summary>The moment this frame shows, in ticks: <see cref="Previous"/>'s tick and the blend toward the next.</summary>
    public float ShownTick => Previous.Tick + Blend;

    /// <summary>
    /// Steps as many ticks as <paramref name="seconds"/> of frame time owe, with the player on <paramref name="command"/>,
    /// and returns what happened in them. A frame time that is not a positive number steps nothing.
    /// </summary>
    public IReadOnlyList<SimEvent> Advance(double seconds, Command command)
    {
        commands[player] = command;
        if (double.IsFinite(seconds) && seconds > 0)
        {
            owed += seconds * TickRate.PerSecond;
        }

        var ticks = (int)Math.Min(Math.Floor(owed), MaxTicksPerFrame);
        owed -= Math.Floor(owed);
        var events = new List<SimEvent>();
        for (var i = 0; i < ticks; i++)
        {
            events.AddRange(sim.Step(commands));
            Previous = Current;
            Current = sim.Snapshot();
        }

        return events;
    }

    /// <summary>
    /// Where <paramref name="agent"/> shows this frame: between its last two positions by <see cref="Blend"/>, or where it
    /// is for a newcomer that has only one.
    /// </summary>
    public Vector2 Position(AgentId agent)
    {
        var current = Current.Agents[agent.Value].Position;
        return agent.Value < Previous.Agents.Count ? Vector2.Lerp(Previous.Agents[agent.Value].Position, current, Blend) : current;
    }
}
