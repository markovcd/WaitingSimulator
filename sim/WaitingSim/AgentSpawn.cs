using System.Numerics;
using System.Text.Json;

namespace WaitingSim;

/// <summary>An agent a level or a replay spawns, in id order: where it starts, and its trait id if it has patience.</summary>
public sealed record AgentSpawn(Vector2 Position, string? Trait)
{
    /// <summary>
    /// Reads the <c>agents</c> array. A trait id must be one of <paramref name="traits"/>, which
    /// <paramref name="traitsName"/> names in the problem; with no traits to check against, any id passes.
    /// </summary>
    internal static List<AgentSpawn> ReadAll(JsonElement root, IReadOnlyList<Trait> traits, string traitsName, List<string> problems)
    {
        var agents = new List<AgentSpawn>();
        var items = JsonFile.Items(root, "agents", problems);
        for (var i = 0; i < items.Count; i++)
        {
            var path = JsonFile.Invariant($"agents[{i}]");
            var position = JsonFile.Point(items[i], $"{path}.position", problems);
            string? trait = null;
            if (items[i].ValueKind == JsonValueKind.Object && items[i].TryGetProperty("trait", out _))
            {
                trait = JsonFile.Text(items[i], $"{path}.trait", problems);
                if (trait.Length > 0 && traits.Count > 0 && traits.All(known => known.Id != trait))
                {
                    problems.Add($"{path}.trait: \"{trait}\" is not one of {traitsName}");
                }
            }

            agents.Add(new AgentSpawn(position, trait));
        }

        return agents;
    }
}
