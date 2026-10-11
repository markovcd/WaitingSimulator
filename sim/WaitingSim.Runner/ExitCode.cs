namespace WaitingSim.Runner;

/// <summary>What the runner's process exit code means. Each value means one thing.</summary>
internal static class ExitCode
{
    public const int Success = 0;

    /// <summary>The command line could not be understood.</summary>
    public const int Usage = 2;
}
