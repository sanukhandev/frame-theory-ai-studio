using FrameTheory.Color;
using FrameTheory.Domain;

namespace FrameTheory.UnitTests;

public sealed class ColorPresetTests
{
    [Xunit.Fact]
    public async Task PresetsPersistApplyToSharedColorPathAndProjectCommandUndoes()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-presets-");
        try
        {
            var path = Path.Combine(directory.FullName, "presets.json");
            var preset = new ColorPreset("Warm", new ColorGrade(exposureEV: 0.5f, temperature: 0.4f), CubeLut.Identity(2));
            var store = new JsonColorPresetStore();
            await store.SaveAtomicAsync(path, [preset], CancellationToken.None);
            var loaded = await store.LoadAsync(path, CancellationToken.None);
            var source = new RgbColor(0.2f, 0.3f, 0.4f);
            var photoOutput = loaded.Single().Apply(ColorGraph.ForSrgb(), source);
            var videoOutput = loaded.Single().Apply(ColorGraph.ForRec709Video(), source);
            var project = new ProjectSnapshot("project", new RationalFrameRate(30, 1), 8, 4, "sRGB");
            var command = new SetColorPresetCommand("Warm");

            Xunit.Assert.Equal("Warm", loaded.Single().Name);
            Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<ColorPreset>>(loaded).IsReadOnly);
            Xunit.Assert.NotEqual(source, photoOutput);
            Xunit.Assert.NotEqual(source, videoOutput);
            Xunit.Assert.Equal("Warm", command.Apply(project).ColorPresetName);
            Xunit.Assert.Null(command.Inverse(project).Apply(command.Apply(project)).ColorPresetName);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task PresetStoreRejectsDuplicateNamesAndMalformedJson()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-presets-");
        try
        {
            var path = Path.Combine(directory.FullName, "presets.json");
            var preset = new ColorPreset("Same", new ColorGrade());
            await Xunit.Assert.ThrowsAsync<ArgumentException>(() => new JsonColorPresetStore().SaveAtomicAsync(" ", [preset], CancellationToken.None));
            await Xunit.Assert.ThrowsAsync<ArgumentException>(() => new JsonColorPresetStore().LoadAsync(" ", CancellationToken.None));
            await Xunit.Assert.ThrowsAsync<ArgumentException>(() => new JsonColorPresetStore().SaveAtomicAsync(path, [null!], CancellationToken.None));
            await Xunit.Assert.ThrowsAsync<ArgumentException>(() => new JsonColorPresetStore().SaveAtomicAsync(path, [preset, new ColorPreset("same", new ColorGrade())], CancellationToken.None));
            await File.WriteAllTextAsync(path, "{ malformed");
            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() => new JsonColorPresetStore().LoadAsync(path, CancellationToken.None));
            await File.WriteAllTextAsync(path, "{\"presets\":[null]}");
            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() => new JsonColorPresetStore().LoadAsync(path, CancellationToken.None));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task PresetStoreRejectsSerializedPayloadsThatCannotReopen()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-presets-size-");
        try
        {
            var path = Path.Combine(directory.FullName, "presets.json");
            var preset = new ColorPreset(new string('p', 5 * 1024 * 1024), new ColorGrade());

            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() =>
                new JsonColorPresetStore().SaveAtomicAsync(path, [preset], CancellationToken.None));

            Xunit.Assert.False(File.Exists(path));
            Xunit.Assert.Empty(directory.EnumerateFiles("*.tmp"));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task PresetStoreRejectsReparsePointFiles()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-presets-boundary-");
        var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-presets-external-");
        try
        {
            var store = new JsonColorPresetStore();
            var preset = new ColorPreset("External", new ColorGrade());
            var externalPath = Path.Combine(externalDirectory.FullName, "presets.json");
            await store.SaveAtomicAsync(externalPath, [preset], CancellationToken.None);
            var linkedPath = Path.Combine(directory.FullName, "presets.json");
            File.CreateSymbolicLink(linkedPath, externalPath);

            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() =>
                store.LoadAsync(linkedPath, CancellationToken.None));
            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() =>
                store.SaveAtomicAsync(linkedPath, [preset], CancellationToken.None));
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
