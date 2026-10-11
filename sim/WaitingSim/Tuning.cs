using System.Globalization;
using System.Text.Json;

namespace WaitingSim;

/// <summary>The knobs a designer turns, read from <c>data/tuning.json</c>.</summary>
/// <param name="GraceWindowTicks">How many ticks a gap can stay open before the grace window expires.</param>
/// <param name="Zones">Where each spacing zone ends.</param>
/// <param name="WalkSpeed">Top walking speed, in meters per second.</param>
/// <param name="WalkAcceleration">How quickly walking speeds up, slows down and turns, in meters per second squared.</param>
public sealed record Tuning(int GraceWindowTicks, ZoneLimits Zones, float WalkSpeed, float WalkAcceleration)
{
    /// <summary>The newest tuning format this sim reads.</summary>
    public const int FormatVersion = 1;

    /// <summary>Reads a tuning file's text. Problems name the field they concern.</summary>
    public static Parsed<Tuning> Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            using var document = JsonDocument.Parse(json);
            return Read(document.RootElement);
        }
        catch (JsonException exception)
        {
            return Refused(Invariant($"not valid JSON at line {(exception.LineNumber ?? 0) + 1}: {exception.Message}"));
        }
    }

    private static Parsed<Tuning> Read(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return Refused("the file is not a JSON object");
        }

        var problems = new List<string>();
        var version = Integer(root, "version", problems);
        if (version > FormatVersion)
        {
            return Refused(Invariant($"version: {version} is from a newer game; this one reads up to {FormatVersion}"));
        }

        if (version < 1 && problems.Count == 0)
        {
            problems.Add(Invariant($"version: {version} is not a tuning format version"));
        }

        var graceWindowTicks = Integer(root, "graceWindowTicks", problems);
        if (graceWindowTicks < 0)
        {
            problems.Add("graceWindowTicks: must not be negative");
        }

        var zones = Section(root, "zones", problems);
        var bumpBelow = Distance(zones, "zones.bumpBelowMeters", problems);
        var tightBelow = Distance(zones, "zones.tightBelowMeters", problems);
        var looseBelow = Distance(zones, "zones.looseBelowMeters", problems);
        if (!(bumpBelow < tightBelow && tightBelow < looseBelow) && problems.Count == 0)
        {
            problems.Add("zones: bumpBelowMeters, tightBelowMeters and looseBelowMeters must increase in that order");
        }

        var walking = Section(root, "walking", problems);
        var speed = Distance(walking, "walking.speedMetersPerSecond", problems);
        var acceleration = Distance(walking, "walking.accelerationMetersPerSecondSquared", problems);

        return problems.Count > 0
            ? new(null, problems)
            : new(new Tuning(graceWindowTicks, new ZoneLimits(bumpBelow, tightBelow, looseBelow), speed, acceleration), []);
    }

    private static Parsed<Tuning> Refused(string problem) => new(null, [problem]);

    private static JsonElement? Section(JsonElement parent, string name, List<string> problems)
    {
        if (parent.TryGetProperty(name, out var section) && section.ValueKind == JsonValueKind.Object)
        {
            return section;
        }

        problems.Add($"{name}: missing, or not an object");
        return null;
    }

    private static int Integer(JsonElement parent, string name, List<string> problems)
    {
        if (parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var integer))
        {
            return integer;
        }

        problems.Add($"{name}: missing, or not a whole number");
        return 0;
    }

    /// <summary>A positive, finite number at <paramref name="path"/>; the last part of the path names it in its section.</summary>
    private static float Distance(JsonElement? section, string path, List<string> problems)
    {
        if (section is not { } parent)
        {
            return 0f;
        }

        var name = path[(path.LastIndexOf('.') + 1)..];
        if (parent.TryGetProperty(name, out var value)
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

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
