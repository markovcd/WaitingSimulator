using WaitingSim.Runner;

namespace WaitingSim.Tests;

public sealed class CliTests
{
    [Theory]
    [InlineData("help")]
    [InlineData("--help")]
    [InlineData("-h")]
    public void Help_prints_usage_on_stdout_and_succeeds(string argument)
    {
        var (code, output, error) = Run(argument);

        Assert.Equal(ExitCode.Success, code);
        Assert.StartsWith("Usage: waitingsim-runner", output, StringComparison.Ordinal);
        Assert.Empty(error);
    }

    [Fact]
    public void An_unknown_command_is_a_usage_error_named_on_stderr()
    {
        var (code, output, error) = Run("queue");

        Assert.Equal(ExitCode.Usage, code);
        Assert.Empty(output);
        Assert.StartsWith("Unknown command: queue", error, StringComparison.Ordinal);
    }

    [Fact]
    public void No_command_is_a_usage_error()
    {
        var (code, output, error) = Run();

        Assert.Equal(ExitCode.Usage, code);
        Assert.Empty(output);
        Assert.StartsWith("No command given.", error, StringComparison.Ordinal);
    }

    private static (int Code, string Output, string Error) Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = Cli.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }
}
