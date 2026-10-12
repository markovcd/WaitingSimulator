using System.Numerics;

namespace WaitingSim;

/// <summary>
/// The ticket an agent holds: now and then it slips and lands just outside the lane beside the agent, and lies there
/// until the agent walks within reach of it. One is on the floor at a time, and it only slips while the agent stands in
/// the line. Reaching the counter without it is being turned away.
/// </summary>
internal sealed class Tickets(DroppedTicketTuning ticket, Lane lane, Agent holder)
{
    /// <summary>How far outside the lane's edge a dropped ticket lands, in meters.</summary>
    private const float BeyondMeters = 0.5f;

    public Agent Holder { get; } = holder;

    /// <summary>The tick the ticket next slips, while it is in hand.</summary>
    public long NextDropAt { get; private set; }

    /// <summary>Draws when the ticket next slips, from <paramref name="tick"/>.</summary>
    public void Schedule(long tick, Rng rng) => NextDropAt = tick + ticket.IntervalTicks.Draw(rng);

    /// <summary>Picks the ticket up if the holder walked within reach of it, or drops it if it is due to slip.</summary>
    public void Update(long tick, Rng rng, List<SimEvent> events)
    {
        if (Holder.TicketAt is { } lying)
        {
            if (Vector2.Distance(Holder.Position, lying) <= ticket.PickupMeters)
            {
                Holder.TicketAt = null;
                Schedule(tick, rng);
                events.Add(new TicketPickedUp(tick, Holder.Id));
            }

            return;
        }

        if (tick >= NextDropAt && Holder is { InLane: true, Refused: false, ReachedCounterAt: null, KickedOutAt: null, TurnedAwayAt: null })
        {
            var on = lane.PointAt(Holder.Lane.Along);
            var front = lane.TowardFront(on);
            var across = new Vector2(-front.Y, front.X);
            var side = Vector2.Dot(Holder.Position - on, across) < 0f ? -1f : 1f;
            Holder.TicketAt = on + (across * side * (lane.HalfWidth + BeyondMeters));
            events.Add(new TicketDropped(tick, Holder.Id, Holder.TicketAt.Value));
        }
    }
}
