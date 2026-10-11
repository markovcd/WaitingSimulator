namespace WaitingSim.Tests;

public sealed class TraitTests
{
    private static readonly Trait HidesUntilFedUp = new("passive-aggressive", 1, 1, 1, new HashSet<Stage> { Stage.Calm, Stage.FedUp });
    private static readonly Trait SkipsPoliteStages = new("aggressive", 1, 1, 1, new HashSet<Stage> { Stage.Calm, Stage.Confronting, Stage.FedUp });

    [Theory]
    [InlineData(Stage.Calm, Stage.Calm)]
    [InlineData(Stage.Annoyed, Stage.Calm)]
    [InlineData(Stage.Confronting, Stage.Calm)]
    [InlineData(Stage.FedUp, Stage.FedUp)]
    public void A_hidden_stage_looks_like_the_last_shown_stage_before_it(Stage real, Stage shown)
    {
        Assert.Equal(shown, HidesUntilFedUp.Shown(real));
    }

    [Theory]
    [InlineData(Stage.Restless, Stage.Calm)]
    [InlineData(Stage.Annoyed, Stage.Calm)]
    [InlineData(Stage.Confronting, Stage.Confronting)]
    public void A_trait_that_skips_polite_stages_goes_from_calm_to_confronting(Stage real, Stage shown)
    {
        Assert.Equal(shown, SkipsPoliteStages.Shown(real));
    }
}
