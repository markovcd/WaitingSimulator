using System.Numerics;
using System.Text.Json;

namespace WaitingSim;

/// <summary>A line level as <c>data/levels/</c> sets it up: the lane, who stands in it, and which of them the player is.</summary>
/// <param name="Seed">The sim's seed; seeded per level, so a level plays the same for everyone.</param>
/// <param name="Lane">The lane.</param>
/// <param name="Agents">The agents, spawned in this order, so an agent's id is its index.</param>
/// <param name="Player">The agent the player drives. It has no trait: the player has no patience.</param>
/// <param name="Clerk">Where the clerk stands, past the front of the line.</param>
/// <param name="Traits">The traits the agents' trait ids name.</param>
public sealed record Level(ulong Seed, Lane Lane, IReadOnlyList<AgentSpawn> Agents, AgentId Player, Vector2 Clerk, IReadOnlyList<Trait> Traits)
{
    /// <summary>The newest level format this sim reads.</summary>
    public const int FormatVersion = 1;

    /// <summary>Reads a level file's text; its agents' traits must be among <paramref name="traits"/>. Problems name the field they concern.</summary>
    public static Parsed<Level> Parse(string json, IReadOnlyList<Trait> traits)
    {
        ArgumentNullException.ThrowIfNull(traits);
        return JsonFile.Read(json, "level", FormatVersion, (root, problems) => Read(root, traits, problems));
    }

    /// <summary>A sim of this level at tick 0, every agent where the level places it.</summary>
    public Sim Start(Tuning tuning)
    {
        var sim = new Sim(tuning, Lane, Seed);
        foreach (var agent in Agents)
        {
            sim.Spawn(agent.Position, agent.Trait is { } id ? Traits.Single(trait => trait.Id == id) : null, agent.Cutter);
        }

        return sim;
    }

    private static Level Read(JsonElement root, IReadOnlyList<Trait> traits, List<string> problems)
    {
        var seed = JsonFile.Unsigned(root, "seed", problems);
        var lane = Lane.Read(root, problems);
        var agents = AgentSpawn.ReadAll(root, traits, "the known traits", problems);
        var player = JsonFile.Integer(root, "player", problems);
        if (player < 0 || player >= agents.Count)
        {
            problems.Add(JsonFile.Invariant($"player: {player} is not one of the {agents.Count} agents"));
        }
        else if (agents[player].Trait is not null)
        {
            problems.Add(JsonFile.Invariant($"agents[{player}].trait: the player has no patience, so no trait"));
        }

        var clerk = JsonFile.Point(root, "clerk", problems);
        return new(seed, lane!, agents, new AgentId(player), clerk, traits);
    }
}
