using FrameTheory.Application;

namespace FrameTheory.UnitTests;

public sealed class DiagnosticsTests
{
    [Xunit.Fact]
    public void LocalDiagnosticsWritesRedactedFailureDetails()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-diagnostics-");
        try
        {
            var path = Path.Combine(directory.FullName, "studio.log");
            var diagnostics = new FileDiagnosticsLog(path);
            diagnostics.Record("export-failed", new InvalidOperationException("key=AIza123456789012345678901234567890"));

            var content = File.ReadAllText(path);
            Xunit.Assert.Contains("export-failed", content, StringComparison.Ordinal);
            Xunit.Assert.Contains("[REDACTED]", content, StringComparison.Ordinal);
            Xunit.Assert.DoesNotContain("AIza123456789012345678901234567890", content, StringComparison.Ordinal);
        }
        finally
        {
            directory.Delete(true);
        }
    }
}
