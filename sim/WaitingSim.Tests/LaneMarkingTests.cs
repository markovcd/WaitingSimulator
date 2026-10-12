using System.Numerics;
using WaitingSimulator.Game;

namespace WaitingSim.Tests;

public sealed class LaneMarkingTests
{
    public static TheoryData<string> Lanes() => ["straight", "bent"];

    [Theory]
    [MemberData(nameof(Lanes))]
    public void The_marking_covers_exactly_where_the_sim_counts_an_agent_in_the_lane(string name)
    {
        var lane = name == "straight"
            ? new Lane([new(0, 0), new(0, 14)], halfWidth: 0.5f)
            : new Lane([new(0, 0), new(0, 4), new(3, 7)], halfWidth: 0.6f);
        var marking = LaneMarking.Of(lane);
        var wrong = new List<string>();

        for (var x = -2f; x <= 5f; x += 0.05f)
        {
            for (var y = -2f; y <= 16f; y += 0.05f)
            {
                var point = new Vector2(x, y);
                var off = lane.Measure(point).Off;
                if (MathF.Abs(off - lane.HalfWidth) > 0.01f && lane.Holds(lane.Measure(point)) != Covered(marking, point))
                {
                    wrong.Add($"({x:0.00}, {y:0.00})");
                }
            }
        }

        Assert.True(wrong.Count == 0, $"{wrong.Count} points disagree, such as {string.Join(", ", wrong.Take(5))}");
    }

    [Fact]
    public void The_lane_shows_the_player_in_the_line_with_a_place_held_or_out_of_it()
    {
        Assert.Equal(LaneState.InLine, LaneMarking.StateOf(Player(inLane: true)));
        Assert.Equal(LaneState.PlaceHeld, LaneMarking.StateOf(Player(inLane: false) with { SpotHeldUntil = 300 }));
        Assert.Equal(LaneState.OutOfLine, LaneMarking.StateOf(Player(inLane: false)));
        Assert.Equal(LaneState.OutOfLine, LaneMarking.StateOf(Player(inLane: true) with { Refused = true }));
    }

    [Fact]
    public void Every_state_has_its_own_fill()
    {
        var fills = Enum.GetValues<LaneState>().Select(LaneMarking.Fill).ToList();

        Assert.Equal(fills.Count, fills.Distinct().Count());
    }

    [Fact]
    public void The_held_place_is_marked_where_it_is_and_shrinks_as_the_hold_runs_out()
    {
        var lane = new Lane([new(0, 0), new(0, 14)], halfWidth: 0.5f);
        var holding = Player(inLane: false) with { SpotHeldUntil = 400, HeldAlong = 6.4f };

        var fresh = LaneMarking.Held(holding, lane, tick: 100, spotHoldTicks: 300)!.Value;
        var half = LaneMarking.Held(holding, lane, tick: 250, spotHoldTicks: 300)!.Value;

        Assert.Equal(new Vector2(0, 6.4f), fresh.Point);
        Assert.Equal(new Vector2(0, -1), fresh.TowardFront);
        Assert.Equal(1f, fresh.Left, 0.0001f);
        Assert.Equal(0.5f, half.Left, 0.0001f);
    }

    [Fact]
    public void No_place_is_marked_while_the_player_is_in_the_line_or_has_none_held()
    {
        var lane = new Lane([new(0, 0), new(0, 14)], halfWidth: 0.5f);

        Assert.Null(LaneMarking.Held(Player(inLane: true), lane, tick: 100, spotHoldTicks: 300));
        Assert.Null(LaneMarking.Held(Player(inLane: false), lane, tick: 100, spotHoldTicks: 300));
    }

    private static AgentSnapshot Player(bool inLane) =>
        new(new AgentId(0), Vector2.Zero, Vector2.Zero, inLane, null, null, null, null, null, null, null, LineStep.Standing, null, false, default, null, null);

    private static bool Covered(LaneMarking marking, Vector2 point) =>
        marking.Caps.Any(cap => Vector2.Distance(cap, point) <= marking.HalfWidth)
        || marking.Strips.Any(strip =>
        {
            var along = Vector2.Normalize(strip.To - strip.From);
            var t = Vector2.Dot(point - strip.From, along);
            var across = MathF.Abs((along.X * (point.Y - strip.From.Y)) - (along.Y * (point.X - strip.From.X)));
            return t >= 0f && t <= Vector2.Distance(strip.From, strip.To) && across <= marking.HalfWidth;
        });
}
