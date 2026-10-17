using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using FrameTheory.Domain;
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
        return new Border
        {
            Background = Brushes.Black,
            Child = status
        };
    }

    private static Control CreateWorkspaceContent(WorkspaceViewModel workspace, MainWindowViewModel viewModel)
    {
        var importStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        importStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.MediaImportStatusText)));
        var importedList = new ListBox { Height = 140 };
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
        }
        else if (workspace.Name == "Color")
        {
            children.Add(CreateColorInspectorPanel(viewModel));
        }
        else if (workspace.Name == "Video")
        {
            children.Add(CreateTimelinePanel(viewModel));
        }

        var stack = new StackPanel { Spacing = 12 };
        stack.Children.AddRange(children);
        return new Border
        {
            Margin = new Thickness(24),
            Padding = new Thickness(24),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(35, 35, 35)),
            CornerRadius = new CornerRadius(8),
            Child = stack
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
        var playhead = new Slider { Minimum = 0, Maximum = Math.Max(1, timeline.TotalFrames - 1), Value = timeline.CurrentFrame };
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
        var tracks = new ListBox { Height = 100 };
        tracks.Bind(ItemsControl.ItemsSourceProperty, new Binding("TimelineEditor.TrackRows"));
        var snap = new CheckBox { Content = "Snap to clip boundaries" };
        snap.Bind(ToggleButton.IsCheckedProperty, new Binding("TimelineEditor.SnapEnabled") { Mode = BindingMode.TwoWay });
        var zoom = new Slider { Minimum = 0.05, Maximum = 20, Value = timeline.FramesPerPixel };
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
        var export = new Button { Content = "Export composite MP4" };
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
                await viewModel.ExportTimelineAsync(path, CancellationToken.None);
            }
        };
        var exportStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        exportStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.TimelineExportStatusText)));
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
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { new TextBlock { Text = "Frame" }, frameStatus, new TextBlock { Text = "Position" }, positionStatus } },
                    playhead,
                    ruler,
                    tracks,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { back, forward, select, selectAudio, scrollBack, scrollForward } },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { copy, paste, split, trim, move, rippleDelete } },
                    operationStatus,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { fadeIn, pushIn, clearAnimation } },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { muteAudio, gainAudio, fadeAudio, clearAudio } },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { titleText, addTitle } },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { importCaptions, exportCaptions } },
                    captionStatus,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { export } },
                    exportStatus,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { snap, new TextBlock { Text = "Zoom" }, zoom } }
                }
            }
        };
    }

    private static Control CreateGradeControl(string label, double minimum, double maximum, float value, Action<float> setter)
    {
        var slider = new Slider { Minimum = minimum, Maximum = maximum, Value = value };
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
