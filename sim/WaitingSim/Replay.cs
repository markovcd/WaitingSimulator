using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace WaitingSim;

/// <summary>
/// A seed, a level setup and a command stream, with the state hashes playing them gave. Self-contained: it carries
/// its own tuning and traits, so a change to the shipped data never changes how a replay plays.
/// </summary>
/// <param name="Seed">The sim's seed.</param>
/// <param name="Ticks">How many ticks the replay runs.</param>
/// <param name="TuningJson">The tuning, as a tuning file's text.</param>
/// <param name="Tuning">The tuning, read.</param>
/// <param name="TraitsJson">The traits, as a traits file's text.</param>
/// <param name="Traits">The traits, read.</param>
/// <param name="Lane">The lane.</param>
/// <param name="Agents">The agents, spawned in this order before the first tick.</param>
/// <param name="Commands">Every change of command, in tick order.</param>
/// <param name="Hashes">The recorded state hash every <see cref="HashEveryTicks"/> ticks and after the last.</param>
public sealed record Replay(
    ulong Seed,
    int Ticks,
    string TuningJson,
    Tuning Tuning,
    string TraitsJson,
    IReadOnlyList<Trait> Traits,
    Lane Lane,
    IReadOnlyList<AgentSpawn> Agents,
    IReadOnlyList<ReplayCommand> Commands,
    IReadOnlyList<ReplayHash> Hashes)
{
    /// <summary>The newest replay format this sim reads.</summary>
    public const int FormatVersion = 1;

    /// <summary>How often a replay records the state hash.</summary>
    public const int HashEveryTicks = TickRate.PerSecond;

    /// <summary>The longest replay read: an hour.</summary>
    public const int MaxTicks = 60 * 60 * TickRate.PerSecond;

    private static readonly JsonWriterOptions Indented = new() { Indented = true };

    /// <summary>Reads a replay file's text. Problems name the field they concern.</summary>
    public static Parsed<Replay> Parse(string json) => JsonFile.Read(json, "replay", FormatVersion, Read);

    /// <summary>Plays the replay from its seed and returns the hashes and events it gives.</summary>
    public ReplayRun Play()
    {
        var sim = new Sim(Tuning, Lane, Seed);
        foreach (var agent in Agents)
        {
            sim.Spawn(agent.Position, agent.Trait is { } id ? Traits.Single(trait => trait.Id == id) : null);
        }

        var held = new Dictionary<AgentId, Command>();
        var hashes = new List<ReplayHash>();
        var events = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var next = 0;
        for (var tick = 1; tick <= Ticks; tick++)
        {
            for (; next < Commands.Count && Commands[next].Tick == tick; next++)
            {
                held[Commands[next].Agent] = new Command(Commands[next].Move);
            }

            foreach (var happened in sim.Step(held))
            {
                var name = happened.GetType().Name;
                events[name] = events.GetValueOrDefault(name) + 1;
            }

            if (tick % HashEveryTicks == 0 || tick == Ticks)
            {
                hashes.Add(new ReplayHash(tick, sim.StateHash()));
            }
        }

        return new ReplayRun(hashes, events);
    }

    /// <summary>Where <paramref name="played"/> first departs from the recorded hashes, or null if it never does.</summary>
    public string? Divergence(IReadOnlyList<ReplayHash> played)
    {
        ArgumentNullException.ThrowIfNull(played);
        var mismatch = Hashes.Zip(played).FirstOrDefault(pair => pair.First != pair.Second);
        if (mismatch != default)
        {
            return JsonFile.Invariant($"tick {mismatch.Second.Tick}: recorded hash {Hex(mismatch.First.Hash)}, played {Hex(mismatch.Second.Hash)}");
        }

        return Hashes.Count == played.Count
            ? null
            : JsonFile.Invariant($"the replay records {Hashes.Count} hashes; playing it gives {played.Count}");
    }

    /// <summary>The replay as a file's text.</summary>
    public string ToJson()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, Indented))
        {
            writer.WriteStartObject();
            writer.WriteNumber("version", FormatVersion);
            writer.WriteNumber("seed", Seed);
            writer.WriteNumber("ticks", Ticks);
            WriteEmbedded(writer, "tuning", TuningJson);
            WriteEmbedded(writer, "traits", TraitsJson);

            writer.WriteStartObject("lane");
            writer.WriteStartArray("points");
            foreach (var point in Lane.Points)
            {
                WritePoint(writer, null, point);
            }

            writer.WriteEndArray();
            writer.WriteNumber("halfWidth", Lane.HalfWidth);
            writer.WriteEndObject();

            writer.WriteStartArray("agents");
            foreach (var agent in Agents)
            {
                writer.WriteStartObject();
                WritePoint(writer, "position", agent.Position);
                if (agent.Trait is { } trait)
                {
                    writer.WriteString("trait", trait);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("commands");
            foreach (var command in Commands)
            {
                writer.WriteStartObject();
                writer.WriteNumber("tick", command.Tick);
                writer.WriteNumber("agent", command.Agent.Value);
                WritePoint(writer, "move", command.Move);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("hashes");
            foreach (var hash in Hashes)
            {
                writer.WriteStartObject();
                writer.WriteNumber("tick", hash.Tick);
                writer.WriteString("hash", Hex(hash.Hash));
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }

    private static Replay Read(JsonElement root, List<string> problems)
    {
        var seed = JsonFile.Unsigned(root, "seed", problems);
        var ticks = JsonFile.Integer(root, "ticks", problems);
        if (ticks is < 1 or > MaxTicks && problems.Count == 0)
        {
            problems.Add(JsonFile.Invariant($"ticks: {ticks} is not from 1 to {MaxTicks}"));
        }

        var (tuningJson, tuning) = Embedded(root, "tuning", Tuning.Parse, problems);
        var (traitsJson, traits) = Embedded(root, "traits", WaitingSim.Traits.Parse, problems);
        var lane = Lane.Read(root, problems);
        var agents = AgentSpawn.ReadAll(root, traits ?? [], "the replay's traits", problems);
        var commands = ReadCommands(root, ticks is >= 1 and <= MaxTicks ? ticks : MaxTicks, agents.Count, problems);
        var hashes = ReadHashes(root, problems);
        return new(seed, ticks, tuningJson, tuning!, traitsJson, traits!, lane!, agents, commands, hashes);
    }

    /// <summary>A whole data file embedded under <paramref name="name"/>: its text, and what its own parser reads from it.</summary>
    private static (string Json, T? Value) Embedded<T>(JsonElement root, string name, Func<string, Parsed<T>> parse, List<string> problems)
        where T : class
    {
        if (JsonFile.Section(root, name, problems) is not { } section)
        {
            return ("", null);
        }

        var json = section.GetRawText();
        var parsed = parse(json);
        problems.AddRange(parsed.Problems.Select(problem => $"{name}: {problem}"));
        return (json, parsed.Value);
    }

    /// <summary>Reads the commands, each between the one before it and <paramref name="lastTick"/>.</summary>
    private static List<ReplayCommand> ReadCommands(JsonElement root, int lastTick, int agentCount, List<string> problems)
    {
        var commands = new List<ReplayCommand>();
        var items = JsonFile.Items(root, "commands", problems);
        for (var i = 0; i < items.Count; i++)
        {
            var path = JsonFile.Invariant($"commands[{i}]");
            var tick = JsonFile.Integer(items[i], $"{path}.tick", problems);
            var agent = JsonFile.Integer(items[i], $"{path}.agent", problems);
            var move = JsonFile.Point(items[i], $"{path}.move", problems);
            var earliest = commands.Count > 0 ? commands[^1].Tick : 1;
            if (tick < earliest || tick > lastTick)
            {
                problems.Add(JsonFile.Invariant($"{path}.tick: {tick} is not from {earliest} to {lastTick}; commands are in tick order"));
            }

            if (agent < 0 || agent >= agentCount)
            {
                problems.Add(JsonFile.Invariant($"{path}.agent: {agent} is not one of the {agentCount} agents"));
            }

            commands.Add(new ReplayCommand(tick, new AgentId(agent), move));
        }

        return commands;
    }

    private static List<ReplayHash> ReadHashes(JsonElement root, List<string> problems)
    {
        var hashes = new List<ReplayHash>();
        var items = JsonFile.Items(root, "hashes", problems);
        for (var i = 0; i < items.Count; i++)
        {
            var path = JsonFile.Invariant($"hashes[{i}]");
            var tick = JsonFile.Integer(items[i], $"{path}.tick", problems);
            var text = JsonFile.Text(items[i], $"{path}.hash", problems);
            if (!ulong.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var hash) && text.Length > 0)
            {
                problems.Add($"{path}.hash: \"{text}\" is not a hexadecimal hash");
            }

            hashes.Add(new ReplayHash(tick, hash));
        }

        return hashes;
    }

    /// <summary>Writes a data file's text as a JSON value, laid out like the rest of the replay.</summary>
    private static void WriteEmbedded(Utf8JsonWriter writer, string name, string json)
    {
        using var document = JsonDocument.Parse(json);
        writer.WritePropertyName(name);
        document.WriteTo(writer);
    }

    private static void WritePoint(Utf8JsonWriter writer, string? name, Vector2 point)
    {
        if (name is not null)
        {
            writer.WritePropertyName(name);
        }

        writer.WriteStartArray();
        writer.WriteNumberValue(point.X);
        writer.WriteNumberValue(point.Y);
        writer.WriteEndArray();
    }

    private static string Hex(ulong hash) => hash.ToString("x16", CultureInfo.InvariantCulture);
}
