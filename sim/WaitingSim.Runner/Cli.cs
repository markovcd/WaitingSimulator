namespace WaitingSim.Runner;

/// <summary>The runner's command line, testable in-process: arguments and two writers in, an exit code out.</summary>
internal static class Cli
{
    private const string UsageText = """
        Usage: waitingsim-runner <command> [options]

        Plays Waiting Simulator levels headlessly.

        Commands:
          help    Print this text.
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

        error.WriteLine(args.Count == 0 ? "No command given." : $"Unknown command: {args[0]}");
        error.WriteLine(UsageText);
        return ExitCode.Usage;
    }
}
