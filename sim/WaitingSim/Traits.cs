using System.Text.Json;

namespace WaitingSim;

/// <summary>Reads <c>data/traits.json</c>: every trait an NPC can have, in file order.</summary>
public static class Traits
{
    /// <summary>The newest traits format this sim reads.</summary>
    public const int FormatVersion = 1;

    /// <summary>Stage names as data files write them. Persisted: never renamed.</summary>
    private static readonly Dictionary<string, Stage> StageNames = new(StringComparer.Ordinal)
    {
        ["calm"] = Stage.Calm,
        ["restless"] = Stage.Restless,
        ["annoyed"] = Stage.Annoyed,
        ["confronting"] = Stage.Confronting,
        ["fedUp"] = Stage.FedUp,
    };

    /// <summary>Reads a traits file's text. Problems name the trait and field they concern.</summary>
    public static Parsed<IReadOnlyList<Trait>> Parse(string json) => JsonFile.Read(json, "traits", FormatVersion, Read);

    private static IReadOnlyList<Trait> Read(JsonElement root, List<string> problems)
    {
        var traits = new List<Trait>();
        var items = JsonFile.Items(root, "traits", problems);
        for (var i = 0; i < items.Count; i++)
        {
            var path = JsonFile.Invariant($"traits[{i}]");
            if (items[i].ValueKind != JsonValueKind.Object)
            {
                problems.Add($"{path}: not an object");
                continue;
            }

            var id = JsonFile.Text(items[i], $"{path}.id", problems);
            if (id.Length > 0 && traits.Any(trait => trait.Id == id))
            {
                problems.Add($"{path}.id: \"{id}\" is already used by an earlier trait");
            }

            traits.Add(new Trait(
                id,
                JsonFile.Positive(items[i], $"{path}.drainScale", problems),
                JsonFile.Positive(items[i], $"{path}.recoveryScale", problems),
                JsonFile.Positive(items[i], $"{path}.bumpScale", problems),
                ShownStages(items[i], $"{path}.shownStages", problems)));
        }

        if (items.Count == 0 && problems.Count == 0)
        {
            problems.Add("traits: there must be at least one");
        }

        return traits;
    }

    private static HashSet<Stage> ShownStages(JsonElement trait, string path, List<string> problems)
    {
        var stages = new HashSet<Stage>();
        var items = JsonFile.Items(trait, path, problems);
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].ValueKind == JsonValueKind.String && StageNames.TryGetValue(items[i].GetString()!, out var stage))
            {
                stages.Add(stage);
            }
            else
            {
                problems.Add(JsonFile.Invariant($"{path}[{i}]: not a stage; stages are {string.Join(", ", StageNames.Keys)}"));
            }
        }

        if (!stages.Contains(Stage.Calm) && problems.Count == 0)
        {
            problems.Add($"{path}: must include calm");
        }

        return stages;
    }
}
