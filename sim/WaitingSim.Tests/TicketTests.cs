using System.Numerics;
using static WaitingSim.Tests.SimTests;

namespace WaitingSim.Tests;

public sealed class TicketTests
{
    private const int DropTicks = 120;
    private const float PickupMeters = 0.4f;

    private static readonly Tuning Dropping = Snappy with
    {
        StepOut = new StepOutTuning(300, 10, 20, 35, 15),
        Distractions = new DistractionTuning(new DroppedTicketTuning(new(DropTicks, DropTicks), PickupMeters)),
    };

    [Fact]
    public void The_ticket_slips_and_lands_just_outside_the_lane_beside_the_player()
    {
        var (sim, player) = Holding(Dropping, at: new(3, 0));

        var events = Run(sim, DropTicks, _ => Commands((player, Vector2.Zero)));

        var dropped = Assert.Single(events.OfType<TicketDropped>());
        Assert.Equal(DropTicks, dropped.Tick);
        var ticket = Agent(sim, player).TicketAt!.Value;
        Assert.Equal(3f, ticket.X, 0.001f);
        Assert.True(Math.Abs(ticket.Y) > Straight.HalfWidth + PickupMeters, $"The ticket at {ticket} can be reached from inside the lane.");
    }

    [Fact]
    public void When_the_ticket_slips_follows_the_seed()
    {
        var tuning = Dropping with { Distractions = new DistractionTuning(new DroppedTicketTuning(new(60, 600), PickupMeters)) };

        var ticks = Enumerable.Range(1, 5).Select(seed => FirstDrop(tuning, (ulong)seed)).ToList();

        Assert.Equal(FirstDrop(tuning, 3), FirstDrop(tuning, 3));
        Assert.All(ticks, tick => Assert.InRange(tick, 60, 600));
        Assert.True(ticks.Distinct().Count() > 1, "Every seed dropped the ticket at the same tick.");
    }

    [Fact]
    public void Walking_within_reach_picks_the_ticket_up_and_only_one_lies_on_the_floor_at_a_time()
    {
        var (sim, player) = Holding(Dropping, at: new(3, 0));
        var drops = Run(sim, 3 * DropTicks, _ => Commands((player, Vector2.Zero))).OfType<TicketDropped>().Count();
        var side = MathF.Sign(Agent(sim, player).TicketAt!.Value.Y);

        var events = Run(sim, 120, snapshot => Commands((player, snapshot.Agents[player.Value].TicketAt is null ? Vector2.Zero : new Vector2(0, side))));

        Assert.Equal(1, drops);
        Assert.Single(events.OfType<TicketPickedUp>());
        Assert.Null(Agent(sim, player).TicketAt);
    }

    [Fact]
    public void Reaching_the_counter_without_the_ticket_turns_the_player_away()
    {
        var (sim, player) = Holding(Dropping, at: new(1.5f, 0));
        Run(sim, DropTicks, _ => Commands((player, Vector2.Zero)));

        var events = Run(sim, 120, _ => Commands((player, TowardFront)));

        Assert.Contains(events, e => e is TurnedAway turned && turned.Agent == player);
        Assert.DoesNotContain(events, e => e is ReachedCounter);
        Assert.NotNull(Agent(sim, player).TurnedAwayAt);
        Assert.Equal(Mistake.NoTicket, Agent(sim, player).Tally.Worst);
    }

    [Fact]
    public void Reaching_the_counter_with_the_ticket_is_served()
    {
        var (sim, player) = Holding(Dropping, at: new(1.5f, 0));

        var events = Run(sim, 60, _ => Commands((player, TowardFront)));

        Assert.Contains(events, e => e is ReachedCounter reached && reached.Agent == player);
        Assert.DoesNotContain(events, e => e is TurnedAway or TicketDropped);
    }

    [Fact]
    public void Without_a_dropped_ticket_tuning_the_ticket_never_slips()
    {
        var (sim, player) = Holding(Snappy, at: new(3, 0));

        var events = Run(sim, 10 * DropTicks, _ => Commands((player, Vector2.Zero)));

        Assert.DoesNotContain(events, e => e is TicketDropped);
    }

    private static (Sim Sim, AgentId Player) Holding(Tuning tuning, Vector2 at)
    {
        var sim = new Sim(tuning, Straight, seed: 1);
        var player = sim.Spawn(at);
        sim.HandTicket(player);
        return (sim, player);
    }

    private static long FirstDrop(Tuning tuning, ulong seed)
    {
        var sim = new Sim(tuning, Straight, seed);
        var player = sim.Spawn(new(3, 0));
        sim.HandTicket(player);
        return Run(sim, 700, _ => Commands((player, Vector2.Zero))).OfType<TicketDropped>().First().Tick;
    }
}
