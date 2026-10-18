using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Reflection;
using FrameTheory.AI;
using FrameTheory.Application;
using FrameTheory.Domain;

namespace FrameTheory.UnitTests;

[Xunit.Collection("Avalonia headless")]
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
    public void LocalPathSecurityRejectsReparsePointAncestors()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-path-security-");
        var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-path-security-external-");
        try
        {
            var linkedDirectory = Path.Combine(directory.FullName, "linked");
            Directory.CreateSymbolicLink(linkedDirectory, externalDirectory.FullName);

            Xunit.Assert.True(LocalPathSecurity.ContainsReparsePoint(Path.Combine(linkedDirectory, "future.json")));
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
    public void LocalPathSecurityRejectsDanglingReparsePoint()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-path-security-dangling-");
        try
        {
            var linkedPath = Path.Combine(directory.FullName, "dangling.json");
            File.CreateSymbolicLink(linkedPath, Path.Combine(directory.FullName, "missing.json"));

            Xunit.Assert.True(LocalPathSecurity.ContainsReparsePoint(linkedPath));
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
    public void LocalPathSecurityAllowsMissingRegularPath()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-path-security-missing-");
        try
        {
            Xunit.Assert.False(LocalPathSecurity.ContainsReparsePoint(Path.Combine(directory.FullName, "future.json")));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public void DesktopShellProvidesAllRequiredWorkspaces()
    {
        var viewModel = new FrameTheory.Desktop.MainWindowViewModel(
            new FrameTheory.Desktop.WorkspaceCatalog(),
            new TestCapabilityDetector());

        Xunit.Assert.Equal(new[] { "Video", "Photo", "Color", "AI" }, viewModel.Workspaces.Select(workspace => workspace.Name));
        Xunit.Assert.Equal("Video", viewModel.SelectedWorkspace.Name);

        viewModel.SelectedWorkspaceIndex = 3;

        Xunit.Assert.Equal("AI", viewModel.SelectedWorkspace.Name);

        viewModel.SelectedWorkspaceIndex = -1;
        viewModel.SelectedWorkspaceIndex = viewModel.Workspaces.Count;

        Xunit.Assert.Equal("AI", viewModel.SelectedWorkspace.Name);

        viewModel.SelectedWorkspaceIndex = -1;
        viewModel.SelectedWorkspaceIndex = viewModel.Workspaces.Count;

        Xunit.Assert.Equal("AI", viewModel.SelectedWorkspace.Name);
    }

    [Xunit.Fact]
    public async Task MainWindowBuildsAccessibleAdaptiveWorkspaceShell()
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApplication));
        await session.Dispatch(() =>
        {
            var viewModel = new FrameTheory.Desktop.MainWindowViewModel(
                new FrameTheory.Desktop.WorkspaceCatalog(),
                new TestCapabilityDetector());

            var window = new FrameTheory.Desktop.MainWindow(viewModel);
            try
            {
                window.Show();
                var layout = Xunit.Assert.IsType<Grid>(window.Content);
                var tabs = Xunit.Assert.IsType<TabControl>(layout.Children[1]);
                var tabItems = tabs.Items.Cast<TabItem>().ToArray();

                Xunit.Assert.Equal(1280, window.Width);
                Xunit.Assert.Equal(800, window.Height);
                Xunit.Assert.Equal(960, window.MinWidth);
                Xunit.Assert.Equal(640, window.MinHeight);
                Xunit.Assert.True(window.UseLayoutRounding);
                Xunit.Assert.Equal("Frame Theory Studio AI", AutomationProperties.GetName(window));
                Xunit.Assert.Equal("Workspaces", AutomationProperties.GetName(tabs));
                Xunit.Assert.Equal(Dock.Left, tabs.TabStripPlacement);
                var projectTitle = window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Current project title");
                Xunit.Assert.Equal("Untitled project", projectTitle.Text);
                Xunit.Assert.Equal(
                    "Current editable project name. It changes after a successful local save or reopen.",
                    ToolTip.GetTip(projectTitle));
                var navigationLabels = tabItems
                    .Select(tab => Xunit.Assert.IsType<Grid>(tab.Header).Children
                        .OfType<StackPanel>()
                        .Single()
                        .Children
                        .OfType<TextBlock>()
                        .First()
                        .Text)
                    .ToArray();
                Xunit.Assert.Equal(new[] { "Video", "Photo", "Color", "AI" }, navigationLabels);
                Xunit.Assert.Equal(
                    "Video: Frame-accurate editing, playback and export.",
                    ToolTip.GetTip(tabItems[0]));
                Xunit.Assert.Equal(
                    "Recheck local FFmpeg, ffprobe and platform capabilities.",
                    ToolTip.GetTip(window.GetVisualDescendants()
                        .OfType<Button>()
                        .Single(control => AutomationProperties.GetName(control) == "Refresh capability check")));
                var shortcutHint = window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Workspace keyboard shortcuts");
                Xunit.Assert.True(shortcutHint.IsVisible);
                window.Width = 960;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.False(shortcutHint.IsVisible);
                window.Width = 1280;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.True(shortcutHint.IsVisible);
                var capabilityStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Capability status");
                Xunit.Assert.Equal("Local FFmpeg, ffprobe and platform capability status.", ToolTip.GetTip(capabilityStatus));
                Xunit.Assert.True(capabilityStatus.IsReadOnly);
                Xunit.Assert.Equal(11, capabilityStatus.FontSize);
                var diagnosticsStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Diagnostics status");
                Xunit.Assert.True(diagnosticsStatus.IsReadOnly);
                Xunit.Assert.Equal(11, diagnosticsStatus.FontSize);
                var importStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Media import status");
                Xunit.Assert.True(importStatus.IsReadOnly);
                Xunit.Assert.Equal(11, importStatus.FontSize);
                Xunit.Assert.Equal("Status for local media import and relink operations.", ToolTip.GetTip(importStatus));
                Xunit.Assert.Equal(
                    new[] { "Video workspace", "Photo workspace", "Color workspace", "AI workspace" },
                    tabItems.Select(AutomationProperties.GetName));
                var activeIndicators = window.GetVisualDescendants()
                    .OfType<Border>()
                    .Where(control => AutomationProperties.GetName(control)?.EndsWith("workspace active indicator", StringComparison.Ordinal) == true)
                    .ToArray();
                Xunit.Assert.Equal(4, activeIndicators.Length);
                Xunit.Assert.Equal(
                    Avalonia.Media.Color.FromRgb(95, 125, 255),
                    Xunit.Assert.IsType<Avalonia.Media.SolidColorBrush>(activeIndicators[0].Background).Color);
                Xunit.Assert.Equal(
                    Avalonia.Media.Color.FromRgb(42, 46, 55),
                    Xunit.Assert.IsType<Avalonia.Media.SolidColorBrush>(activeIndicators[1].Background).Color);
                var workspaceScrollers = tabItems
                    .Select(tab => Xunit.Assert.IsType<ScrollViewer>(tab.Content))
                    .ToArray();
                Xunit.Assert.All(workspaceScrollers, scroller =>
                {
                    Xunit.Assert.Equal(HorizontalAlignment.Stretch, scroller.HorizontalContentAlignment);
                    Xunit.Assert.Equal(VerticalAlignment.Top, scroller.VerticalContentAlignment);
                });
                Xunit.Assert.Equal(
                    new[] { "Video workspace content", "Photo workspace content", "Color workspace content", "AI workspace content" },
                    workspaceScrollers.Select(AutomationProperties.GetName));
                var videoEditorLayout = workspaceScrollers[0].GetVisualDescendants()
                    .OfType<Grid>()
                    .Single(control => AutomationProperties.GetName(control) == "Video editor workspace layout");
                Xunit.Assert.Equal(3, videoEditorLayout.ColumnDefinitions.Count);
                Xunit.Assert.Equal(300, videoEditorLayout.ColumnDefinitions[0].Width.Value);
                Xunit.Assert.Equal(1, videoEditorLayout.ColumnDefinitions[1].Width.Value);
                for (var workspaceIndex = 1; workspaceIndex < tabItems.Length; workspaceIndex++)
                {
                    tabs.SelectedIndex = workspaceIndex;
                    Dispatcher.UIThread.RunJobs();
                    var workspaceName = new[] { "Video", "Photo", "Color", "AI" }[workspaceIndex];
                    var editorLayout = window.GetVisualDescendants()
                        .OfType<Grid>()
                        .Single(control => AutomationProperties.GetName(control) == $"{workspaceName} editor workspace layout");
                    Xunit.Assert.Equal(3, editorLayout.ColumnDefinitions.Count);
                    Xunit.Assert.Equal(300, editorLayout.ColumnDefinitions[0].Width.Value);
                    Xunit.Assert.Equal(1, editorLayout.ColumnDefinitions[1].Width.Value);
                }
                tabs.SelectedIndex = 0;
                Dispatcher.UIThread.RunJobs();
                var monitorTimecode = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline monitor timecode");
                Xunit.Assert.Equal(viewModel.TimelineEditor.CurrentTimecode, monitorTimecode.Text);
                Xunit.Assert.True(monitorTimecode.IsReadOnly);
                Xunit.Assert.Equal("Current program monitor position.", ToolTip.GetTip(monitorTimecode));
                var positionStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline position status");
                Xunit.Assert.Equal(viewModel.TimelineEditor.CurrentTimecode, positionStatus.Text);
                Xunit.Assert.True(positionStatus.IsReadOnly);
                Xunit.Assert.Equal(11, positionStatus.FontSize);
                Xunit.Assert.Equal("Current local timeline timecode.", ToolTip.GetTip(positionStatus));
                var frameStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline frame status");
                Xunit.Assert.Equal(viewModel.TimelineEditor.CurrentFrame.ToString(), frameStatus.Text);
                Xunit.Assert.True(frameStatus.IsReadOnly);
                Xunit.Assert.Equal(11, frameStatus.FontSize);
                Xunit.Assert.Equal("Current local timeline frame number.", ToolTip.GetTip(frameStatus));
                var activeWorkspaceStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Active workspace status");
                Xunit.Assert.Equal(viewModel.ActiveWorkspaceStatusText, activeWorkspaceStatus.Text);
                Xunit.Assert.Equal(
                    "Live save, export, edit or provider status for the active workspace.",
                    ToolTip.GetTip(activeWorkspaceStatus));
                tabs.SelectedIndex = 1;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Equal(
                    Avalonia.Media.Color.FromRgb(95, 125, 255),
                    Xunit.Assert.IsType<Avalonia.Media.SolidColorBrush>(activeIndicators[1].Background).Color);
                Xunit.Assert.Equal(
                    Avalonia.Media.Color.FromRgb(42, 46, 55),
                    Xunit.Assert.IsType<Avalonia.Media.SolidColorBrush>(activeIndicators[0].Background).Color);
                Xunit.Assert.Equal(viewModel.ActiveWorkspaceStatusText, activeWorkspaceStatus.Text);
                tabs.SelectedIndex = 2;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Equal(viewModel.ActiveWorkspaceStatusText, activeWorkspaceStatus.Text);
                tabs.SelectedIndex = 3;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Equal(viewModel.ActiveWorkspaceStatusText, activeWorkspaceStatus.Text);
                tabs.SelectedIndex = 0;
                Dispatcher.UIThread.RunJobs();
                var mediaBin = window.GetVisualDescendants()
                    .OfType<Border>()
                    .Single(control => AutomationProperties.GetName(control) == "Media bin");
                var mediaBinGrid = Xunit.Assert.IsType<Grid>(mediaBin.Child);
                Xunit.Assert.Equal(5, mediaBinGrid.RowDefinitions.Count);
                Xunit.Assert.Contains(mediaBin.GetVisualDescendants().OfType<Grid>(), control => control.ColumnDefinitions.Count == 2);
                var importedMediaList = window.GetVisualDescendants()
                    .OfType<ListBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Imported media");
                Xunit.Assert.NotNull(importedMediaList.ItemTemplate);
                Xunit.Assert.NotNull(importedMediaList.ItemsPanel);
                Xunit.Assert.Equal(176, importedMediaList.Height);
                var importedMediaSummary = window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Imported media summary");
                Xunit.Assert.Equal(viewModel.ImportedMediaSummary, importedMediaSummary.Text);
                Xunit.Assert.Equal(
                    "No media selected",
                    window.GetVisualDescendants()
                        .OfType<TextBlock>()
                        .Single(control => AutomationProperties.GetName(control) == "Selected media summary")
                        .Text);
                var selectedMediaChip = window.GetVisualDescendants()
                    .OfType<Border>()
                    .Single(control => AutomationProperties.GetName(control) == "Selected media chip");
                Xunit.Assert.Equal(new Avalonia.Thickness(7, 3), selectedMediaChip.Padding);
                Xunit.Assert.Equal(
                    "Current media selection used by the timeline and color workspace.",
                    ToolTip.GetTip(selectedMediaChip));
                Xunit.Assert.IsType<TextBlock>(selectedMediaChip.Child);
                Xunit.Assert.Equal(
                    "Imported local media available to the active workspace.",
                    ToolTip.GetTip(importedMediaList));
                var mediaSearch = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Search imported media");
                Xunit.Assert.True(double.IsNaN(mediaSearch.Width));
                Xunit.Assert.Equal(HorizontalAlignment.Stretch, mediaSearch.HorizontalAlignment);
                var mediaTypeFilter = window.GetVisualDescendants()
                    .OfType<ComboBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Filter imported media by type");
                Xunit.Assert.Equal(4, mediaTypeFilter.Items.Count);
                Xunit.Assert.Equal("Show all imported assets or filter the media bin by type.", ToolTip.GetTip(mediaTypeFilter));
                Xunit.Assert.Equal("Filter imported media by filename.", ToolTip.GetTip(mediaSearch));
                Xunit.Assert.False(window.GetVisualDescendants()
                    .OfType<Button>()
                    .Single(control => AutomationProperties.GetName(control) == "Clear media search")
                    .IsEnabled);
                Xunit.Assert.False(window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Media search empty state")
                    .IsVisible);
                Xunit.Assert.True(window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Media bin empty state")
                    .IsVisible);
                var timelineTracks = window.GetVisualDescendants()
                    .OfType<ListBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline tracks");
                Xunit.Assert.NotNull(timelineTracks.ItemTemplate);
                Xunit.Assert.Equal(124, timelineTracks.Height);
                Xunit.Assert.Equal("Select clips in the video and audio edit lanes.", ToolTip.GetTip(timelineTracks));
                var timelineSectionNames = new HashSet<string>(StringComparer.Ordinal)
                {
                    "Analysis and rough cut section",
                    "Clip editing section",
                    "Motion and speed section",
                    "Audio section",
                    "Titles and captions section",
                    "Project and export section",
                    "Timeline view section"
                };
                var timelineSections = window.GetVisualDescendants()
                    .OfType<Expander>()
                    .Where(control => timelineSectionNames.Contains(AutomationProperties.GetName(control)!))
                    .ToDictionary(control => AutomationProperties.GetName(control)!);
                Xunit.Assert.Equal(7, timelineSections.Count);
                Xunit.Assert.False(timelineSections["Analysis and rough cut section"].IsExpanded);
                Xunit.Assert.True(timelineSections["Clip editing section"].IsExpanded);
                Xunit.Assert.False(timelineSections["Project and export section"].IsExpanded);
                Xunit.Assert.False(timelineSections["Timeline view section"].IsExpanded);
                var aspectRatio = window.GetVisualDescendants()
                    .OfType<ComboBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Video export aspect ratio");
                Xunit.Assert.True(aspectRatio.IsVisible);
                Xunit.Assert.Equal(150, aspectRatio.MinWidth);
                Xunit.Assert.Equal(HorizontalAlignment.Left, aspectRatio.HorizontalContentAlignment);
                timelineSections["Project and export section"].IsExpanded = true;
                timelineSections["Timeline view section"].IsExpanded = true;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Equal(
                    "Scrub to an exact timeline frame.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<Slider>().Single(control => AutomationProperties.GetName(control) == "Timeline playhead frame")));
                Xunit.Assert.Equal(
                    "Adjust timeline scale in frames per pixel.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<Slider>().Single(control => AutomationProperties.GetName(control) == "Timeline zoom")));
                Xunit.Assert.Equal(
                    "Choose the output framing used by the local video renderer.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<ComboBox>().Single(control => AutomationProperties.GetName(control) == "Video export aspect ratio")));
                Xunit.Assert.Equal(
                    "Current frame and timeline position.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<TextBlock>().Single(control => AutomationProperties.GetName(control) == "Timeline ruler")));
                var rulerPlayhead = window.GetVisualDescendants()
                    .OfType<Border>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline ruler playhead");
                Xunit.Assert.Equal(2, rulerPlayhead.Width);
                Xunit.Assert.Equal(30, rulerPlayhead.Height);
                Xunit.Assert.Equal(
                    "Click the ruler to seek to an exact timeline frame.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<Border>().Single(control => AutomationProperties.GetName(control) == "Timeline visual ruler")));
                Xunit.Assert.True(window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline selection context")
                    .IsReadOnly);
                var timelineOperationStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline operation status");
                Xunit.Assert.True(timelineOperationStatus.IsReadOnly);
                Xunit.Assert.Equal("Validation or edit result for the last timeline action.", ToolTip.GetTip(timelineOperationStatus));
                Xunit.Assert.All(timelineSections.Values, section =>
                {
                    Xunit.Assert.NotNull(ToolTip.GetTip(section));
                    Xunit.Assert.NotNull(section.Background);
                    Xunit.Assert.NotNull(section.BorderBrush);
                    Xunit.Assert.Equal(HorizontalAlignment.Stretch, section.HorizontalAlignment);
                    Xunit.Assert.Equal(HorizontalAlignment.Stretch, section.HorizontalContentAlignment);
                    Xunit.Assert.Equal(VerticalAlignment.Top, section.VerticalContentAlignment);
                    Xunit.Assert.True(section.Focusable);
                    Xunit.Assert.True(section.IsTabStop);
                });
                timelineSections["Analysis and rough cut section"].IsExpanded = true;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Contains(
                    window.GetVisualDescendants().OfType<Button>(),
                    button => AutomationProperties.GetName(button) == "Analyze timeline motion" && button.IsVisible);
                timelineSections["Analysis and rough cut section"].IsExpanded = false;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.False(timelineSections["Analysis and rough cut section"].IsExpanded);
                Xunit.Assert.All(
                    window.GetVisualDescendants()
                        .OfType<ComboBox>()
                        .Where(control => AutomationProperties.GetName(control) is "Reference source preview" or "Reference target preview"),
                    selector => Xunit.Assert.NotNull(selector.ItemTemplate));

                var performanceStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline preview performance status");
                Xunit.Assert.Equal(viewModel.TimelinePerformanceStatusText, performanceStatus.Text);
                Xunit.Assert.True(performanceStatus.IsReadOnly);
                Xunit.Assert.Equal(11, performanceStatus.FontSize);
                Xunit.Assert.Equal(
                    "Performance details for the last local timeline preview.",
                    ToolTip.GetTip(performanceStatus));
                Xunit.Assert.Equal(
                    viewModel.TimelinePreviewStatusText,
                    window.GetVisualDescendants()
                        .OfType<TextBox>()
                        .Single(control => AutomationProperties.GetName(control) == "Timeline preview status")
                        .Text);
                Xunit.Assert.Equal(
                    "Current local timeline frame preview status.",
                    ToolTip.GetTip(window.GetVisualDescendants()
                        .OfType<TextBox>()
                        .Single(control => AutomationProperties.GetName(control) == "Timeline preview status")));
                Xunit.Assert.Equal(
                    viewModel.MotionAnalysisStatusText,
                    window.GetVisualDescendants()
                        .OfType<TextBox>()
                        .Single(control => AutomationProperties.GetName(control) == "Timeline motion analysis status")
                        .Text);
                Xunit.Assert.Equal(
                    "Local motion analysis result for the active timeline.",
                    ToolTip.GetTip(window.GetVisualDescendants()
                        .OfType<TextBox>()
                        .Single(control => AutomationProperties.GetName(control) == "Timeline motion analysis status")));
                Xunit.Assert.True(window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline preview empty state")
                    .IsVisible);
                var timelinePreview = window.GetVisualDescendants()
                    .OfType<Image>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline current frame preview");
                Xunit.Assert.True(double.IsNaN(timelinePreview.Width));
                Xunit.Assert.True(double.IsNaN(timelinePreview.Height));
                Xunit.Assert.Equal(HorizontalAlignment.Stretch, timelinePreview.HorizontalAlignment);
                Xunit.Assert.Equal(VerticalAlignment.Stretch, timelinePreview.VerticalAlignment);
                Xunit.Assert.Equal(Stretch.Uniform, timelinePreview.Stretch);
                var playTimeline = window.GetVisualDescendants()
                    .OfType<Button>()
                    .Single(control => AutomationProperties.GetName(control) == "Play timeline preview");
                Xunit.Assert.False(playTimeline.IsEnabled);
                Xunit.Assert.Equal("Play timeline", playTimeline.Content);
                var outputLabel = window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => control.Text == "OUTPUT");
                Xunit.Assert.Equal(10, outputLabel.FontSize);
                Xunit.Assert.Equal(
                    Avalonia.Media.Color.FromRgb(126, 143, 190),
                    Xunit.Assert.IsType<Avalonia.Media.SolidColorBrush>(outputLabel.Foreground).Color);
                Xunit.Assert.Equal(
                    "READY",
                    window.GetVisualDescendants()
                        .OfType<TextBlock>()
                        .Single(control => AutomationProperties.GetName(control) == "Timeline playback state")
                        .Text);
                Xunit.Assert.Equal(
                    viewModel.TimelineExportStatusText,
                    window.GetVisualDescendants()
                        .OfType<TextBox>()
                        .Single(control => AutomationProperties.GetName(control) == "Quick export status")
                        .Text);
                var projectStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline project status");
                Xunit.Assert.Equal(viewModel.TimelineProjectStatusText, projectStatus.Text);
                Xunit.Assert.Equal("Current local timeline project save status.", ToolTip.GetTip(projectStatus));
                var exportStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline export status");
                Xunit.Assert.Equal(viewModel.TimelineExportStatusText, exportStatus.Text);
                Xunit.Assert.Equal("Current local timeline video export status.", ToolTip.GetTip(exportStatus));
                Xunit.Assert.Equal(
                    viewModel.TimelineProjectStatusText,
                    window.GetVisualDescendants()
                        .OfType<TextBox>()
                        .Single(control => AutomationProperties.GetName(control) == "Quick save status")
                        .Text);
                Xunit.Assert.Equal(
                    11,
                    window.GetVisualDescendants()
                        .OfType<TextBox>()
                        .Single(control => AutomationProperties.GetName(control) == "Quick save status")
                        .FontSize);
                var timelineActionStatus = window.GetVisualDescendants()
                    .OfType<Border>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline action status");
                Xunit.Assert.True(timelineActionStatus.IsVisible);
                Xunit.Assert.Equal(
                    "Status for the latest local project save and timeline export.",
                    ToolTip.GetTip(timelineActionStatus));
                timelineSections["Analysis and rough cut section"].IsExpanded = true;
                timelineSections["Titles and captions section"].IsExpanded = true;
                Dispatcher.UIThread.RunJobs();
                var roughCutStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Rough cut status");
                Xunit.Assert.True(roughCutStatus.IsReadOnly);
                Xunit.Assert.Equal(11, roughCutStatus.FontSize);
                Xunit.Assert.Equal("Status of the latest waveform activity rough-cut plan.", ToolTip.GetTip(roughCutStatus));
                var beatMarkers = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline beat markers");
                Xunit.Assert.True(beatMarkers.IsReadOnly);
                Xunit.Assert.Equal(11, beatMarkers.FontSize);
                Xunit.Assert.Equal("Local waveform beat markers detected for the active timeline.", ToolTip.GetTip(beatMarkers));
                var titleAnimationStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Title animation status");
                Xunit.Assert.True(titleAnimationStatus.IsReadOnly);
                Xunit.Assert.Equal(11, titleAnimationStatus.FontSize);
                Xunit.Assert.Equal("Status of the latest title animation edit.", ToolTip.GetTip(titleAnimationStatus));
                var captionStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Caption status");
                Xunit.Assert.True(captionStatus.IsReadOnly);
                Xunit.Assert.Equal(11, captionStatus.FontSize);
                Xunit.Assert.Equal("Status of the latest local caption import or transcription.", ToolTip.GetTip(captionStatus));
                var watermarkStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Watermark status");
                Xunit.Assert.True(watermarkStatus.IsReadOnly);
                Xunit.Assert.Equal(11, watermarkStatus.FontSize);
                Xunit.Assert.Equal("Status of the selected watermark for timeline export.", ToolTip.GetTip(watermarkStatus));
                Xunit.Assert.False(window.GetVisualDescendants()
                    .OfType<Button>()
                    .Single(control => AutomationProperties.GetName(control) == "Stop timeline preview")
                    .IsEnabled);
                var selectionInspector = window.GetVisualDescendants()
                    .OfType<Border>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline selection inspector");
                Xunit.Assert.Contains(
                    selectionInspector.GetVisualDescendants().OfType<TextBox>(),
                    control => AutomationProperties.GetName(control) == "Timeline selection context");
                tabs.SelectedIndex = 1;
                Dispatcher.UIThread.RunJobs();
                var photoRecoverySection = window.GetVisualDescendants()
                    .OfType<Expander>()
                    .Single(control => AutomationProperties.GetName(control) == "Project recovery and media relink section");
                Xunit.Assert.False(photoRecoverySection.IsExpanded);
                photoRecoverySection.IsExpanded = true;
                Dispatcher.UIThread.RunJobs();
                var photoRecoveryStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Photo recovery status");
                Xunit.Assert.True(photoRecoveryStatus.IsReadOnly);
                Xunit.Assert.Equal(11, photoRecoveryStatus.FontSize);
                Xunit.Assert.Equal("Current local photo project recovery and relink status.", ToolTip.GetTip(photoRecoveryStatus));
                photoRecoverySection.IsExpanded = false;
                var photoSections = window.GetVisualDescendants()
                    .OfType<Expander>()
                    .ToDictionary(control => AutomationProperties.GetName(control)!, StringComparer.Ordinal);
                Xunit.Assert.False(photoSections["Layers and blend section"].IsExpanded);
                Xunit.Assert.True(photoSections["Grade section"].IsExpanded);
                Xunit.Assert.True(photoSections["Export section"].IsExpanded);
                photoSections["Layers and blend section"].IsExpanded = true;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Equal("100%", window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Photo overlay opacity value")
                    .Text);
                Xunit.Assert.Equal("95", window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Photo export quality value")
                    .Text);
                photoSections["Layers and blend section"].IsExpanded = false;
                Xunit.Assert.True(window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Photo preview empty state")
                    .IsVisible);
                Xunit.Assert.True(window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Photo original preview empty state")
                    .IsVisible);
                var photoEditorStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Photo editor status");
                Xunit.Assert.True(photoEditorStatus.IsReadOnly);
                Xunit.Assert.Equal(11, photoEditorStatus.FontSize);
                Xunit.Assert.Equal("Current local non-destructive photo edit status.", ToolTip.GetTip(photoEditorStatus));
                var photoComparison = window.GetVisualDescendants()
                    .OfType<Border>()
                    .Single(control => AutomationProperties.GetName(control) == "Photo before and after preview");
                Xunit.Assert.Equal(
                    "Compare the original source pixels with the current non-destructive edit.",
                    ToolTip.GetTip(photoComparison));
                tabs.SelectedIndex = 2;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.True(window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Color before preview empty state")
                    .IsVisible);
                Xunit.Assert.Equal(
                    180,
                    window.GetVisualDescendants()
                        .OfType<Slider>()
                        .Single(control => AutomationProperties.GetName(control) == "Exposure EV")
                        .Width);
                Xunit.Assert.True(window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Color after preview empty state")
                    .IsVisible);
                var colorComparison = window.GetVisualDescendants()
                    .OfType<Border>()
                    .Single(control => AutomationProperties.GetName(control) == "Color before and after preview");
                var colorInspectorStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Color inspector status");
                Xunit.Assert.True(colorInspectorStatus.IsReadOnly);
                Xunit.Assert.Equal(11, colorInspectorStatus.FontSize);
                Xunit.Assert.Equal("Current local color grade and preview status.", ToolTip.GetTip(colorInspectorStatus));
                var colorSections = window.GetVisualDescendants()
                    .OfType<Expander>()
                    .ToDictionary(control => AutomationProperties.GetName(control)!, StringComparer.Ordinal);
                colorSections["Style library section"].IsExpanded = true;
                colorSections["Reference match section"].IsExpanded = true;
                Dispatcher.UIThread.RunJobs();
                var beforeSummary = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Color grade before summary");
                Xunit.Assert.True(beforeSummary.IsReadOnly);
                Xunit.Assert.Equal(11, beforeSummary.FontSize);
                Xunit.Assert.Equal("Summary of the ungraded source preview.", ToolTip.GetTip(beforeSummary));
                var afterSummary = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Color grade after summary");
                Xunit.Assert.True(afterSummary.IsReadOnly);
                Xunit.Assert.Equal(11, afterSummary.FontSize);
                Xunit.Assert.Equal("Summary of the current graded preview.", ToolTip.GetTip(afterSummary));
                var scopeSummary = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Color scope summary");
                Xunit.Assert.True(scopeSummary.IsReadOnly);
                Xunit.Assert.Equal(11, scopeSummary.FontSize);
                Xunit.Assert.Equal("Summary of the local post-grade waveform and vectorscope scope.", ToolTip.GetTip(scopeSummary));
                var colorStyleStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Color style library status");
                Xunit.Assert.True(colorStyleStatus.IsReadOnly);
                Xunit.Assert.Equal(11, colorStyleStatus.FontSize);
                Xunit.Assert.Equal("Status of local reusable color style save and load operations.", ToolTip.GetTip(colorStyleStatus));
                var referenceStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Reference analysis status");
                Xunit.Assert.True(referenceStatus.IsReadOnly);
                Xunit.Assert.Equal(11, referenceStatus.FontSize);
                Xunit.Assert.Equal("Status of the latest bounded visual-reference analysis.", ToolTip.GetTip(referenceStatus));
                Xunit.Assert.Equal(
                    "Compare the ungraded source with the current shared color grade.",
                    ToolTip.GetTip(colorComparison));
                var colorScopes = window.GetVisualDescendants()
                    .OfType<Border>()
                    .Single(control => AutomationProperties.GetName(control) == "Color scopes preview");
                Xunit.Assert.Equal(
                    "Inspect the post-grade waveform and vectorscope generated from the local preview.",
                    ToolTip.GetTip(colorScopes));
                viewModel.ColorInspector.ExposureEV = 1.25f;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Equal(1.25, window.GetVisualDescendants()
                    .OfType<Slider>()
                    .Single(control => AutomationProperties.GetName(control) == "Exposure EV")
                    .Value);
                Xunit.Assert.Equal("1.25", window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Exposure EV value")
                    .Text);
                Xunit.Assert.Equal(
                    "Adjust Exposure EV from -8.00 to 8.00.",
                    ToolTip.GetTip(window.GetVisualDescendants()
                        .OfType<Slider>()
                        .Single(control => AutomationProperties.GetName(control) == "Exposure EV")));
                Xunit.Assert.Null(window.GetVisualDescendants()
                    .OfType<Image>()
                    .Single(control => AutomationProperties.GetName(control) == "Color waveform scope")
                    .Source);
                Xunit.Assert.Null(window.GetVisualDescendants()
                    .OfType<Image>()
                    .Single(control => AutomationProperties.GetName(control) == "Color vectorscope")
                    .Source);
                tabs.SelectedIndex = 3;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.True(window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "AI proposal empty state")
                    .IsVisible);
                var aiProposalStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "AI proposal status");
                Xunit.Assert.True(aiProposalStatus.IsReadOnly);
                Xunit.Assert.Equal(11, aiProposalStatus.FontSize);
                Xunit.Assert.Equal("Current AI proposal review and validation status.", ToolTip.GetTip(aiProposalStatus));
                var aiApprovalSummary = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "AI approval summary");
                Xunit.Assert.True(aiApprovalSummary.IsReadOnly);
                Xunit.Assert.Equal(11, aiApprovalSummary.FontSize);
                Xunit.Assert.Equal("Summary of the validated AI actions and their approval state.", ToolTip.GetTip(aiApprovalSummary));
                var aiRevisionStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "AI revision status");
                Xunit.Assert.True(aiRevisionStatus.IsReadOnly);
                Xunit.Assert.Equal(11, aiRevisionStatus.FontSize);
                Xunit.Assert.Equal("Status of the latest bounded AI proposal revision.", ToolTip.GetTip(aiRevisionStatus));
                var recipeSection = window.GetVisualDescendants()
                    .OfType<Expander>()
                    .Single(control => AutomationProperties.GetName(control) == "Reusable recipe section");
                recipeSection.IsExpanded = true;
                Dispatcher.UIThread.RunJobs();
                var aiRecipeStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "AI recipe status");
                Xunit.Assert.True(aiRecipeStatus.IsReadOnly);
                Xunit.Assert.Equal(11, aiRecipeStatus.FontSize);
                Xunit.Assert.Equal("Status of local AI recipe save and load operations.", ToolTip.GetTip(aiRecipeStatus));
                var geminiProviderStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Gemini provider status");
                Xunit.Assert.True(geminiProviderStatus.IsReadOnly);
                Xunit.Assert.Equal(11, geminiProviderStatus.FontSize);
                Xunit.Assert.Equal("Current optional Gemini provider configuration status.", ToolTip.GetTip(geminiProviderStatus));
                var activitySection = window.GetVisualDescendants()
                    .OfType<Expander>()
                    .Single(control => AutomationProperties.GetName(control) == "Activity section");
                activitySection.IsExpanded = true;
                Dispatcher.UIThread.RunJobs();
                var geminiAnalysisStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Gemini analysis status");
                Xunit.Assert.True(geminiAnalysisStatus.IsReadOnly);
                Xunit.Assert.Equal(11, geminiAnalysisStatus.FontSize);
                Xunit.Assert.Equal("Status of the latest bounded Gemini analysis request.", ToolTip.GetTip(geminiAnalysisStatus));
                var geminiUsageStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Gemini usage status");
                Xunit.Assert.True(geminiUsageStatus.IsReadOnly);
                Xunit.Assert.Equal(11, geminiUsageStatus.FontSize);
                Xunit.Assert.Equal("Usage and response metadata for the latest bounded Gemini request.", ToolTip.GetTip(geminiUsageStatus));
                var automationNames = new HashSet<string>(StringComparer.Ordinal);
                for (var workspaceIndex = 0; workspaceIndex < tabs.Items.Count; workspaceIndex++)
                {
                    tabs.SelectedIndex = workspaceIndex;
                    Dispatcher.UIThread.RunJobs();
                    foreach (var section in window.GetVisualDescendants().OfType<Expander>())
                    {
                        section.IsExpanded = true;
                    }

                    Dispatcher.UIThread.RunJobs();
                    automationNames.UnionWith(window.GetVisualDescendants()
                        .OfType<Control>()
                        .Select(AutomationProperties.GetName)
                        .Where(name => !string.IsNullOrWhiteSpace(name))!);
                }

                tabs.SelectedIndex = 0;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.DoesNotContain(window.GetVisualDescendants()
                    .OfType<Button>(),
                    button => button.Content is not null && string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)));
                Xunit.Assert.DoesNotContain(window.GetVisualDescendants()
                    .OfType<TextBox>(),
                    textBox => !string.IsNullOrWhiteSpace(textBox.PlaceholderText) && string.IsNullOrWhiteSpace(AutomationProperties.GetName(textBox)));
                foreach (var primaryActionName in new[]
                {
                    "Import local media",
                    "Play timeline preview",
                    "Export timeline MP4",
                    "Quick export timeline MP4"
                })
                {
                    Xunit.Assert.Contains(
                        window.GetVisualDescendants().OfType<Button>(),
                        button => AutomationProperties.GetName(button) == primaryActionName && button.Classes.Contains("primary"));
                }
                Xunit.Assert.Equal(
                    "Render the frame at the current playhead position.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<Button>().Single(control => AutomationProperties.GetName(control) == "Preview current timeline frame")));
                Xunit.Assert.Equal(
                    "Play the timeline from the current playhead position.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<Button>().Single(control => AutomationProperties.GetName(control) == "Play timeline preview")));
                Xunit.Assert.Equal(
                    "Render the editable timeline to an MP4 using the selected aspect ratio.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<Button>().Single(control => AutomationProperties.GetName(control) == "Export timeline MP4")));

                tabs.SelectedIndex = 1;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Equal(1, viewModel.SelectedWorkspaceIndex);
                Xunit.Assert.Contains(
                    window.GetVisualDescendants().OfType<Button>(),
                    button => AutomationProperties.GetName(button) == "Open image" && button.Classes.Contains("primary"));
                Xunit.Assert.Equal(
                    "Open a local image for non-destructive editing.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Open image")));
                Xunit.Assert.Contains(
                    window.GetVisualDescendants().OfType<Button>(),
                    button => AutomationProperties.GetName(button) == "Export photo image" && button.Classes.Contains("primary"));
                Xunit.Assert.Equal(
                    "Save the photo, adjustment layers and media references to a project file.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<Button>().Single(control => AutomationProperties.GetName(control) == "Save photo project")));
                Xunit.Assert.Equal(
                    "Render the current photo edit to the selected image format and dimensions.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<Button>().Single(control => AutomationProperties.GetName(control) == "Export photo image")));

                viewModel.SelectedWorkspaceIndex = 2;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Equal(2, tabs.SelectedIndex);
                foreach (var primaryActionName in new[]
                {
                    "Apply loaded color style",
                    "Analyze approved visual reference",
                    "Apply reviewed reference grade"
                })
                {
                    Xunit.Assert.Contains(
                        window.GetVisualDescendants().OfType<Button>(),
                        button => AutomationProperties.GetName(button) == primaryActionName && button.Classes.Contains("primary"));
                }
                Xunit.Assert.Equal(
                    "Apply the selected reusable style to the local color preview.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<Button>().Single(control => AutomationProperties.GetName(control) == "Apply loaded color style")));
                Xunit.Assert.Equal(
                    "Apply the reviewed reference grade to the local preview without uploading it again.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<Button>().Single(control => AutomationProperties.GetName(control) == "Apply reviewed reference grade")));

                var shortcuts = new[]
                {
                    (Key.D1, PhysicalKey.Digit1, "Video"),
                    (Key.D2, PhysicalKey.Digit2, "Photo"),
                    (Key.D3, PhysicalKey.Digit3, "Color"),
                    (Key.D4, PhysicalKey.Digit4, "AI")
                };
                foreach (var (key, physicalKey, workspaceName) in shortcuts)
                {
                    window.KeyPress(key, RawInputModifiers.Control, physicalKey, key.ToString());
                    window.KeyRelease(key, RawInputModifiers.Control, physicalKey, key.ToString());
                    Xunit.Assert.Equal(workspaceName, viewModel.SelectedWorkspace.Name);
                    automationNames.UnionWith(window.GetVisualDescendants()
                        .OfType<Control>()
                        .Select(AutomationProperties.GetName)
                        .Where(name => !string.IsNullOrWhiteSpace(name))!);
                }
                foreach (var primaryActionName in new[]
                {
                    "Analyze approved samples with Gemini",
                    "Apply approved AI actions"
                })
                {
                    Xunit.Assert.Contains(
                        window.GetVisualDescendants().OfType<Button>(),
                        button => AutomationProperties.GetName(button) == primaryActionName && button.Classes.Contains("primary"));
                }
                Xunit.Assert.Equal(
                    "Apply only checked, validated actions transactionally to the editable project.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<Button>().Single(control => AutomationProperties.GetName(control) == "Apply approved AI actions")));
                Xunit.Assert.Equal(
                    "Undo the last successful AI edit transaction without changing source media.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<Button>().Single(control => AutomationProperties.GetName(control) == "Undo last AI apply")));

                foreach (var requiredName in new[]
                {
                    "Imported media",
                    "Media bin",
                    "Import local media",
                    "Local media import drop zone",
                    "Capability status",
                    "Diagnostics status",
                    "Workspace keyboard shortcuts",
                    "Media import status",
                    "Search imported media",
                    "Clear media search",
                    "Selected media summary",
                    "Media search empty state",
                    "Media bin empty state",
                    "Active workspace status",
                    "Timeline preview status",
                    "Timeline program monitor",
                    "Timeline monitor controls",
                    "Timeline navigation controls",
                    "Timeline frame readout",
                    "Timeline position readout",
                    "Timeline zoom value",
                    "Timeline visual ruler",
                    "Timeline motion analysis status",
                    "Rough cut status",
                    "Title animation status",
                    "Caption status",
                    "Timeline project status",
                    "Timeline export status",
                    "Quick save status",
                    "Quick export status",
                    "Timeline action status",
                    "Watermark status",
                    "Missing photo media",
                    "Photo recovery status",
                    "Photo editor status",
                    "Photo original preview",
                    "Photo edit preview",
                    "Color grade before summary",
                    "Color grade after summary",
                    "Color scope summary",
                    "Color inspector status",
                    "Color waveform scope",
                    "Color vectorscope",
                    "Exposure EV value",
                    "Contrast value",
                    "Saturation value",
                    "Temperature value",
                    "Tint value",
                    "Color style library status",
                    "Reference analysis status",
                    "Gemini provider status",
                    "Gemini video frame scope",
                    "Gemini audio waveform scope",
                    "Gemini photo preview scope",
                    "Gemini upload consent",
                    "Gemini analysis status",
                    "AI proposal status",
                    "AI approval summary",
                    "AI revision status",
                    "AI recipe status",
                    "Timeline frame status",
                    "Timeline position status",
                    "Timeline ruler",
                    "Timeline selection context",
                    "Timeline beat markers",
                    "Timeline operation status",
                    "Preview current timeline frame",
                    "Save timeline project",
                    "Open timeline project",
                    "Export timeline MP4",
                    "Color grade before preview",
                    "Color grade after preview",
                    "Photo export format",
                    "Photo export quality",
                    "Gemini model",
                    "Analyze approved visual reference",
                    "Apply reviewed reference grade",
                    "Analyze approved samples with Gemini",
                    "Validate AI proposal",
                    "Apply approved AI actions",
                    "Undo last AI apply",
                    "Rotate photo 90 degrees clockwise",
                    "Undo last photo edit",
                    "Clear photo overlays",
                    "Clear last photo overlay mask",
                    "Clear photo clone retouch",
                    "Mask last photo grade",
                    "Clear last photo grade mask",
                    "Clear AI proposal",
                    "Apply fade-in animation preset",
                    "Apply push-in animation preset",
                    "Clear selected clip animation",
                    "Duck selected audio by 12 dB",
                    "Clear selected audio ducking",
                    "Clear selected audio settings",
                    "Apply fade-in animation to last title",
                    "Apply push-in animation to last title"
                })
                {
                    Xunit.Assert.Contains(requiredName, automationNames);
                }
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);
    }

    [Xunit.Fact]
    public async Task ColorScopesRenderFromTheRealInspectorSource()
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApplication));
        await session.Dispatch(() =>
        {
            var viewModel = new FrameTheory.Desktop.MainWindowViewModel(
                new FrameTheory.Desktop.WorkspaceCatalog(),
                new TestCapabilityDetector());
            viewModel.ColorInspector.SetSource([.. "P6\n2 1\n255\n"u8, 255, 0, 0, 0, 0, 255]);
            var window = new FrameTheory.Desktop.MainWindow(viewModel);
            try
            {
                window.Show();
                var tabs = Xunit.Assert.IsType<TabControl>(Xunit.Assert.IsType<Grid>(window.Content).Children[1]);
                tabs.SelectedIndex = 2;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.NotNull(window.GetVisualDescendants()
                    .OfType<Image>()
                    .Single(control => AutomationProperties.GetName(control) == "Color waveform scope")
                    .Source);
                Xunit.Assert.NotNull(window.GetVisualDescendants()
                    .OfType<Image>()
                    .Single(control => AutomationProperties.GetName(control) == "Color vectorscope")
                    .Source);
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);
    }

    [Xunit.Fact]
    public async Task ValidatedAiActionsRenderAsNamedReviewCards()
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApplication));
        await session.Dispatch(() =>
        {
            var viewModel = new FrameTheory.Desktop.MainWindowViewModel(
                new FrameTheory.Desktop.WorkspaceCatalog(),
                new TestCapabilityDetector());
            viewModel.AiProposal.Validate(
                """
                {"schemaVersion":"1.0","planId":"plan-1","projectId":"active-project","intent":"grade","actions":[{"id":"grade-1","type":"color.adjust","targetId":"clip-1","startFrame":0,"durationFrames":10,"params":{"exposureEV":0.25,"contrast":0,"saturation":0,"temperature":0,"tint":0}}]}
                """,
                new AiPlanValidationContext("active-project", new HashSet<string>(["clip-1"]), 20));
            var window = new FrameTheory.Desktop.MainWindow(viewModel);
            try
            {
                window.Show();
                var tabs = window.GetVisualDescendants()
                    .OfType<TabControl>()
                    .Single(control => AutomationProperties.GetName(control) == "Workspaces");
                tabs.SelectedIndex = 3;
                Dispatcher.UIThread.RunJobs();
                var approval = window.GetVisualDescendants()
                    .OfType<CheckBox>()
                    .Single(control => AutomationProperties.GetName(control)?.StartsWith("Approve AI action:", StringComparison.Ordinal) == true);

                Xunit.Assert.IsType<Border>(approval.Parent);
                Xunit.Assert.Contains("Approve AI action:", AutomationProperties.GetName(approval), StringComparison.Ordinal);
                var approvalCard = Xunit.Assert.IsType<Border>(approval.Parent);
                Xunit.Assert.Equal(
                    Avalonia.Media.Color.FromRgb(24, 27, 33),
                    Xunit.Assert.IsType<Avalonia.Media.SolidColorBrush>(approvalCard.Background).Color);
                approval.IsChecked = true;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Equal(
                    Avalonia.Media.Color.FromRgb(95, 125, 255),
                    Xunit.Assert.IsType<Avalonia.Media.SolidColorBrush>(approvalCard.BorderBrush).Color);
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);
    }

    [Xunit.Fact]
    public void DesktopKeyboardShortcutsSwitchWorkspacesAndStepTimeline()
    {
        var viewModel = new FrameTheory.Desktop.MainWindowViewModel(
            new FrameTheory.Desktop.WorkspaceCatalog(),
            new TestCapabilityDetector());

        Xunit.Assert.True(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.AiWorkspace));
        Xunit.Assert.Equal("AI", viewModel.SelectedWorkspace.Name);
        Xunit.Assert.True(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.VideoWorkspace));
        Xunit.Assert.True(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.NextFrame));
        Xunit.Assert.Equal(0, viewModel.TimelineEditor.CurrentFrame);
        Xunit.Assert.False(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.UndoAiEdit));
    }

    [Xunit.Fact]
    public void DesktopKeyboardShortcutsCoverEveryWorkspace()
    {
        var viewModel = new FrameTheory.Desktop.MainWindowViewModel(
            new FrameTheory.Desktop.WorkspaceCatalog(),
            new TestCapabilityDetector());

        Xunit.Assert.True(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.VideoWorkspace));
        Xunit.Assert.Equal("Video", viewModel.SelectedWorkspace.Name);
        Xunit.Assert.True(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.PhotoWorkspace));
        Xunit.Assert.Equal("Photo", viewModel.SelectedWorkspace.Name);
        Xunit.Assert.True(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.ColorWorkspace));
        Xunit.Assert.Equal("Color", viewModel.SelectedWorkspace.Name);
        Xunit.Assert.True(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.AiWorkspace));
        Xunit.Assert.Equal("AI", viewModel.SelectedWorkspace.Name);
    }

    [Xunit.Fact]
    public void DesktopWorkspaceSelectionPersistsAndRejectsInvalidSavedState()
    {
        var state = new TestWorkspaceStateStore(99);
        var first = new FrameTheory.Desktop.MainWindowViewModel(
            new FrameTheory.Desktop.WorkspaceCatalog(),
            new TestCapabilityDetector(),
            workspaceStateStore: state);

        Xunit.Assert.Equal("Video", first.SelectedWorkspace.Name);

        first.SelectedWorkspaceIndex = 2;

        var reopened = new FrameTheory.Desktop.MainWindowViewModel(
            new FrameTheory.Desktop.WorkspaceCatalog(),
            new TestCapabilityDetector(),
            workspaceStateStore: state);

        Xunit.Assert.Equal("Color", reopened.SelectedWorkspace.Name);
    }

    [Xunit.Fact]
    public async Task FileWorkspaceStateStoreRoundTripsAtomically()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-workspace-state-");
        try
        {
            var path = Path.Combine(directory.FullName, "state.json");
            var store = new FrameTheory.Desktop.FileWorkspaceStateStore(path);

            store.SaveSelectedWorkspaceIndex(3);

            Xunit.Assert.Equal(3, store.LoadSelectedWorkspaceIndex());
            Xunit.Assert.Contains("SelectedWorkspaceIndex", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
            Xunit.Assert.Empty(Directory.GetFiles(directory.FullName, "*.tmp"));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public void FileWorkspaceStateStoreIgnoresReparsePointStateFiles()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-workspace-boundary-");
        var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-workspace-external-");
        try
        {
            var externalPath = Path.Combine(externalDirectory.FullName, "state.json");
            File.WriteAllText(externalPath, "{\"SelectedWorkspaceIndex\":3}");
            var linkedPath = Path.Combine(directory.FullName, "state.json");
            File.CreateSymbolicLink(linkedPath, externalPath);
            var store = new FrameTheory.Desktop.FileWorkspaceStateStore(linkedPath);

            Xunit.Assert.Equal(0, store.LoadSelectedWorkspaceIndex());
            store.SaveSelectedWorkspaceIndex(2);
            Xunit.Assert.Equal("{\"SelectedWorkspaceIndex\":3}", File.ReadAllText(externalPath));
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
    public void CapabilitySummaryProvidesActionableMediaDependencyGuidance()
    {
        var report = new CapabilityReport("Test", "x64",
        [
            new("FFmpeg", CapabilityState.Missing, "Not found on PATH."),
            new("ffprobe", CapabilityState.Missing, "Not found on PATH."),
            new("Storage", CapabilityState.Available, "ready")
        ]);

        Xunit.Assert.Contains("install FFmpeg and add it to PATH", report.Summary, StringComparison.Ordinal);
        Xunit.Assert.Contains("ffprobe: missing", report.Summary, StringComparison.Ordinal);
    }

    [Xunit.Fact]
    public async Task CapabilityDetectorReportsHostAndHonorsCancellation()
    {
        var detector = new FrameTheory.Platform.SystemCapabilityDetector();
        var report = await detector.DetectAsync(CancellationToken.None);

        Xunit.Assert.NotEmpty(report.Platform);
        Xunit.Assert.Contains(report.Entries, entry => entry.Name == "FFmpeg");
        Xunit.Assert.Contains(report.Entries, entry => entry.Name == "ffprobe");
        Xunit.Assert.Contains(report.Entries, entry => entry.Name == "Storage");

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Xunit.Assert.ThrowsAsync<OperationCanceledException>(() => detector.DetectAsync(cancellation.Token));
    }

    [Xunit.Fact]
    public async Task CapabilityDetectorReportsNonzeroDependencyProbeAsUnknown()
    {
        var detector = new FrameTheory.Platform.SystemCapabilityDetector(
            "dotnet",
            "dotnet",
            "--frame-theory-invalid-probe",
            gpuExecutable: "dotnet",
            gpuArguments: ["--frame-theory-invalid-gpu-probe"]);

        var report = await detector.DetectAsync(CancellationToken.None);

        Xunit.Assert.All(report.Entries.Where(entry => entry.Name is "FFmpeg" or "ffprobe"), entry =>
        {
            Xunit.Assert.Equal(CapabilityState.Unknown, entry.State);
            Xunit.Assert.Contains("Exited with code", entry.Detail, StringComparison.Ordinal);
        });
        Xunit.Assert.Contains("FFmpeg: unknown (Exited with code", report.Summary, StringComparison.Ordinal);
        Xunit.Assert.Contains(report.Entries, entry => entry.Name == "GPU" && entry.State == CapabilityState.Unknown);
    }

    [Xunit.Fact]
    public async Task CapabilityDetectorUsesGpuProbeWithoutClaimingAcceleration()
    {
        var detector = new FrameTheory.Platform.SystemCapabilityDetector(
            "dotnet",
            "dotnet",
            "--version",
            gpuExecutable: "dotnet",
            gpuArguments: ["--version"]);

        var report = await detector.DetectAsync(CancellationToken.None);
        var gpu = Xunit.Assert.Single(report.Entries, entry => entry.Name == "GPU");

        Xunit.Assert.Equal(CapabilityState.Unknown, gpu.State);
        Xunit.Assert.DoesNotContain("not configured", gpu.Detail, StringComparison.OrdinalIgnoreCase);
        Xunit.Assert.Contains("CPU fallback", gpu.Detail, StringComparison.Ordinal);
    }

    [Xunit.Fact]
    public async Task CapabilityFailureIsVisibleAndRecordedLocally()
    {
        var diagnostics = new RecordingDiagnostics();
        var viewModel = new FrameTheory.Desktop.MainWindowViewModel(
            new FrameTheory.Desktop.WorkspaceCatalog(),
            new ThrowingCapabilityDetector(),
            diagnosticsLog: diagnostics);

        await viewModel.RefreshCapabilitiesAsync(CancellationToken.None);

        Xunit.Assert.Contains("Capability check failed", viewModel.CapabilityStatusText, StringComparison.Ordinal);
        Xunit.Assert.Equal("capability-check-failed", diagnostics.EventName);
    }

    [Xunit.Fact]
    public async Task MediaProcessRunnerPassesArgumentsWithoutShellParsing()
    {
        var runner = new FrameTheory.Media.FfmpegProcessRunner();
        var result = await runner.RunAsync("dotnet", ["--version"], CancellationToken.None);

        Xunit.Assert.Equal(0, result.ExitCode);
        Xunit.Assert.Contains("10.0", result.StandardOutput);
    }

    [Xunit.Fact]
    public async Task MediaProcessRunnerHonorsPreCanceledWork()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var runner = new FrameTheory.Media.FfmpegProcessRunner();

        await Xunit.Assert.ThrowsAsync<OperationCanceledException>(() =>
            runner.RunAsync("dotnet", ["--version"], cancellation.Token));
    }

    [Xunit.Fact]
    public void RationalFrameRateMapsVariableRatesWithoutFloatingPointClocks()
    {
        var normalized = new FrameTheory.Domain.RationalFrameRate(60, 2);
        var ntsc = new FrameTheory.Domain.RationalFrameRate(30000, 1001);

        Xunit.Assert.Equal("30/1", normalized.ToString());
        Xunit.Assert.Equal(90, normalized.FrameIndexAt(TimeSpan.FromSeconds(3)));
        Xunit.Assert.Equal(29, ntsc.FrameIndexAt(TimeSpan.FromSeconds(1)));
        Xunit.Assert.Equal(TimeSpan.FromSeconds(3), normalized.TimeAtFrame(90));
    }

    [Xunit.Fact]
    public void DomainFrameAndMediaTypesRejectInvalidBoundsAndRemainStable()
    {
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new FrameTheory.Domain.RationalFrameRate(0, 1));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new FrameTheory.Domain.RationalFrameRate(30, 0));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new FrameTheory.Domain.RationalFrameRate(30, 1).FrameIndexAt(TimeSpan.FromSeconds(-1)));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new FrameTheory.Domain.FrameRange(10, 0));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new FrameTheory.Domain.FrameRange(long.MaxValue, 2));

        var range = new FrameTheory.Domain.FrameRange(10, 5);
        Xunit.Assert.True(range.Contains(10));
        Xunit.Assert.False(range.Contains(15));

        var source = new Uri("file:///media/clip.mov");
        var firstId = FrameTheory.Domain.MediaId.Create(source, "sha256:abc");
        var secondId = FrameTheory.Domain.MediaId.Create(source, "sha256:abc");
        Xunit.Assert.Equal(firstId, secondId);

        Xunit.Assert.Throws<ArgumentException>(() => FrameTheory.Domain.MediaId.Create(new Uri("file:///media/clip.mov"), ""));
        Xunit.Assert.Throws<ArgumentException>(() => new FrameTheory.Domain.MediaSourceMetadata(
            firstId, source, FrameTheory.Domain.MediaKind.Video, 1920, 1080, null, 30));
    }

    private sealed class TestCapabilityDetector : ICapabilityDetector
    {
        public Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken) => Task.FromResult(
            new CapabilityReport("Test OS", "x64", [new("Storage", CapabilityState.Available, "ready")]));
    }

    private sealed class HeadlessTestApplication : Avalonia.Application
    {
        public HeadlessTestApplication()
        {
            Styles.Add(new FluentTheme());
        }
    }

    private sealed class TestWorkspaceStateStore(int selectedWorkspaceIndex) : FrameTheory.Desktop.IWorkspaceStateStore
    {
        private int _selectedWorkspaceIndex = selectedWorkspaceIndex;

        public int LoadSelectedWorkspaceIndex() => _selectedWorkspaceIndex;

        public void SaveSelectedWorkspaceIndex(int index) => _selectedWorkspaceIndex = index;
    }

    private sealed class ThrowingCapabilityDetector : ICapabilityDetector
    {
        public Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken) =>
            Task.FromException<CapabilityReport>(new InvalidOperationException("synthetic capability failure"));
    }

    private sealed class RecordingDiagnostics : IDiagnosticsLog
    {
        public string LogPath => "synthetic";

        public string? EventName { get; private set; }

        public void Record(string eventName, Exception? exception = null) => EventName = eventName;
    }
}
