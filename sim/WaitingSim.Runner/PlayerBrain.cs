namespace WaitingSim.Runner;

/// <summary>Turns what a scripted player sees into its command each tick.</summary>
internal sealed class PlayerBrain(PlayerBot bot, Rng rng)
{
    /// <summary>The spacing a bot closes to, in meters; walking stops short of it by the braking distance.</summary>
    public const float WantedSpacing = 0.75f;

    private const int SlowReactionTicks = 90;
    private const int PhoneChancePerTick = 600;

    private int ticksSinceGap;
    private int phoneTicksLeft;

    public Command Next(AgentSnapshot self)
    {
        var behind = self.Spacing > WantedSpacing;
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

        var walking = bot == PlayerBot.Slow ? ticksSinceGap > SlowReactionTicks : behind;
        return walking ? new(SweepLine.TowardFront) : Command.Idle;
    }
}
