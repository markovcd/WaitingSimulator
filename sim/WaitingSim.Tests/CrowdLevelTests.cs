using System.Numerics;

namespace WaitingSim.Tests;

public sealed class CrowdLevelTests
{
    private const string Valid = """
        {
          "version": 1,
          "kind": "crowd",
          "seed": 7,
          "counter": { "from": [-2, 0], "to": [2, 0] },
          "clerk": [0, -1.0],
          "agents": [
            { "position": [0, 0.5], "trait": "anxious" },
            { "position": [1, 0.5] }
          ],
          "player": 1,
          "exit": [-4, 4]
        }
        """;

    private static readonly IReadOnlyList<Trait> Traits = WaitingSim.Traits.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "traits.json"))).Value!;

    [Fact]
    public void A_crowd_level_starts_each_agent_where_it_places_it_in_the_order_they_arrived()
    {
        var level = CrowdLevel.Parse(Valid, Traits).Value!;

        var agents = level.Start(CrowdTests.Crowding).Snapshot().Agents;

        Assert.Equal([new(0, 0.5f), new(1, 0.5f)], agents.Select(agent => agent.Position).ToList());
        Assert.Equal(["anxious", null], agents.Select(agent => agent.Trait).ToList());
        Assert.Equal(new AgentId(1), level.Player);
        Assert.Equal(new Vector2(0, 1), level.Counter.TowardCrowd);
    }

    [Fact]
    public void The_crowd_stands_on_the_side_of_the_counter_away_from_the_clerk()
    {
        var level = CrowdLevel.Parse(Valid.Replace("\"clerk\": [0, -1.0]", "\"clerk\": [0, 1.0]", StringComparison.Ordinal), Traits).Value!;

        Assert.Equal(new Vector2(0, -1), level.Counter.TowardCrowd);
    }

    [Theory]
    [InlineData("\"kind\": \"crowd\"", "\"kind\": \"line\"", "kind: \"line\" is not \"crowd\"")]
    [InlineData("\"to\": [2, 0]", "\"to\": [-2, 0]", "counter: from and to must be apart, with the clerk to one side of the line between them")]
    [InlineData("\"clerk\": [0, -1.0]", "\"clerk\": [1, 0]", "counter: from and to must be apart, with the clerk to one side of the line between them")]
    [InlineData("\"player\": 1", "\"player\": 0", "agents[0].trait: the player has no patience, so no trait")]
    [InlineData("\"exit\": [-4, 4]", "\"exit\": 4", "exit: missing, or not a point [x, y] of two finite numbers")]
    public void A_bad_crowd_level_field_is_reported_by_name(string field, string replacement, string problem) =>
        Assert.Equal([problem], CrowdLevel.Parse(Valid.Replace(field, replacement, StringComparison.Ordinal), Traits).Problems);
}
