namespace WaitingSim.Runner;

/// <summary>How the runner's commands read the sim's data files.</summary>
internal static class DataFile
{
    /// <summary>Reads and parses one data file, or writes what is wrong with it, naming the file, and returns null.</summary>
    public static T? Load<T>(string folder, string name, Func<string, Parsed<T>> parse, TextWriter error)
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
}
