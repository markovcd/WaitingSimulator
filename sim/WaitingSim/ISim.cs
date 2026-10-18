using System.Diagnostics.CodeAnalysis;

namespace WaitingSim;

/// <summary>A level being played, stepped one tick at a time by its agents' commands: a line's <see cref="Sim"/> or a <see cref="Crowd"/>.</summary>
public interface ISim
{
    /// <summary>How many ticks have been stepped.</summary>
    public long Tick { get; }

    /// <summary>Advances one tick, each agent acting on its command in <paramref name="commands"/>, and returns what happened.</summary>
    [SuppressMessage("Naming", "CA1716", Justification = "Step is the sim's word for advancing a tick, and nothing calls the sim from Visual Basic.")]
    public IReadOnlyList<SimEvent> Step(IReadOnlyDictionary<AgentId, Command> commands);

    /// <summary>The state as of the last tick.</summary>
    public Snapshot Snapshot();

    /// <summary>A hash of everything the next tick depends on: two with the same hash play on identically.</summary>
    public ulong StateHash();
}
