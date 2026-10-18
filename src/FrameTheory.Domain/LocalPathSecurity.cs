namespace FrameTheory.Domain;

public static class LocalPathSecurity
{
    public static bool ContainsReparsePoint(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var current = File.Exists(fullPath) || Directory.Exists(fullPath)
            ? fullPath
            : Path.GetDirectoryName(fullPath);
        while (!string.IsNullOrWhiteSpace(current))
        {
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
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
}
