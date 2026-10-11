using System.Numerics;
using static WaitingSim.Tests.SimTests;

namespace WaitingSim.Tests;

public sealed class BodiesTests
{
    private const float Radius = 0.18f;

    private static readonly Tuning Solid = Snappy with { WalkAcceleration = 3f, Bodies = new BodyTuning(Radius, Rebound: 0.5f) };

    [Fact]
    public void Walking_into_the_person_ahead_stops_at_their_body_and_leaves_them_standing()
    {
        var sim = new Sim(Solid, Straight, seed: 1);
        var ahead = sim.Spawn(new(2, 0));
        var player = sim.Spawn(new(2.8f, 0));

        var closest = Closest(sim, 180, player, ahead, TowardFront);

        Assert.True(closest >= 2 * Radius - 0.0001f, $"The player came within {closest} m of the person ahead.");
        Assert.Equal(new Vector2(2, 0), Agent(sim, ahead).Position);
    }

    [Fact]
    public void Walking_into_someone_from_the_side_stops_at_their_body()
    {
        var sim = new Sim(Solid, Straight, seed: 1);
        var cutter = sim.Spawn(new(4, 1.5f), cutter: true);
        var player = sim.Spawn(new(4, 0));

        var closest = Closest(sim, 180, player, cutter, new Vector2(0, 1));

        Assert.True(closest >= 2 * Radius - 0.0001f, $"The player came within {closest} m of the agent beside the line.");
    }

    [Fact]
    public void Walking_into_the_person_ahead_bounces_the_walker_back()
    {
        var sim = new Sim(Solid, Straight, seed: 1);
        sim.Spawn(new(2, 0));
        var player = sim.Spawn(new(2.8f, 0));

        var velocities = new List<float>();
        for (var tick = 0; tick < 180; tick++)
        {
            sim.Step(Commands((player, TowardFront)));
            velocities.Add(Agent(sim, player).Velocity.X);
        }

        Assert.Contains(velocities, velocity => velocity > 0f);
    }

    [Fact]
    public void Walking_into_the_person_ahead_is_still_a_bump()
    {
        var sim = new Sim(Solid, Straight, seed: 1);
        sim.Spawn(new(2, 0));
        var player = sim.Spawn(new(2.8f, 0));

        var events = Run(sim, 180, _ => Commands((player, TowardFront)));

        Assert.Contains(events, e => e is ZoneChanged { To: Zone.Bump } bump && bump.Agent == player);
    }

    [Fact]
    public void Two_agents_walking_into_each_other_alike_share_the_push()
    {
        var sim = new Sim(Solid, Straight, seed: 1);
        var first = sim.Spawn(new(4, 0));
        var second = sim.Spawn(new(5, 0));

        Run(sim, 180, _ => Commands((first, new Vector2(1, 0)), (second, new Vector2(-1, 0))));

        Assert.Equal(4.5f, (Agent(sim, first).Position.X + Agent(sim, second).Position.X) / 2, 0.0001f);
        Assert.True(Agent(sim, second).Position.X - Agent(sim, first).Position.X >= 2 * Radius - 0.0001f);
    }

    [Fact]
    public void Without_bodies_agents_walk_through_each_other()
    {
        var sim = new Sim(Solid with { Bodies = null }, Straight, seed: 1);
        sim.Spawn(new(2, 0));
        var player = sim.Spawn(new(2.8f, 0));

        Run(sim, 180, _ => Commands((player, TowardFront)));

        Assert.True(Agent(sim, player).Position.X < 2f);
    }

    /// <summary>The closest <paramref name="walker"/> comes to <paramref name="other"/> walking <paramref name="move"/> for some ticks.</summary>
    private static float Closest(Sim sim, int ticks, AgentId walker, AgentId other, Vector2 move)
    {
        var closest = float.MaxValue;
        for (var tick = 0; tick < ticks; tick++)
        {
            sim.Step(Commands((walker, move)));
            closest = Math.Min(closest, Vector2.Distance(Agent(sim, walker).Position, Agent(sim, other).Position));
        }

        return closest;
    }
}
