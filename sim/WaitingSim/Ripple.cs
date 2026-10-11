using System.Numerics;

namespace WaitingSim;

/// <summary>
/// How the line moves an agent nobody commands. It notices the person ahead move off and steps up after a reaction
/// delay; at the front it walks up to the counter, is served, and leaves the lane to the side.
/// </summary>
internal sealed class Ripple(RippleTuning ripple, Lane lane, float walkAcceleration)
{
    /// <summary>The agent's command this tick, moving it on through its <see cref="LineStep"/> as it goes.</summary>
    public Command Next(Agent agent, long tick)
    {
        // How far back the agent is from where it wants to stand: its spacing behind the person ahead, or the counter.
        var toGo = agent.Spacing is { } spacing ? spacing - agent.PreferredSpacing : agent.Lane.Along;
        switch (agent.Step)
        {
            case LineStep.Standing when agent is { InLane: true, Refused: false } && toGo > ripple.NoticeMeters:
                Begin(agent, LineStep.Noticed, ripple.ReactionTicks, tick);
                break;
            case LineStep.Standing when agent is { InLane: true, Refused: false, Ahead: null }:
                Begin(agent, LineStep.BeingServed, ripple.ServiceTicks, tick);
                break;
            case LineStep.Noticed when tick >= agent.StepEndsAt:
                agent.Step = agent is { InLane: true, Refused: false } && toGo > 0f ? LineStep.SteppingUp : LineStep.Standing;
                break;
            case LineStep.BeingServed when tick >= agent.StepEndsAt:
                agent.Step = LineStep.Leaving;
                break;
            case LineStep.Leaving when agent.Lane.Off >= lane.HalfWidth + ripple.LeaveMeters:
                agent.Step = LineStep.Done;
                break;
        }

        // Stops walking once what is left is what it takes to slow down.
        var braking = agent.Velocity.LengthSquared() / (2f * walkAcceleration);
        if (agent.Step == LineStep.SteppingUp && (agent is not { InLane: true, Refused: false } || toGo <= braking))
        {
            agent.Step = LineStep.Standing;
        }

        return agent.Step switch
        {
            LineStep.SteppingUp => new(lane.TowardFront(agent.Position)),
            LineStep.Leaving => new(Aside(lane.TowardFront(agent.Position))),
            _ => Command.Idle,
        };
    }

    /// <summary>A quarter turn from <paramref name="direction"/>: the way a served agent leaves the lane.</summary>
    private static Vector2 Aside(Vector2 direction) => new(-direction.Y, direction.X);

    /// <summary>Puts the agent at <paramref name="step"/> until a duration drawn from <paramref name="ticks"/> has passed.</summary>
    private static void Begin(Agent agent, LineStep step, TickRange ticks, long tick)
    {
        agent.Step = step;
        agent.StepEndsAt = tick + ticks.Draw(agent.Rng);
    }
}
