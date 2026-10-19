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
            Xunit.Assert.Throws<ArgumentException>(() => new MediaReference(
                default,
                "media/clip.mov",
                "sha256:unsafe"));
            Xunit.Assert.Throws<ArgumentException>(() => new MediaSourceMetadata(
                default,
                new Uri("file:///clip.mov"),
                MediaKind.Video,
                640,
                480,
                new RationalFrameRate(24, 1),
                1));
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

    [Xunit.Fact]
    public async Task CorruptAndOversizedProjectInputsAreRejectedWithoutProcessFailure()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-project-");
        try
        {
            var path = Path.Combine(directory.FullName, "project.ftstudio");
            var repository = new JsonProjectRepository();
            await File.WriteAllTextAsync(path, "{ not valid project json");
            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() => repository.LoadAsync(path, CancellationToken.None));

            await File.WriteAllTextAsync(path, new string('x', 10 * 1024 * 1024 + 1));
            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() => repository.LoadAsync(path, CancellationToken.None));

            var random = new Random(1729);
            for (var iteration = 0; iteration < 32; iteration++)
            {
                var bytes = new byte[random.Next(1, 2048)];
                random.NextBytes(bytes);
                await File.WriteAllBytesAsync(path, bytes);
                var exception = await Xunit.Record.ExceptionAsync(() => repository.LoadAsync(path, CancellationToken.None));
                Xunit.Assert.IsType<InvalidDataException>(exception);
            }
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task ProjectRepositoryRejectsSerializedPayloadsThatCannotReopen()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-project-size-");
        try
        {
            var path = Path.Combine(directory.FullName, "project.ftstudio");
            var snapshot = new ProjectSnapshot(
                new string('p', 10 * 1024 * 1024),
                new RationalFrameRate(24, 1),
                640,
                480,
                "sRGB");

            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() =>
                new JsonProjectRepository().SaveAtomicAsync(path, snapshot, CancellationToken.None));

            Xunit.Assert.False(File.Exists(path));
            Xunit.Assert.Empty(directory.EnumerateFiles("*.tmp"));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task ProjectRepositoryRejectsNullAndDuplicateMediaEntries()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-project-media-boundary-");
        try
        {
            var path = Path.Combine(directory.FullName, "project.ftstudio");
            var mediaId = new string('a', 64);
            var media = $$"""
                {
                  "schemaVersion": 1,
                  "projectId": "project",
                  "frameRateNumerator": 24,
                  "frameRateDenominator": 1,
                  "canvasWidth": 640,
                  "canvasHeight": 480,
                  "workingColorSpace": "sRGB",
                  "media": [
                    { "id": "{{mediaId}}", "relativePath": "media/one.mov", "contentFingerprint": "sha256:one" },
                    { "id": "{{mediaId}}", "relativePath": "media/two.mov", "contentFingerprint": "sha256:two" }
                  ]
                }
                """;
            await File.WriteAllTextAsync(path, media);

            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() => new JsonProjectRepository().LoadAsync(path, CancellationToken.None));

            await File.WriteAllTextAsync(path, media.Replace("{ \"id\": \"" + mediaId, "null, { \"id\": \"", StringComparison.Ordinal));
            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() => new JsonProjectRepository().LoadAsync(path, CancellationToken.None));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task ProjectRepositoryRejectsReparsePointProjectPaths()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-project-boundary-");
        var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-project-external-");
        try
        {
            var projectDirectory = Path.Combine(directory.FullName, "project");
            Directory.CreateSymbolicLink(projectDirectory, externalDirectory.FullName);
            var projectPath = Path.Combine(projectDirectory, "project.ftstudio");
            var snapshot = new ProjectSnapshot("project", new RationalFrameRate(24, 1), 640, 480, "sRGB");
            var repository = new JsonProjectRepository();

            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() =>
                repository.SaveAtomicAsync(projectPath, snapshot, CancellationToken.None));

            Directory.Delete(projectDirectory);
            Directory.CreateDirectory(projectDirectory);
            var externalProjectPath = Path.Combine(externalDirectory.FullName, "external.ftstudio");
            await repository.SaveAtomicAsync(externalProjectPath, snapshot, CancellationToken.None);
            var linkedProjectPath = Path.Combine(projectDirectory, "linked.ftstudio");
            File.CreateSymbolicLink(linkedProjectPath, externalProjectPath);

            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() =>
                repository.LoadAsync(linkedProjectPath, CancellationToken.None));
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (IOException)
        {
        }
        finally
        {
            directory.Delete(true);
            externalDirectory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task ProjectRepositoryRejectsReparsePointProjectAncestors()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-project-ancestor-boundary-");
        var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-project-ancestor-external-");
        try
        {
            var linkedRoot = Path.Combine(directory.FullName, "linked");
            Directory.CreateSymbolicLink(linkedRoot, externalDirectory.FullName);
            var projectDirectory = Directory.CreateDirectory(Path.Combine(linkedRoot, "nested"));
            var snapshot = new ProjectSnapshot("project", new RationalFrameRate(24, 1), 640, 480, "sRGB");

            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() =>
                new JsonProjectRepository().SaveAtomicAsync(
                    Path.Combine(projectDirectory.FullName, "project.ftstudio"),
                    snapshot,
                    CancellationToken.None));
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (IOException)
        {
        }
        finally
        {
            directory.Delete(true);
            externalDirectory.Delete(true);
        }
    }
}
