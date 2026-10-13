using System.Text;
using System.Text.Json;

namespace WaitingSimulator.Game;

/// <summary>
/// What the player chose that outlives a launch, kept in a small versioned JSON file in the user's folder. Loading is
/// tolerant: a missing, damaged or newer file plays on the defaults, and a newer one is never saved over.
/// </summary>
internal sealed record Settings(Difficulty Difficulty)
{
    /// <summary>The newest settings format this game reads and writes.</summary>
    public const int FormatVersion = 1;

    public static Settings Defaults { get; } = new(Difficulty.Normal);

    /// <summary>The settings in the file at <paramref name="path"/>, or the defaults and why, when it cannot be read; no file is the defaults and no problem.</summary>
    public static (Settings Settings, string? Problem) Load(string path)
    {
        if (!File.Exists(path))
        {
            return (Defaults, null);
        }

        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return (Defaults, $"{path}: {exception.Message}");
        }

        var (settings, problem) = Parse(text);
        return (settings ?? Defaults, problem is null ? null : $"{path}: {problem}");
    }

    /// <summary>Writes <paramref name="settings"/> to <paramref name="path"/>; the problems say why it was not, such as a file from a newer game being there.</summary>
    public static IReadOnlyList<string> Save(string path, Settings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        try
        {
            if (File.Exists(path) && Version(File.ReadAllText(path)) > FormatVersion)
            {
                return [$"{path}: kept as it is; it is from a newer game"];
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path, settings.ToJson());
            return [];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [$"{path}: {exception.Message}"];
        }
    }

    private string ToJson()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("version", FormatVersion);
            writer.WriteString("difficulty", Difficulties.Name(Difficulty));
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }

    private static (Settings? Settings, string? Problem) Parse(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return (null, "not a JSON object");
            }

            if (!root.TryGetProperty("version", out var version) || !version.TryGetInt32(out var number) || number < 1)
            {
                return (null, "version: missing, or not a settings format version");
            }

            if (number > FormatVersion)
            {
                return (null, $"version: {number} is from a newer game; this one reads up to {FormatVersion}");
            }

            if (!root.TryGetProperty("difficulty", out var named)
                || named.ValueKind != JsonValueKind.String
                || !Enum.TryParse<Difficulty>(named.GetString(), ignoreCase: true, out var difficulty)
                || !Enum.IsDefined(difficulty))
            {
                return (null, "difficulty: takes easy, normal or hard");
            }

            return (new Settings(difficulty), null);
        }
        catch (JsonException exception)
        {
            return (null, $"not valid JSON: {exception.Message}");
        }
    }

    /// <summary>The version a settings file's text says it is, or 0 when it says none.</summary>
    private static int Version(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("version", out var version)
                && version.TryGetInt32(out var number) ? number : 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }
}
