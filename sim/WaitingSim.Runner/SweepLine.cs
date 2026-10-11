using System.Numerics;

namespace WaitingSim.Runner;

/// <summary>
/// One line played for the sweep: people ahead, the scripted player, and the person behind with the trait under test.
/// The line moves everyone but the player.
/// </summary>
internal static class SweepLine
{
    public const int Seconds = 120;
    public const int SampleSeconds = 10;

    public static readonly Vector2 TowardFront = new(-1, 0);

    // Enough people ahead that the player never reaches the counter in a run.
    private const int PeopleAhead = 30;
    private const float StartSpacing = 0.8f;

    // A straight lane, front at the origin, long enough for the whole line.
    private static readonly Lane Lane = new([new(0, 0), new(40, 0)], halfWidth: 0.5f);

    /// <summary>
    /// Plays the line. Nobody ahead sees the player or the person behind, so the same seed moves the people ahead the
    /// same way whatever the trait and bot.
    /// </summary>
    public static LineResult Play(Tuning tuning, Trait trait, PlayerBot bot, ulong seed)
    {
        var sim = new Sim(tuning, Lane, seed);
        for (var i = 0; i < PeopleAhead; i++)
        {
            sim.Spawn(new(i * StartSpacing, 0));
        }

        var player = sim.Spawn(new(PeopleAhead * StartSpacing, 0));
        var behind = sim.Spawn(new((PeopleAhead + 1) * StartSpacing, 0), trait);
        var brain = new PlayerBrain(bot, new Rng(seed, stream: 2));

        var ranOut = false;
        var graceExpiries = 0;
        var curve = new List<float>();
        var commands = new Dictionary<AgentId, Command>();
        for (var tick = 1; tick <= Seconds * TickRate.PerSecond; tick++)
        {
            commands[player] = brain.Next(sim.Snapshot().Agents[player.Value]);
            foreach (var happened in sim.Step(commands))
            {
                ranOut |= happened is PatienceRanOut ranOutEvent && ranOutEvent.Agent == behind;
                graceExpiries += happened is GraceExpired expired && expired.Agent == player ? 1 : 0;
            }

            if (tick % (SampleSeconds * TickRate.PerSecond) == 0)
            {
                curve.Add(Patience(sim, behind));
            }
        }

        return new(Patience(sim, behind), ranOut, graceExpiries, curve);
    }

    private static float Patience(Sim sim, AgentId agent) => sim.Snapshot().Agents[agent.Value].Patience ?? 0f;
}
