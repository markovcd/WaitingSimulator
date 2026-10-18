using System.Numerics;
using System.Text.Json;

namespace WaitingSim;

/// <summary>A crowd level as <c>data/levels/</c> sets it up: the counter, who stands at it, and which of them the player is.</summary>
/// <param name="Seed">The sim's seed; seeded per level, so a level plays the same for everyone.</param>
/// <param name="Counter">The counter the crowd presses against.</param>
/// <param name="Agents">The agents, spawned in this order, which is the order they arrived in, so an agent's id is its index.</param>
/// <param name="Player">The agent the player drives. It has no trait: the player has no patience.</param>
/// <param name="Clerk">Where the clerk stands, behind the counter.</param>
/// <param name="Traits">The traits the agents' trait ids name, and newcomers are drawn from.</param>
/// <param name="Exit">Where the served and those staff move out walk out.</param>
/// <param name="Entrance">Where newcomers arrive; a level without one has none.</param>
public sealed record CrowdLevel(ulong Seed, Counter Counter, IReadOnlyList<AgentSpawn> Agents, AgentId Player, Vector2 Clerk, IReadOnlyList<Trait> Traits, Vector2 Exit, Vector2? Entrance = null)
{
    /// <summary>What a crowd level's <c>kind</c> field says.</summary>
    public const string Kind = "crowd";

    /// <summary>Reads a crowd level file's text; its agents' traits must be among <paramref name="traits"/>. Problems name the field they concern.</summary>
    public static Parsed<CrowdLevel> Parse(string json, IReadOnlyList<Trait> traits)
    {
        ArgumentNullException.ThrowIfNull(traits);
        return JsonFile.Read(json, "level", Level.FormatVersion, (root, problems) => Read(root, traits, problems));
    }

    /// <summary>A crowd at this level at tick 0, every agent where the level places it.</summary>
    public Crowd Start(Tuning tuning)
    {
        var crowd = new Crowd(tuning, Counter, Exit, Seed);
        foreach (var agent in Agents)
        {
            crowd.Spawn(agent.Position, agent.Trait is { } id ? Traits.Single(trait => trait.Id == id) : null, agent.Cutter);
        }

        if (Entrance is { } entrance)
        {
            crowd.ArriveAt(entrance, Traits);
        }

        return crowd;
    }

    private static CrowdLevel Read(JsonElement root, IReadOnlyList<Trait> traits, List<string> problems)
    {
        var kind = JsonFile.Text(root, "kind", problems);
        if (kind != Kind && problems.Count == 0)
        {
            problems.Add($"kind: \"{kind}\" is not \"{Kind}\"");
        }

        var seed = JsonFile.Unsigned(root, "seed", problems);
        var clerk = JsonFile.Point(root, "clerk", problems);
        var section = JsonFile.Section(root, "counter", problems);
        var from = JsonFile.Point(section, "counter.from", problems);
        var to = JsonFile.Point(section, "counter.to", problems);
        var counter = Counter.Between(from, to, clerk);
        if (counter is null && problems.Count == 0)
        {
            problems.Add("counter: from and to must be apart, with the clerk to one side of the line between them");
        }

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

        var exit = JsonFile.Point(root, "exit", problems);
        Vector2? entrance = root.TryGetProperty("entrance", out _) ? JsonFile.Point(root, "entrance", problems) : null;
        return new(seed, counter!, agents, new AgentId(player), clerk, traits, exit, entrance);
    }
}
