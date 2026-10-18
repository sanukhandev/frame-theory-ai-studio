using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Reflection;
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
                Xunit.Assert.Equal(
                    "Local FFmpeg, ffprobe and platform capability status.",
                    ToolTip.GetTip(window.GetVisualDescendants()
                        .OfType<TextBlock>()
                        .Single(control => AutomationProperties.GetName(control) == "Capability status")));
                Xunit.Assert.Equal(
                    new[] { "Video workspace", "Photo workspace", "Color workspace", "AI workspace" },
                    tabItems.Select(AutomationProperties.GetName));
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
                var activeWorkspaceStatus = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Active workspace status");
                Xunit.Assert.Equal(viewModel.ActiveWorkspaceStatusText, activeWorkspaceStatus.Text);
                Xunit.Assert.Equal(
                    "Live save, export, edit or provider status for the active workspace.",
                    ToolTip.GetTip(activeWorkspaceStatus));
                tabs.SelectedIndex = 1;
                Dispatcher.UIThread.RunJobs();
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
                Xunit.Assert.Equal(2, mediaBinGrid.ColumnDefinitions.Count);
                Xunit.Assert.Contains(mediaBin.GetVisualDescendants().OfType<Grid>(), control => control.ColumnDefinitions.Count == 4);
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
                var mediaSearch = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Search imported media");
                Xunit.Assert.Equal(190, mediaSearch.Width);
                Xunit.Assert.Equal("Filter imported media by filename.", ToolTip.GetTip(mediaSearch));
                Xunit.Assert.False(window.GetVisualDescendants()
                    .OfType<Button>()
                    .Single(control => AutomationProperties.GetName(control) == "Clear media search")
                    .IsEnabled);
                Xunit.Assert.False(window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Media search empty state")
                    .IsVisible);
                var timelineTracks = window.GetVisualDescendants()
                    .OfType<ListBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline tracks");
                Xunit.Assert.NotNull(timelineTracks.ItemTemplate);
                Xunit.Assert.Equal(124, timelineTracks.Height);
                Xunit.Assert.Equal(
                    "Scrub to an exact timeline frame.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<Slider>().Single(control => AutomationProperties.GetName(control) == "Timeline playhead frame")));
                Xunit.Assert.Equal(
                    "Current frame and timeline position.",
                    ToolTip.GetTip(window.GetVisualDescendants().OfType<TextBlock>().Single(control => AutomationProperties.GetName(control) == "Timeline ruler")));
                Xunit.Assert.True(window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline operation status")
                    .IsReadOnly);
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
                Xunit.Assert.True(timelineSections["Project and export section"].IsExpanded);
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
                Xunit.Assert.True(window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline preview empty state")
                    .IsVisible);
                var selectionInspector = window.GetVisualDescendants()
                    .OfType<Border>()
                    .Single(control => AutomationProperties.GetName(control) == "Timeline selection inspector");
                Xunit.Assert.Contains(
                    selectionInspector.GetVisualDescendants().OfType<TextBlock>(),
                    control => AutomationProperties.GetName(control) == "Timeline selection context");
                tabs.SelectedIndex = 1;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.True(window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Photo preview empty state")
                    .IsVisible);
                tabs.SelectedIndex = 2;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.True(window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Color before preview empty state")
                    .IsVisible);
                Xunit.Assert.True(window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "Color after preview empty state")
                    .IsVisible);
                tabs.SelectedIndex = 3;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.True(window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => AutomationProperties.GetName(control) == "AI proposal empty state")
                    .IsVisible);
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
                    "Export timeline MP4"
                })
                {
                    Xunit.Assert.Contains(
                        window.GetVisualDescendants().OfType<Button>(),
                        button => AutomationProperties.GetName(button) == primaryActionName && button.Classes.Contains("primary"));
                }

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
                    "Media search empty state",
                    "Timeline preview status",
                    "Timeline motion analysis status",
                    "Rough cut status",
                    "Title animation status",
                    "Caption status",
                    "Timeline project status",
                    "Timeline export status",
                    "Watermark status",
                    "Missing photo media",
                    "Photo recovery status",
                    "Photo editor status",
                    "Color grade before summary",
                    "Color grade after summary",
                    "Color scope summary",
                    "Color inspector status",
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
