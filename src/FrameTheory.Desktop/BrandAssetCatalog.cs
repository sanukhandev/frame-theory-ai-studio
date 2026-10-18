namespace FrameTheory.Desktop;

internal static class BrandAssetCatalog
{
    private static readonly string[] ApprovedLogoSegments = ["assets", "brand", "frame-theory-original.png"];

    public static bool TryGetApprovedLogoPath(out string path)
    {
        path = Path.Combine([AppContext.BaseDirectory, .. ApprovedLogoSegments]);
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
}
