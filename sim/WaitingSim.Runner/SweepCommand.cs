using System.Globalization;
using System.Text.Json;

namespace WaitingSim.Runner;

/// <summary>
/// <c>sweep</c>: plays every trait against every scripted player bot over many seeded lines and reports how the
/// person behind fared.
/// </summary>
internal static class SweepCommand
{
    private const int MaxLines = 10_000;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        var dataFolder = "data";
        ulong seed = 1;
        var lines = 20;
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
                case "--seed" when ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out seed):
                    i++;
                    break;
                case "--lines" when int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out lines) && lines is > 0 and <= MaxLines:
                    i++;
                    break;
                case "--json":
                    json = true;
                    break;
                default:
                    error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"sweep: {args[i]} is not an option, or its value is missing or out of range (--lines takes 1 to {MaxLines})."));
                    return ExitCode.Usage;
            }
        }

        var tuning = Load(dataFolder, "tuning.json", Tuning.Parse, error);
        var traits = Load(dataFolder, "traits.json", Traits.Parse, error);
        if (tuning is null || traits is null)
        {
            return ExitCode.DataProblem;
        }

        if (tuning.Ripple is null)
        {
            error.WriteLine($"{Path.Combine(dataFolder, "tuning.json")}: ripple: missing; a swept line moves only by its ripple");
            return ExitCode.DataProblem;
        }

        var rows = Sweep(tuning, traits, seed, lines);
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(new { seed, lines, SweepLine.Seconds, SweepLine.SampleSeconds, rows }, JsonOptions));
        }
        else
        {
            WriteTable(rows, seed, lines, output);
        }

        return ExitCode.Success;
    }

    private static List<SweepRow> Sweep(Tuning tuning, IReadOnlyList<Trait> traits, ulong seed, int lines)
    {
        var rows = new List<SweepRow>();
        foreach (var trait in traits)
        {
            foreach (var bot in Enum.GetValues<PlayerBot>())
            {
                var results = Enumerable.Range(0, lines)
                    .Select(line => SweepLine.Play(tuning, trait, bot, seed + (ulong)line))
                    .ToList();
                rows.Add(new SweepRow(
                    trait.Id,
                    bot.ToString().ToLowerInvariant(),
                    Rounded(results.Average(result => result.FinalPatience)),
                    Rounded(results.Count(result => result.RanOut) / (double)lines),
                    Rounded(results.Average(result => result.GraceExpiries)),
                    [.. Enumerable.Range(0, results[0].PatienceCurve.Count).Select(i => Rounded(results.Average(result => result.PatienceCurve[i])))]));
            }
        }

        return rows;
    }

    /// <summary>Reads and parses one data file, or writes what is wrong with it, naming the file, and returns null.</summary>
    private static T? Load<T>(string folder, string name, Func<string, Parsed<T>> parse, TextWriter error)
        where T : class
    {
        var path = Path.Combine(folder, name);
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"{path}: {exception.Message}");
            return null;
        }

        var parsed = parse(text);
        foreach (var problem in parsed.Problems)
        {
            error.WriteLine($"{path}: {problem}");
        }

        return parsed.Value;
    }

    private static void WriteTable(List<SweepRow> rows, ulong seed, int lines, TextWriter output)
    {
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{lines} lines of {SweepLine.Seconds} s from seed {seed}; patience of the person behind the player."));
        output.WriteLine();
        output.WriteLine($"{"trait",-20} {"bot",-12} {"final",7} {"ran out",8} {"grace expiries",15}");
        foreach (var row in rows)
        {
            output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{row.Trait,-20} {row.Bot,-12} {row.MeanFinalPatience,7:0.0} {row.RanOutShare,8:0%} {row.MeanGraceExpiries,15:0.0}"));
        }
    }

    private static double Rounded(double value) => Math.Round(value, 2, MidpointRounding.ToEven);
}
