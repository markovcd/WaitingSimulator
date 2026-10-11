namespace WaitingSim.Tests;

public sealed class EngineIndependenceTests
{
    [Fact]
    public void The_sim_references_no_engine_assembly()
    {
        var engineReferences = typeof(TickRate).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? "")
            .Where(name => name.StartsWith("Godot", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(engineReferences);
    }
}
