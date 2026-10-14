using System.Numerics;
using static WaitingSim.Tests.SimTests;

namespace WaitingSim.Tests;

public sealed class StaringTests
{
    private const int AfterTicks = 60;
    private const float DrainPerSecond = 12f;

    private static readonly Trait Plain = new("plain", 1, 1, 1, new HashSet<Stage>(Enum.GetValues<Stage>()));

    private static readonly Tuning Watched = Snappy with { Staring = new StaringTuning(AfterTicks, DrainPerSecond) };

    [Fact]
    public void A_glance_back_shorter_than_the_grace_costs_nothing()
    {
        var (sim, player, behind) = Line(Watched, Plain);

        var events = Run(sim, AfterTicks - 1, _ => Glancing(player, true));
        Run(sim, AfterTicks, _ => Glancing(player, false));

        Assert.Equal(PatienceTuning.Full, Agent(sim, behind).Patience);
        Assert.DoesNotContain(events, e => e is StaredAt);
    }

    [Fact]
    public void Staring_back_past_the_grace_drains_the_person_behind()
    {
        var (sim, player, behind) = Line(Watched, Plain);

        var events = Run(sim, AfterTicks + TickRate.PerSecond, _ => Glancing(player, true));

        var stared = Assert.Single(events.OfType<StaredAt>());
        Assert.Equal((AfterTicks, behind, player), (stared.Tick, stared.Agent, stared.By));
        Assert.Equal(PatienceTuning.Full - (DrainPerSecond * (TickRate.PerSecond + 1) / TickRate.PerSecond), Agent(sim, behind).Patience!.Value, 0.001f);
        Assert.True(Agent(sim, player).Staring);
    }

    [Fact]
    public void A_trait_scales_how_fast_staring_drains()
    {
        var (sim, player, behind) = Line(Watched, Plain with { DrainScale = 2 });

        Run(sim, AfterTicks + TickRate.PerSecond, _ => Glancing(player, true));

        Assert.Equal(PatienceTuning.Full - (2 * DrainPerSecond * (TickRate.PerSecond + 1) / TickRate.PerSecond), Agent(sim, behind).Patience!.Value, 0.001f);
    }

    [Fact]
    public void Looking_away_ends_the_stare_and_the_next_one_waits_out_the_grace_again()
    {
        var (sim, player, _) = Line(Watched, Plain);
        Run(sim, AfterTicks + 10, _ => Glancing(player, true));
        Run(sim, 1, _ => Glancing(player, false));

        var events = Run(sim, AfterTicks - 1, _ => Glancing(player, true));

        Assert.False(Agent(sim, player).Staring);
        Assert.DoesNotContain(events, e => e is StaredAt);
    }

    [Fact]
    public void Glancing_back_while_the_person_behind_makes_small_talk_is_listening_not_staring()
    {
        var chatty = Watched with
        {
            Distractions = new DistractionTuning(SmallTalk: new SmallTalkTuning(new(1, 1), new(300, 300), 0.5f, 5, 15)),
        };
        var (sim, player, behind) = Line(chatty, Plain);
        sim.TalkTo(player);

        var events = Run(sim, 250, _ => Glancing(player, true));

        Assert.Contains(events, e => e is SmallTalkStarted);
        Assert.DoesNotContain(events, e => e is StaredAt);
        Assert.Equal(PatienceTuning.Full, Agent(sim, behind).Patience);
    }

    [Fact]
    public void Without_a_staring_tuning_glancing_back_costs_nothing()
    {
        var (sim, player, behind) = Line(Snappy, Plain);

        var events = Run(sim, 10 * AfterTicks, _ => Glancing(player, true));

        Assert.DoesNotContain(events, e => e is StaredAt);
        Assert.Equal(PatienceTuning.Full, Agent(sim, behind).Patience);
    }

    /// <summary>A still line, everyone standing loose so nothing else drains or recovers patience: someone ahead, the player, and the person behind.</summary>
    private static (Sim Sim, AgentId Player, AgentId Behind) Line(Tuning tuning, Trait trait)
    {
        var sim = new Sim(tuning, Straight, seed: 1);
        sim.Spawn(new(2, 0));
        var player = sim.Spawn(new(3.2f, 0));
        var behind = sim.Spawn(new(4.4f, 0), trait);
        return (sim, player, behind);
    }

    private static Dictionary<AgentId, Command> Glancing(AgentId player, bool glancingBack) =>
        new() { [player] = new Command(Vector2.Zero, glancingBack) };
}
