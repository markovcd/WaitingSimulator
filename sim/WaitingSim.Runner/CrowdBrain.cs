using System.Numerics;

namespace WaitingSim.Runner;

/// <summary>
/// Turns what a scripted player in a crowd sees into its command each tick: make for the counter straight ahead without
/// pushing anyone, stepping round whoever is in the way, and once at the front, call out when the clerk asks someone who
/// arrived later.
/// </summary>
internal sealed class CrowdBrain(Counter counter, CrowdTuning crowd, BodyTuning body, AgentId self)
{
    /// <summary>How square on someone has to stand to be in the way: the cosine of the widest angle off the path.</summary>
    private const float InTheWay = 0.5f;

    private static readonly float SideStep = MathF.Sqrt(0.5f);

    public Command Next(Snapshot snapshot)
    {
        var you = snapshot.Agents[self.Value];
        var (along, @out) = counter.Measure(you.Position);
        if (@out <= body.Radius + crowd.FrontMeters)
        {
            var asked = snapshot.Clerk?.Picked is { } picked ? snapshot.Agents[picked.Value] : null;
            var later = asked is not null && asked.Id != self && (asked.ArrivedAt > you.ArrivedAt || (asked.ArrivedAt == you.ArrivedAt && asked.Id.Value > self.Value));
            return Command.Idle with { CallOut = later };
        }

        var ahead = Vector2.Normalize(counter.At(along, body.Radius) - you.Position);
        var left = new Vector2((ahead.X * SideStep) - (ahead.Y * SideStep), (ahead.X * SideStep) + (ahead.Y * SideStep));
        var right = new Vector2((ahead.X * SideStep) + (ahead.Y * SideStep), (ahead.Y * SideStep) - (ahead.X * SideStep));
        foreach (var way in new[] { ahead, left, right })
        {
            if (Clear(snapshot, you, way))
            {
                return new(way);
            }
        }

        return Command.Idle;
    }

    private bool Clear(Snapshot snapshot, AgentSnapshot you, Vector2 way)
    {
        var room = (body.Radius * 2f) + crowd.StandOffMeters;
        return snapshot.Agents.All(other =>
        {
            if (other.Id == self || other.Step is LineStep.Exiting or LineStep.Gone)
            {
                return true;
            }

            var offset = other.Position - you.Position;
            var distance = offset.Length();
            return distance >= room || (distance > 0f && Vector2.Dot(offset / distance, way) <= InTheWay);
        });
    }
}
