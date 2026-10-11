namespace WaitingSim.Tests;

public sealed class TuningTests
{
    private const string Valid = """
        {
          "version": 1,
          "graceWindowTicks": 90,
          "zones": { "bumpBelowMeters": 0.4, "tightBelowMeters": 0.9, "looseBelowMeters": 1.5 },
          "walking": { "speedMetersPerSecond": 1.2, "accelerationMetersPerSecondSquared": 3.0 },
          "patience": {
            "stages": { "restlessBelow": 70, "annoyedBelow": 50, "confrontingBelow": 30, "fedUpBelow": 15, "recoverMargin": 5 },
            "gapDrainPerSecond": 4.0,
            "tightRecoveryPerSecond": 0.5,
            "bumpHit": 5
          }
        }
        """;

    private const string Ripple = """
        "ripple": {
            "serviceTicks": { "min": 120, "max": 360 },
            "reactionTicks": { "min": 15, "max": 60 },
            "spacingMeters": { "min": 0.6, "max": 1.1 },
            "noticeMeters": 0.2,
            "leaveMeters": 1.0
          },
          "patience": {
        """;

    private static readonly string WithRipple = Valid.Replace("\"patience\": {", Ripple, StringComparison.Ordinal);

    [Fact]
    public void The_shipped_tuning_loads_without_problems()
    {
        var parsed = Tuning.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "tuning.json")));

        Assert.Empty(parsed.Problems);
        Assert.NotNull(parsed.Value);
    }

    [Fact]
    public void A_tuning_file_reads_into_its_values()
    {
        var tuning = Tuning.Parse(Valid).Value;

        Assert.Equal(
            new Tuning(90, new ZoneLimits(0.4f, 0.9f, 1.5f), 1.2f, 3.0f, new PatienceTuning(new StageLimits(70, 50, 30, 15, 5), 4.0f, 0.5f, 5f)),
            tuning);
    }

    [Fact]
    public void A_tuning_file_without_a_ripple_has_a_line_that_moves_only_on_command()
    {
        Assert.Null(Tuning.Parse(Valid).Value!.Ripple);
    }

    [Fact]
    public void A_ripple_reads_into_its_values()
    {
        var ripple = Tuning.Parse(WithRipple).Value!.Ripple;

        Assert.Equal(new RippleTuning(new(120, 360), new(15, 60), new(0.6f, 1.1f), 0.2f, 1.0f), ripple);
    }

    [Theory]
    [InlineData("\"min\": 120", "\"min\": -1", "ripple.serviceTicks: min must be from 0 up, and max from min up to 216000")]
    [InlineData("\"max\": 60", "\"max\": 10", "ripple.reactionTicks: min must be from 0 up, and max from min up to 216000")]
    [InlineData("\"max\": 360", "\"max\": 300000", "ripple.serviceTicks: min must be from 0 up, and max from min up to 216000")]
    [InlineData("\"min\": 0.6", "\"min\": 0.3", "ripple.spacingMeters: min must be from zones.bumpBelowMeters up, and max from min up to below zones.looseBelowMeters")]
    [InlineData("\"max\": 1.1", "\"max\": 1.5", "ripple.spacingMeters: min must be from zones.bumpBelowMeters up, and max from min up to below zones.looseBelowMeters")]
    [InlineData("\"noticeMeters\": 0.2", "\"noticeMeters\": 0", "ripple.noticeMeters: missing, or not a positive number")]
    [InlineData("\"leaveMeters\": 1.0", "\"leaveMeters\": \"far\"", "ripple.leaveMeters: missing, or not a positive number")]
    [InlineData("\"reactionTicks\": { \"min\": 15, \"max\": 60 },", "", "ripple.reactionTicks: missing, or not an object")]
    public void A_bad_ripple_field_is_reported_by_name(string field, string replacement, string problem)
    {
        var parsed = Tuning.Parse(WithRipple.Replace(field, replacement, StringComparison.Ordinal));

        Assert.Null(parsed.Value);
        Assert.Equal([problem], parsed.Problems);
    }

    [Theory]
    [InlineData("\"graceWindowTicks\": 90,", "", "graceWindowTicks: missing, or not a whole number")]
    [InlineData("\"graceWindowTicks\": 90,", "\"graceWindowTicks\": -1,", "graceWindowTicks: must not be negative")]
    [InlineData("\"bumpBelowMeters\": 0.4", "\"bumpBelowMeters\": \"near\"", "zones.bumpBelowMeters: missing, or not a positive number")]
    [InlineData("\"tightBelowMeters\": 0.9", "\"tightBelowMeters\": 2.0", "zones: bumpBelowMeters, tightBelowMeters and looseBelowMeters must increase in that order")]
    [InlineData("\"speedMetersPerSecond\": 1.2", "\"speedMetersPerSecond\": 0", "walking.speedMetersPerSecond: missing, or not a positive number")]
    [InlineData("\"version\": 1", "\"version\": 0", "version: 0 is not a tuning format version")]
    [InlineData("\"annoyedBelow\": 50", "\"annoyedBelow\": 80", "patience.stages: fedUpBelow, confrontingBelow, annoyedBelow and restlessBelow must increase in that order, below 100")]
    [InlineData("\"bumpHit\": 5", "\"bumpHit\": -5", "patience.bumpHit: missing, or not a positive number")]
    public void A_bad_field_is_reported_by_name(string field, string replacement, string problem)
    {
        var parsed = Tuning.Parse(Valid.Replace(field, replacement, StringComparison.Ordinal));

        Assert.Null(parsed.Value);
        Assert.Equal([problem], parsed.Problems);
    }

    [Fact]
    public void A_file_from_a_newer_version_is_refused_whole()
    {
        var parsed = Tuning.Parse(Valid.Replace("\"version\": 1", "\"version\": 2", StringComparison.Ordinal));

        Assert.Null(parsed.Value);
        Assert.Equal(["version: 2 is from a newer game; this one reads up to 1"], parsed.Problems);
    }

    [Fact]
    public void Text_that_is_not_JSON_is_reported_with_its_line()
    {
        var parsed = Tuning.Parse("{\n  \"version\": 1,\n  oops\n}");

        Assert.Null(parsed.Value);
        Assert.StartsWith("not valid JSON at line 3", Assert.Single(parsed.Problems), StringComparison.Ordinal);
    }
}
