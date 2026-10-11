using WaitingSim.Runner;

namespace WaitingSim.Tests;

public sealed class ReplayCommandLineTests : IDisposable
{
    private readonly string folder = Directory.CreateTempSubdirectory("replay-").FullName;

    public void Dispose() => Directory.Delete(folder, recursive: true);

    [Fact]
    public void A_replay_that_matches_its_hashes_succeeds()
    {
        var (code, output, error) = Run("replay", Copy(), "--json");

        Assert.Equal(ExitCode.Success, code);
        Assert.Contains("\"divergence\":null", output, StringComparison.Ordinal);
        Assert.Empty(error);
    }

    [Fact]
    public void A_replay_that_diverges_names_the_first_tick_on_stderr()
    {
        var file = Copy();
        var replay = Replay.Parse(File.ReadAllText(file)).Value!;
        var hashes = replay.Hashes.ToList();
        hashes[3] = hashes[3] with { Hash = hashes[3].Hash ^ 1 };
        File.WriteAllText(file, (replay with { Hashes = hashes }).ToJson());

        var (code, _, error) = Run("replay", file);

        Assert.Equal(ExitCode.ReplayDiverged, code);
        Assert.StartsWith($"{file}: tick {hashes[3].Tick}: recorded hash", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Recording_writes_the_hashes_the_replay_plays_to()
    {
        var file = Copy();
        var replay = Replay.Parse(File.ReadAllText(file)).Value!;
        File.WriteAllText(file, (replay with { Hashes = [] }).ToJson());

        var recorded = Run("replay", file, "--record");
        var checkedAfter = Run("replay", file);

        Assert.Equal(ExitCode.Success, recorded.Code);
        Assert.Equal(ExitCode.Success, checkedAfter.Code);
        Assert.Equal(replay.Hashes, Replay.Parse(File.ReadAllText(file)).Value!.Hashes);
    }

    [Fact]
    public void A_missing_replay_file_is_a_data_problem_naming_it()
    {
        var file = Path.Combine(folder, "nowhere.json");

        var (code, _, error) = Run("replay", file);

        Assert.Equal(ExitCode.DataProblem, code);
        Assert.StartsWith($"{file}: ", error, StringComparison.Ordinal);
    }

    [Fact]
    public void A_replay_with_no_file_is_a_usage_error()
    {
        var (code, _, error) = Run("replay");

        Assert.Equal(ExitCode.Usage, code);
        Assert.StartsWith("replay: name the replay file.", error, StringComparison.Ordinal);
    }

    /// <summary>A copy of the first golden replay in this test's folder.</summary>
    private string Copy()
    {
        var golden = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Replays"), "*.json").Order(StringComparer.Ordinal).First();
        var file = Path.Combine(folder, Path.GetFileName(golden));
        File.Copy(golden, file);
        return file;
    }

    private static (int Code, string Output, string Error) Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = Cli.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }
}
