using System.Text.Json;

namespace WaitingSim;

/// <summary>The knobs a designer turns, read from <c>data/tuning.json</c>.</summary>
/// <param name="GraceWindowTicks">How many ticks a gap can stay open before the grace window expires.</param>
/// <param name="Zones">Where each spacing zone ends.</param>
/// <param name="WalkSpeed">Top walking speed, in meters per second.</param>
/// <param name="WalkAcceleration">How quickly walking speeds up, slows down and turns, in meters per second squared.</param>
/// <param name="Patience">How patience drains, recovers and shows.</param>
/// <param name="Ripple">How the line moves on its own; without it, an agent given no command stands still.</param>
public sealed record Tuning(int GraceWindowTicks, ZoneLimits Zones, float WalkSpeed, float WalkAcceleration, PatienceTuning Patience, RippleTuning? Ripple = null)
{
    /// <summary>The newest tuning format this sim reads.</summary>
    public const int FormatVersion = 1;

    /// <summary>The longest duration a ripple draws: an hour.</summary>
    public const int MaxRippleTicks = 60 * 60 * TickRate.PerSecond;

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

        var limits = new ZoneLimits(bumpBelow, tightBelow, looseBelow);
        return new(graceWindowTicks, limits, speed, acceleration, ReadPatience(root, problems), ReadRipple(root, limits, problems));
    }

    /// <summary>Reads the <c>ripple</c> object, which a tuning file may leave out.</summary>
    private static RippleTuning? ReadRipple(JsonElement root, ZoneLimits zones, List<string> problems)
    {
        if (!root.TryGetProperty("ripple", out _))
        {
            return null;
        }

        var ripple = JsonFile.Section(root, "ripple", problems);
        var service = ReadTicks(ripple, "ripple.serviceTicks", problems);
        var reaction = ReadTicks(ripple, "ripple.reactionTicks", problems);
        var spacingSection = JsonFile.Section(ripple, "ripple.spacingMeters", problems);
        var spacing = new MeterRange(
            JsonFile.Positive(spacingSection, "ripple.spacingMeters.min", problems),
            JsonFile.Positive(spacingSection, "ripple.spacingMeters.max", problems));
        if (!(zones.BumpBelow <= spacing.Min && spacing.Min <= spacing.Max && spacing.Max < zones.LooseBelow) && problems.Count == 0)
        {
            problems.Add("ripple.spacingMeters: min must be from zones.bumpBelowMeters up, and max from min up to below zones.looseBelowMeters");
        }

        return new(
            service,
            reaction,
            spacing,
            JsonFile.Positive(ripple, "ripple.noticeMeters", problems),
            JsonFile.Positive(ripple, "ripple.leaveMeters", problems));
    }

    private static TickRange ReadTicks(JsonElement? parent, string path, List<string> problems)
    {
        var section = JsonFile.Section(parent, path, problems);
        var range = new TickRange(JsonFile.Integer(section, $"{path}.min", problems), JsonFile.Integer(section, $"{path}.max", problems));
        if (!(range.Min >= 0 && range.Min <= range.Max && range.Max <= MaxRippleTicks) && problems.Count == 0)
        {
            problems.Add(JsonFile.Invariant($"{path}: min must be from 0 up, and max from min up to {MaxRippleTicks}"));
        }

        return range;
    }

    private static PatienceTuning ReadPatience(JsonElement root, List<string> problems)
    {
        var patience = JsonFile.Section(root, "patience", problems);
        var stages = JsonFile.Section(patience, "patience.stages", problems);
        var restlessBelow = JsonFile.Positive(stages, "patience.stages.restlessBelow", problems);
        var annoyedBelow = JsonFile.Positive(stages, "patience.stages.annoyedBelow", problems);
        var confrontingBelow = JsonFile.Positive(stages, "patience.stages.confrontingBelow", problems);
        var fedUpBelow = JsonFile.Positive(stages, "patience.stages.fedUpBelow", problems);
        var recoverMargin = JsonFile.Positive(stages, "patience.stages.recoverMargin", problems);
        if (!(fedUpBelow < confrontingBelow && confrontingBelow < annoyedBelow && annoyedBelow < restlessBelow && restlessBelow < PatienceTuning.Full)
            && problems.Count == 0)
        {
            problems.Add(JsonFile.Invariant($"patience.stages: fedUpBelow, confrontingBelow, annoyedBelow and restlessBelow must increase in that order, below {PatienceTuning.Full}"));
        }

        return new(
            new StageLimits(restlessBelow, annoyedBelow, confrontingBelow, fedUpBelow, recoverMargin),
            JsonFile.Positive(patience, "patience.gapDrainPerSecond", problems),
            JsonFile.Positive(patience, "patience.tightRecoveryPerSecond", problems),
            JsonFile.Positive(patience, "patience.bumpHit", problems));
    }
}
