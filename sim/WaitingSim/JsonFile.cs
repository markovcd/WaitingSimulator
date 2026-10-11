using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace WaitingSim;

/// <summary>
/// Reads the sim's versioned JSON data files. Every field is read by its path in the file,
/// and a field that is missing or wrong adds a problem naming that path instead of throwing.
/// </summary>
internal static class JsonFile
{
    /// <summary>
    /// Parses <paramref name="json"/>, checks its version against <paramref name="newestVersion"/>, then lets
    /// <paramref name="read"/> read the rest. A file from a newer version is refused whole.
    /// <paramref name="kind"/> is what the file holds, as problems name it ("tuning").
    /// </summary>
    public static Parsed<T> Read<T>(string json, string kind, int newestVersion, Func<JsonElement, List<string>, T> read)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Refused<T>("the file is not a JSON object");
            }

            var problems = new List<string>();
            var version = Integer(root, "version", problems);
            if (version > newestVersion)
            {
                return Refused<T>(Invariant($"version: {version} is from a newer game; this one reads up to {newestVersion}"));
            }

            if (version < 1 && problems.Count == 0)
            {
                problems.Add(Invariant($"version: {version} is not a {kind} format version"));
            }

            var value = read(root, problems);
            return problems.Count > 0 ? new(null, problems) : new(value, []);
        }
        catch (JsonException exception)
        {
            return Refused<T>(Invariant($"not valid JSON at line {(exception.LineNumber ?? 0) + 1}: {exception.Message}"));
        }
    }

    /// <summary>The object at <paramref name="path"/>, or null with a problem.</summary>
    public static JsonElement? Section(JsonElement? parent, string path, List<string> problems)
    {
        if (parent is not { } element)
        {
            return null;
        }

        if (element.TryGetProperty(Name(path), out var section) && section.ValueKind == JsonValueKind.Object)
        {
            return section;
        }

        problems.Add($"{path}: missing, or not an object");
        return null;
    }

    /// <summary>The whole number at <paramref name="path"/>, or 0 with a problem.</summary>
    public static int Integer(JsonElement? parent, string path, List<string> problems)
    {
        if (parent is not { } element)
        {
            return 0;
        }

        if (element.TryGetProperty(Name(path), out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var integer))
        {
            return integer;
        }

        problems.Add($"{path}: missing, or not a whole number");
        return 0;
    }

    /// <summary>The positive, finite number at <paramref name="path"/>, or 0 with a problem.</summary>
    public static float Positive(JsonElement? parent, string path, List<string> problems)
    {
        if (parent is not { } element)
        {
            return 0f;
        }

        if (element.TryGetProperty(Name(path), out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetSingle(out var number)
            && float.IsFinite(number)
            && number > 0f)
        {
            return number;
        }

        problems.Add($"{path}: missing, or not a positive number");
        return 0f;
    }

    /// <summary>The non-negative whole number at <paramref name="path"/>, up to 2^64 - 1, or 0 with a problem.</summary>
    public static ulong Unsigned(JsonElement? parent, string path, List<string> problems)
    {
        if (parent is not { } element)
        {
            return 0;
        }

        if (element.TryGetProperty(Name(path), out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetUInt64(out var number))
        {
            return number;
        }

        problems.Add($"{path}: missing, or not a whole number from 0 up");
        return 0;
    }

    /// <summary>The point written as <c>[x, y]</c> at <paramref name="path"/>, or zero with a problem.</summary>
    public static Vector2 Point(JsonElement? parent, string path, List<string> problems)
    {
        if (parent is not { } element)
        {
            return Vector2.Zero;
        }

        if (element.TryGetProperty(Name(path), out var value) && Point(value) is { } point)
        {
            return point;
        }

        problems.Add($"{path}: missing, or not a point [x, y] of two finite numbers");
        return Vector2.Zero;
    }

    /// <summary>An <c>[x, y]</c> array of two finite numbers as a point, or null.</summary>
    public static Vector2? Point(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 2)
        {
            return null;
        }

        return value[0].ValueKind == JsonValueKind.Number && value[0].TryGetSingle(out var x) && float.IsFinite(x)
            && value[1].ValueKind == JsonValueKind.Number && value[1].TryGetSingle(out var y) && float.IsFinite(y)
                ? new Vector2(x, y)
                : null;
    }

    /// <summary>The non-empty string at <paramref name="path"/>, or an empty one with a problem.</summary>
    public static string Text(JsonElement? parent, string path, List<string> problems)
    {
        if (parent is not { } element)
        {
            return "";
        }

        if (element.TryGetProperty(Name(path), out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } text)
        {
            return text;
        }

        problems.Add($"{path}: missing, or not a non-empty string");
        return "";
    }

    /// <summary>The items of the array at <paramref name="path"/>, or none with a problem.</summary>
    public static IReadOnlyList<JsonElement> Items(JsonElement? parent, string path, List<string> problems)
    {
        if (parent is not { } element)
        {
            return [];
        }

        if (element.TryGetProperty(Name(path), out var value) && value.ValueKind == JsonValueKind.Array)
        {
            return [.. value.EnumerateArray()];
        }

        problems.Add($"{path}: missing, or not an array");
        return [];
    }

    public static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

    private static Parsed<T> Refused<T>(string problem)
        where T : class => new(null, [problem]);

    /// <summary>A field's name within its parent: the last part of its path.</summary>
    private static string Name(string path) => path[(path.LastIndexOf('.') + 1)..];
}
