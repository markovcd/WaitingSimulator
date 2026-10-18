using System.Numerics;
using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class CrowdClientTests : IDisposable
{
    private static readonly string Data = Path.Combine(AppContext.BaseDirectory, "data");

    private readonly string folder = Directory.CreateTempSubdirectory("levels-").FullName;

    public void Dispose() => Directory.Delete(folder, recursive: true);

    [Fact]
    public void The_shipped_deli_counter_loads_as_a_crowd_level()
    {
        var loaded = CrowdData.Load(Data, "deli-counter");

        Assert.Empty(loaded.Problems);
        Assert.True(Levels.IsCrowd(Data, "deli-counter"));
        Assert.False(Levels.IsCrowd(Data, Levels.Default));
    }

    [Fact]
    public void A_crowd_level_without_the_crowd_tuning_is_reported()
    {
        Directory.CreateDirectory(Path.Combine(folder, "levels"));
        File.Copy(Path.Combine(Data, "traits.json"), Path.Combine(folder, "traits.json"));
        File.Copy(Path.Combine(Data, "levels", "deli-counter.json"), Path.Combine(folder, "levels", "deli-counter.json"));
        var tuning = File.ReadAllText(Path.Combine(Data, "tuning.json"));
        File.WriteAllText(Path.Combine(folder, "tuning.json"), tuning[..tuning.IndexOf("  \"crowd\"", StringComparison.Ordinal)] + tuning[tuning.IndexOf("  \"patience\"", StringComparison.Ordinal)..]);

        Assert.Equal([$"{Path.Combine(folder, "tuning.json")}: a crowd level needs the crowd and bodies sections"], CrowdData.Load(folder, "deli-counter").Problems);
    }

    [Theory]
    [InlineData(new string[0], "post-office", 0)]
    [InlineData(new[] { "--smoke", "--level", "deli-counter" }, "deli-counter", 0)]
    [InlineData(new[] { "--level", "../secrets" }, "post-office", 1)]
    [InlineData(new[] { "--level" }, "post-office", 1)]
    public void The_command_line_names_the_level_and_a_bad_name_plays_the_default(string[] args, string level, int problemCount)
    {
        var problems = new List<string>();

        Assert.Equal(level, Levels.From(args, problems));
        Assert.Equal(problemCount, problems.Count);
    }

    [Fact]
    public void The_next_level_follows_by_name_and_wraps_to_the_first()
    {
        Directory.CreateDirectory(Path.Combine(folder, "levels"));
        foreach (var name in new[] { "bank", "deli-counter", "post-office" })
        {
            File.WriteAllText(Path.Combine(folder, "levels", name + ".json"), "{}");
        }

        Assert.Equal("deli-counter", Levels.Next(folder, "bank"));
        Assert.Equal("post-office", Levels.Next(folder, "deli-counter"));
        Assert.Equal("bank", Levels.Next(folder, "post-office"));
    }

    [Fact]
    public void A_call_out_is_one_press_however_many_ticks_its_frame_steps()
    {
        var crowd = new Crowd(CrowdTests.Crowding, CrowdTests.Bar, exit: new(5, 5), seed: 1);
        var player = crowd.Spawn(new(-1.5f, 0.18f));
        crowd.Spawn(new(0f, 0.18f), new Trait("plain", 1, 0, 1, new HashSet<Stage> { Stage.Calm }));
        var driver = new Driver(crowd, player);
        driver.Advance(1d / 60, Command.Idle);

        var events = driver.Advance(1d / 16, new Command(Vector2.Zero, CallOut: true)).ToList();
        events.AddRange(driver.Advance(1d / 16, Command.Idle));

        Assert.Single(events.OfType<CalledOut>());
    }

    [Fact]
    public void The_crowd_overlay_shows_whom_the_clerk_asks_and_every_agent_s_arrival_and_patience()
    {
        var game = CrowdData.Load(Data, "deli-counter").Value!;

        var text = CrowdOverlay.Text(game.Level.Start(game.Tuning).Snapshot(), game.Level);

        Assert.StartsWith("TICK 0  SEED 1  ASKING -  SERVING -\n#0 patient", text, StringComparison.Ordinal);
        Assert.Contains("#7 player", text, StringComparison.Ordinal);
        Assert.Contains("arrived    0.00 s  step standing", text, StringComparison.Ordinal);
        Assert.Contains("patience 100.0  stage calm", text, StringComparison.Ordinal);
    }

    [Fact]
    public void How_to_play_in_a_crowd_names_its_keys_and_shows_at_the_start()
    {
        var game = CrowdData.Load(Data, "deli-counter").Value!;

        Assert.Contains("WAVE", HowToPlay.CrowdText, StringComparison.Ordinal);
        Assert.Contains("I WAS HERE FIRST", HowToPlay.CrowdText, StringComparison.Ordinal);
        Assert.True(HowToPlay.Shown(game.Level.Start(game.Tuning).Snapshot(), game.Level));
    }

    [Fact]
    public void A_player_moved_out_of_a_crowd_sees_staff_instead_of_the_clerk()
    {
        var player = new AgentSnapshot(new AgentId(0), Vector2.Zero, Vector2.Zero, false, null, null, null, null, null, null, null, LineStep.Exiting, null, false, new Tally(0, 0, 0, 0, 0, 0, FalseClaims: 2), null, 30 * TickRate.PerSecond);

        var text = ResultScreen.CrowdText(player);

        Assert.StartsWith("STAFF: \"I'M GOING TO HAVE TO ASK YOU TO LEAVE.\"\n\nMOVED OUT\n", text, StringComparison.Ordinal);
        Assert.Contains("TIME IN THE CROWD       00:30", text, StringComparison.Ordinal);
        Assert.Contains("FALSE \"I WAS HERE FIRST\"    2", text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_player_served_in_a_crowd_hears_the_clerk_on_their_worst_mistake()
    {
        var player = new AgentSnapshot(new AgentId(0), Vector2.Zero, Vector2.Zero, false, null, null, null, null, null, null, null, LineStep.BeingServed, null, false, new Tally(0, 0, 0, 0, 0, 0, ServedAhead: 1), 50 * TickRate.PerSecond, null);

        Assert.StartsWith($"CLERK: \"{ClerkLines.For(Mistake.ServedAhead)}\"\n\nSERVED\n", ResultScreen.CrowdText(player), StringComparison.Ordinal);
    }
}
