using System.Numerics;
using static WaitingSim.Tests.SimTests;

namespace WaitingSim.Tests;

public sealed class PatienceTests
{
    private static readonly HashSet<Stage> EveryStage = [.. Enum.GetValues<Stage>()];
    private static readonly Trait Plain = new("plain", 1, 1, 1, EveryStage);

    [Fact]
    public void The_person_behind_loses_patience_only_once_the_grace_window_expires()
    {
        var (sim, lead, player, behind) = Line(Plain);

        var events = Run(sim, 200, OpenGap(lead, player));

        var expired = events.OfType<GraceExpired>().Single(e => e.Agent == player).Tick;
        var ticksDraining = sim.Tick - expired + 1;
        Assert.Equal(PatienceTuning.Full - (6f * ticksDraining / TickRate.PerSecond), Agent(sim, behind).Patience!.Value, 0.001f);
    }

    [Fact]
    public void A_trait_scales_how_fast_patience_drains()
    {
        var (plainSim, plainLead, plainPlayer, plainBehind) = Line(Plain);
        var (quickSim, quickLead, quickPlayer, quickBehind) = Line(Plain with { DrainScale = 3 });

        Run(plainSim, 200, OpenGap(plainLead, plainPlayer));
        Run(quickSim, 200, OpenGap(quickLead, quickPlayer));

        var plainLoss = PatienceTuning.Full - Agent(plainSim, plainBehind).Patience!.Value;
        var quickLoss = PatienceTuning.Full - Agent(quickSim, quickBehind).Patience!.Value;
        Assert.Equal(plainLoss * 3, quickLoss, 0.001f);
    }

    [Fact]
    public void Patience_recovers_while_the_person_ahead_stays_tight()
    {
        var (sim, lead, player, behind) = Line(Plain);
        Run(sim, 200, OpenGap(lead, player));
        var drained = Agent(sim, behind).Patience!.Value;

        Run(sim, 120, snapshot => Commands((player, snapshot.Agents[player.Value].Spacing > 0.6f ? TowardFront : Vector2.Zero), (behind, snapshot.Agents[behind.Value].Spacing > 0.6f ? TowardFront : Vector2.Zero)));

        Assert.Equal(Zone.Tight, Agent(sim, player).Zone);
        Assert.True(Agent(sim, behind).Patience > drained, $"patience stayed at {Agent(sim, behind).Patience} after the gap closed");
    }

    [Fact]
    public void Walking_into_the_person_ahead_costs_them_one_hit()
    {
        var (sim, lead, player, _) = Line(Plain);

        var events = Run(sim, 60, snapshot => Commands((player, snapshot.Agents[player.Value].Zone == Zone.Bump ? Vector2.Zero : TowardFront)));

        Assert.Single(events, e => e is ZoneChanged { To: Zone.Bump } bump && bump.Agent == player);
        Assert.Equal(PatienceTuning.Full - 5, Agent(sim, lead).Patience!.Value, 0.001f);
    }

    [Fact]
    public void A_trait_that_hides_its_cues_shows_one_stage_change_then_runs_out_once()
    {
        var (sim, lead, player, behind) = Line(Plain with { ShownStages = new HashSet<Stage> { Stage.Calm, Stage.FedUp } });

        var events = Run(sim, 60 * TickRate.PerSecond, OpenGap(lead, player));

        Assert.Equal(
            [new StageChanged(0, behind, Stage.Calm, Stage.FedUp)],
            events.OfType<StageChanged>().Select(e => e with { Tick = 0 }).ToList());
        Assert.Single(events, e => e is PatienceRanOut ranOut && ranOut.Agent == behind);
        Assert.Equal(0f, Agent(sim, behind).Patience);
    }

    [Fact]
    public void An_agent_without_a_trait_has_no_patience()
    {
        var (sim, _, player, _) = Line(Plain);

        Assert.Null(Agent(sim, player).Patience);
        Assert.Null(Agent(sim, player).Stage);
    }

    /// <summary>The lead walks off until a 2 m gap opens in front of the player, then waits.</summary>
    private static Func<Snapshot, Dictionary<AgentId, Command>> OpenGap(AgentId lead, AgentId player) =>
        snapshot => Commands((lead, snapshot.Agents[player.Value].Spacing < 2f ? TowardFront : Vector2.Zero));

    /// <summary>A lead and the person behind with <paramref name="trait"/>, and the player between them, tight.</summary>
    private static (Sim Sim, AgentId Lead, AgentId Player, AgentId Behind) Line(Trait trait)
    {
        var sim = new Sim(Snappy, Straight, seed: 1);
        return (sim, sim.Spawn(new(5, 0), trait), sim.Spawn(new(5.7f, 0)), sim.Spawn(new(6.4f, 0), trait));
    }
}
