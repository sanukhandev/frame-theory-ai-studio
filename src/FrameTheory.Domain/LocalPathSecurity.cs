namespace FrameTheory.Domain;

public static class LocalPathSecurity
{
    public static bool ContainsReparsePoint(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var current = fullPath;
        while (!string.IsNullOrWhiteSpace(current))
        {
            var normalizedCurrent = Path.TrimEndingDirectorySeparator(current);
            if (!IsMacOsSystemAlias(normalizedCurrent) && TryGetAttributes(current, out var attributes) &&
                (attributes & FileAttributes.ReparsePoint) != 0)
            {
                return true;
            }

            var parent = Directory.GetParent(current)?.FullName;
            if (parent is null || string.Equals(parent, current, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            current = parent;
        }

        return false;
    }

    private static bool TryGetAttributes(string path, out FileAttributes attributes)
    {
        try
        {
            attributes = File.GetAttributes(path);
            return true;
        }
        catch (FileNotFoundException)
        {
            attributes = default;
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            attributes = default;
            return false;
        }
    }

    private static bool IsMacOsSystemAlias(string path) =>
        OperatingSystem.IsMacOS() && path is "/var" or "/tmp" or "/etc" or "/Users";
}
