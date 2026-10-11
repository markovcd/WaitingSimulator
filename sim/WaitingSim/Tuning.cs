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
    public static Parsed<Tuning> Parse(string json) => JsonFile.Read(json, "tuning", FormatVersion, Read);

    private static Tuning Read(JsonElement root, List<string> problems)
    {
        var graceWindowTicks = JsonFile.Integer(root, "graceWindowTicks", problems);
        if (graceWindowTicks < 0)
        {
            problems.Add("graceWindowTicks: must not be negative");
        }

        var zones = JsonFile.Section(root, "zones", problems);
        var bumpBelow = JsonFile.Positive(zones, "zones.bumpBelowMeters", problems);
        var tightBelow = JsonFile.Positive(zones, "zones.tightBelowMeters", problems);
        var looseBelow = JsonFile.Positive(zones, "zones.looseBelowMeters", problems);
        if (!(bumpBelow < tightBelow && tightBelow < looseBelow) && problems.Count == 0)
        {
            problems.Add("zones: bumpBelowMeters, tightBelowMeters and looseBelowMeters must increase in that order");
        }

        var walking = JsonFile.Section(root, "walking", problems);
        var speed = JsonFile.Positive(walking, "walking.speedMetersPerSecond", problems);
        var acceleration = JsonFile.Positive(walking, "walking.accelerationMetersPerSecondSquared", problems);

        return new(graceWindowTicks, new ZoneLimits(bumpBelow, tightBelow, looseBelow), speed, acceleration);
    }
}
