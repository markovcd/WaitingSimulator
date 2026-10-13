using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>Which cues a frame's events call for. A stage getting better has no cue of its own; the pose eases back.</summary>
internal static class Cues
{
    /// <summary>The cues for <paramref name="events"/>, in event order; <paramref name="after"/> is the state they left.</summary>
    public static List<Cue> From(IReadOnlyList<SimEvent> events, Snapshot after)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(after);
        var cues = new List<Cue>();
        foreach (var happened in events)
        {
            var agent = after.Agents[happened.Agent.Value];
            Cue? cue = happened switch
            {
                ZoneChanged { To: Zone.Bump } when agent.Ahead is { } ahead =>
                    new Cue(ahead, CueKind.Bumped, after.Agents[ahead.Value].Stage),
                StageChanged changed when changed.To > changed.From => new Cue(changed.Agent, CueKind.Escalated, changed.To),
                PatienceRanOut ranOut => new Cue(ranOut.Agent, CueKind.RanOut, agent.Stage),
                TicketDropped dropped => new Cue(dropped.Agent, CueKind.TicketDropped, null),
                SmallTalkStarted talk => new Cue(talk.Agent, CueKind.SmallTalk, null),
                _ => null,
            };
            if (cue is { } found)
            {
                cues.Add(found);
            }
        }

        return cues;
    }
}
