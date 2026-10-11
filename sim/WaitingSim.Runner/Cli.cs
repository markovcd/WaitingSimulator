namespace WaitingSim.Runner;

/// <summary>The runner's command line, testable in-process: arguments and two writers in, an exit code out.</summary>
internal static class Cli
{
    private const string UsageText = """
        Usage: waitingsim-runner <command> [options]

        Plays Waiting Simulator levels headlessly.

        Commands:
          help    Print this text.
          sweep   Play every trait against every scripted player bot over many seeded
                  lines, and report the patience of the person behind the player.
                    --data <folder>  Where tuning.json and traits.json are (default: data).
                    --seed <n>       The first line's seed (default: 1).
                    --lines <n>      Lines per trait and bot, 1 to 10000 (default: 20).
                    --json           Report as JSON.
        """;

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (args is ["help" or "--help" or "-h"])
        {
            output.WriteLine(UsageText);
            return ExitCode.Success;
        }

        if (args is ["sweep", ..])
        {
            return SweepCommand.Run([.. args.Skip(1)], output, error);
        }

        error.WriteLine(args.Count == 0 ? "No command given." : $"Unknown command: {args[0]}");
        error.WriteLine(UsageText);
        return ExitCode.Usage;
    }
}
