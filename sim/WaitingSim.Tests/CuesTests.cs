using System.Numerics;
using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class CuesTests
{
    private static readonly AgentId Ahead = new(0);
    private static readonly AgentId Player = new(1);
    private static readonly AgentId Behind = new(2);

    // The player in the middle, walked into from behind: the player's spacing is tight, the person behind's is a bump.
    private static readonly Snapshot After = new(10, [
        Agent(Ahead, null, null, null),
        Agent(Player, Ahead, Zone.Tight, null),
        Agent(Behind, Player, Zone.Bump, Stage.Annoyed),
    ]);

    [Fact]
    public void Walking_into_someone_makes_the_one_walked_into_stumble()
    {
        var cues = Cues.From([new ZoneChanged(10, Behind, Zone.Tight, Zone.Bump)], After);

        Assert.Equal([new Cue(Player, CueKind.Bumped, null)], cues);
    }

    [Fact]
    public void A_stage_getting_worse_is_cued_and_one_getting_better_is_not()
    {
        var cues = Cues.From([
            new StageChanged(10, Behind, Stage.Restless, Stage.Annoyed),
            new StageChanged(10, Behind, Stage.Confronting, Stage.Annoyed),
        ], After);

        Assert.Equal([new Cue(Behind, CueKind.Escalated, Stage.Annoyed)], cues);
    }

    [Fact]
    public void Patience_running_out_is_cued()
    {
        var cues = Cues.From([new PatienceRanOut(10, Behind)], After);

        Assert.Equal([new Cue(Behind, CueKind.RanOut, Stage.Annoyed)], cues);
    }

    [Fact]
    public void Events_nobody_would_see_have_no_cue()
    {
        var cues = Cues.From([
            new GraceExpired(10, Player),
            new ZoneChanged(10, Player, Zone.Loose, Zone.Tight),
            new LeftLane(10, Ahead),
        ], After);

        Assert.Empty(cues);
    }

    private static AgentSnapshot Agent(AgentId id, AgentId? ahead, Zone? zone, Stage? stage) =>
        new(id, Vector2.Zero, Vector2.Zero, true, ahead, zone is null ? null : 0.5f, zone, null, stage is null ? null : "anxious", stage is null ? null : 50f, stage, LineStep.Standing, null, false, default, null);
}
