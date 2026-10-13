using System.Numerics;

namespace WaitingSim.Runner;

/// <summary>
/// Turns what a scripted player sees into its command each tick: close up on the person ahead, or at the front, step
/// up to the counter. Every bot but the distracted one fetches a dropped ticket and steps back into the lane, and
/// glances back to listen to small talk without missing a step.
/// </summary>
internal sealed class PlayerBrain(PlayerBot bot, Lane lane, Rng rng)
{
    /// <summary>The spacing a bot closes to, in meters; walking stops short of it by the braking distance.</summary>
    public const float WantedSpacing = 0.75f;

    private const int SlowReactionTicks = 90;
    private const int PhoneChancePerTick = 600;

    private int ticksSinceGap;
    private int phoneTicksLeft;

    public Command Next(AgentSnapshot self)
    {
        var move = Move(self);
        return bot != PlayerBot.Distracted && self.SmallTalkEndsAt is not null ? move with { GlancingBack = true } : move;
    }

    private Command Move(AgentSnapshot self)
    {
        var behind = self.Spacing is { } spacing ? spacing > WantedSpacing : self is { InLane: true, ReachedCounterAt: null };
        ticksSinceGap = behind ? ticksSinceGap + 1 : 0;
        if (bot == PlayerBot.Distracted)
        {
            if (phoneTicksLeft > 0)
            {
                phoneTicksLeft--;
                return Command.Idle;
            }

            if (rng.NextBelow(PhoneChancePerTick) == 0)
            {
                phoneTicksLeft = (3 * TickRate.PerSecond) + rng.NextBelow(5 * TickRate.PerSecond);
                return Command.Idle;
            }
        }

        if (bot != PlayerBot.Distracted && self.TicketAt is { } ticket)
        {
            return new(Vector2.Normalize(ticket - self.Position));
        }

        if (!self.InLane && self.ReachedCounterAt is null)
        {
            var back = lane.PointAt(lane.Measure(self.Position).Along) - self.Position;
            return back == Vector2.Zero ? Command.Idle : new(Vector2.Normalize(back));
        }

        var walking = bot == PlayerBot.Slow ? ticksSinceGap > SlowReactionTicks : behind;
        return walking ? new(lane.TowardFront(self.Position)) : Command.Idle;
    }
}
