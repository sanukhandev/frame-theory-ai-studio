namespace FrameTheory.Desktop;

internal static class BrandAssetCatalog
{
    public const string ApprovedLogoRelativePath = "assets/brand/frame-theory-original.png";

    public static bool TryGetApprovedLogoPath(out string path)
    {
        path = Path.Combine(AppContext.BaseDirectory, ApprovedLogoRelativePath);
        if (!File.Exists(path))
        {
            path = string.Empty;
            return false;
        }

        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            {
                path = string.Empty;
                return false;
            }
        }
        catch (IOException)
        {
            path = string.Empty;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            path = string.Empty;
            return false;
        }

        return true;
    }

    public static bool IsApprovedLogoPath(string path) =>
        string.Equals(
            Path.GetFullPath(path),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ApprovedLogoRelativePath)),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
