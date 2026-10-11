using System.Numerics;

namespace WaitingSim.Tests;

public sealed class ReplayTests
{
    private static readonly string Goldens = Path.Combine(AppContext.BaseDirectory, "Replays");

    public static TheoryData<string> GoldenReplays() => [.. Directory.GetFiles(Goldens, "*.json").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal)];

    [Fact]
    public void There_is_a_golden_replay()
    {
        Assert.NotEmpty(GoldenReplays());
    }

    [Theory]
    [MemberData(nameof(GoldenReplays))]
    public void Every_golden_replay_plays_to_its_recorded_hashes(string name)
    {
        var replay = Golden(name);

        Assert.NotEmpty(replay.Hashes);
        Assert.Null(replay.Divergence(replay.Play().Hashes));
    }

    [Theory]
    [MemberData(nameof(GoldenReplays))]
    public void A_replay_reads_back_from_the_text_it_writes(string name)
    {
        var text = File.ReadAllText(Path.Combine(Goldens, name));

        Assert.Equal(text, Replay.Parse(text).Value!.ToJson());
    }

    [Fact]
    public void A_nudged_command_diverges_from_the_recording()
    {
        var replay = Golden(GoldenReplays().First());
        var commands = replay.Commands.ToList();
        commands[0] = commands[0] with { Move = commands[0].Move * 0.999f };

        var divergence = replay.Divergence((replay with { Commands = commands }).Play().Hashes);

        Assert.StartsWith("tick ", divergence, StringComparison.Ordinal);
    }

    [Fact]
    public void A_replay_carries_its_own_tuning_whatever_the_shipped_data_says()
    {
        var replay = Golden(GoldenReplays().First());
        var shipped = Tuning.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "tuning.json"))).Value!;

        var played = (replay with { Tuning = shipped with { WalkSpeed = shipped.WalkSpeed * 2 } }).Play().Hashes;

        Assert.NotNull(replay.Divergence(played));
        Assert.Null(replay.Divergence(Replay.Parse(replay.ToJson()).Value!.Play().Hashes));
    }

    [Theory]
    [InlineData("\"version\": 1,\n  \"seed\"", "\"version\": 2,\n  \"seed\"", "version: 2 is from a newer game; this one reads up to 1")]
    [InlineData("\"ticks\": 1800", "\"ticks\": 0", "ticks: 0 is not from 1 to 216000")]
    [InlineData("\"trait\": \"anxious\"", "\"trait\": \"grumpy\"", "agents[2].trait: \"grumpy\" is not one of the replay's traits")]
    [InlineData("\"graceWindowTicks\": 90", "\"graceWindowTicks\": -1", "tuning: graceWindowTicks: must not be negative")]
    [InlineData("\"halfWidth\": 0.5", "\"halfWidth\": 0", "lane.halfWidth: missing, or not a positive number")]
    public void A_bad_replay_field_is_reported_by_name(string field, string replacement, string problem)
    {
        var text = File.ReadAllText(Path.Combine(Goldens, GoldenReplays().First()));
        Assert.Contains(field, text, StringComparison.Ordinal);

        var parsed = Replay.Parse(text.Replace(field, replacement, StringComparison.Ordinal));

        Assert.Null(parsed.Value);
        Assert.Equal([problem], parsed.Problems);
    }

    [Fact]
    public void Commands_out_of_tick_order_or_for_a_missing_agent_are_reported()
    {
        var replay = Golden(GoldenReplays().First());
        var commands = replay.Commands.ToList();
        commands.Add(new ReplayCommand(1, new AgentId(9), Vector2.Zero));

        var parsed = Replay.Parse((replay with { Commands = commands }).ToJson());

        Assert.Equal(
            [
                $"commands[{commands.Count - 1}].tick: 1 is not from {commands[^2].Tick} to {replay.Ticks}; commands are in tick order",
                $"commands[{commands.Count - 1}].agent: 9 is not one of the {replay.Agents.Count} agents",
            ],
            parsed.Problems);
    }

    private static Replay Golden(string name) => Replay.Parse(File.ReadAllText(Path.Combine(Goldens, name))).Value!;
}
