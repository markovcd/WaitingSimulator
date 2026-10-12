using WaitingSim.Runner;
using static WaitingSim.Tests.SimTests;

namespace WaitingSim.Tests;

public sealed class PlayerBrainTests
{
    private static readonly Tuning Dropping = Snappy with
    {
        StepOut = new StepOutTuning(300, 10, 20, 35, 15),
        Distractions = new DistractionTuning(new DroppedTicketTuning(new(60, 60), 0.4f)),
    };

    [Theory]
    [InlineData("Attentive")]
    [InlineData("Slow")]
    public void A_bot_fetches_its_dropped_ticket_and_steps_back_into_the_lane(string bot)
    {
        var (sim, player, events) = Play(Enum.Parse<PlayerBot>(bot));

        Assert.Contains(events, e => e is TicketPickedUp picked && picked.Agent == player);
        Assert.True(Agent(sim, player).InLane, "The bot never stepped back into the lane.");
    }

    [Fact]
    public void A_distracted_bot_leaves_its_dropped_ticket_on_the_floor()
    {
        var (sim, player, events) = Play(PlayerBot.Distracted);

        Assert.DoesNotContain(events, e => e is TicketPickedUp);
        Assert.NotNull(Agent(sim, player).TicketAt);
    }

    /// <summary>The bot standing in a still line for two seconds while its ticket slips after one.</summary>
    private static (Sim Sim, AgentId Player, List<SimEvent> Events) Play(PlayerBot bot)
    {
        var sim = new Sim(Dropping, Straight, seed: 1);
        sim.Spawn(new(2, 0));
        var player = sim.Spawn(new(2.75f, 0));
        sim.HandTicket(player);
        var brain = new PlayerBrain(bot, Straight, new Rng(1, stream: 2));
        var events = Run(sim, 2 * TickRate.PerSecond, snapshot => Commands((player, brain.Next(snapshot.Agents[player.Value]).Move)));
        return (sim, player, events);
    }
}
