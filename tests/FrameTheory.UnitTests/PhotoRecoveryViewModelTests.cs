using FrameTheory.Desktop;
using FrameTheory.Domain;
using FrameTheory.Storage;

namespace FrameTheory.UnitTests;

public sealed class PhotoRecoveryViewModelTests
{
    [Xunit.Fact]
    public void PhotoRecoveryRequiresStorageServices()
    {
        var repository = new JsonProjectRepository();

        Xunit.Assert.Throws<ArgumentNullException>(() => new PhotoRecoveryViewModel(null!, repository));
        Xunit.Assert.Throws<ArgumentNullException>(() => new PhotoRecoveryViewModel(new ProjectRecoveryService(repository), null!));
    }

    [Xunit.Fact]
    public async Task PhotoRecoveryShowsMissingMediaAndPersistsRelinkWithoutChangingReplacement()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-photo-recovery-");
        try
        {
            var projectPath = Path.Combine(directory.FullName, "photo.ftstudio");
            var mediaDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "media"));
            var replacementPath = Path.Combine(mediaDirectory.FullName, "replacement.png");
            await File.WriteAllTextAsync(replacementPath, "replacement");
            var mediaId = MediaId.Create(new Uri("file:///missing.png"), "sha256:missing");
            var repository = new JsonProjectRepository();
            await repository.SaveAtomicAsync(
                projectPath,
                new ProjectSnapshot("photo", new RationalFrameRate(30, 1), 100, 100, "sRGB", [
                    new MediaReference(mediaId, "media/missing.png", "sha256:missing")
                ]),
                CancellationToken.None);
            var viewModel = new PhotoRecoveryViewModel(new ProjectRecoveryService(repository), repository)
            {
                ProjectPath = projectPath
            };

            Xunit.Assert.False(viewModel.CanRelink);
            await viewModel.OpenProjectAsync(CancellationToken.None);
            Xunit.Assert.Single(viewModel.MissingMedia);
            Xunit.Assert.True(viewModel.CanRelink);
            Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<MissingMediaViewModel>>(viewModel.MissingMedia).IsReadOnly);
            Xunit.Assert.Contains("1 missing", viewModel.StatusText);

            var corruptProjectPath = Path.Combine(directory.FullName, "corrupt.ftstudio");
            await File.WriteAllTextAsync(corruptProjectPath, "{not-json");
            viewModel.ProjectPath = corruptProjectPath;
            await viewModel.OpenProjectAsync(CancellationToken.None);
            Xunit.Assert.Contains("could not be opened", viewModel.StatusText);
            Xunit.Assert.Single(viewModel.MissingMedia);
            viewModel.ProjectPath = projectPath;

            await viewModel.RelinkSelectedAsync(replacementPath, CancellationToken.None);
            var loaded = await repository.LoadAsync(projectPath, CancellationToken.None);
            Xunit.Assert.Empty(viewModel.MissingMedia);
            Xunit.Assert.False(viewModel.CanRelink);
            Xunit.Assert.Equal("media/replacement.png", loaded.MediaReferences.Single().RelativePath);
            Xunit.Assert.NotEqual("sha256:missing", loaded.MediaReferences.Single().ContentFingerprint);
            Xunit.Assert.Equal("replacement", await File.ReadAllTextAsync(replacementPath));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task PhotoRecoveryReportsCorruptProjectWithoutThrowing()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-photo-recovery-");
        try
        {
            var projectPath = Path.Combine(directory.FullName, "corrupt.ftstudio");
            await File.WriteAllTextAsync(projectPath, "{not-json");
            var repository = new JsonProjectRepository();
            var viewModel = new PhotoRecoveryViewModel(new ProjectRecoveryService(repository), repository)
            {
                ProjectPath = projectPath
            };

            await viewModel.OpenProjectAsync(CancellationToken.None);

            Xunit.Assert.Contains("could not be opened", viewModel.StatusText);
            Xunit.Assert.Empty(viewModel.MissingMedia);
        }
        finally
        {
            directory.Delete(true);
        }
    }
}
