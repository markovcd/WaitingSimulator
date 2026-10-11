using System.Text.Json;
using WaitingSim.Runner;

namespace WaitingSim.Tests;

public sealed class PlayCommandTests : IDisposable
{
    private static readonly string ShippedData = Path.Combine(AppContext.BaseDirectory, "data");

    private readonly string folder = Directory.CreateTempSubdirectory("play-").FullName;

    public void Dispose() => Directory.Delete(folder, recursive: true);

    [Fact]
    public void An_attentive_bot_reaches_the_counter_of_every_shipped_level()
    {
        var levels = Directory.GetFiles(Path.Combine(ShippedData, "levels"), "*.json").Select(Path.GetFileNameWithoutExtension).OfType<string>();

        Assert.All(levels, level =>
        {
            var (code, output, error) = Run("play", "--data", ShippedData, "--level", level, "--json");

            Assert.Equal(ExitCode.Success, code);
            Assert.Empty(error);
            using var report = JsonDocument.Parse(output);
            Assert.True(report.RootElement.GetProperty("reachedCounter").GetBoolean(), $"{level}: the bot never reached the counter.");
            Assert.True(report.RootElement.GetProperty("seconds").GetDouble() > 0);
        });
    }

    [Fact]
    public void A_bot_that_gives_up_first_says_it_never_reached_the_counter()
    {
        var (code, output, _) = Run("play", "--data", ShippedData, "--seconds", "1");

        Assert.Equal(ExitCode.Success, code);
        Assert.Equal("post-office: the attentive bot did not reach the counter in 1 s; worst mistake: none.", output.Trim());
    }

    [Fact]
    public void The_same_level_and_bot_play_to_the_same_report()
    {
        Assert.Equal(Run("play", "--data", ShippedData, "--bot", "distracted", "--json"), Run("play", "--data", ShippedData, "--bot", "distracted", "--json"));
    }

    [Fact]
    public void A_missing_level_is_named_on_stderr()
    {
        var (code, output, error) = Run("play", "--data", ShippedData, "--level", "bank");

        Assert.Equal(ExitCode.DataProblem, code);
        Assert.Empty(output);
        Assert.Contains(Path.Combine(ShippedData, "levels", "bank.json"), error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--level", "../tuning")]
    [InlineData("--level", "Post-Office")]
    [InlineData("--bot", "psychic")]
    [InlineData("--bot", "1")]
    [InlineData("--bot", "attentive,slow")]
    [InlineData("--seconds", "0")]
    [InlineData("--fast")]
    public void A_bad_option_is_a_usage_error(params string[] options)
    {
        var (code, _, error) = Run(["play", "--data", folder, .. options]);

        Assert.Equal(ExitCode.Usage, code);
        Assert.StartsWith($"play: {options[0]} is not an option", error, StringComparison.Ordinal);
    }

    private static (int Code, string Output, string Error) Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = Cli.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }
}
