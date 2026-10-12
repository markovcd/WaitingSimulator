using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class FaceTests
{
    private static readonly IReadOnlyList<Trait> Traits = WaitingSim.Traits.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "traits.json"))).Value!;

    [Fact]
    public void A_calm_face_smiles_a_little_and_a_fed_up_one_shouts()
    {
        Assert.True(Face.For(Stage.Calm).MouthCurve > 0f);
        Assert.True(Face.For(Stage.FedUp).MouthOpen > Face.For(Stage.Calm).MouthOpen);
    }

    [Fact]
    public void Each_stage_s_face_is_angrier_than_the_one_before()
    {
        var stages = Enum.GetValues<Stage>();
        var problems = new List<string>();
        for (var i = 1; i < stages.Length; i++)
        {
            var (before, after) = (Face.For(stages[i - 1]), Face.For(stages[i]));
            if (!(after.BrowTilt > before.BrowTilt && after.MouthCurve < before.MouthCurve && after.Squint >= before.Squint && after.MouthOpen >= before.MouthOpen))
            {
                problems.Add($"{stages[i]} is no angrier than {stages[i - 1]}");
            }
        }

        Assert.Empty(problems);
    }

    [Fact]
    public void An_agent_without_patience_keeps_a_calm_face()
    {
        Assert.Equal(Face.For(Stage.Calm), Face.For(null));
    }

    [Fact]
    public void A_trait_that_hides_a_stage_shows_the_face_of_the_last_stage_it_shows()
    {
        var hiding = Traits.Single(trait => trait.Id == "passive-aggressive");

        Assert.Equal(Face.For(Stage.Calm), Face.For(hiding.Shown(Stage.Confronting)));
        Assert.Equal(Face.For(Stage.FedUp), Face.For(hiding.Shown(Stage.FedUp)));
    }

    [Fact]
    public void A_blink_is_brief_and_neighbors_do_not_blink_together()
    {
        var first = Enumerable.Range(0, 600).Where(tick => Face.Blinking(0, tick)).ToList();
        var second = Enumerable.Range(0, 600).Where(tick => Face.Blinking(1, tick)).ToList();

        Assert.InRange(first.Count, 1, 600 / 20);
        Assert.Empty(first.Intersect(second));
    }
}
