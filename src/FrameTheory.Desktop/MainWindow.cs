using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using FrameTheory.AI;
using FrameTheory.Domain;
using FrameTheory.Media;
using System.Runtime.InteropServices;
using FrameTheory.Photo;

namespace FrameTheory.Desktop;

public sealed class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        DragDrop.SetAllowDrop(this, true);
        DragDrop.AddDropHandler(this, OnDrop);
        KeyDown += OnKeyDown;
        Focusable = true;
        Title = "Frame Theory Studio AI";
        Width = 1280;
        Height = 800;
        MinWidth = 960;
        MinHeight = 640;

        var tabs = new TabControl();
        foreach (var workspace in viewModel.Workspaces)
        {
            tabs.Items.Add(new TabItem
            {
                Header = workspace.Name,
                Content = CreateWorkspaceContent(workspace, viewModel)
            });
        }
        tabs.Bind(SelectingItemsControl.SelectedIndexProperty, new Binding(nameof(MainWindowViewModel.SelectedWorkspaceIndex))
        {
            Mode = BindingMode.TwoWay
        });

        var header = new Border
        {
            Padding = new Thickness(20, 16),
            Background = Brushes.Black,
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock
                    {
                        Text = "FRAME THEORY STUDIO AI",
                        FontSize = 20,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = Brushes.White
                    },
                    new TextBlock
                    {
                        Text = "Offline-first creative editing",
                        Foreground = Brushes.LightGray
                    },
                    new TextBlock
                    {
                        Text = "Shortcuts: Ctrl/⌘+1–4 workspaces · Left/Right step video frames · Ctrl/⌘+Z undo AI apply",
                        Foreground = Brushes.LightGray,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };

        var layout = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Children =
            {
                header,
                tabs,
                CreateStatusBar(viewModel)
            }
        };
        Grid.SetRow(tabs, 1);
        Grid.SetRow(layout.Children[2], 2);
        Content = layout;
    }

    private void OnKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Source is TextBox || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var commandModifier = (args.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;
        var shortcut = commandModifier
            ? args.Key switch
            {
                Key.D1 => EditorShortcut.VideoWorkspace,
                Key.D2 => EditorShortcut.PhotoWorkspace,
                Key.D3 => EditorShortcut.ColorWorkspace,
                Key.D4 => EditorShortcut.AiWorkspace,
                Key.Z => EditorShortcut.UndoAiEdit,
                _ => (EditorShortcut?)null
            }
            : viewModel.SelectedWorkspaceIndex == 0
                ? args.Key switch
                {
                    Key.Left => EditorShortcut.PreviousFrame,
                    Key.Right => EditorShortcut.NextFrame,
                    _ => (EditorShortcut?)null
                }
                : null;

        if (shortcut is not null && viewModel.HandleShortcut(shortcut.Value))
        {
            args.Handled = true;
        }
    }

    private async void OnDrop(object? sender, DragEventArgs args)
    {
        var files = args.DataTransfer.TryGetFiles()?
            .Select(item => item.TryGetLocalPath())
            .Where(path => path is not null)
            .Select(path => path!)
            .ToArray() ?? [];
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.ImportFilesAsync(files, CancellationToken.None);
        }
    }

    private static Control CreateStatusBar(MainWindowViewModel viewModel)
    {
        var status = new TextBlock
        {
            Margin = new Thickness(16, 8),
            Foreground = Brushes.LightGray
        };
        status.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.CapabilityStatusText)));
        var diagnostics = new TextBlock
        {
            Margin = new Thickness(16, 0, 16, 8),
            Foreground = Brushes.Gray,
            TextWrapping = TextWrapping.Wrap
        };
        diagnostics.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.DiagnosticsStatusText)));
        return new Border
        {
            Background = Brushes.Black,
            Child = new StackPanel { Children = { status, diagnostics } }
        };
    }

    private static Control CreateWorkspaceContent(WorkspaceViewModel workspace, MainWindowViewModel viewModel)
    {
        var importStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        importStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.MediaImportStatusText)));
        var importedList = new ListBox { Height = 140 };
        AutomationProperties.SetName(importedList, "Imported media");
        importedList.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainWindowViewModel.ImportedMedia)));
        var children = new List<Control>
        {
            new TextBlock
            {
                Text = workspace.Name,
                FontSize = 28,
                FontWeight = FontWeight.SemiBold
            },
            new TextBlock
            {
                Text = workspace.Description,
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap
            },
            new Border
            {
                Margin = new Thickness(0, 12, 0, 0),
                Padding = new Thickness(12),
                Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 48, 48)),
                Child = new TextBlock
                {
                    Text = workspace.OfflineMessage,
                    TextWrapping = TextWrapping.Wrap
                }
            },
            new TextBlock
            {
                Margin = new Thickness(0, 16, 0, 0),
                Text = "Drop local media files here"
            },
            importStatus,
            importedList
        };
        if (workspace.Name == "Photo")
        {
            children.Add(CreatePhotoRecoveryPanel(viewModel));
            if (viewModel.PhotoEditor is not null)
            {
                children.Add(CreatePhotoEditorPanel(viewModel.PhotoEditor));
            }
        }
        else if (workspace.Name == "Color")
        {
            children.Add(CreateColorInspectorPanel(viewModel));
        }
        else if (workspace.Name == "Video")
        {
            children.Add(CreateTimelinePanel(viewModel));
        }
        else if (workspace.Name == "AI")
        {
            children.Add(CreateGeminiSettingsPanel(viewModel));
            children.Add(CreateAiProposalPanel(viewModel));
        }

        var stack = new StackPanel { Spacing = 12 };
        stack.Children.AddRange(children);
        return new ScrollViewer
        {
            Content = new Border
            {
                Margin = new Thickness(24),
                Padding = new Thickness(24),
                Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(35, 35, 35)),
                CornerRadius = new CornerRadius(8),
                Child = stack
            }
        };
    }

    private static Control CreatePhotoRecoveryPanel(MainWindowViewModel viewModel)
    {
        var projectPath = new TextBox { PlaceholderText = "Project path (.ftstudio)" };
        projectPath.Bind(TextBox.TextProperty, new Binding("PhotoRecovery.ProjectPath") { Mode = BindingMode.TwoWay });
        var replacementPath = new TextBox { PlaceholderText = "Replacement media path" };
        var missingList = new ListBox { Height = 90 };
        missingList.Bind(ItemsControl.ItemsSourceProperty, new Binding("PhotoRecovery.MissingMedia"));
        missingList.Bind(SelectingItemsControl.SelectedItemProperty, new Binding("PhotoRecovery.SelectedMissingMedia")
        {
            Mode = BindingMode.TwoWay
        });
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        status.Bind(TextBlock.TextProperty, new Binding("PhotoRecovery.StatusText"));
        var open = new Button { Content = "Open / recover photo project" };
        open.Click += async (_, _) => await viewModel.PhotoRecovery.OpenProjectAsync(CancellationToken.None);
        var relink = new Button { Content = "Relink selected media" };
        relink.Click += async (_, _) => await viewModel.PhotoRecovery.RelinkSelectedAsync(replacementPath.Text ?? string.Empty, CancellationToken.None);
        return new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(12),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 48, 48)),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Project recovery and media relink", FontWeight = FontWeight.SemiBold },
                    projectPath,
                    open,
                    status,
                    new TextBlock { Text = "Missing media" },
                    missingList,
                    replacementPath,
                    relink
                }
            }
        };
    }

    private static Control CreatePhotoEditorPanel(PhotoEditorViewModel editor)
    {
        var preview = new Image { Width = 320, Height = 180, Stretch = Stretch.Uniform };
        void RefreshPreview() => preview.Source = CreateBitmap(editor.PreviewPpm);
        editor.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(PhotoEditorViewModel.PreviewPpm) or nameof(PhotoEditorViewModel.Current))
            {
                RefreshPreview();
            }
        };
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        status.Bind(TextBlock.TextProperty, new Binding(nameof(PhotoEditorViewModel.StatusText)));
        var open = new Button { Content = "Open image" };
        open.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(open);
            if (topLevel is null)
            {
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Images") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp", "*.tif", "*.tiff"] }]
            });
            var path = files.SingleOrDefault()?.TryGetLocalPath();
            if (path is not null)
            {
                await editor.OpenAsync(path, CancellationToken.None);
            }
        };
        var rotate = new Button { Content = "Rotate 90°" };
        rotate.Click += (_, _) => editor.RotateRight();
        var undo = new Button { Content = "Undo photo edit" };
        undo.Click += (_, _) => editor.Undo();
        var format = new ComboBox { ItemsSource = new[] { "PNG", "JPEG" }, SelectedIndex = 0, Width = 90 };
        var quality = new Slider { Minimum = 1, Maximum = 100, Value = 95, Width = 140 };
        var socialPreset = new ComboBox { ItemsSource = new[] { "Original", "16:9 landscape", "1:1 square", "9:16 portrait" }, SelectedIndex = 0, Width = 150 };
        AutomationProperties.SetName(socialPreset, "Photo export social dimensions");
        var export = new Button { Content = "Export image" };
        export.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(export);
            if (topLevel is null)
            {
                return;
            }

            var jpeg = format.SelectedIndex == 1;
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = jpeg ? "frame-theory-photo.jpg" : "frame-theory-photo.png",
                FileTypeChoices = [new FilePickerFileType(jpeg ? "JPEG image" : "PNG image") { Patterns = [jpeg ? "*.jpg" : "*.png"] }]
            });
            var path = file?.TryGetLocalPath();
            if (path is not null)
            {
                var selectedPreset = socialPreset.SelectedIndex switch
                {
                    1 => SocialPhotoExportPreset.Create(SocialAspectRatio.Landscape16By9),
                    2 => SocialPhotoExportPreset.Create(SocialAspectRatio.Square1By1),
                    3 => SocialPhotoExportPreset.Create(SocialAspectRatio.Portrait9By16),
                    _ => null
                };
                await editor.ExportAsync(path, jpeg ? PhotoExportFormat.Jpeg : PhotoExportFormat.Png, (int)Math.Round(quality.Value), CancellationToken.None, selectedPreset);
            }
        };
        return new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(12),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 48, 48)),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Non-destructive photo editor", FontWeight = FontWeight.SemiBold },
                    preview,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { open, rotate, undo } },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { format, quality, socialPreset, export } },
                    status
                }
            }
        };
    }

    private static Control CreateGeminiSettingsPanel(MainWindowViewModel viewModel)
    {
        var settings = viewModel.GeminiSettings;
        var model = new ComboBox { ItemsSource = settings.Models, SelectedIndex = 0 };
        model.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(GeminiSettingsViewModel.SelectedModelId))
        {
            Mode = BindingMode.TwoWay
        });
        var apiKey = new TextBox { PlaceholderText = "Gemini API key (stored securely)", PasswordChar = '•' };
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        status.Bind(TextBlock.TextProperty, new Binding(nameof(GeminiSettingsViewModel.StatusText)));
        var save = new Button { Content = "Store Gemini key" };
        save.Click += async (_, _) =>
        {
            await settings.SaveAsync(apiKey.Text ?? string.Empty, CancellationToken.None);
            apiKey.Text = string.Empty;
        };
        var clear = new Button { Content = "Remove Gemini key" };
        clear.Click += async (_, _) => await settings.ClearAsync(CancellationToken.None);
        var videoFrames = new CheckBox { Content = "Video frames", IsChecked = true };
        var audioWaveform = new CheckBox { Content = "Audio waveform" };
        var photoPreview = new CheckBox { Content = "Photo preview" };
        var uploadConsent = new CheckBox { Content = "I approve uploading this exact sample scope to Gemini" };
        var prepareAnalysis = new Button { Content = "Prepare local analysis samples" };
        prepareAnalysis.Click += async (_, _) =>
        {
            var scopes = GeminiAnalysisScope.None;
            if (videoFrames.IsChecked == true)
            {
                scopes |= GeminiAnalysisScope.VideoFrames;
            }

            if (audioWaveform.IsChecked == true)
            {
                scopes |= GeminiAnalysisScope.AudioWaveform;
            }

            if (photoPreview.IsChecked == true)
            {
                scopes |= GeminiAnalysisScope.PhotoPreview;
            }

            await viewModel.PrepareLocalAnalysisAsync(scopes, uploadConsent.IsChecked == true, CancellationToken.None);
        };
        var analysisStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        analysisStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.AnalysisStatusText)));
        _ = settings.LoadAsync(CancellationToken.None);
        return new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(12),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 48, 48)),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Optional Gemini Creative Director", FontWeight = FontWeight.SemiBold },
                    new TextBlock { Text = "Manual import, editing, preview and export never require a key.", TextWrapping = TextWrapping.Wrap },
                    model,
                    apiKey,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { save, clear } },
                    status,
                    new TextBlock { Text = "Analysis scope (prepared locally; no upload occurs here):", FontWeight = FontWeight.SemiBold },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { videoFrames, audioWaveform, photoPreview } },
                    uploadConsent,
                    prepareAnalysis,
                    analysisStatus
                }
            }
        };
    }

    private static Control CreateAiProposalPanel(MainWindowViewModel viewModel)
    {
        var planJson = new TextBox
        {
            AcceptsReturn = true,
            Height = 180,
            TextWrapping = TextWrapping.Wrap,
            PlaceholderText = "Paste a schema v1 AI edit plan JSON for local validation"
        };
        var actions = new StackPanel { Spacing = 4 };
        void RefreshActions()
        {
            actions.Children.Clear();
            foreach (var action in viewModel.AiProposal.Actions)
            {
                var approval = new CheckBox { Content = action.Summary, IsChecked = action.IsApproved };
                approval.PropertyChanged += (_, args) =>
                {
                    if (args.Property == ToggleButton.IsCheckedProperty)
                    {
                        action.IsApproved = approval.IsChecked == true;
                    }
                };
                actions.Children.Add(approval);
            }
        }

        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        status.Bind(TextBlock.TextProperty, new Binding("AiProposal.StatusText"));
        var approvalSummary = new TextBlock { TextWrapping = TextWrapping.Wrap };
        approvalSummary.Bind(TextBlock.TextProperty, new Binding("AiProposal.ApprovalSummary"));
        viewModel.AiProposal.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AiProposalViewModel.Actions))
            {
                RefreshActions();
            }

            if (args.PropertyName == nameof(AiProposalViewModel.ValidatedJson) && viewModel.AiProposal.ValidatedJson is not null)
            {
                planJson.Text = viewModel.AiProposal.ValidatedJson;
            }
        };
        var validate = new Button { Content = "Validate proposal" };
        validate.Click += (_, _) => viewModel.ValidateAiPlan(planJson.Text ?? string.Empty);
        var revisionInstruction = new TextBox { PlaceholderText = "Natural-language revision request", Width = 420 };
        var requestRevision = new Button { Content = "Ask Gemini to revise" };
        AutomationProperties.SetName(requestRevision, "Ask Gemini to revise proposal");
        requestRevision.Click += async (_, _) => await viewModel.RequestAiRevisionAsync(
            revisionInstruction.Text ?? string.Empty,
            planJson.Text ?? string.Empty,
            CancellationToken.None);
        var revisionStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        revisionStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.AiRevisionStatusText)));
        var approve = new Button { Content = "Apply approved actions" };
        approve.Click += (_, _) => viewModel.ApplyApprovedAiPlan();
        var undo = new Button { Content = "Undo last AI apply" };
        undo.Click += (_, _) => viewModel.UndoLastAiEdit();
        var clear = new Button { Content = "Clear proposal" };
        clear.Click += (_, _) => viewModel.AiProposal.Clear();
        return new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(12),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 48, 48)),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "AI proposal review", FontWeight = FontWeight.SemiBold },
                    new TextBlock { Text = "Plans are untrusted data. Only checked actions are applied, atomically, through editable timeline operations; failures roll back the whole group.", TextWrapping = TextWrapping.Wrap },
                    planJson,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { revisionInstruction, requestRevision } },
                    revisionStatus,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { validate, approve, undo, clear } },
                    approvalSummary,
                    actions,
                    status
                }
            }
        };
    }

    private static Control CreateColorInspectorPanel(MainWindowViewModel viewModel)
    {
        var inspector = viewModel.ColorInspector;
        var beforeImage = new Image { Width = 240, Height = 135, Stretch = Stretch.Uniform };
        var afterImage = new Image { Width = 240, Height = 135, Stretch = Stretch.Uniform };
        void RefreshPreviews()
        {
            beforeImage.Source = CreateBitmap(inspector.BeforePreviewPpm);
            afterImage.Source = CreateBitmap(inspector.AfterPreviewPpm);
        }

        inspector.PropertyChanged += (_, _) => RefreshPreviews();
        RefreshPreviews();
        var beforeSummary = new TextBlock { TextWrapping = TextWrapping.Wrap };
        beforeSummary.Bind(TextBlock.TextProperty, new Binding("ColorInspector.BeforeSummary"));
        var afterSummary = new TextBlock { TextWrapping = TextWrapping.Wrap };
        afterSummary.Bind(TextBlock.TextProperty, new Binding("ColorInspector.AfterSummary"));
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        status.Bind(TextBlock.TextProperty, new Binding("ColorInspector.StatusText"));
        var controls = new StackPanel { Spacing = 4 };
        controls.Children.Add(CreateGradeControl("Exposure EV", -8, 8, inspector.ExposureEV, value => inspector.ExposureEV = value));
        controls.Children.Add(CreateGradeControl("Contrast", -1, 1, inspector.Contrast, value => inspector.Contrast = value));
        controls.Children.Add(CreateGradeControl("Saturation", -1, 1, inspector.Saturation, value => inspector.Saturation = value));
        controls.Children.Add(CreateGradeControl("Temperature", -1, 1, inspector.Temperature, value => inspector.Temperature = value));
        controls.Children.Add(CreateGradeControl("Tint", -1, 1, inspector.Tint, value => inspector.Tint = value));
        var reset = new Button { Content = "Reset grade" };
        reset.Click += (_, _) => inspector.ResetGrade();
        controls.Children.Add(reset);
        return new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(12),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 48, 48)),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Color grade inspector", FontWeight = FontWeight.SemiBold },
                    status,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 12,
                        Children =
                        {
                            new StackPanel { Spacing = 4, Children = { new TextBlock { Text = "Before" }, beforeImage, beforeSummary } },
                            new StackPanel { Spacing = 4, Children = { new TextBlock { Text = "After" }, afterImage, afterSummary } }
                        }
                    },
                    controls
                }
            }
        };
    }

    private static Control CreateTimelinePanel(MainWindowViewModel viewModel)
    {
        var timeline = viewModel.TimelineEditor;
        var timelinePreview = new Image { Width = 320, Height = 180, Stretch = Stretch.Uniform };
        var timelinePreviewStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        timelinePreviewStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.TimelinePreviewStatusText)));
        void RefreshTimelinePreview() => timelinePreview.Source = CreateBitmap(viewModel.TimelinePreviewPpm);
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(MainWindowViewModel.TimelinePreviewPpm))
            {
                RefreshTimelinePreview();
            }
        };
        var previewCurrentFrame = new Button { Content = "Preview current frame" };
        previewCurrentFrame.Click += async (_, _) => await viewModel.PreviewCurrentTimelineFrameAsync(CancellationToken.None);
        var playhead = new Slider { Minimum = 0, Maximum = Math.Max(1, timeline.TotalFrames - 1), Value = timeline.CurrentFrame };
        AutomationProperties.SetName(playhead, "Timeline playhead frame");
        playhead.ValueChanged += (_, args) => timeline.SeekFrame((long)Math.Round(args.NewValue, MidpointRounding.AwayFromZero));
        timeline.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(TimelineEditorViewModel.TotalFrames) or nameof(TimelineEditorViewModel.CurrentFrame))
            {
                playhead.Maximum = Math.Max(1, timeline.TotalFrames - 1);
                playhead.Value = timeline.CurrentFrame;
            }
        };
        var frameStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        frameStatus.Bind(TextBlock.TextProperty, new Binding("TimelineEditor.CurrentFrame"));
        var positionStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        positionStatus.Bind(TextBlock.TextProperty, new Binding("TimelineEditor.CurrentPosition"));
        var ruler = new TextBlock { TextWrapping = TextWrapping.Wrap };
        ruler.Bind(TextBlock.TextProperty, new Binding("TimelineEditor.RulerText"));
        var selectionContext = new TextBlock { TextWrapping = TextWrapping.Wrap };
        selectionContext.Bind(TextBlock.TextProperty, new Binding("TimelineEditor.SelectionContextText"));
        var tracks = new ListBox { Height = 100 };
        AutomationProperties.SetName(tracks, "Timeline tracks");
        tracks.Bind(ItemsControl.ItemsSourceProperty, new Binding("TimelineEditor.TrackRows"));
        var beatMarkers = new TextBlock { TextWrapping = TextWrapping.Wrap };
        beatMarkers.Bind(TextBlock.TextProperty, new Binding("TimelineEditor.BeatMarkersText"));
        var beatDetection = new ComboBox { ItemsSource = new[] { "Amplitude peaks", "Energy onsets" }, SelectedIndex = 0, Width = 150 };
        AutomationProperties.SetName(beatDetection, "Beat detection mode");
        beatDetection.SelectionChanged += (_, _) => timeline.BeatDetectionMode = beatDetection.SelectedIndex == 1
            ? BeatDetectionMode.EnergyOnsets
            : BeatDetectionMode.AmplitudePeaks;
        var snap = new CheckBox { Content = "Snap to clip boundaries" };
        snap.Bind(ToggleButton.IsCheckedProperty, new Binding("TimelineEditor.SnapEnabled") { Mode = BindingMode.TwoWay });
        var zoom = new Slider { Minimum = 0.05, Maximum = 20, Value = timeline.FramesPerPixel };
        AutomationProperties.SetName(zoom, "Timeline zoom");
        zoom.ValueChanged += (_, args) => timeline.SetZoom(args.NewValue);
        var back = new Button { Content = "◀ Frame" };
        back.Click += (_, _) => timeline.Step(-1);
        var forward = new Button { Content = "Frame ▶" };
        forward.Click += (_, _) => timeline.Step(1);
        var select = new Button { Content = "Select at playhead" };
        select.Click += (_, _) => timeline.SelectAt(timeline.CurrentFrame, "Video 1");
        var selectAudio = new Button { Content = "Select audio" };
        selectAudio.Click += (_, _) => timeline.SelectAt(timeline.CurrentFrame, "Audio 1");
        var operationStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        void RunTimelineAction(Action action)
        {
            try
            {
                action();
                operationStatus.Text = string.Empty;
            }
            catch (ArgumentException exception)
            {
                operationStatus.Text = exception.Message;
            }
            catch (InvalidOperationException exception)
            {
                operationStatus.Text = exception.Message;
            }
        }

        TimelineClipboard? clipboard = null;
        var copy = new Button { Content = "Copy" };
        copy.Click += (_, _) =>
        {
            clipboard = timeline.CopySelected();
            operationStatus.Text = clipboard is null ? "Select a clip before copying." : "Clip copied.";
        };
        var paste = new Button { Content = "Paste at playhead" };
        paste.Click += (_, _) =>
        {
            if (clipboard is null)
            {
                operationStatus.Text = "Copy a clip before pasting.";
                return;
            }

            RunTimelineAction(() => timeline.Paste(clipboard, timeline.CurrentFrame));
        };
        var split = new Button { Content = "Split" };
        split.Click += (_, _) => RunTimelineAction(() => timeline.SplitSelectedAt(timeline.CurrentFrame));
        var trim = new Button { Content = "Trim right" };
        trim.Click += (_, _) => RunTimelineAction(() => timeline.TrimSelectedEndAt(timeline.CurrentFrame));
        var move = new Button { Content = "Move to playhead" };
        move.Click += (_, _) => RunTimelineAction(() => timeline.MoveSelected(timeline.CurrentFrame));
        var rippleDelete = new Button { Content = "Ripple delete" };
        rippleDelete.Click += (_, _) => RunTimelineAction(timeline.RippleDeleteSelected);
        var fadeIn = new Button { Content = "Fade in preset" };
        fadeIn.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAnimationPresetToSelected(TimelineAnimationPreset.FadeIn));
        var pushIn = new Button { Content = "Push in preset" };
        pushIn.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAnimationPresetToSelected(TimelineAnimationPreset.PushIn));
        var clearAnimation = new Button { Content = "Clear animation" };
        clearAnimation.Click += (_, _) => RunTimelineAction(timeline.ClearSelectedAnimation);
        var muteAudio = new Button { Content = "Mute audio" };
        muteAudio.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAudioSettingsToSelected(0, muted: true));
        var gainAudio = new Button { Content = "Audio +3 dB" };
        gainAudio.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAudioSettingsToSelected(3, muted: false));
        var fadeAudio = new Button { Content = "Audio fades" };
        fadeAudio.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAudioSettingsToSelected(0, muted: false, fadeInFrames: 2, fadeOutFrames: 2));
        var duckAudio = new Button { Content = "Duck audio -12 dB" };
        duckAudio.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAudioDuckingToSelected());
        var clearDucking = new Button { Content = "Clear ducking" };
        clearDucking.Click += (_, _) => RunTimelineAction(timeline.ClearAudioDuckingFromSelected);
        var clearAudio = new Button { Content = "Clear audio" };
        clearAudio.Click += (_, _) => RunTimelineAction(timeline.ClearSelectedAudioSettings);
        var titleText = new TextBox { PlaceholderText = "Title text", Width = 220 };
        var addTitle = new Button { Content = "Add title" };
        addTitle.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(titleText.Text))
            {
                operationStatus.Text = "Enter title text before adding a title.";
                return;
            }

            var topLevel = TopLevel.GetTopLevel(addTitle);
            if (topLevel is null)
            {
                return;
            }

            var font = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("OpenType or TrueType font") { Patterns = ["*.ttf", "*.otf"] }]
            });
            var fontPath = font.SingleOrDefault()?.TryGetLocalPath();
            if (fontPath is null)
            {
                operationStatus.Text = "Choose a licensed local .ttf or .otf font.";
                return;
            }

            RunTimelineAction(() => timeline.AddTitle(titleText.Text!, fontPath));
        };
        var fadeTitle = new Button { Content = "Fade title in" };
        fadeTitle.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAnimationPresetToLastTitle(TimelineAnimationPreset.FadeIn));
        var pushTitle = new Button { Content = "Push title in" };
        pushTitle.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAnimationPresetToLastTitle(TimelineAnimationPreset.PushIn));
        var titleAnimationStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        titleAnimationStatus.Bind(TextBlock.TextProperty, new Binding(nameof(TimelineEditorViewModel.TitleAnimationStatus)));
        var importCaptions = new Button { Content = "Import SRT" };
        importCaptions.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(importCaptions);
            if (topLevel is null)
            {
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("SubRip captions") { Patterns = ["*.srt"] }]
            });
            var path = files.SingleOrDefault()?.TryGetLocalPath();
            if (path is not null)
            {
                await viewModel.ImportCaptionsAsync(path, CancellationToken.None);
            }
        };
        var exportCaptions = new Button { Content = "Export SRT" };
        exportCaptions.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(exportCaptions);
            if (topLevel is null)
            {
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = "captions.srt",
                FileTypeChoices = [new FilePickerFileType("SubRip captions") { Patterns = ["*.srt"] }]
            });
            var path = file?.TryGetLocalPath();
            if (path is not null)
            {
                await viewModel.ExportCaptionsAsync(path, CancellationToken.None);
            }
        };
        var captionStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        captionStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.CaptionStatusText)));
        var transcriptionConsent = new CheckBox { Content = "I approve uploading this exact local audio file or extracted video audio track to Gemini" };
        AutomationProperties.SetName(transcriptionConsent, "Gemini transcription upload consent");
        var transcribeAudio = new Button { Content = "Transcribe approved audio" };
        transcribeAudio.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(transcribeAudio);
            if (topLevel is null)
            {
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Audio or video with audio") { Patterns = ["*.wav", "*.mp3", "*.m4a", "*.aac", "*.flac", "*.ogg", "*.opus", "*.aif", "*.aiff", "*.avi", "*.mkv", "*.mp4", "*.mov", "*.mpeg", "*.mpg", "*.webm"] }]
            });
            var path = files.SingleOrDefault()?.TryGetLocalPath();
            if (path is not null)
            {
                await viewModel.TranscribeAudioAsync(path, transcriptionConsent.IsChecked == true, CancellationToken.None);
            }
        };
        var captionFont = new Button { Content = "Choose caption font" };
        captionFont.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(captionFont);
            if (topLevel is null)
            {
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("OpenType or TrueType font") { Patterns = ["*.ttf", "*.otf"] }]
            });
            var path = files.SingleOrDefault()?.TryGetLocalPath();
            if (path is not null)
            {
                RunTimelineAction(() => timeline.SetCaptionFontPath(path));
            }
        };
        var export = new Button { Content = "Export composite MP4" };
        var cancelExport = new Button { Content = "Cancel export" };
        cancelExport.Click += (_, _) => viewModel.CancelTimelineExport();
        var aspectRatio = new ComboBox { ItemsSource = new[] { "16:9 landscape", "1:1 square", "9:16 portrait" }, SelectedIndex = 0, Width = 150 };
        AutomationProperties.SetName(aspectRatio, "Video export aspect ratio");
        export.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(export);
            if (topLevel is null)
            {
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = "frame-theory-timeline.mp4",
                FileTypeChoices = [new FilePickerFileType("MP4 video") { Patterns = ["*.mp4"] }]
            });
            var path = file?.TryGetLocalPath();
            if (path is not null)
            {
                var selectedAspect = aspectRatio.SelectedIndex switch
                {
                    1 => SocialAspectRatio.Square1By1,
                    2 => SocialAspectRatio.Portrait9By16,
                    _ => SocialAspectRatio.Landscape16By9
                };
                await viewModel.ExportTimelineAsync(path, CancellationToken.None, selectedAspect);
            }
        };
        var exportStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        exportStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.TimelineExportStatusText)));
        var retryExport = new Button { Content = "Retry last export" };
        retryExport.Click += async (_, _) => await viewModel.RetryLastTimelineExportAsync(CancellationToken.None);
        var watermarkStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        watermarkStatus.Bind(TextBlock.TextProperty, new Binding("TimelineEditor.WatermarkStatusText"));
        var selectWatermark = new Button { Content = "Select watermark" };
        selectWatermark.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(selectWatermark);
            if (topLevel is null)
            {
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("PNG, JPEG or WebP image") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp"] }]
            });
            var path = files.SingleOrDefault()?.TryGetLocalPath();
            if (path is not null)
            {
                RunTimelineAction(() => timeline.SetWatermark(path));
            }
        };
        var clearWatermark = new Button { Content = "Clear watermark" };
        clearWatermark.Click += (_, _) => timeline.ClearWatermark();
        var scrollBack = new Button { Content = "Scroll ◀" };
        scrollBack.Click += (_, _) => timeline.ScrollBy(-30);
        var scrollForward = new Button { Content = "Scroll ▶" };
        scrollForward.Click += (_, _) => timeline.ScrollBy(30);
        return new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(12),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 48, 48)),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Frame-accurate timeline", FontWeight = FontWeight.SemiBold },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { timelinePreview, new StackPanel { Spacing = 8, Children = { previewCurrentFrame, timelinePreviewStatus } } } },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { new TextBlock { Text = "Frame" }, frameStatus, new TextBlock { Text = "Position" }, positionStatus } },
                    playhead,
                    ruler,
                    selectionContext,
                    tracks,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { beatDetection, beatMarkers } },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { back, forward, select, selectAudio, scrollBack, scrollForward } },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { copy, paste, split, trim, move, rippleDelete } },
                    operationStatus,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { fadeIn, pushIn, clearAnimation } },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { muteAudio, gainAudio, fadeAudio, duckAudio, clearDucking, clearAudio } },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { titleText, addTitle, fadeTitle, pushTitle } },
                    titleAnimationStatus,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { importCaptions, exportCaptions } },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { transcribeAudio, transcriptionConsent } },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { captionFont } },
                    captionStatus,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { aspectRatio, export, cancelExport, retryExport } },
                    exportStatus,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { selectWatermark, clearWatermark } },
                    watermarkStatus,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { snap, new TextBlock { Text = "Zoom" }, zoom } }
                }
            }
        };
    }

    private static Control CreateGradeControl(string label, double minimum, double maximum, float value, Action<float> setter)
    {
        var slider = new Slider { Minimum = minimum, Maximum = maximum, Value = value };
        AutomationProperties.SetName(slider, label);
        slider.ValueChanged += (_, args) => setter((float)args.NewValue);
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { new TextBlock { Text = label, Width = 110 }, slider }
        };
    }

    private static Bitmap? CreateBitmap(byte[]? previewPpm)
    {
        if (previewPpm is null)
        {
            return null;
        }

        var raster = PpmRaster.Parse(previewPpm);
        var bitmap = new WriteableBitmap(
            new PixelSize(raster.Width, raster.Height),
            new Vector(96, 96),
            PixelFormat.Rgba8888,
            AlphaFormat.Opaque);
        var rgba = new byte[checked(raster.Width * raster.Height * 4)];
        for (var index = 0; index < raster.Width * raster.Height; index++)
        {
            var sourceOffset = index * 3;
            var targetOffset = index * 4;
            rgba[targetOffset] = raster.RgbBytes[sourceOffset];
            rgba[targetOffset + 1] = raster.RgbBytes[sourceOffset + 1];
            rgba[targetOffset + 2] = raster.RgbBytes[sourceOffset + 2];
            rgba[targetOffset + 3] = raster.AlphaBytes?[index] ?? 255;
        }

        using var framebuffer = bitmap.Lock();
        for (var row = 0; row < raster.Height; row++)
        {
            Marshal.Copy(rgba, row * raster.Width * 4, framebuffer.Address + row * framebuffer.RowBytes, raster.Width * 4);
        }

        return bitmap;
    }
}
