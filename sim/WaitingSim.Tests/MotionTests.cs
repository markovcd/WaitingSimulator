using System.Numerics;
using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class MotionTests
{
    [Fact]
    public void Each_worse_stage_leans_no_less_into_the_line_and_a_confronting_one_visibly()
    {
        var leans = Enum.GetValues<Stage>().Select(stage => Motion.Of(stage).Lean).ToList();

        List<float> ascending = [.. leans.Order()];
        Assert.Equal(ascending, leans);
        Assert.Equal(0f, Motion.Of(Stage.Calm).Lean);
        Assert.True(Motion.Of(Stage.Confronting).Lean >= 0.1f);
    }

    [Fact]
    public void An_agent_without_patience_moves_like_a_calm_one() => Assert.Equal(Motion.Of(Stage.Calm), Motion.Of(null));

    [Fact]
    public void A_stumble_lurches_forward_and_is_over_once_its_time_is_up()
    {
        var motion = new Motion(1);
        var agent = Agent(0, Stage.Calm);
        motion.Show(new Cue(agent.Id, CueKind.Bumped, Stage.Calm), tick: 100);

        var during = motion.PoseOf(agent, 100f + (Motion.StumbleTicks / 4f));
        float[] stumblesLater = [1f, 1.25f, 1.5f, 3f];
        var after = stumblesLater.Select(times => motion.PoseOf(agent, 100f + (Motion.StumbleTicks * times)).Lean);

        Assert.True(during.Lean > 0.1f);
        Assert.All(after, lean => Assert.Equal(0f, lean));
    }

    [Fact]
    public void Neighbors_in_the_same_stage_do_not_sway_in_step()
    {
        var motion = new Motion(2);

        var first = motion.PoseOf(Agent(0, Stage.Restless), 30f);
        var second = motion.PoseOf(Agent(1, Stage.Restless), 30f);

        Assert.NotEqual(first.Sway, second.Sway, 0.001f);
    }

    [Fact]
    public void Only_a_foot_tapping_stage_bobs()
    {
        var motion = new Motion(1);
        var ticks = Enumerable.Range(0, 120).Select(tick => (float)tick).ToList();

        Assert.All(ticks, tick => Assert.Equal(0f, motion.PoseOf(Agent(0, Stage.Restless), tick).Bob));
        Assert.Contains(ticks, tick => motion.PoseOf(Agent(0, Stage.Annoyed), tick).Bob > 0.01f);
    }

    private static AgentSnapshot Agent(int id, Stage stage) =>
        new(new AgentId(id), Vector2.Zero, Vector2.Zero, true, null, null, null, null, "anxious", 50f, stage, LineStep.Standing, null, false, default, null, null);
}
