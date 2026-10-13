using System.Numerics;
using static WaitingSim.Tests.SimTests;

namespace WaitingSim.Tests;

public sealed class SmallTalkTests
{
    private const int DueTicks = 60;
    private const int TalkTicks = 120;
    private const float ListenedRecovery = 10f;
    private const float IgnoredHit = 20f;

    private static readonly Trait Plain = new("plain", 1, 1, 1, new HashSet<Stage>(Enum.GetValues<Stage>()));

    private static readonly Tuning Chatty = Snappy with
    {
        Distractions = new DistractionTuning(SmallTalk: new SmallTalkTuning(new(DueTicks, DueTicks), new(TalkTicks, TalkTicks), 0.5f, ListenedRecovery, IgnoredHit)),
    };

    [Fact]
    public void The_person_behind_starts_talking_when_small_talk_is_due()
    {
        var (sim, player, behind) = Line(Chatty);

        var events = Run(sim, DueTicks, _ => Facing(player, glancingBack: false));

        var started = Assert.Single(events.OfType<SmallTalkStarted>());
        Assert.Equal((DueTicks, behind, player), (started.Tick, started.Agent, started.Listener));
        Assert.Equal(DueTicks + TalkTicks, Agent(sim, player).SmallTalkEndsAt);
    }

    [Fact]
    public void Ignoring_the_talk_costs_the_talker_patience()
    {
        var (sim, player, behind) = Line(Chatty);

        var events = Run(sim, DueTicks + TalkTicks, _ => Facing(player, glancingBack: false));

        var ended = Assert.Single(events.OfType<SmallTalkEnded>());
        Assert.False(ended.Listened);
        Assert.Equal(PatienceTuning.Full - IgnoredHit, Agent(sim, behind).Patience!.Value, 0.001f);
        Assert.Null(Agent(sim, player).SmallTalkEndsAt);
    }

    [Fact]
    public void Glancing_back_through_the_talk_is_listening_and_the_talker_recovers()
    {
        var (sim, player, behind) = Line(Chatty);
        Run(sim, DueTicks + TalkTicks, _ => Facing(player, glancingBack: false));

        var events = Run(sim, DueTicks + TalkTicks, _ => Facing(player, glancingBack: true));

        Assert.True(Assert.Single(events.OfType<SmallTalkEnded>()).Listened);
        Assert.Equal(PatienceTuning.Full - IgnoredHit + ListenedRecovery, Agent(sim, behind).Patience!.Value, 0.001f);
    }

    [Fact]
    public void Glancing_back_for_less_than_the_listening_share_is_ignoring()
    {
        var (sim, player, _) = Line(Chatty);

        var events = Run(sim, DueTicks + TalkTicks, snapshot => Facing(player, glancingBack: snapshot.Tick < DueTicks + (TalkTicks / 2) - 1));

        Assert.False(Assert.Single(events.OfType<SmallTalkEnded>()).Listened);
    }

    [Fact]
    public void An_annoyed_person_behind_makes_no_small_talk()
    {
        var harsh = Chatty with { Distractions = new DistractionTuning(SmallTalk: Chatty.Distractions!.SmallTalk! with { IgnoredHit = 60 }) };
        var (sim, player, behind) = Line(harsh);
        Run(sim, DueTicks + TalkTicks, _ => Facing(player, glancingBack: false));

        var events = Run(sim, 10 * DueTicks, _ => Facing(player, glancingBack: false));

        Assert.True(Agent(sim, behind).Stage >= Stage.Annoyed, "The ignored talk did not annoy the person behind.");
        Assert.DoesNotContain(events, e => e is SmallTalkStarted);
    }

    [Fact]
    public void A_person_behind_without_patience_makes_no_small_talk()
    {
        var sim = new Sim(Chatty, Straight, seed: 1);
        sim.Spawn(new(2, 0));
        var player = sim.Spawn(new(3.2f, 0));
        sim.Spawn(new(4.4f, 0));
        sim.TalkTo(player);

        var events = Run(sim, 3 * DueTicks, _ => Facing(player, glancingBack: false));

        Assert.DoesNotContain(events, e => e is SmallTalkStarted);
    }

    [Fact]
    public void When_small_talk_starts_follows_the_seed()
    {
        var tuning = Chatty with { Distractions = new DistractionTuning(SmallTalk: Chatty.Distractions!.SmallTalk! with { IntervalTicks = new(60, 600) }) };

        var ticks = Enumerable.Range(1, 5).Select(seed => FirstTalk(tuning, (ulong)seed)).ToList();

        Assert.Equal(FirstTalk(tuning, 3), FirstTalk(tuning, 3));
        Assert.All(ticks, tick => Assert.InRange(tick, 60, 600));
        Assert.True(ticks.Distinct().Count() > 1, "Every seed started small talk at the same tick.");
    }

    [Fact]
    public void Without_a_small_talk_tuning_nobody_talks()
    {
        var (sim, player, _) = Line(Snappy);

        var events = Run(sim, 10 * DueTicks, _ => Facing(player, glancingBack: false));

        Assert.DoesNotContain(events, e => e is SmallTalkStarted);
    }

    [Fact]
    public void Glancing_back_survives_a_command_being_validated()
    {
        var (sim, player, _) = Line(Chatty);
        Run(sim, DueTicks, _ => Facing(player, glancingBack: false));

        var events = Run(sim, TalkTicks, _ => new() { [player] = new Command(new Vector2(float.NaN, 5), GlancingBack: true) });

        Assert.True(Assert.Single(events.OfType<SmallTalkEnded>()).Listened);
    }

    /// <summary>A still line, everyone standing loose so nothing drains or recovers patience: someone ahead, the player, and a plain person behind.</summary>
    private static (Sim Sim, AgentId Player, AgentId Behind) Line(Tuning tuning, ulong seed = 1)
    {
        var sim = new Sim(tuning, Straight, seed);
        sim.Spawn(new(2, 0));
        var player = sim.Spawn(new(3.2f, 0));
        var behind = sim.Spawn(new(4.4f, 0), Plain);
        sim.TalkTo(player);
        return (sim, player, behind);
    }

    private static Dictionary<AgentId, Command> Facing(AgentId player, bool glancingBack) =>
        new() { [player] = new Command(Vector2.Zero, glancingBack) };

    private static long FirstTalk(Tuning tuning, ulong seed)
    {
        var (sim, player, _) = Line(tuning, seed);
        return Run(sim, 700, _ => Facing(player, glancingBack: false)).OfType<SmallTalkStarted>().First().Tick;
    }
}
