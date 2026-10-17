using FrameTheory.Domain;
using FrameTheory.Storage;

namespace FrameTheory.UnitTests;

public sealed class ProjectRepositoryTests
{
    [Xunit.Fact]
    public async Task ProjectRoundTripUsesVersionedRelativeMediaReferencesAndAtomicReplacement()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-project-");
        try
        {
            var projectPath = Path.Combine(directory.FullName, "project.ftstudio");
            var sourceUri = new Uri("file:///media/clip.mov");
            var mediaId = MediaId.Create(sourceUri, "sha256:abc");
            var snapshot = new ProjectSnapshot(
                "project-1",
                new RationalFrameRate(30000, 1001),
                1080,
                1920,
                "sRGB",
                [new MediaReference(mediaId, "media/clip.mov", "sha256:abc")]);
            var repository = new JsonProjectRepository();

            await repository.SaveAtomicAsync(projectPath, snapshot, CancellationToken.None);
            var firstJson = await File.ReadAllTextAsync(projectPath);
            await repository.SaveAtomicAsync(projectPath, snapshot, CancellationToken.None);
            var loaded = await repository.LoadAsync(projectPath, CancellationToken.None);

            Xunit.Assert.Equal(firstJson, await File.ReadAllTextAsync(projectPath));
            Xunit.Assert.Equal(1, loaded.SchemaVersion);
            Xunit.Assert.Equal(snapshot.ProjectId, loaded.ProjectId);
            Xunit.Assert.Equal(snapshot.FrameRate, loaded.FrameRate);
            Xunit.Assert.Equal("media/clip.mov", loaded.MediaReferences.Single().RelativePath);
            Xunit.Assert.DoesNotContain("C:\\", firstJson);
            Xunit.Assert.Contains("schemaVersion", firstJson);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task ProjectRepositoryMigratesLegacyFrameRateAndRejectsUnsafeInput()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-project-");
        try
        {
            var projectPath = Path.Combine(directory.FullName, "legacy.ftstudio");
            await File.WriteAllTextAsync(projectPath, """
                {
                  "projectId": "legacy",
                  "frameRate": "24/1",
                  "canvasWidth": 640,
                  "canvasHeight": 480,
                  "workingColorSpace": "sRGB",
                  "media": []
                }
                """);

            var loaded = await new JsonProjectRepository().LoadAsync(projectPath, CancellationToken.None);

            Xunit.Assert.Equal(1, loaded.SchemaVersion);
            Xunit.Assert.Equal(new RationalFrameRate(24, 1), loaded.FrameRate);
            Xunit.Assert.Throws<ArgumentException>(() => new MediaReference(
                MediaId.Parse(new string('a', 64)), "../outside.mov", "sha256:unsafe"));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task CanceledSaveLeavesThePreviouslyCommittedProjectIntact()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-project-");
        try
        {
            var projectPath = Path.Combine(directory.FullName, "project.ftstudio");
            var sourceUri = new Uri("file:///media/clip.mov");
            var mediaId = MediaId.Create(sourceUri, "sha256:abc");
            var firstSnapshot = new ProjectSnapshot("first", new RationalFrameRate(24, 1), 640, 480, "sRGB");
            var secondSnapshot = new ProjectSnapshot(
                "second",
                new RationalFrameRate(24, 1),
                640,
                480,
                "sRGB",
                [new MediaReference(mediaId, "media/clip.mov", "sha256:abc")]);
            var repository = new JsonProjectRepository();
            await repository.SaveAtomicAsync(projectPath, firstSnapshot, CancellationToken.None);

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Xunit.Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                repository.SaveAtomicAsync(projectPath, secondSnapshot, cancellation.Token));

            var loaded = await repository.LoadAsync(projectPath, CancellationToken.None);
            Xunit.Assert.Equal("first", loaded.ProjectId);
            Xunit.Assert.Empty(Directory.EnumerateFiles(directory.FullName, ".*.tmp"));
        }
        finally
        {
            directory.Delete(true);
        }
    }
}
