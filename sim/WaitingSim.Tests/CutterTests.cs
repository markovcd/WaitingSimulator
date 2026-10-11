using System.Numerics;
using static WaitingSim.Tests.SimTests;

namespace WaitingSim.Tests;

public sealed class CutterTests
{
    private const int AfterGraceTicks = 60;
    private const float BlameHit = 40;

    private static readonly Tuning Cutting = Snappy with { Cutter = new CutterTuning(AfterGraceTicks, BlameHit) };

    // Patience that neither drains nor recovers, so only one-off hits move it.
    private static readonly Trait Still = new("still", DrainScale: 0, RecoveryScale: 0, BumpScale: 1, new HashSet<Stage>(Enum.GetValues<Stage>()));

    [Fact]
    public void A_cutter_slides_into_a_gap_left_open_past_grace_and_the_person_behind_blames_the_player()
    {
        var (sim, lead, player, behind, cutter) = Line(Cutting);

        var events = OpenGap(sim, lead, ticks: 400);

        var entered = Assert.Single(events.OfType<CutterEntered>());
        Assert.Equal((cutter, player), (entered.Agent, entered.Behind));
        Assert.Equal(cutter, Agent(sim, player).Ahead);
        Assert.Equal(lead, Agent(sim, cutter).Ahead);
        Assert.Equal(PatienceTuning.Full - BlameHit, Agent(sim, behind).Patience!.Value, 0.0001f);
        Assert.Equal(1, Agent(sim, player).Tally.CuttersLetIn);
        Assert.Equal(0, Agent(sim, cutter).Tally.CuttersLetIn);
    }

    [Fact]
    public void A_cutter_waits_until_the_gap_has_stayed_open_past_grace_for_its_delay()
    {
        var (sim, lead, player, _, cutter) = Line(Cutting);
        var cuttingFrom = -1L;

        var events = Run(sim, 400, snapshot =>
        {
            if (cuttingFrom < 0 && snapshot.Agents[cutter.Value].Step == LineStep.Cutting)
            {
                cuttingFrom = snapshot.Tick;
            }

            return Commands((lead, snapshot.Agents[lead.Value].Position.X > 3 ? TowardFront : Vector2.Zero));
        });

        var opened = events.OfType<ZoneChanged>().First(e => e.Agent == player && e.To == Zone.Gap).Tick;
        Assert.Equal(opened + GraceWindowTicks + AfterGraceTicks, cuttingFrom);
    }

    [Fact]
    public void A_cutter_gives_up_when_the_gap_closes_before_it_gets_there()
    {
        var (sim, lead, player, behind, cutter) = Line(Cutting, cutterAt: new(5, 3));
        var leadMoved = false;

        var events = Run(sim, 600, snapshot =>
        {
            var cutting = snapshot.Agents[cutter.Value].Step == LineStep.Cutting;
            leadMoved |= snapshot.Agents[lead.Value].Position.X <= 3;
            return Commands(
                (lead, leadMoved ? Vector2.Zero : TowardFront),
                (player, cutting && snapshot.Agents[player.Value].Spacing > 0.7f ? TowardFront : Vector2.Zero),
                (behind, snapshot.Agents[behind.Value].Spacing > 0.7f ? TowardFront : Vector2.Zero));
        });

        Assert.DoesNotContain(events, e => e is CutterEntered);
        Assert.Equal(LineStep.Standing, Agent(sim, cutter).Step);
        Assert.False(Agent(sim, cutter).InLane);
    }

    [Fact]
    public void Without_a_cutter_tuning_a_cutter_stands_where_it_is()
    {
        var (sim, lead, _, _, cutter) = Line(Snappy);

        var events = OpenGap(sim, lead, ticks: 400);

        Assert.DoesNotContain(events, e => e is CutterEntered);
        Assert.Equal(new Vector2(5, 1.5f), Agent(sim, cutter).Position);
    }

    private static (Sim Sim, AgentId Lead, AgentId Player, AgentId Behind, AgentId Cutter) Line(Tuning tuning, Vector2? cutterAt = null)
    {
        var sim = new Sim(tuning, Straight, seed: 1);
        var lead = sim.Spawn(new(5, 0));
        var player = sim.Spawn(new(5.7f, 0));
        var behind = sim.Spawn(new(6.4f, 0), Still);
        var cutter = sim.Spawn(cutterAt ?? new(5, 1.5f), cutter: true);
        return (sim, lead, player, behind, cutter);
    }

    /// <summary>The lead walks two meters off and the player stays put.</summary>
    private static List<SimEvent> OpenGap(Sim sim, AgentId lead, int ticks) =>
        Run(sim, ticks, snapshot => Commands((lead, snapshot.Agents[lead.Value].Position.X > 3 ? TowardFront : Vector2.Zero)));
}
