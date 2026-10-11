using System.Text.Json;

namespace WaitingSim.Runner;

/// <summary>
/// <c>replay &lt;file&gt;</c>: plays a replay and checks it reaches the state hashes it recorded,
/// or with <c>--record</c> writes the hashes it plays to into the file.
/// </summary>
internal static class ReplayCommandLine
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        string? file = null;
        var record = false;
        var json = false;
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "--record":
                    record = true;
                    break;
                case "--json":
                    json = true;
                    break;
                case not null when !arg.StartsWith('-') && file is null:
                    file = arg;
                    break;
                default:
                    error.WriteLine($"replay: {arg} is not an option, or a second file.");
                    return ExitCode.Usage;
            }
        }

        if (file is null)
        {
            error.WriteLine("replay: name the replay file.");
            return ExitCode.Usage;
        }

        string text;
        try
        {
            text = File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"{file}: {exception.Message}");
            return ExitCode.DataProblem;
        }

        var parsed = Replay.Parse(text);
        if (parsed.Value is not { } replay)
        {
            foreach (var problem in parsed.Problems)
            {
                error.WriteLine($"{file}: {problem}");
            }

            return ExitCode.DataProblem;
        }

        var run = replay.Play();
        var divergence = record ? null : replay.Divergence(run.Hashes);
        if (record)
        {
            File.WriteAllText(file, (replay with { Hashes = run.Hashes }).ToJson());
        }

        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(new { file, replay.Ticks, recorded = record, divergence, events = run.Events }, JsonOptions));
        }
        else
        {
            output.WriteLine(record
                ? $"{file}: recorded {run.Hashes.Count} hashes over {replay.Ticks} ticks."
                : divergence is null ? $"{file}: all {run.Hashes.Count} hashes match over {replay.Ticks} ticks." : $"{file}: diverged.");
            foreach (var (name, count) in run.Events)
            {
                output.WriteLine($"  {name}: {count}");
            }
        }

        if (divergence is not null)
        {
            error.WriteLine($"{file}: {divergence}");
            return ExitCode.ReplayDiverged;
        }

        return ExitCode.Success;
    }
}
