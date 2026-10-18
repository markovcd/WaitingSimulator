using Godot;

namespace WaitingSimulator.Game;

/// <summary>
/// Where the data files are read from. Run from the project, that is the repo's <c>data/</c> beside it. An exported
/// build carries them packed (ci/export.sh copies <c>data/</c> into the project before exporting), and the sim reads
/// files from disk, so the build unpacks them into the user's folder once per launch.
/// </summary>
internal static class ShippedData
{
    private const string Packed = "res://data";
    private const string Unpacked = "user://data";

    private static string? folder;

    public static string Folder() => folder ??= OS.HasFeature("template") ? Unpack() : Repo();

    private static string Repo() => Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "data"));

    private static string Unpack()
    {
        var target = ProjectSettings.GlobalizePath(Unpacked);
        try
        {
            // A level a newer build dropped must not linger from an older one.
            if (Directory.Exists(target))
            {
                Directory.Delete(target, recursive: true);
            }

            Copy(Packed, target);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PrintErr($"{target}: {exception.Message}");
        }

        return target;
    }

    private static void Copy(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var file in DirAccess.GetFilesAt(from))
        {
            File.WriteAllBytes(Path.Combine(to, file), Godot.FileAccess.GetFileAsBytes($"{from}/{file}"));
        }

        foreach (var child in DirAccess.GetDirectoriesAt(from))
        {
            Copy($"{from}/{child}", Path.Combine(to, child));
        }
    }
}
