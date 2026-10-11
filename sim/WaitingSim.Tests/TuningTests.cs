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

    private const string StepOut = """
        "stepOut": {
            "spotHoldTicks": 300,
            "cutIn": { "pastGraceHit": 10, "gapHit": 20, "looseHit": 35, "missHit": 15 }
          },
          "patience": {
        """;

    private static readonly string WithStepOut = Valid.Replace("\"patience\": {", StepOut, StringComparison.Ordinal);

    private static readonly string WithBodies = Valid.Replace(
        "\"patience\": {", "\"bodies\": { \"radiusMeters\": 0.18, \"rebound\": 0.5 },\n  \"patience\": {", StringComparison.Ordinal);

    [Fact]
    public void The_shipped_tuning_loads_without_problems()
    {
        var parsed = Tuning.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "tuning.json")));

        Assert.Empty(parsed.Problems);
        Assert.NotNull(parsed.Value);
    }

    [Fact]
    public void The_shipped_tuning_keeps_agents_out_of_each_other()
    {
        var tuning = Tuning.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "tuning.json"))).Value!;

        Assert.NotNull(tuning.Bodies);
    }

    [Fact]
    public void Bodies_read_into_their_values_and_a_tuning_file_may_leave_them_out()
    {
        Assert.Equal(new BodyTuning(0.18f, 0.5f), Tuning.Parse(WithBodies).Value!.Bodies);
        Assert.Null(Tuning.Parse(Valid).Value!.Bodies);
    }

    [Theory]
    [InlineData("\"radiusMeters\": 0.18", "\"radiusMeters\": 0.2", "bodies.radiusMeters: two radii must be below zones.bumpBelowMeters, or walking into someone is never a bump")]
    [InlineData("\"radiusMeters\": 0.18", "\"radiusMeters\": 0", "bodies.radiusMeters: missing, or not a positive number")]
    [InlineData("\"rebound\": 0.5", "\"rebound\": 1.5", "bodies.rebound: must be at most 1")]
    public void A_bad_bodies_field_is_reported_by_name(string field, string replacement, string problem)
    {
        var parsed = Tuning.Parse(WithBodies.Replace(field, replacement, StringComparison.Ordinal));

        Assert.Null(parsed.Value);
        Assert.Equal([problem], parsed.Problems);
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

    [Fact]
    public void A_cutter_reads_into_its_values_and_a_tuning_file_may_leave_it_out()
    {
        var withCutter = Valid.Replace("\"patience\": {", "\"cutter\": { \"afterGraceTicks\": 120, \"blameHit\": 40 },\n  \"patience\": {", StringComparison.Ordinal);

        Assert.Equal(new CutterTuning(120, 40), Tuning.Parse(withCutter).Value!.Cutter);
        Assert.Null(Tuning.Parse(Valid).Value!.Cutter);
    }

    [Theory]
    [InlineData("\"afterGraceTicks\": 120", "\"afterGraceTicks\": -5", "cutter.afterGraceTicks: must be from 0 up to 216000")]
    [InlineData("\"blameHit\": 40", "\"blameHit\": 0", "cutter.blameHit: missing, or not a positive number")]
    public void A_bad_cutter_field_is_reported_by_name(string field, string replacement, string problem)
    {
        var withCutter = Valid.Replace("\"patience\": {", "\"cutter\": { \"afterGraceTicks\": 120, \"blameHit\": 40 },\n  \"patience\": {", StringComparison.Ordinal);

        var parsed = Tuning.Parse(withCutter.Replace(field, replacement, StringComparison.Ordinal));

        Assert.Null(parsed.Value);
        Assert.Equal([problem], parsed.Problems);
    }

    [Fact]
    public void A_tuning_file_without_a_step_out_holds_no_place_and_charges_nothing_for_cutting_in()
    {
        Assert.Null(Tuning.Parse(Valid).Value!.StepOut);
    }

    [Fact]
    public void A_step_out_reads_into_its_values()
    {
        var stepOut = Tuning.Parse(WithStepOut).Value!.StepOut;

        Assert.Equal(new StepOutTuning(300, 10, 20, 35, 15), stepOut);
    }

    [Theory]
    [InlineData("\"spotHoldTicks\": 300", "\"spotHoldTicks\": -1", "stepOut.spotHoldTicks: must be from 0 up to 216000")]
    [InlineData("\"spotHoldTicks\": 300", "\"spotHoldTicks\": 1.5", "stepOut.spotHoldTicks: missing, or not a whole number")]
    [InlineData("\"gapHit\": 20", "\"gapHit\": 40", "stepOut.cutIn: pastGraceHit, gapHit and looseHit must not decrease in that order")]
    [InlineData("\"missHit\": 15", "\"missHit\": 0", "stepOut.cutIn.missHit: missing, or not a positive number")]
    [InlineData("\"cutIn\"", "\"cutOut\"", "stepOut.cutIn: missing, or not an object")]
    public void A_bad_step_out_field_is_reported_by_name(string field, string replacement, string problem)
    {
        var parsed = Tuning.Parse(WithStepOut.Replace(field, replacement, StringComparison.Ordinal));

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
