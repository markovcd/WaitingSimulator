using System.Numerics;
using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class HudTests
{
    private const int GraceWindowTicks = 90;

    private static readonly AgentId Player = new(1);

    private static readonly Tuning Tuning = SimTests.Snappy with { GraceWindowTicks = GraceWindowTicks };

    [Fact]
    public void Hard_shows_nothing_even_with_a_gap_open()
    {
        Assert.Equal("", Hud.Text(Line(gapOpenedAt: 100, tick: 130), Player, Tuning, Difficulty.Hard));
    }

    [Fact]
    public void Normal_shows_only_that_a_gap_has_opened()
    {
        Assert.Equal("GAP", Hud.Text(Line(gapOpenedAt: 100, tick: 130), Player, Tuning, Difficulty.Normal));
    }

    [Fact]
    public void Normal_shows_nothing_while_the_player_keeps_up()
    {
        Assert.Equal("", Hud.Text(Line(gapOpenedAt: null, tick: 130), Player, Tuning, Difficulty.Normal));
    }

    [Fact]
    public void Easy_shows_the_patience_of_the_person_behind_and_the_grace_left_on_the_gap()
    {
        var text = Hud.Text(Line(gapOpenedAt: 100, tick: 130), Player, Tuning, Difficulty.Easy);

        Assert.Equal("PATIENCE OF PERSON BEHIND: 62%\nGAP: 1.00 S OF GRACE LEFT", text);
    }

    [Fact]
    public void Easy_says_when_the_grace_on_the_gap_has_run_out()
    {
        var text = Hud.Text(Line(gapOpenedAt: 100, tick: 100 + GraceWindowTicks), Player, Tuning, Difficulty.Easy);

        Assert.EndsWith("GAP: GRACE RAN OUT", text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_dropped_ticket_shows_on_easy_and_normal_and_not_on_hard()
    {
        var snapshot = Line(gapOpenedAt: null, tick: 130);
        var dropped = snapshot with { Agents = [.. snapshot.Agents.Select(agent => agent.Id == Player ? agent with { TicketAt = new(1, 1) } : agent)] };

        Assert.EndsWith("TICKET DROPPED", Hud.Text(dropped, Player, Tuning, Difficulty.Easy), StringComparison.Ordinal);
        Assert.Equal("TICKET", Hud.Text(dropped, Player, Tuning, Difficulty.Normal));
        Assert.Equal("", Hud.Text(dropped, Player, Tuning, Difficulty.Hard));
    }

    [Fact]
    public void Small_talk_from_behind_shows_on_easy_with_the_key_to_listen_and_on_normal_as_a_word()
    {
        var snapshot = Line(gapOpenedAt: null, tick: 130);
        var talking = snapshot with { Agents = [.. snapshot.Agents.Select(agent => agent.Id == Player ? agent with { SmallTalkEndsAt = 300 } : agent)] };

        Assert.EndsWith("SMALL TALK: LISTEN (Q)", Hud.Text(talking, Player, Tuning, Difficulty.Easy), StringComparison.Ordinal);
        Assert.Equal("TALK", Hud.Text(talking, Player, Tuning, Difficulty.Normal));
        Assert.Equal("", Hud.Text(talking, Player, Tuning, Difficulty.Hard));
    }

    [Fact]
    public void Staring_back_shows_on_easy_only()
    {
        var snapshot = Line(gapOpenedAt: null, tick: 130);
        var staring = snapshot with { Agents = [.. snapshot.Agents.Select(agent => agent.Id == Player ? agent with { Staring = true } : agent)] };

        Assert.EndsWith("STARING AT PERSON BEHIND", Hud.Text(staring, Player, Tuning, Difficulty.Easy), StringComparison.Ordinal);
        Assert.Equal("", Hud.Text(staring, Player, Tuning, Difficulty.Normal));
    }

    [Fact]
    public void Easy_with_no_one_behind_and_no_gap_shows_nothing()
    {
        var snapshot = Line(gapOpenedAt: null, tick: 130);

        Assert.Equal("", Hud.Text(snapshot with { Agents = [.. snapshot.Agents.Take(2)] }, Player, Tuning, Difficulty.Easy));
    }

    [Theory]
    [InlineData(new string[0], "Normal")]
    [InlineData(new[] { "--difficulty", "easy" }, "Easy")]
    [InlineData(new[] { "--smoke", "--difficulty", "HARD" }, "Hard")]
    public void The_difficulty_comes_from_the_command_line_and_is_normal_without_one(string[] arguments, string difficulty)
    {
        var problems = new List<string>();

        Assert.Equal(Enum.Parse<Difficulty>(difficulty), Difficulties.From(arguments, Difficulty.Normal, problems));
        Assert.Empty(problems);
    }

    [Theory]
    [InlineData("--difficulty", "brutal")]
    [InlineData("--difficulty")]
    public void A_difficulty_the_game_does_not_have_is_reported_and_plays_normal(params string[] arguments)
    {
        var problems = new List<string>();

        Assert.Equal(Difficulty.Normal, Difficulties.From(arguments, Difficulty.Normal, problems));
        Assert.Equal(["--difficulty: takes easy, normal or hard"], problems);
    }

    [Fact]
    public void Without_a_difficulty_on_the_command_line_the_saved_one_plays()
    {
        var problems = new List<string>();

        Assert.Equal(Difficulty.Hard, Difficulties.From(["--smoke"], Difficulty.Hard, problems));
        Assert.Equal(Difficulty.Easy, Difficulties.From(["--difficulty", "easy"], Difficulty.Hard, problems));
        Assert.Empty(problems);
    }

    [Fact]
    public void Cycling_the_difficulty_goes_through_every_one_and_back()
    {
        var seen = new List<Difficulty>();
        var difficulty = Difficulty.Normal;
        do
        {
            seen.Add(difficulty);
            difficulty = Difficulties.Next(difficulty);
        }
        while (difficulty != Difficulty.Normal);

        Assert.Equal(Enum.GetValues<Difficulty>().Order(), seen.Order());
    }

    /// <summary>A person ahead, the player, and a person behind at 61.7 patience; the player's gap opened at <paramref name="gapOpenedAt"/>.</summary>
    private static Snapshot Line(long? gapOpenedAt, long tick) => new(tick,
    [
        Agent(new AgentId(0), ahead: null, zone: null, gapOpenedAt: null, patience: null),
        Agent(Player, ahead: new AgentId(0), zone: gapOpenedAt is null ? Zone.Tight : Zone.Gap, gapOpenedAt, patience: null),
        Agent(new AgentId(2), ahead: Player, zone: Zone.Tight, gapOpenedAt: null, patience: 61.7f),
    ]);

    private static AgentSnapshot Agent(AgentId id, AgentId? ahead, Zone? zone, long? gapOpenedAt, float? patience) =>
        new(id, Vector2.Zero, Vector2.Zero, true, ahead, null, zone, gapOpenedAt, patience is null ? null : "patient", patience, null,
            LineStep.Standing, null, false, default, null, null);
}
