using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class SettingsTests : IDisposable
{
    private readonly string folder = Directory.CreateTempSubdirectory("settings-").FullName;

    public void Dispose() => Directory.Delete(folder, recursive: true);

    private string File => Path.Combine(folder, "settings.json");

    [Fact]
    public void Saved_settings_load_back_as_the_same_settings()
    {
        Assert.Empty(Settings.Save(File, new Settings(Difficulty.Hard)));

        Assert.Equal((new Settings(Difficulty.Hard), (string?)null), Settings.Load(File));
    }

    [Fact]
    public void No_settings_file_is_the_defaults_without_a_problem()
    {
        Assert.Equal((Settings.Defaults, (string?)null), Settings.Load(File));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ \"version\": 1, \"difficulty\": ")]
    [InlineData("[1, 2]")]
    [InlineData("{ \"version\": 1, \"difficulty\": \"brutal\" }")]
    [InlineData("{ \"difficulty\": \"hard\" }")]
    public void A_damaged_settings_file_loads_as_the_defaults_and_says_why(string text)
    {
        System.IO.File.WriteAllText(File, text);

        var (settings, problem) = Settings.Load(File);

        Assert.Equal(Settings.Defaults, settings);
        Assert.StartsWith($"{File}: ", problem, StringComparison.Ordinal);
    }

    [Fact]
    public void A_settings_file_from_a_newer_game_loads_as_the_defaults_and_is_never_saved_over()
    {
        const string Newer = "{ \"version\": 2, \"difficulty\": \"hard\", \"volume\": 0.5 }";
        System.IO.File.WriteAllText(File, Newer);

        var (settings, problem) = Settings.Load(File);
        var saved = Settings.Save(File, new Settings(Difficulty.Easy));

        Assert.Equal(Settings.Defaults, settings);
        Assert.Contains("newer game", problem, StringComparison.Ordinal);
        Assert.Contains("newer game", Assert.Single(saved), StringComparison.Ordinal);
        Assert.Equal(Newer, System.IO.File.ReadAllText(File));
    }

    [Fact]
    public void Saving_into_a_folder_that_does_not_exist_yet_creates_it()
    {
        var nested = Path.Combine(folder, "user", "settings.json");

        Assert.Empty(Settings.Save(nested, new Settings(Difficulty.Easy)));
        Assert.Equal(Difficulty.Easy, Settings.Load(nested).Settings.Difficulty);
    }
}
