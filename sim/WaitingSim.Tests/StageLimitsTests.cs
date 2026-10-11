namespace WaitingSim.Tests;

public sealed class StageLimitsTests
{
    private static readonly StageLimits Limits = new(70, 50, 30, 15, RecoverMargin: 5);

    [Theory]
    [InlineData(Stage.Calm, 70f, Stage.Calm)]
    [InlineData(Stage.Calm, 69.9f, Stage.Restless)]
    [InlineData(Stage.Calm, 20f, Stage.Confronting)]
    [InlineData(Stage.Calm, 0f, Stage.FedUp)]
    [InlineData(Stage.Restless, 74.9f, Stage.Restless)]
    [InlineData(Stage.Restless, 75f, Stage.Calm)]
    [InlineData(Stage.FedUp, 54f, Stage.Annoyed)]
    [InlineData(Stage.FedUp, 100f, Stage.Calm)]
    public void A_stage_starts_below_its_limit_and_ends_only_a_margin_above_it(Stage current, float patience, Stage after)
    {
        Assert.Equal(after, Limits.After(current, patience));
    }
}
