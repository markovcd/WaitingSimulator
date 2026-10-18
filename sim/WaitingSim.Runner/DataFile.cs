namespace WaitingSim.Runner;

/// <summary>How the runner's commands read the sim's data files.</summary>
internal static class DataFile
{
    /// <summary>Reads and parses one data file, or writes what is wrong with it, naming the file, and returns null.</summary>
    public static T? Load<T>(string folder, string name, Func<string, Parsed<T>> parse, TextWriter error)
        where T : class
    {
        var path = Path.Combine(folder, name);
        return Text(path, error) is { } text ? Report(path, parse(text), error) : null;
    }

    /// <summary>The text of the file at <paramref name="path"/>, or null after writing why it could not be read.</summary>
    public static string? Text(string path, TextWriter error)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"{path}: {exception.Message}");
            return null;
        }
    }

    /// <summary>The value <paramref name="parsed"/> read from <paramref name="path"/>, or null after writing its problems, naming the file.</summary>
    public static T? Report<T>(string path, Parsed<T> parsed, TextWriter error)
        where T : class
    {
        foreach (var problem in parsed.Problems)
        {
            error.WriteLine($"{path}: {problem}");
        }

        return parsed.Value;
    }
}
