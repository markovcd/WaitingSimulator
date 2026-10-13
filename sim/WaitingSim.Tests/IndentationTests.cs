namespace WaitingSim.Tests;

/// <summary>
/// Source indentation, which the editorconfig sets to spaces. Godot's script editor indents with tabs unless told
/// otherwise and rewrites a script it saves, so a tab-indented script is caught here rather than in review.
/// </summary>
public sealed class IndentationTests
{
    private static readonly string[] Folders = ["sim", "game"];

    [Fact]
    public void No_script_is_indented_with_tabs()
    {
        var tabbed = new List<string>();
        foreach (var path in Scripts())
        {
            var lines = File.ReadAllLines(path);
            var at = Array.FindIndex(lines, line => line.TrimStart(' ').StartsWith('\t'));
            if (at >= 0)
            {
                tabbed.Add($"{Path.GetRelativePath(PipelineTests.Root, path)}:{at + 1}");
            }
        }

        Assert.True(tabbed.Count == 0, $"Indented with tabs ({tabbed.Count}; reindent with spaces): {string.Join(", ", tabbed.Take(5))}");
    }

    /// <summary>Every C# file in the sim and the game, leaving out build output.</summary>
    private static IEnumerable<string> Scripts() =>
        Folders
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(PipelineTests.Root, folder), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj" or ".godot"));
}
