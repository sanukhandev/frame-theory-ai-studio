using FrameTheory.Domain;
using FrameTheory.Storage;

namespace FrameTheory.UnitTests;

public sealed class RecoveryTests
{
    [Xunit.Fact]
    public void RecoveryRequiresProjectRepository()
    {
        Xunit.Assert.Throws<ArgumentNullException>(() => new ProjectRecoveryService(null!));
    }

    [Xunit.Fact]
    public async Task AutosaveRecoveryAndRelinkDoNotChangeTheOriginalSource()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-recovery-");
        try
        {
            var projectPath = Path.Combine(directory.FullName, "project.ftstudio");
            var mediaDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "media"));
            var missingPath = Path.Combine(mediaDirectory.FullName, "missing.mov");
            var replacementPath = Path.Combine(mediaDirectory.FullName, "replacement.mov");
            await File.WriteAllTextAsync(replacementPath, "synthetic media");
            var mediaId = MediaId.Create(new Uri("file:///media/missing.mov"), "sha256:missing");
            var snapshot = new ProjectSnapshot(
                "project",
                new RationalFrameRate(30, 1),
                1920,
                1080,
                "sRGB",
                [new MediaReference(mediaId, "media/missing.mov", "sha256:missing")]);
            var recovery = new ProjectRecoveryService(new JsonProjectRepository());

            await recovery.SaveAutosaveAsync(projectPath, snapshot, CancellationToken.None);
            var recovered = await recovery.TryLoadAutosaveAsync(projectPath, CancellationToken.None);
            var missing = recovery.FindMissingMedia(projectPath, snapshot);
            var relinked = await recovery.RelinkMediaAsync(projectPath, snapshot, mediaId, replacementPath, CancellationToken.None);

            Xunit.Assert.NotNull(recovered);
            Xunit.Assert.Single(missing);
            Xunit.Assert.Equal("media/replacement.mov", relinked.MediaReferences.Single().RelativePath);
            Xunit.Assert.Equal(
                $"sha256:{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(replacementPath))).ToLowerInvariant()}",
                relinked.MediaReferences.Single().ContentFingerprint);
            Xunit.Assert.False(File.Exists(missingPath));
            Xunit.Assert.Equal("synthetic media", await File.ReadAllTextAsync(replacementPath));
            recovery.ClearAutosave(projectPath);
            Xunit.Assert.Null(await recovery.TryLoadAutosaveAsync(projectPath, CancellationToken.None));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task CacheIndexPersistsAndEvictsOldestEntriesWithinBudget()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-cache-");
        try
        {
            var index = new CacheIndex();
            var older = DateTimeOffset.UtcNow.AddMinutes(-2);
            index.Register("old", "cache/old.bin", 8, older);
            index.Register("new", "cache/new.bin", 8, older.AddMinutes(1));
            Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<CacheIndexEntry>>(index.Entries).IsReadOnly);
            var evicted = index.EvictToSize(8);

            Xunit.Assert.Equal("old", Xunit.Assert.Single(evicted).Key);
            Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<CacheIndexEntry>>(evicted).IsReadOnly);
            var path = Path.Combine(directory.FullName, "cache-index.json");
            var store = new JsonCacheIndexStore();
            await store.SaveAtomicAsync(path, index, CancellationToken.None);
            var loaded = await store.LoadAsync(path, CancellationToken.None);

            Xunit.Assert.True(loaded.TryGet("new", out var entry));
            Xunit.Assert.Equal("cache/new.bin", entry!.RelativePath);
            Xunit.Assert.Equal(8, loaded.TotalSizeBytes);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task CacheIndexStoreDoesNotReplaceExistingFileWhenCanceled()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-cache-cancel-");
        try
        {
            var path = Path.Combine(directory.FullName, "index.json");
            var store = new JsonCacheIndexStore();
            var original = new CacheIndex();
            original.Register("original", "cache/original.bin", 1, DateTimeOffset.UtcNow);
            await store.SaveAtomicAsync(path, original, CancellationToken.None);

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var replacement = new CacheIndex();
            replacement.Register("replacement", "cache/replacement.bin", 2, DateTimeOffset.UtcNow);

            await Xunit.Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                store.SaveAtomicAsync(path, replacement, cancellation.Token));

            var loaded = await store.LoadAsync(path, CancellationToken.None);
            Xunit.Assert.True(loaded.TryGet("original", out _));
            Xunit.Assert.False(loaded.TryGet("replacement", out _));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task CacheIndexStoreRejectsReparsePointFiles()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-cache-boundary-");
        var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-cache-external-");
        try
        {
            var store = new JsonCacheIndexStore();
            var index = new CacheIndex();
            index.Register("entry", "cache/item.bin", 1, DateTimeOffset.UtcNow);
            var externalPath = Path.Combine(externalDirectory.FullName, "index.json");
            await store.SaveAtomicAsync(externalPath, index, CancellationToken.None);
            var linkedPath = Path.Combine(directory.FullName, "index.json");
            File.CreateSymbolicLink(linkedPath, externalPath);

            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() =>
                store.LoadAsync(linkedPath, CancellationToken.None));
            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() =>
                store.SaveAtomicAsync(linkedPath, index, CancellationToken.None));

            var danglingPath = Path.Combine(directory.FullName, "dangling.json");
            File.CreateSymbolicLink(danglingPath, Path.Combine(externalDirectory.FullName, "missing.json"));
            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() =>
                store.LoadAsync(danglingPath, CancellationToken.None));
            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() =>
                store.SaveAtomicAsync(danglingPath, index, CancellationToken.None));
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
    public async Task CacheIndexStoreRejectsCorruptEntriesAsInvalidData()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-cache-corrupt-");
        try
        {
            var path = Path.Combine(directory.FullName, "index.json");
            await File.WriteAllTextAsync(path, "[{\"key\":\"escape\",\"relativePath\":\"../outside.bin\",\"sizeBytes\":1,\"lastAccessUtc\":\"2026-01-01T00:00:00Z\"}]");

            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() => new JsonCacheIndexStore().LoadAsync(path, CancellationToken.None));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task RelinkRejectsDanglingReplacementBeforeMissingFileFallback()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-recovery-dangling-");
        try
        {
            var projectPath = Path.Combine(directory.FullName, "project.ftstudio");
            var mediaId = MediaId.Create(new Uri("file:///media/missing.mov"), "sha256:missing");
            var snapshot = new ProjectSnapshot(
                "project",
                new RationalFrameRate(30, 1),
                1920,
                1080,
                "sRGB",
                [new MediaReference(mediaId, "media/missing.mov", "sha256:missing")]);
            var linkedReplacement = Path.Combine(directory.FullName, "linked.mov");
            File.CreateSymbolicLink(linkedReplacement, Path.Combine(directory.FullName, "missing.mov"));

            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() =>
                new ProjectRecoveryService(new JsonProjectRepository()).RelinkMediaAsync(
                    projectPath,
                    snapshot,
                    mediaId,
                    linkedReplacement,
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
        }
    }

    [Xunit.Fact]
    public void RecoveryRejectsTraversalAndReparsePointDirectories()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-recovery-boundary-");
        var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-recovery-external-");
        try
        {
            var projectPath = Path.Combine(directory.FullName, "project.ftstudio");
            var mediaId = MediaId.Create(new Uri("file:///media/clip.mov"), "sha256:clip");
            Xunit.Assert.Throws<ArgumentException>(() => new MediaReference(mediaId, "../outside/clip.mov", "sha256:clip"));
            Xunit.Assert.Throws<ArgumentException>(() => new MediaReference(mediaId, $" {Path.DirectorySeparatorChar}outside{Path.DirectorySeparatorChar}clip.mov ", "sha256:clip"));
            Xunit.Assert.Throws<ArgumentException>(() => new CacheIndex().Register("entry", "../outside/cache.bin", 1, DateTimeOffset.UtcNow));
            Xunit.Assert.Throws<ArgumentException>(() => new CacheIndex().Register(
                "entry",
                $" {Path.DirectorySeparatorChar}outside{Path.DirectorySeparatorChar}cache.bin ",
                1,
                DateTimeOffset.UtcNow));
            var snapshot = new ProjectSnapshot(
                "project",
                new RationalFrameRate(30, 1),
                1920,
                1080,
                "sRGB",
                [new MediaReference(mediaId, "media/clip.mov", "sha256:clip")]);
            var recovery = new ProjectRecoveryService(new JsonProjectRepository());

            var mediaDirectory = Path.Combine(directory.FullName, "media");
            Directory.CreateSymbolicLink(mediaDirectory, externalDirectory.FullName);

            Xunit.Assert.Throws<InvalidDataException>(() => recovery.FindMissingMedia(projectPath, snapshot));
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
    public void RecoveryDoesNotTreatLinuxCaseVariantSiblingAsContained()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var directory = Directory.CreateTempSubdirectory("frame-theory-recovery-case-");
        var siblingPath = Path.Combine(directory.Parent!.FullName, directory.Name.ToUpperInvariant());
        try
        {
            Directory.CreateDirectory(siblingPath);
            var relativePath = Path.Combine("..", Path.GetFileName(siblingPath), "escape.mov");

            Xunit.Assert.Throws<InvalidDataException>(() =>
                ProjectRecoveryService.ResolveContainedPath(directory.FullName, relativePath));
        }
        finally
        {
            Directory.Delete(siblingPath, recursive: true);
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task CanceledRelinkDoesNotChangeTheSnapshot()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-recovery-cancel-");
        try
        {
            var projectPath = Path.Combine(directory.FullName, "project.ftstudio");
            var replacementPath = Path.Combine(directory.FullName, "replacement.mov");
            await File.WriteAllTextAsync(replacementPath, "replacement");
            var mediaId = MediaId.Create(new Uri("file:///media/missing.mov"), "sha256:missing");
            var snapshot = new ProjectSnapshot(
                "project",
                new RationalFrameRate(30, 1),
                1920,
                1080,
                "sRGB",
                [new MediaReference(mediaId, "media/missing.mov", "sha256:missing")]);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Xunit.Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                new ProjectRecoveryService(new JsonProjectRepository()).RelinkMediaAsync(
                    projectPath,
                    snapshot,
                    mediaId,
                    replacementPath,
                    cancellation.Token));

            Xunit.Assert.Equal("media/missing.mov", snapshot.MediaReferences.Single().RelativePath);
            Xunit.Assert.Equal("sha256:missing", snapshot.MediaReferences.Single().ContentFingerprint);
        }
        finally
        {
            directory.Delete(true);
        }
    }
}
