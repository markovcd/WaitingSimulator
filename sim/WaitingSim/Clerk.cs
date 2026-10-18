namespace WaitingSim;

/// <summary>
/// The clerk behind a crowd's counter. Their glance sweeps along the counter, back and forth, and lands on the agent at
/// the front it passes, the one who arrived first if it passes several; the clerk half-remembers the invisible line, so
/// someone at the front who arrived much earlier is served instead. The clerk asks "Yes?", which is the moment to call
/// out, then serves, then glances on.
/// </summary>
internal sealed class Clerk(CrowdTuning tuning, Counter counter, float radius)
{
    private int sweep = 1;

    /// <summary>Where along the counter the glance is, in meters from its start.</summary>
    public float GlanceAlong { get; private set; } = counter.Length / 2f;

    public Agent? Picked { get; private set; }

    public long PickEndsAt { get; private set; }

    public Agent? Serving { get; private set; }

    public long ServiceEndsAt { get; private set; }

    /// <summary>Whether <paramref name="agent"/> stands at the front of the crowd, against the counter.</summary>
    public bool AtFront(Agent agent)
    {
        var (along, @out) = counter.Measure(agent.Position);
        return along >= -radius && along <= counter.Length + radius && @out <= radius + tuning.FrontMeters;
    }

    /// <summary>Whether <paramref name="first"/> is ahead of <paramref name="second"/> in the invisible line.</summary>
    public static bool Before(Agent first, Agent second) =>
        first.ArrivedAt < second.ArrivedAt || (first.ArrivedAt == second.ArrivedAt && first.Id.Value < second.Id.Value);

    /// <summary>Stops asking or serving <paramref name="agent"/>, when staff move it out.</summary>
    public void Forget(Agent agent)
    {
        if (Picked == agent)
        {
            Picked = null;
        }

        if (Serving == agent)
        {
            Serving = null;
        }
    }

    /// <summary>
    /// Moves the glance on, or asks, hears a call out, serves or finishes serving, among the <paramref name="waiting"/>,
    /// with the agents <paramref name="waving"/> and <paramref name="callingOut"/> this tick.
    /// </summary>
    public void Update(IReadOnlyList<Agent> waiting, IReadOnlySet<Agent> waving, IReadOnlyList<Agent> callingOut, long tick, Rng rng, float[] changes, Blame blame, List<SimEvent> events)
    {
        if (Serving is { } served && tick >= ServiceEndsAt)
        {
            served.Step = LineStep.Exiting;
            Serving = null;
        }

        if (Serving is not null)
        {
            return;
        }

        if (Picked is { } asked)
        {
            Hear(asked, callingOut, waiting, tick, changes, blame, events);
            if (tick >= PickEndsAt)
            {
                Serve(waiting, tick, rng, changes, blame, events);
            }

            return;
        }

        Sweep();
        var front = waiting.Where(AtFront).ToList();
        var seen = front.Where(agent => Math.Abs(Math.Clamp(counter.Measure(agent.Position).Along, 0f, counter.Length) - GlanceAlong)
            <= (waving.Contains(agent) ? tuning.WaveReachMeters : tuning.GlanceReachMeters)).ToList();
        if (seen.Count == 0)
        {
            return;
        }

        var landed = seen.Aggregate((first, second) => Before(first, second) ? first : second);
        var longest = front.Aggregate((first, second) => Before(first, second) ? first : second);
        Pick(landed.ArrivedAt - longest.ArrivedAt >= tuning.ClerkMemoryTicks ? longest : landed, tick, events);
    }

    /// <summary>Adds the glance, whom it asks and whom it serves to <paramref name="hash"/>.</summary>
    public void Hash(StateHash hash) =>
        hash.Add(GlanceAlong).Add((long)sweep).Add((long)(Picked?.Id.Value ?? -1)).Add(PickEndsAt).Add((long)(Serving?.Id.Value ?? -1)).Add(ServiceEndsAt);

    private void Sweep()
    {
        GlanceAlong += sweep * tuning.GlanceMetersPerSecond / TickRate.PerSecond;
        if (GlanceAlong >= counter.Length || GlanceAlong <= 0f)
        {
            GlanceAlong = Math.Clamp(GlanceAlong, 0f, counter.Length);
            sweep = -sweep;
        }
    }

    private void Pick(Agent agent, long tick, List<SimEvent> events)
    {
        Picked = agent;
        PickEndsAt = tick + tuning.PickTicks;
        events.Add(new Picked(tick, agent.Id));
    }

    /// <summary>The first at the front calling out against <paramref name="asked"/>: one who was there first is asked instead; one who was not costs everyone near.</summary>
    private void Hear(Agent asked, IReadOnlyList<Agent> callingOut, IReadOnlyList<Agent> waiting, long tick, float[] changes, Blame blame, List<SimEvent> events)
    {
        foreach (var caller in callingOut.Where(caller => caller != asked && waiting.Contains(caller) && AtFront(caller)))
        {
            var honest = Before(caller, asked);
            events.Add(new CalledOut(tick, caller.Id, asked.Id, honest));
            if (honest)
            {
                Pick(caller, tick, events);
                return;
            }

            foreach (var near in waiting.Where(near => near != caller && Near(near, caller)))
            {
                changes[near.Id.Value] -= tuning.FalseClaimHit;
                blame.Charge(near, caller, tuning.FalseClaimHit);
            }
        }
    }

    /// <summary>Serves whom the clerk asked, if they are still at the front; whoever was there first and sees it takes it badly.</summary>
    private void Serve(IReadOnlyList<Agent> waiting, long tick, Rng rng, float[] changes, Blame blame, List<SimEvent> events)
    {
        var served = Picked!;
        Picked = null;
        if (!waiting.Contains(served) || !AtFront(served))
        {
            return;
        }

        Serving = served;
        ServiceEndsAt = tick + tuning.ServiceTicks.Draw(rng);
        served.Step = LineStep.BeingServed;
        served.ReachedCounterAt = tick;
        events.Add(new ReachedCounter(tick, served.Id));
        foreach (var wronged in waiting.Where(other => other != served && Before(other, served) && Near(other, served)))
        {
            changes[wronged.Id.Value] -= tuning.ServedAheadHit;
            blame.Charge(wronged, served, tuning.ServedAheadHit);
            events.Add(new ServedAhead(tick, served.Id, wronged.Id));
        }
    }

    private bool Near(Agent first, Agent second) => System.Numerics.Vector2.Distance(first.Position, second.Position) <= tuning.SightMeters;
}
