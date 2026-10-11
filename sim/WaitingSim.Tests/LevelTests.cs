using System.Numerics;

namespace WaitingSim.Tests;

public sealed class LevelTests
{
    private const string Valid = """
        {
          "version": 1,
          "seed": 7,
          "lane": { "points": [[0, 0], [0, 10]], "halfWidth": 0.5 },
          "agents": [
            { "position": [0, 1.0] },
            { "position": [0, 1.8] },
            { "position": [0, 2.6], "trait": "anxious" }
          ],
          "player": 1,
          "clerk": [0, -1.0]
        }
        """;

    private static readonly string Levels = Path.Combine(AppContext.BaseDirectory, "data", "levels");

    private static readonly Tuning Tuning = Tuning.Parse(Shipped("tuning.json")).Value!;

    private static readonly IReadOnlyList<Trait> Traits = WaitingSim.Traits.Parse(Shipped("traits.json")).Value!;

    public static TheoryData<string> ShippedLevels() => [.. Directory.GetFiles(Levels, "*.json").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal)];

    [Fact]
    public void There_is_a_shipped_level() => Assert.NotEmpty(ShippedLevels());

    [Theory]
    [MemberData(nameof(ShippedLevels))]
    public void Every_shipped_level_loads_without_problems(string name)
    {
        var parsed = Level.Parse(File.ReadAllText(Path.Combine(Levels, name)), Traits);

        Assert.Empty(parsed.Problems);
        Assert.NotNull(parsed.Value);
    }

    [Fact]
    public void A_level_starts_each_agent_where_it_places_it_with_the_trait_it_names()
    {
        var level = Level.Parse(Valid, Traits).Value!;

        var agents = level.Start(Tuning).Snapshot().Agents;

        Assert.Equal([new(0, 1.0f), new(0, 1.8f), new(0, 2.6f)], agents.Select(agent => agent.Position).ToList());
        Assert.Equal([null, null, "anxious"], agents.Select(agent => agent.Trait).ToList());
        Assert.Equal(new AgentId(1), level.Player);
        Assert.Equal(new Vector2(0, -1.0f), level.Clerk);
    }

    [Fact]
    public void A_level_plays_the_same_from_its_seed_every_time()
    {
        var level = Level.Parse(Valid, Traits).Value!;

        Assert.Equal(level.Start(Tuning).StateHash(), level.Start(Tuning).StateHash());
        Assert.Equal(7UL, level.Seed);
    }

    [Theory]
    [InlineData("\"player\": 1", "\"player\": 3", "player: 3 is not one of the 3 agents")]
    [InlineData("\"player\": 1", "\"player\": 2", "agents[2].trait: the player has no patience, so no trait")]
    [InlineData("\"trait\": \"anxious\"", "\"trait\": \"grumpy\"", "agents[2].trait: \"grumpy\" is not one of the known traits")]
    [InlineData("\"clerk\": [0, -1.0]", "\"clerk\": [0]", "clerk: missing, or not a point [x, y] of two finite numbers")]
    [InlineData("\"halfWidth\": 0.5", "\"halfWidth\": 0", "lane.halfWidth: missing, or not a positive number")]
    [InlineData("\"seed\": 7", "\"seed\": -7", "seed: missing, or not a whole number from 0 up")]
    public void A_bad_level_field_is_reported_by_name(string field, string replacement, string problem)
    {
        var parsed = Level.Parse(Valid.Replace(field, replacement, StringComparison.Ordinal), Traits);

        Assert.Null(parsed.Value);
        Assert.Equal([problem], parsed.Problems);
    }

    [Fact]
    public void A_trait_id_is_checked_even_when_no_traits_are_known()
    {
        var parsed = Level.Parse(Valid, []);

        Assert.Equal(["agents[2].trait: \"anxious\" is not one of the known traits"], parsed.Problems);
    }

    [Fact]
    public void A_level_from_a_newer_version_is_refused_whole()
    {
        var parsed = Level.Parse(Valid.Replace("\"version\": 1", "\"version\": 2", StringComparison.Ordinal), Traits);

        Assert.Null(parsed.Value);
        Assert.Equal(["version: 2 is from a newer game; this one reads up to 1"], parsed.Problems);
    }

    private static string Shipped(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", name));
}
