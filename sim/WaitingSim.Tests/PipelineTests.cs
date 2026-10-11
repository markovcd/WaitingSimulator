using System.Text.RegularExpressions;

namespace WaitingSim.Tests;

/// <summary>Versions the repo names in more than one place, held equal.</summary>
public sealed partial class PipelineTests
{
    private static readonly string Root = FindRoot();

    [Fact]
    public void The_gate_image_carries_the_Godot_the_game_is_built_against()
    {
        var game = Match(Read("game/WaitingSimulator.Game.csproj"), GodotSdk());
        var image = Match(Read("ci/gate.Dockerfile"), GodotArg());

        Assert.Equal(game, image);
    }

    [Fact]
    public void The_gate_image_carries_the_dotnet_SDK_global_json_names()
    {
        var pinned = Match(Read("global.json"), SdkVersion());
        var image = Match(Read("ci/gate.Dockerfile"), SdkImage());

        Assert.Equal(pinned, image);
    }

    [Fact]
    public void The_game_targets_the_framework_everything_else_does()
    {
        var shared = Match(Read("Directory.Build.props"), TargetFramework());
        var game = Match(Read("game/WaitingSimulator.Game.csproj"), TargetFramework());

        Assert.Equal(shared, game);
    }

    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    private static string Match(string text, Regex pattern)
    {
        var match = pattern.Match(text);
        Assert.True(match.Success, $"no match for {pattern}");
        return match.Groups[1].Value;
    }

    /// <summary>The checkout's root: the nearest folder above the tests that holds the solution.</summary>
    private static string FindRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "WaitingSimulator.slnx")))
            {
                return folder.FullName;
            }
        }

        throw new InvalidOperationException($"No WaitingSimulator.slnx above {AppContext.BaseDirectory}.");
    }

    [GeneratedRegex(@"Sdk=""Godot\.NET\.Sdk/([^""]+)""")]
    private static partial Regex GodotSdk();

    [GeneratedRegex(@"ARG GODOT_VERSION=(\S+)")]
    private static partial Regex GodotArg();

    [GeneratedRegex(@"""version"":\s*""([^""]+)""")]
    private static partial Regex SdkVersion();

    [GeneratedRegex(@"FROM mcr\.microsoft\.com/dotnet/sdk:([0-9.]+)-")]
    private static partial Regex SdkImage();

    [GeneratedRegex("<TargetFramework>([^<]+)</TargetFramework>")]
    private static partial Regex TargetFramework();
}
