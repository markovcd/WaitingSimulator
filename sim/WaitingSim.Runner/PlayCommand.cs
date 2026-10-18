using System.Globalization;
using System.Text.Json;

namespace WaitingSim.Runner;

/// <summary>
/// <c>play</c>: plays a level with a scripted player bot until the player reaches the counter or is kicked out, and
/// reports which, how long it took and the mistakes it made on the way. A crowd level is played by the one crowd bot,
/// whatever <c>--bot</c> says.
/// </summary>
internal static class PlayCommand
{
    private const int MaxSeconds = 3600;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        var dataFolder = "data";
        var levelName = "post-office";
        var bot = PlayerBot.Attentive;
        var seconds = 600;
        var json = false;
        for (var i = 0; i < args.Count; i++)
        {
            var value = i + 1 < args.Count ? args[i + 1] : null;
            switch (args[i])
            {
                case "--data" when value is not null:
                    dataFolder = value;
                    i++;
                    break;
                case "--level" when value is not null && IsLevelName(value):
                    levelName = value;
                    i++;
                    break;
                case "--bot" when Bot(value) is { } named:
                    bot = named;
                    i++;
                    break;
                case "--seconds" when int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out seconds) && seconds is > 0 and <= MaxSeconds:
                    i++;
                    break;
                case "--json":
                    json = true;
                    break;
                default:
                    error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"play: {args[i]} is not an option, or its value is missing or out of range (--level takes a level file's name, --bot one of {string.Join(", ", Enum.GetNames<PlayerBot>()).ToLowerInvariant()}, --seconds 1 to {MaxSeconds})."));
                    return ExitCode.Usage;
            }
        }

        var tuning = DataFile.Load(dataFolder, "tuning.json", Tuning.Parse, error);
        var traits = DataFile.Load(dataFolder, "traits.json", Traits.Parse, error);
        var path = Path.Combine(dataFolder, "levels", levelName + ".json");
        var text = traits is null ? null : DataFile.Text(path, error);
        if (tuning is null || traits is null || text is null)
        {
            return ExitCode.DataProblem;
        }

        ulong seed;
        AgentSnapshot player;
        if (Level.IsCrowd(text))
        {
            if (DataFile.Report(path, CrowdLevel.Parse(text, traits), error) is not { } crowd)
            {
                return ExitCode.DataProblem;
            }

            if (tuning is not { Crowd: { } crowdTuning, Bodies: { } bodies })
            {
                error.WriteLine($"{Path.Combine(dataFolder, "tuning.json")}: a crowd level needs the crowd and bodies sections");
                return ExitCode.DataProblem;
            }

            seed = crowd.Seed;
            player = Play(crowd.Start(tuning), crowd.Player, new CrowdBrain(crowd.Counter, crowdTuning, bodies, crowd.Player).Next, seconds);
        }
        else
        {
            if (DataFile.Report(path, Level.Parse(text, traits), error) is not { } level)
            {
                return ExitCode.DataProblem;
            }

            seed = level.Seed;
            var brain = new PlayerBrain(bot, level.Lane, new Rng(level.Seed, stream: 2));
            player = Play(level.Start(tuning), level.Player, snapshot => brain.Next(snapshot.Agents[level.Player.Value]), seconds);
        }

        var botName = bot.ToString().ToLowerInvariant();
        var worst = player.Tally.Worst is { } mistake ? JsonNamingPolicy.CamelCase.ConvertName(mistake.ToString()) : null;
        var kicked = player.ReachedCounterAt is null && player.KickedOutAt is not null;
        var turnedAway = player.TurnedAwayAt is not null;
        double? endedSeconds = (player.ReachedCounterAt ?? player.KickedOutAt ?? player.TurnedAwayAt) is { } tick ? Math.Round((double)tick / TickRate.PerSecond, 2) : null;
        if (json)
        {
            var tally = player.Tally;
            output.WriteLine(JsonSerializer.Serialize(
                new
                {
                    level = levelName,
                    bot = botName,
                    seed,
                    reachedCounter = player.ReachedCounterAt is not null,
                    kickedOut = kicked,
                    turnedAway,
                    seconds = endedSeconds,
                    tally = new { tally.Bumps, tally.GapsLeftOpen, tally.PlacesLost, tally.CuttersLetIn, tally.MissedCutIns, tally.CutIns, tally.NoTickets, tally.ServedAhead, tally.FalseClaims },
                    worst,
                },
                JsonOptions));
        }
        else
        {
            output.WriteLine(endedSeconds is { } s
                ? string.Create(CultureInfo.InvariantCulture, $"{levelName}: the {botName} bot {(kicked ? "was kicked out" : turnedAway ? "was turned away without its ticket" : "reached the counter")} after {s:0.00} s; worst mistake: {worst ?? "none"}.")
                : string.Create(CultureInfo.InvariantCulture, $"{levelName}: the {botName} bot did not reach the counter in {seconds} s; worst mistake: {worst ?? "none"}."));
        }

        return ExitCode.Success;
    }

    /// <summary>The player as it stands once it reaches the counter, is turned away there or is kicked out, or after <paramref name="seconds"/> if neither happens.</summary>
    private static AgentSnapshot Play(ISim sim, AgentId self, Func<Snapshot, Command> brain, int seconds)
    {
        var commands = new Dictionary<AgentId, Command>();
        var snapshot = sim.Snapshot();
        var player = snapshot.Agents[self.Value];
        for (var tick = 0; tick < seconds * TickRate.PerSecond && player is { ReachedCounterAt: null, KickedOutAt: null, TurnedAwayAt: null }; tick++)
        {
            commands[self] = brain(snapshot);
            sim.Step(commands);
            snapshot = sim.Snapshot();
            player = snapshot.Agents[self.Value];
        }

        return player;
    }

    /// <summary>The bot <paramref name="name"/> names, ignoring case, or null.</summary>
    private static PlayerBot? Bot(string? name) =>
        Enum.GetValues<PlayerBot>().Select(bot => (PlayerBot?)bot).FirstOrDefault(bot => string.Equals(bot.ToString(), name, StringComparison.OrdinalIgnoreCase));

    /// <summary>A level file's name: lowercase letters, digits and dashes, so it can only name a file in the levels folder.</summary>
    private static bool IsLevelName(string name) =>
        name.Length > 0 && name.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-');
}
