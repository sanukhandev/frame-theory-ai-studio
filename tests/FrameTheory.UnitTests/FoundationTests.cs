using System.Reflection;

namespace FrameTheory.UnitTests;

public sealed class FoundationTests
{
    [Xunit.Fact]
    public void FoundationAssembliesAreLoadable()
    {
        var expected = new[]
        {
            "FrameTheory.Application",
            "FrameTheory.AI",
            "FrameTheory.Captions",
            "FrameTheory.Color",
            "FrameTheory.Domain",
            "FrameTheory.Media",
            "FrameTheory.Photo",
            "FrameTheory.Platform",
            "FrameTheory.Storage"
        };

        var loaded = expected.Select(Assembly.Load).Select(assembly => assembly.GetName().Name).ToArray();

        Xunit.Assert.Equal(expected, loaded);
    }

    [Xunit.Fact]
    public void DesktopShellProvidesAllRequiredWorkspaces()
    {
        var viewModel = new FrameTheory.Desktop.MainWindowViewModel(new FrameTheory.Desktop.WorkspaceCatalog());

        Xunit.Assert.Equal(new[] { "Video", "Photo", "Color", "AI" }, viewModel.Workspaces.Select(workspace => workspace.Name));
        Xunit.Assert.Equal("Video", viewModel.SelectedWorkspace.Name);

        viewModel.SelectedWorkspaceIndex = 3;

        Xunit.Assert.Equal("AI", viewModel.SelectedWorkspace.Name);

        viewModel.SelectedWorkspaceIndex = -1;
        viewModel.SelectedWorkspaceIndex = viewModel.Workspaces.Count;

        Xunit.Assert.Equal("AI", viewModel.SelectedWorkspace.Name);
    }
}
