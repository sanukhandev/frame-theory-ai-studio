using System.Security.Cryptography;
using FrameTheory.Domain;

namespace FrameTheory.Desktop;

internal static class BrandAssetCatalog
{
    public const string ApprovedLogoRelativePath = "assets/brand/frame-theory-original.png";
    private const string ApprovedLogoSha256 = "0BD29D37FBCC4C753B8870A65E3A02E23460C1A5C305E53E6CD2D9D2F151F388";

    public static bool TryGetApprovedLogoPath(out string path)
    {
        path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ApprovedLogoRelativePath));
        if (!File.Exists(path))
        {
            path = string.Empty;
            return false;
        }

        try
        {
            if (LocalPathSecurity.ContainsReparsePoint(path))
            {
                path = string.Empty;
                return false;
            }

            if (!string.Equals(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))), ApprovedLogoSha256, StringComparison.Ordinal))
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
        TryGetApprovedLogoPath(out var approvedPath) &&
        string.Equals(
            Path.GetFullPath(path),
            approvedPath,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
