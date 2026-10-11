using System.Text.Json;
using System.Text.Json.Nodes;
using WaitingSim.Runner;

namespace WaitingSim.Tests;

// The suite's slowest tests, about two seconds a sweep: one line per trait and bot is still 15 lines of 7,200 ticks,
// each with the 32 agents a line needs for the player never to reach the counter.
public sealed class SweepCommandTests : IDisposable
{
    private static readonly string ShippedData = Path.Combine(AppContext.BaseDirectory, "data");

    private readonly string folder = Directory.CreateTempSubdirectory("sweep-").FullName;

    public void Dispose() => Directory.Delete(folder, recursive: true);

    [Fact]
    public void A_sweep_reports_every_trait_against_every_bot_with_a_patience_curve()
    {
        var (code, output, error) = Run("sweep", "--data", ShippedData, "--lines", "1", "--json");

        Assert.Equal(ExitCode.Success, code);
        Assert.Empty(error);
        using var report = JsonDocument.Parse(output);
        var rows = report.RootElement.GetProperty("rows").EnumerateArray().ToList();
        var traits = Traits.Parse(File.ReadAllText(Path.Combine(ShippedData, "traits.json"))).Value!;
        Assert.Equal(traits.Count * Enum.GetValues<PlayerBot>().Length, rows.Count);
        Assert.All(rows, row => Assert.Equal(SweepLine.Seconds / SweepLine.SampleSeconds, row.GetProperty("meanPatienceCurve").GetArrayLength()));
    }

    [Fact]
    public void The_same_seed_sweeps_to_the_same_report()
    {
        var first = Run("sweep", "--data", ShippedData, "--lines", "1", "--seed", "9", "--json");
        var second = Run("sweep", "--data", ShippedData, "--lines", "1", "--seed", "9", "--json");

        Assert.Equal(first, second);
    }

    [Fact]
    public void A_missing_data_file_is_named_on_stderr()
    {
        var (code, output, error) = Run("sweep", "--data", folder);

        Assert.Equal(ExitCode.DataProblem, code);
        Assert.Empty(output);
        Assert.Contains(Path.Combine(folder, "tuning.json"), error, StringComparison.Ordinal);
    }

    [Fact]
    public void A_data_file_with_problems_is_named_with_each_problem()
    {
        File.Copy(Path.Combine(ShippedData, "tuning.json"), Path.Combine(folder, "tuning.json"));
        File.WriteAllText(Path.Combine(folder, "traits.json"), """{ "version": 1, "traits": [] }""");

        var (code, _, error) = Run("sweep", "--data", folder);

        Assert.Equal(ExitCode.DataProblem, code);
        Assert.Equal($"{Path.Combine(folder, "traits.json")}: traits: there must be at least one", error.Trim());
    }

    [Fact]
    public void A_tuning_without_a_ripple_is_refused_since_nothing_would_move_the_line()
    {
        var tuning = JsonNode.Parse(File.ReadAllText(Path.Combine(ShippedData, "tuning.json")))!.AsObject();
        tuning.Remove("ripple");
        File.WriteAllText(Path.Combine(folder, "tuning.json"), tuning.ToJsonString());
        File.Copy(Path.Combine(ShippedData, "traits.json"), Path.Combine(folder, "traits.json"));

        var (code, output, error) = Run("sweep", "--data", folder);

        Assert.Equal(ExitCode.DataProblem, code);
        Assert.Empty(output);
        Assert.Equal($"{Path.Combine(folder, "tuning.json")}: ripple: missing; a swept line moves only by its ripple", error.Trim());
    }

    [Theory]
    [InlineData("--lines", "0")]
    [InlineData("--lines", "many")]
    [InlineData("--seed")]
    [InlineData("--fast")]
    public void A_bad_option_is_a_usage_error(params string[] options)
    {
        var (code, _, error) = Run(["sweep", .. options]);

        Assert.Equal(ExitCode.Usage, code);
        Assert.StartsWith($"sweep: {options[0]} is not an option", error, StringComparison.Ordinal);
    }

    private static (int Code, string Output, string Error) Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = Cli.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }
}
