using System.Security.Cryptography;
using FrameTheory.Desktop;

namespace FrameTheory.UnitTests;

public sealed class BrandAssetTests
{
    private const string ApprovedLogoHash = "0BD29D37FBCC4C753B8870A65E3A02E23460C1A5C305E53E6CD2D9D2F151F388";

    [Xunit.Fact]
    public void ApprovedLogoIsPresentAndByteVerified()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "assets", "brand", "frame-theory-original.png");

        Xunit.Assert.True(File.Exists(path), $"Approved logo was not copied to {path}.");
        Xunit.Assert.Equal(0, (int)(File.GetAttributes(path) & FileAttributes.ReparsePoint));
        Xunit.Assert.Equal(ApprovedLogoHash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
        Xunit.Assert.True(BrandAssetCatalog.TryGetApprovedLogoPath(out var resolvedPath));
        Xunit.Assert.Equal(Path.GetFullPath(path), resolvedPath);
        Xunit.Assert.True(BrandAssetCatalog.IsApprovedLogoPath(path));
    }
}
