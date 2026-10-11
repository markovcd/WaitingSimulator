using System.Numerics;
using System.Text.Json;

namespace WaitingSim;

/// <summary>
/// An agent a level or a replay spawns, in id order: where it starts, its trait id if it has patience, and whether it
/// is a cutter.
/// </summary>
public sealed record AgentSpawn(Vector2 Position, string? Trait, bool Cutter = false)
{
    /// <summary>
    /// Reads the <c>agents</c> array. A trait id must be one of <paramref name="traits"/>, which
    /// <paramref name="traitsName"/> names in the problem; null traits could not be read, and are not checked against.
    /// </summary>
    internal static List<AgentSpawn> ReadAll(JsonElement root, IReadOnlyList<Trait>? traits, string traitsName, List<string> problems)
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
                if (trait.Length > 0 && traits is not null && traits.All(known => known.Id != trait))
                {
                    problems.Add($"{path}.trait: \"{trait}\" is not one of {traitsName}");
                }
            }

            var cutter = items[i].ValueKind == JsonValueKind.Object && items[i].TryGetProperty("cutter", out _)
                && JsonFile.Boolean(items[i], $"{path}.cutter", problems);
            agents.Add(new AgentSpawn(position, trait, cutter));
        }

        return agents;
    }
}
