namespace WaitingSim;

/// <summary>The result of reading data handed to the sim: the value, or the problems that kept it from being read.</summary>
public sealed record Parsed<T>(T? Value, IReadOnlyList<string> Problems)
    where T : class;
