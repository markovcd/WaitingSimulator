namespace WaitingSim;

/// <summary>
/// Small talk from the person behind: now and then, whoever stands behind the listener starts talking to them, and the
/// listener has to glance back for enough of it. Listened to, the talker gets a little patience back; ignored, they take
/// a hit. Only someone with patience talks, and not once they are annoyed. One talk at a time.
/// </summary>
internal sealed class SmallTalk(SmallTalkTuning talk, Line line, Agent listener)
{
    public Agent Listener { get; } = listener;

    /// <summary>The tick the next talk is due, while none is under way.</summary>
    public long NextAt { get; private set; }

    /// <summary>Who is talking, while a talk is under way.</summary>
    public Agent? Talker { get; private set; }

    public long StartedAt { get; private set; }

    /// <summary>How many ticks of the talk under way the listener spent glancing back.</summary>
    public int ListenedTicks { get; private set; }

    /// <summary>Draws when the next talk is due, from <paramref name="tick"/>.</summary>
    public void Schedule(long tick, Rng rng) => NextAt = tick + talk.IntervalTicks.Draw(rng);

    /// <summary>
    /// Counts this tick toward listening if the listener is <paramref name="glancingBack"/>, ends a talk that is over
    /// with its patience change in <paramref name="hits"/>, or starts one that is due.
    /// </summary>
    public void Update(long tick, Rng rng, bool glancingBack, float[] hits, List<SimEvent> events)
    {
        if (Talker is { } talker)
        {
            if (glancingBack)
            {
                ListenedTicks++;
            }

            if (tick < Listener.SmallTalkEndsAt)
            {
                return;
            }

            var listened = ListenedTicks >= talk.ListenShare * (Listener.SmallTalkEndsAt!.Value - StartedAt);
            hits[talker.Id.Value] += listened ? -talk.ListenedRecovery : talk.IgnoredHit;
            events.Add(new SmallTalkEnded(tick, talker.Id, Listener.Id, listened));
            Talker = null;
            Listener.SmallTalkEndsAt = null;
            Schedule(tick, rng);
            return;
        }

        if (tick >= NextAt
            && Listener is { InLane: true, Refused: false, ReachedCounterAt: null, KickedOutAt: null, TurnedAwayAt: null }
            && line.Behind(Listener) is { Trait: not null, Stage: < Stage.Annoyed } behind)
        {
            Talker = behind;
            StartedAt = tick;
            ListenedTicks = 0;
            Listener.SmallTalkEndsAt = tick + talk.TalkTicks.Draw(rng);
            events.Add(new SmallTalkStarted(tick, behind.Id, Listener.Id));
        }
    }
}
