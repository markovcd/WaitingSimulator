using System.Numerics;
using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class DriverTests
{
    // Frame times that are exact in binary, so tick counts are exact: 1/16 s is 3.75 ticks, 1/128 s is 0.46875.
    private const double SixteenthOfASecond = 1d / 16;
    private const double HundredTwentyEighthOfASecond = 1d / 128;

    private static readonly Tuning Tuning = Tuning.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "tuning.json"))).Value!;

    private static readonly Command Forward = new(new Vector2(0, -1));

    [Fact]
    public void A_frame_steps_one_tick_for_each_sixtieth_of_a_second_it_took()
    {
        var (driver, _) = Line();

        for (var frame = 0; frame < 4; frame++)
        {
            driver.Advance(SixteenthOfASecond, Command.Idle);
        }

        Assert.Equal(15, driver.Current.Tick);
        Assert.Equal(14, driver.Previous.Tick);
    }

    [Fact]
    public void A_newcomer_shows_where_it_arrived_before_it_has_a_tick_to_blend_from()
    {
        var sim = new Sim(Tuning with { Arrivals = new ArrivalTuning(new(1, 1)) }, new Lane([new(0, 0), new(0, 10)], 0.5f), seed: 1);
        var player = sim.Spawn(new(0, 1));
        sim.ArriveAt(new(2, 11), [new Trait("calm", 1, 1, 1, new HashSet<Stage> { Stage.Calm })]);
        var driver = new Driver(sim, player);

        driver.Advance(HundredTwentyEighthOfASecond * 3, Command.Idle);
        driver.Advance(HundredTwentyEighthOfASecond, Command.Idle);

        var newcomer = new AgentId(1);
        Assert.Equal(2, driver.Current.Agents.Count);
        Assert.Single(driver.Previous.Agents);
        Assert.Equal(driver.Current.Agents[newcomer.Value].Position, driver.Position(newcomer));
    }

    [Fact]
    public void Frames_shorter_than_a_tick_carry_the_rest_and_blend_toward_the_next_tick()
    {
        var (driver, _) = Line();

        driver.Advance(HundredTwentyEighthOfASecond, Command.Idle);
        driver.Advance(HundredTwentyEighthOfASecond, Command.Idle);
        Assert.Equal(0, driver.Current.Tick);
        Assert.Equal(0.9375f, driver.Blend);

        driver.Advance(HundredTwentyEighthOfASecond, Command.Idle);
        Assert.Equal(1, driver.Current.Tick);
        Assert.Equal(0.40625f, driver.Blend);
    }

    [Fact]
    public void A_frame_shows_the_moment_between_the_last_two_ticks_it_blends()
    {
        var (driver, _) = Line();
        Assert.Equal(0f, driver.ShownTick);

        driver.Advance(SixteenthOfASecond, Command.Idle);

        Assert.Equal(2.75f, driver.ShownTick);
    }

    [Fact]
    public void A_hitch_steps_at_most_the_frame_limit_and_drops_the_rest()
    {
        var (driver, _) = Line();

        driver.Advance(1.0, Command.Idle);

        Assert.Equal(Driver.MaxTicksPerFrame, driver.Current.Tick);
        Assert.Equal(0f, driver.Blend);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_frame_time_that_is_not_a_positive_number_steps_nothing(double seconds)
    {
        var (driver, _) = Line();

        driver.Advance(seconds, Forward);

        Assert.Equal(0, driver.Current.Tick);
        Assert.Equal(0f, driver.Blend);
    }

    [Fact]
    public void The_sim_reaches_the_same_state_whatever_frame_rate_drives_it()
    {
        var (slow, slowSim) = Line();
        var (fast, fastSim) = Line();

        for (var frame = 0; frame < 32; frame++)
        {
            slow.Advance(SixteenthOfASecond, Forward);
        }

        for (var frame = 0; frame < 256; frame++)
        {
            fast.Advance(HundredTwentyEighthOfASecond, Forward);
        }

        Assert.Equal(120, slow.Current.Tick);
        Assert.Equal(120, fast.Current.Tick);
        Assert.Equal(slowSim.StateHash(), fastSim.StateHash());
    }

    [Fact]
    public void An_agent_shows_part_way_between_its_last_two_positions()
    {
        var (driver, _) = Line();
        for (var frame = 0; frame < 4; frame++)
        {
            driver.Advance(SixteenthOfASecond, Forward);
        }

        driver.Advance(HundredTwentyEighthOfASecond, Forward);

        var before = driver.Previous.Agents[1].Position;
        var after = driver.Current.Agents[1].Position;
        var shown = driver.Position(new AgentId(1));

        Assert.NotEqual(before, after);
        Assert.Equal(before.Y + ((after.Y - before.Y) * 0.46875f), shown.Y, 0.00001f);
    }

    [Fact]
    public void Only_the_player_follows_the_command()
    {
        var (driver, _) = Line();

        driver.Advance(SixteenthOfASecond, Forward);

        Assert.Equal(new Vector2(0, 1), driver.Current.Agents[0].Position);
        Assert.True(driver.Current.Agents[1].Position.Y < 2f);
    }

    /// <summary>A person ahead and the player behind them, in a straight lane whose front is at the origin.</summary>
    private static (Driver Driver, Sim Sim) Line()
    {
        var sim = new Sim(Tuning, new Lane([new(0, 0), new(0, 10)], halfWidth: 0.5f), seed: 1);
        sim.Spawn(new Vector2(0, 1));
        var player = sim.Spawn(new Vector2(0, 2));
        return (new Driver(sim, player), sim);
    }
}
