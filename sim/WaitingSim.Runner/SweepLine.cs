using System.Numerics;

namespace WaitingSim.Runner;

/// <summary>
/// One line played for the sweep: a lead stepping forward in bursts, the scripted player, and the person behind
/// with the trait under test, who follows the player.
/// </summary>
internal static class SweepLine
{
    public const int Seconds = 120;
    public const int SampleSeconds = 10;

    public static readonly Vector2 TowardFront = new(-1, 0);

    private const float FollowSpacing = 0.8f;

    // A straight lane, front at the origin, long enough that the lead never reaches the front in a run.
    private static readonly Lane Lane = new([new(0, 0), new(40, 0)], halfWidth: 0.5f);

    /// <summary>Plays the line. The same seed moves the lead the same way whatever the trait and bot.</summary>
    public static LineResult Play(Tuning tuning, Trait trait, PlayerBot bot, ulong seed)
    {
        var sim = new Sim(tuning, Lane, seed);
        var lead = sim.Spawn(new(30f, 0));
        var player = sim.Spawn(new(30.7f, 0));
        var behind = sim.Spawn(new(31.4f, 0), trait);
        var leadRng = new Rng(seed, stream: 1);
        var brain = new PlayerBrain(bot, new Rng(seed, stream: 2));

        var leadWait = LeadWait(leadRng);
        float? leadStopAt = null;
        var ranOut = false;
        var graceExpiries = 0;
        var curve = new List<float>();
        var commands = new Dictionary<AgentId, Command>();
        for (var tick = 1; tick <= Seconds * TickRate.PerSecond; tick++)
        {
            var snapshot = sim.Snapshot();
            var leadAlong = Lane.Measure(snapshot.Agents[lead.Value].Position).Along;
            if (leadStopAt is null && --leadWait <= 0)
            {
                leadStopAt = leadAlong - (0.6f + (leadRng.NextBelow(61) / 100f));
            }

            if (leadStopAt is { } stop && leadAlong <= stop)
            {
                leadStopAt = null;
                leadWait = LeadWait(leadRng);
            }

            commands[lead] = leadStopAt is null ? Command.Idle : new(TowardFront);
            commands[player] = brain.Next(snapshot.Agents[player.Value]);
            commands[behind] = snapshot.Agents[behind.Value].Spacing > FollowSpacing ? new(TowardFront) : Command.Idle;

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

    /// <summary>Two to six seconds of standing still.</summary>
    private static int LeadWait(Rng rng) => (2 * TickRate.PerSecond) + rng.NextBelow(4 * TickRate.PerSecond);

    private static float Patience(Sim sim, AgentId agent) => sim.Snapshot().Agents[agent.Value].Patience ?? 0f;
}
