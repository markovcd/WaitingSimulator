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
    public void Newcomers_arrive_at_a_level_s_entrance_and_none_at_a_level_without_one()
    {
        var tuning = Tuning with { Arrivals = new ArrivalTuning(new(60, 60)) };
        var withEntrance = Level.Parse(Valid.Replace("\"clerk\": [0, -1.0]", "\"clerk\": [0, -1.0],\n  \"entrance\": [2, 11]", StringComparison.Ordinal), Traits).Value!;
        var without = Level.Parse(Valid, Traits).Value!;

        Assert.Equal(new Vector2(2, 11), withEntrance.Entrance);
        Assert.Contains(SimTests.Run(withEntrance.Start(tuning), 61, _ => SimTests.Commands()), e => e is Arrived);
        Assert.DoesNotContain(SimTests.Run(without.Start(tuning), 61, _ => SimTests.Commands()), e => e is Arrived);
    }

    [Fact]
    public void Served_people_walk_out_of_a_level_s_exit_and_stay_beside_a_level_without_one()
    {
        var withExit = Level.Parse(Valid.Replace("\"clerk\": [0, -1.0]", "\"clerk\": [0, -1.0],\n  \"exit\": [3, -1]", StringComparison.Ordinal), Traits).Value!;
        var without = Level.Parse(Valid, Traits).Value!;

        var gone = withExit.Start(Tuning);
        var stays = without.Start(Tuning);
        SimTests.Run(gone, 20 * TickRate.PerSecond, _ => SimTests.Commands());
        SimTests.Run(stays, 20 * TickRate.PerSecond, _ => SimTests.Commands());

        Assert.Equal(new Vector2(3, -1), withExit.Exit);
        Assert.Equal(LineStep.Gone, gone.Snapshot().Agents[0].Step);
        Assert.Equal(LineStep.Done, stays.Snapshot().Agents[0].Step);
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
    [InlineData("\"clerk\": [0, -1.0]", "\"clerk\": [0, -1.0], \"entrance\": [2]", "entrance: missing, or not a point [x, y] of two finite numbers")]
    [InlineData("\"clerk\": [0, -1.0]", "\"clerk\": [0, -1.0], \"exit\": \"door\"", "exit: missing, or not a point [x, y] of two finite numbers")]
    public void A_bad_level_field_is_reported_by_name(string field, string replacement, string problem)
    {
        var parsed = Level.Parse(Valid.Replace(field, replacement, StringComparison.Ordinal), Traits);

        Assert.Null(parsed.Value);
        Assert.Equal([problem], parsed.Problems);
    }

    [Fact]
    public void An_agent_marked_as_a_cutter_is_spawned_as_one()
    {
        var level = Level.Parse(Valid.Replace("\"trait\": \"anxious\" }", "\"trait\": \"anxious\" },\n    { \"position\": [1.5, 3], \"cutter\": true }", StringComparison.Ordinal), Traits).Value!;

        Assert.Equal([false, false, false, true], level.Agents.Select(agent => agent.Cutter).ToList());
    }

    [Fact]
    public void A_cutter_mark_that_is_not_true_or_false_is_reported()
    {
        var parsed = Level.Parse(Valid.Replace("\"trait\": \"anxious\" }", "\"trait\": \"anxious\", \"cutter\": \"yes\" }", StringComparison.Ordinal), Traits);

        Assert.Equal(["agents[2].cutter: missing, or not true or false"], parsed.Problems);
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

    [Fact]
    public void In_the_post_office_walking_up_beside_the_line_and_cutting_in_at_the_front_is_refused()
    {
        var level = Level.Parse(File.ReadAllText(Path.Combine(Levels, "post-office.json")), Traits).Value!;
        var sim = level.Start(Tuning);
        var player = level.Player;

        // Out of the lane on the side away from the cutter, up to the counter, then in once nobody stands in the way.
        var events = SimTests.Run(sim, 60 * TickRate.PerSecond, snapshot =>
        {
            var me = snapshot.Agents[player.Value];
            var move = me switch
            {
                { KickedOutAt: not null } or { ReachedCounterAt: not null } => Vector2.Zero,
                { Position.Y: > 0.15f, Position.X: > -0.9f } => new Vector2(-1, 0),
                { Position.Y: > 0.15f } => new Vector2(0, -1),
                { InLane: false } when snapshot.Agents.Any(other => other.Id != player && other.InLane && other.Position.Y < 0.6f) => Vector2.Zero,
                { Position.X: < 0f } => new Vector2(1, 0),
                _ => Vector2.Zero,
            };
            return SimTests.Commands((player, move));
        });

        Assert.Contains(events, e => e is CutInMissed missed && missed.Agent == player);
        Assert.DoesNotContain(events, e => e is CutIn cut && cut.Agent == player);
        Assert.DoesNotContain(events, e => e is ReachedCounter reached && reached.Agent == player);
    }

    [Fact]
    public void In_the_post_office_every_newcomer_joins_the_line_and_is_served_in_turn()
    {
        var level = Level.Parse(File.ReadAllText(Path.Combine(Levels, "post-office.json")), Traits).Value!;
        var sim = level.Start(Tuning);
        var joined = new HashSet<AgentId>();

        // Nobody commands the player, so the line moves everyone, the player too.
        var events = SimTests.Run(sim, 120 * TickRate.PerSecond, snapshot =>
        {
            joined.UnionWith(snapshot.Agents.Where(agent => agent is { InLane: true, Refused: false }).Select(agent => agent.Id));
            return SimTests.Commands();
        });

        var arrived = events.OfType<Arrived>().Select(e => e.Agent).ToList();
        Assert.InRange(arrived.Count, 5, 20);
        Assert.DoesNotContain(events, e => e is CutIn or CutInMissed or KickedOut);
        Assert.All(arrived.SkipLast(1), agent => Assert.Contains(agent, joined));
        var served = events.OfType<ReachedCounter>().Select(e => e.Agent).Where(arrived.Contains).ToList();
        Assert.Equal(arrived.Take(served.Count), served);
    }

    private static string Shipped(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", name));
}
