using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class TuningReloadTests : IDisposable
{
    private static readonly string ShippedData = Path.Combine(AppContext.BaseDirectory, "data");

    private readonly string folder = Directory.CreateTempSubdirectory("reload-").FullName;

    public TuningReloadTests()
    {
        File.Copy(Path.Combine(ShippedData, "tuning.json"), Path.Combine(folder, "tuning.json"));
        File.Copy(Path.Combine(ShippedData, "traits.json"), Path.Combine(folder, "traits.json"));
        Directory.CreateDirectory(Path.Combine(folder, "levels"));
        File.Copy(Path.Combine(ShippedData, "levels", Client.LevelName + ".json"), Path.Combine(folder, "levels", Client.LevelName + ".json"));
    }

    public void Dispose() => Directory.Delete(folder, recursive: true);

    [Fact]
    public void A_reload_reads_the_tuning_as_the_file_now_says()
    {
        Edit("tuning.json", "\"speedMetersPerSecond\": 1.2", "\"speedMetersPerSecond\": 1.7");

        var (data, notice) = TuningReload.Try(folder, Client.LevelName);

        Assert.Equal(1.7f, data!.Tuning.WalkSpeed);
        Assert.Equal("TUNING RELOADED", notice);
    }

    [Fact]
    public void A_tuning_file_with_a_problem_reloads_nothing_and_names_the_file_and_field()
    {
        Edit("tuning.json", "\"speedMetersPerSecond\": 1.2", "\"speedMetersPerSecond\": 0");

        var (data, notice) = TuningReload.Try(folder, Client.LevelName);

        Assert.Null(data);
        Assert.Equal(
            $"TUNING NOT RELOADED\n{Path.Combine(folder, "tuning.json")}: walking.speedMetersPerSecond: missing, or not a positive number",
            notice);
    }

    [Fact]
    public void A_traits_file_with_a_problem_reloads_nothing()
    {
        Edit("traits.json", "\"drainScale\": 0.5", "\"drainScale\": \"lots\"");

        var (data, notice) = TuningReload.Try(folder, Client.LevelName);

        Assert.Null(data);
        Assert.Contains(Path.Combine(folder, "traits.json") + ": ", notice, StringComparison.Ordinal);
    }

    private void Edit(string file, string from, string to)
    {
        var path = Path.Combine(folder, file);
        var text = File.ReadAllText(path);
        Assert.Contains(from, text, StringComparison.Ordinal);
        File.WriteAllText(path, text.Replace(from, to, StringComparison.Ordinal));
    }
}
