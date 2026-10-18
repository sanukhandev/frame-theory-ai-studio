using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FrameTheory.AI;
using FrameTheory.Color;
using FrameTheory.Domain;
using FrameTheory.Media;
using System.Runtime.InteropServices;
using System.Globalization;
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
        AutomationProperties.SetName(this, Title);
        Width = 1280;
        Height = 800;
        MinWidth = 960;
        MinHeight = 640;
        UseLayoutRounding = true;
        Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(13, 15, 18));

        var tabs = new TabControl();
        AutomationProperties.SetName(tabs, "Workspaces");
        tabs.TabStripPlacement = Dock.Left;
        tabs.Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(13, 15, 18));
        tabs.BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55));
        tabs.BorderThickness = new Thickness(0, 0, 1, 0);
        tabs.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        tabs.VerticalContentAlignment = VerticalAlignment.Stretch;
        var navigationIndicators = new List<Border>();
        foreach (var workspace in viewModel.Workspaces)
        {
            var navigationHeader = CreateWorkspaceNavHeader(workspace, tabs.Items.Count, out var navigationIndicator);
            var tab = new TabItem
            {
                Header = navigationHeader,
                Content = CreateWorkspaceContent(workspace, viewModel),
                MinWidth = 152,
                MinHeight = 62,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            AutomationProperties.SetName(tab, $"{workspace.Name} workspace");
            ToolTip.SetTip(tab, $"{workspace.Name}: {workspace.Description}");
            tabs.Items.Add(tab);
            navigationIndicators.Add(navigationIndicator);
        }
        tabs.Bind(SelectingItemsControl.SelectedIndexProperty, new Binding(nameof(MainWindowViewModel.SelectedWorkspaceIndex))
        {
            Mode = BindingMode.TwoWay
        });
        void RefreshWorkspaceNavigation()
        {
            for (var index = 0; index < navigationIndicators.Count; index++)
            {
                navigationIndicators[index].Background = new SolidColorBrush(index == tabs.SelectedIndex
                    ? Avalonia.Media.Color.FromRgb(95, 125, 255)
                    : Avalonia.Media.Color.FromRgb(42, 46, 55));
            }
        }

        tabs.SelectionChanged += (_, _) => RefreshWorkspaceNavigation();
        RefreshWorkspaceNavigation();

        var brand = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (CreateApprovedLogo() is { } logo)
        {
            brand.Children.Add(logo);
        }
        brand.Children.Add(new StackPanel
        {
            Spacing = 1,
            Children =
            {
                new TextBlock
                {
                    Text = "FRAME THEORY STUDIO AI",
                    FontSize = 16,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Brushes.White
                },
                new TextBlock
                {
                    Text = "Offline-first creative editing",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(159, 166, 181))
                }
            }
        });
        var shortcutHint = new TextBlock
        {
            Text = "Ctrl+1 Video  ·  Ctrl+2 Photo  ·  Ctrl+3 Color  ·  Ctrl+4 AI",
            FontSize = 10,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(122, 130, 145)),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 360,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(shortcutHint, "Workspace keyboard shortcuts");

        var header = new Border
        {
            Height = 68,
            Padding = new Thickness(20, 12),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(18, 20, 25)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Children =
                {
                    brand,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 12,
                        Children =
                        {
                            shortcutHint,
                            new Border
                            {
                                Padding = new Thickness(10, 5),
                                CornerRadius = new CornerRadius(5),
                                Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(29, 44, 42)),
                                Child = new TextBlock
                                {
                                    Text = "LOCAL MODE  ·  GEMINI OPTIONAL",
                                    FontSize = 10,
                                    FontWeight = FontWeight.SemiBold,
                                    Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(148, 218, 188))
                                }
                            }
                        }
                    }
                }
            }
        };
        Grid.SetColumn(header.Child, 0);
        if (header.Child is Grid headerGrid)
        {
            Grid.SetColumn(headerGrid.Children[1], 1);
        }

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
        NameUnnamedButtonsForAccessibility();
        Opened += (_, _) => NameUnnamedButtonsForAccessibility();
    }

    private void OnKeyDown(object? sender, KeyEventArgs args)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var commandModifier = (args.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;
        var isWorkspaceShortcut = commandModifier && args.Key is Key.D1 or Key.NumPad1 or Key.D2 or Key.NumPad2 or Key.D3 or Key.NumPad3 or Key.D4 or Key.NumPad4;
        if (args.Source is TextBox && !isWorkspaceShortcut)
        {
            return;
        }

        var shortcut = commandModifier
            ? args.Key switch
            {
                Key.D1 or Key.NumPad1 => EditorShortcut.VideoWorkspace,
                Key.D2 or Key.NumPad2 => EditorShortcut.PhotoWorkspace,
                Key.D3 or Key.NumPad3 => EditorShortcut.ColorWorkspace,
                Key.D4 or Key.NumPad4 => EditorShortcut.AiWorkspace,
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

    private void NameUnnamedButtonsForAccessibility()
    {
        NameUnnamedButtons(this);
        foreach (var textBox in this.GetVisualDescendants().OfType<TextBox>())
        {
            if (!string.IsNullOrWhiteSpace(textBox.PlaceholderText) && string.IsNullOrWhiteSpace(AutomationProperties.GetName(textBox)))
            {
                AutomationProperties.SetName(textBox, textBox.PlaceholderText);
            }
        }
    }

    private static void NameUnnamedButtons(Control root)
    {
        foreach (var button in root.GetVisualDescendants().OfType<Button>())
        {
            if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)) && button.Content is not null)
            {
                var text = button.Content switch
                {
                    string value => value,
                    TextBlock value => value.Text,
                    _ => null
                };
                AutomationProperties.SetName(button, string.IsNullOrWhiteSpace(text) ? "Interactive button" : text);
            }
        }
    }

    private static Control CreateWorkspaceNavHeader(WorkspaceViewModel workspace, int index, out Border activeIndicator)
    {
        var shortName = workspace.Name switch
        {
            "Video" => "V",
            "Photo" => "P",
            "Color" => "C",
            "AI" => "AI",
            _ => "•"
        };
        var badge = new Border
        {
            Width = 30,
            Height = 30,
            CornerRadius = new CornerRadius(7),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(29, 33, 40)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(59, 68, 83)),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = shortName,
                FontSize = shortName.Length == 1 ? 15 : 11,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.White
            }
        };
        var title = new TextBlock
        {
            Text = workspace.Name,
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        };
        var descriptor = new TextBlock
        {
            Text = workspace.Name == "AI" ? "Creative direction" : "Local editor",
            FontSize = 10,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(122, 130, 145)),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var keyHint = new TextBlock
        {
            Text = $"{index + 1}",
            FontSize = 10,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(122, 130, 145)),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        var titleStack = new StackPanel
        {
            Margin = new Thickness(10, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 1,
            Children = { title, descriptor }
        };
        activeIndicator = new Border
        {
            Width = 3,
            Margin = new Thickness(0, 6, 10, 6),
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55))
        };
        AutomationProperties.SetName(activeIndicator, $"{workspace.Name} workspace active indicator");
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto"),
            Children = { activeIndicator, badge, titleStack, keyHint }
        };
        Grid.SetColumn(badge, 1);
        Grid.SetColumn(titleStack, 2);
        Grid.SetColumn(keyHint, 3);
        return header;
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
            Margin = new Thickness(16, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(186, 193, 207)),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        status.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.CapabilityStatusText)));
        AutomationProperties.SetName(status, "Capability status");
        ToolTip.SetTip(status, "Local FFmpeg, ffprobe and platform capability status.");
        var refresh = new Button { Content = "Refresh capabilities", Margin = new Thickness(12, 8, 16, 8) };
        AutomationProperties.SetName(refresh, "Refresh capability check");
        ToolTip.SetTip(refresh, "Recheck local FFmpeg, ffprobe and platform capabilities.");
        refresh.Click += async (_, _) => await viewModel.RefreshCapabilitiesAsync(CancellationToken.None);
        var diagnostics = new TextBlock
        {
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(122, 130, 145)),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 320
        };
        diagnostics.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.DiagnosticsStatusText)));
        AutomationProperties.SetName(diagnostics, "Diagnostics status");
        ToolTip.SetTip(diagnostics, "Local diagnostics log and failure-reporting status.");
        return new Border
        {
            Height = 48,
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(18, 20, 25)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
                Children =
                {
                    status,
                    diagnostics,
                    refresh
                }
            }
        };
    }

    private static Control CreateWorkspaceContent(WorkspaceViewModel workspace, MainWindowViewModel viewModel)
    {
        var importStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(importStatus, "Media import status");
        importStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.MediaImportStatusText)));
        var mediaCards = new Dictionary<ImportedMediaViewModel, Border>();
        var selectedMediaBackground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(49, 67, 111));
        var unselectedMediaBackground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(29, 33, 40));
        var selectedMediaBorder = new SolidColorBrush(Avalonia.Media.Color.FromRgb(95, 125, 255));
        var unselectedMediaBorder = new SolidColorBrush(Avalonia.Media.Color.FromRgb(59, 68, 83));
        var importedList = new ListBox
        {
            Height = 176,
            Margin = new Thickness(0, 8, 0, 0),
            Padding = new Thickness(4),
            ItemsPanel = new FuncTemplate<Panel?>(() => new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            }),
            ItemTemplate = new FuncDataTemplate<ImportedMediaViewModel>((item, _) =>
            {
                if (item is null)
                {
                    return new TextBlock { Text = "Unavailable media" };
                }

                var thumbnail = new Image
                {
                    Width = 110,
                    Height = 52,
                    Stretch = Stretch.UniformToFill,
                    Source = CreateBitmap(item.ThumbnailPpm)
                };
                AutomationProperties.SetName(thumbnail, $"Thumbnail for {item.Name}");
                var card = new Border
                {
                    Width = 124,
                    Height = 96,
                    Margin = new Thickness(2, 2, 4, 4),
                    Padding = new Thickness(6),
                    CornerRadius = new CornerRadius(5),
                    Background = unselectedMediaBackground,
                    BorderBrush = unselectedMediaBorder,
                    BorderThickness = new Thickness(1),
                    Child = new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new Border
                            {
                                Width = 110,
                                Height = 52,
                                Background = Brushes.Black,
                                Child = new Grid
                                {
                                    Children =
                                    {
                                        thumbnail,
                                        new Border
                                        {
                                            Margin = new Thickness(4),
                                            Padding = new Thickness(3, 1),
                                            CornerRadius = new CornerRadius(3),
                                            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(13, 15, 18)),
                                            HorizontalAlignment = HorizontalAlignment.Left,
                                            VerticalAlignment = VerticalAlignment.Bottom,
                                            Child = new TextBlock
                                            {
                                                Text = item.Kind?.ToString().ToUpperInvariant() ?? "MEDIA",
                                                FontSize = 8,
                                                FontWeight = FontWeight.SemiBold,
                                                Foreground = Brushes.White
                                            }
                                        }
                                    }
                                }
                            },
                            new TextBlock
                            {
                                Text = item.Name,
                                FontSize = 11,
                                FontWeight = FontWeight.SemiBold,
                                TextTrimming = TextTrimming.CharacterEllipsis
                            },
                            new TextBlock
                            {
                                Text = item.Summary,
                                FontSize = 10,
                                Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(159, 166, 181)),
                                TextTrimming = TextTrimming.CharacterEllipsis
                            }
                        }
                    }
                };
                AutomationProperties.SetName(card, $"Imported media card: {item.Name}");
                ToolTip.SetTip(card, $"{item.Kind?.ToString() ?? "Media"} · {item.Name} · {item.Summary}");
                mediaCards[item] = card;
                return card;
            }, supportsRecycling: false)
        };
        AutomationProperties.SetName(importedList, "Imported media");
        ToolTip.SetTip(importedList, "Imported local media available to the active workspace.");
        var mediaSearch = new TextBox
        {
            Width = 190,
            PlaceholderText = "Search media"
        };
        AutomationProperties.SetName(mediaSearch, "Search imported media");
        ToolTip.SetTip(mediaSearch, "Filter imported media by filename.");
        var clearMediaSearch = new Button
        {
            Content = "Clear",
            Width = 58,
            IsEnabled = false
        };
        AutomationProperties.SetName(clearMediaSearch, "Clear media search");
        ToolTip.SetTip(clearMediaSearch, "Clear the media filename filter.");
        var mediaSearchEmptyState = new TextBlock
        {
            Text = "No matching media.",
            IsVisible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 11,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(122, 130, 145))
        };
        AutomationProperties.SetName(mediaSearchEmptyState, "Media search empty state");
        var mediaEmptyState = new TextBlock
        {
            Text = "No media imported yet. Use Import media or drop files here.",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            FontSize = 11,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(122, 130, 145))
        };
        AutomationProperties.SetName(mediaEmptyState, "Media bin empty state");
        ImportedMediaViewModel? preservedSelection = null;
        void RefreshMediaList()
        {
            var query = mediaSearch.Text?.Trim();
            var selectedId = preservedSelection?.Id;
            var filtered = string.IsNullOrEmpty(query)
                ? viewModel.ImportedMedia
                : viewModel.ImportedMedia.Where(item => item.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
            mediaCards.Clear();
            importedList.ItemsSource = filtered;
            if (selectedId is { } id)
            {
                importedList.SelectedItem = filtered.FirstOrDefault(item => item.Id == id);
            }
            clearMediaSearch.IsEnabled = !string.IsNullOrEmpty(query);
            mediaSearchEmptyState.IsVisible = !string.IsNullOrEmpty(query) && filtered.Count == 0;
            mediaEmptyState.IsVisible = viewModel.ImportedMedia.Count == 0;
            Dispatcher.UIThread.Post(RefreshSelectedMediaCard);
        }

        void RefreshSelectedMediaCard()
        {
            foreach (var card in mediaCards.Values)
            {
                card.Background = unselectedMediaBackground;
                card.BorderBrush = unselectedMediaBorder;
            }

            if (importedList.SelectedItem is ImportedMediaViewModel selected && mediaCards.TryGetValue(selected, out var selectedCard))
            {
                selectedCard.Background = selectedMediaBackground;
                selectedCard.BorderBrush = selectedMediaBorder;
            }
        }

        mediaSearch.TextChanged += (_, _) => RefreshMediaList();
        clearMediaSearch.Click += (_, _) => mediaSearch.Text = string.Empty;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainWindowViewModel.ImportedMedia))
            {
                RefreshMediaList();
            }
        };
        viewModel.TimelineEditor.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(TimelineEditorViewModel.SelectedMediaId) || viewModel.TimelineEditor.SelectedMediaId is not { } selectedMediaId)
            {
                return;
            }

            if (importedList.ItemsSource is IEnumerable<ImportedMediaViewModel> visibleMedia)
            {
                var selected = visibleMedia.FirstOrDefault(item => item.Id?.Value == selectedMediaId);
                if (selected is not null)
                {
                    importedList.SelectedItem = selected;
                }
            }
        };
        RefreshMediaList();
        importedList.SelectionChanged += (_, _) =>
        {
            if (importedList.SelectedItem is not ImportedMediaViewModel item || item.Id is not MediaId mediaId)
            {
                return;
            }

            preservedSelection = item;
            RefreshSelectedMediaCard();
            if (item.ThumbnailPpm is not null)
            {
                viewModel.ColorInspector.SetSource(item.ThumbnailPpm);
            }

            if (item.Kind is MediaKind.Video)
            {
                viewModel.TimelineEditor.SelectMedia(mediaId.Value, TimelineTrackKind.Video);
            }
            else if (item.Kind is MediaKind.Audio)
            {
                viewModel.TimelineEditor.SelectMedia(mediaId.Value, TimelineTrackKind.Audio);
            }
        };
        var importMedia = new Button { Content = "Import media", HorizontalAlignment = HorizontalAlignment.Left };
        importMedia.Classes.Add("primary");
        AutomationProperties.SetName(importMedia, "Import local media");
        ToolTip.SetTip(importMedia, "Import local video, photo or audio files.");
        importMedia.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(importMedia);
            if (topLevel is null)
            {
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = true,
                FileTypeFilter =
                [
                    new FilePickerFileType("Frame Theory media")
                    {
                        Patterns = ["*.mp4", "*.mov", "*.mkv", "*.webm", "*.avi", "*.mp3", "*.wav", "*.m4a", "*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp", "*.tif", "*.tiff"]
                    }
                ]
            });
            var paths = files
                .Select(file => file.TryGetLocalPath())
                .Where(path => path is not null)
                .Select(path => path!)
                .ToArray();
            await viewModel.ImportFilesAsync(paths, CancellationToken.None);
        };
        var importZone = new Border
        {
            Margin = new Thickness(0, 18, 0, 0),
            Padding = new Thickness(18, 14),
            MinHeight = 76,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(29, 33, 40)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(59, 68, 83)),
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Spacing = 3,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Drop media to begin",
                        FontSize = 14,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = Brushes.White
                    },
                    new TextBlock
                    {
                        Text = "Video, photo and audio stay local until you explicitly choose an upload.",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(159, 166, 181)),
                        TextWrapping = TextWrapping.Wrap
                    },
                    importMedia
                }
            }
        };
        AutomationProperties.SetName(importZone, "Local media import drop zone");
        var mediaBinPanel = CreateMediaBin(importZone, importStatus, importedList, mediaSearch, clearMediaSearch, mediaSearchEmptyState, mediaEmptyState);
        var children = new List<Control>
        {
            CreateWorkspaceHeader(workspace, viewModel),
            new Border
            {
                Margin = new Thickness(0, 14, 0, 0),
                Padding = new Thickness(12, 10),
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(28, 46, 45)),
                BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(47, 84, 76)),
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = workspace.OfflineMessage,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(171, 218, 198)),
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };
        if (workspace.Name == "Photo")
        {
            var photoEditor = new StackPanel { Spacing = 12 };
            photoEditor.Children.Add(CreatePhotoRecoveryPanel(viewModel));
            if (viewModel.PhotoEditor is not null)
            {
                photoEditor.Children.Add(CreatePhotoEditorPanel(viewModel.PhotoEditor));
            }

            children.Add(CreateMediaWorkspaceLayout(mediaBinPanel, photoEditor, "Photo editor workspace layout"));
        }
        else if (workspace.Name == "Color")
        {
            children.Add(CreateMediaWorkspaceLayout(mediaBinPanel, CreateColorInspectorPanel(viewModel), "Color editor workspace layout"));
        }
        else if (workspace.Name == "Video")
        {
            children.Add(CreateMediaWorkspaceLayout(mediaBinPanel, CreateTimelinePanel(viewModel), "Video editor workspace layout"));
        }
        else if (workspace.Name == "AI")
        {
            var aiEditor = new StackPanel
            {
                Spacing = 12,
                Children = { CreateGeminiSettingsPanel(viewModel), CreateAiProposalPanel(viewModel) }
            };
            children.Add(CreateMediaWorkspaceLayout(mediaBinPanel, aiEditor, "AI editor workspace layout"));
        }

        var stack = new StackPanel { Spacing = 12 };
        stack.Children.AddRange(children);
        var workspaceScrollViewer = new ScrollViewer
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Top,
            Content = new Border
            {
                Margin = new Thickness(20),
                Padding = new Thickness(24, 22),
                Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(24, 27, 33)),
                BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = stack
            }
        };
        AutomationProperties.SetName(workspaceScrollViewer, $"{workspace.Name} workspace content");
        return workspaceScrollViewer;
    }

    private static Control CreateWorkspaceHeader(WorkspaceViewModel workspace, MainWindowViewModel viewModel)
    {
        var modeLabel = workspace.Name == "AI" ? "OPTIONAL AI" : "LOCAL WORKSPACE";
        var modeForeground = workspace.Name == "AI"
            ? new SolidColorBrush(Avalonia.Media.Color.FromRgb(205, 184, 255))
            : new SolidColorBrush(Avalonia.Media.Color.FromRgb(148, 218, 188));
        var modeBackground = workspace.Name == "AI"
            ? new SolidColorBrush(Avalonia.Media.Color.FromRgb(43, 34, 61))
            : new SolidColorBrush(Avalonia.Media.Color.FromRgb(29, 44, 42));
        var status = new TextBox
        {
            IsReadOnly = true,
            MaxWidth = 380,
            FontSize = 11,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(159, 166, 181)),
            HorizontalAlignment = HorizontalAlignment.Right,
            TextWrapping = TextWrapping.NoWrap,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            MinHeight = 0,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(status, "Active workspace status");
        ToolTip.SetTip(status, "Live save, export, edit or provider status for the active workspace.");
        status.Bind(TextBox.TextProperty, new Binding(nameof(MainWindowViewModel.ActiveWorkspaceStatusText)));
        var mode = new Border
        {
            Margin = new Thickness(12, 2, 0, 0),
            Padding = new Thickness(10, 6),
            CornerRadius = new CornerRadius(5),
            Background = modeBackground,
            VerticalAlignment = VerticalAlignment.Top,
            Child = new TextBlock
            {
                Text = modeLabel,
                FontSize = 10,
                FontWeight = FontWeight.SemiBold,
                Foreground = modeForeground
            }
        };
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children =
            {
                new StackPanel
                {
                    Spacing = 3,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = workspace.Name,
                            FontSize = 24,
                            FontWeight = FontWeight.SemiBold,
                            Foreground = Brushes.White
                        },
                        new TextBlock
                        {
                            Text = workspace.Description,
                            FontSize = 13,
                            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(159, 166, 181)),
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                },
                new StackPanel
                {
                    Spacing = 3,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { status, mode }
                }
            }
        };
        Grid.SetColumn(header.Children[1], 1);
        return header;
    }

    private static Control CreateMediaBin(
        Border importZone,
        TextBlock importStatus,
        ListBox importedList,
        TextBox mediaSearch,
        Button clearMediaSearch,
        TextBlock mediaSearchEmptyState,
        TextBlock mediaEmptyState)
    {
        importZone.Margin = new Thickness(0);
        var mediaBin = new Border
        {
            Margin = new Thickness(0, 18, 0, 0),
            Padding = new Thickness(12),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(20, 23, 29)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 54, 65)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto")
            }
        };
        AutomationProperties.SetName(mediaBin, "Media bin");
        var grid = (Grid)mediaBin.Child;
        var assetSummary = new TextBlock
        {
            FontSize = 10,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(122, 130, 145)),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(assetSummary, "Imported media summary");
        assetSummary.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.ImportedMediaSummary)));
        var mediaTitle = new TextBlock
        {
            Text = "MEDIA BIN",
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(126, 143, 190))
        };
        var mediaHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children = { mediaTitle, assetSummary }
        };
        Grid.SetColumn(assetSummary, 1);
        grid.Children.Add(mediaHeader);
        var mediaSearchRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children = { mediaSearch, clearMediaSearch }
        };
        Grid.SetColumn(clearMediaSearch, 1);
        grid.Children.Add(mediaSearchRow);
        grid.Children.Add(new Grid
        {
            Children = { importedList, mediaSearchEmptyState, mediaEmptyState }
        });
        grid.Children.Add(importStatus);
        grid.Children.Add(importZone);
        Grid.SetRow(mediaSearchRow, 1);
        Grid.SetRow(grid.Children[2], 2);
        Grid.SetRow(importStatus, 3);
        Grid.SetRow(importZone, 4);
        return mediaBin;
    }

    private static Control CreateMediaWorkspaceLayout(Control mediaBin, Control editor, string automationName)
    {
        var layout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("300,1,*"),
            ColumnSpacing = 12,
            Children =
            {
                mediaBin,
                new Border
                {
                    Width = 1,
                    Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 54, 65)),
                    VerticalAlignment = VerticalAlignment.Stretch
                },
                editor
            }
        };
        Grid.SetColumn(layout.Children[1], 1);
        Grid.SetColumn(editor, 2);
        AutomationProperties.SetName(layout, automationName);
        return layout;
    }

    private static Control CreatePhotoRecoveryPanel(MainWindowViewModel viewModel)
    {
        var projectPath = new TextBox { PlaceholderText = "Project path (.ftstudio)" };
        projectPath.Bind(TextBox.TextProperty, new Binding("PhotoRecovery.ProjectPath") { Mode = BindingMode.TwoWay });
        var replacementPath = new TextBox { PlaceholderText = "Replacement media path" };
        var missingList = new ListBox { Height = 90 };
        AutomationProperties.SetName(missingList, "Missing photo media");
        missingList.Bind(ItemsControl.ItemsSourceProperty, new Binding("PhotoRecovery.MissingMedia"));
        missingList.Bind(SelectingItemsControl.SelectedItemProperty, new Binding("PhotoRecovery.SelectedMissingMedia")
        {
            Mode = BindingMode.TwoWay
        });
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(status, "Photo recovery status");
        status.Bind(TextBlock.TextProperty, new Binding("PhotoRecovery.StatusText"));
        var open = new Button { Content = "Open / recover photo project" };
        AutomationProperties.SetName(open, "Open or recover photo project");
        open.Click += async (_, _) => await viewModel.PhotoRecovery.OpenProjectAsync(CancellationToken.None);
        var relink = new Button { Content = "Relink selected media" };
        AutomationProperties.SetName(relink, "Relink selected photo media");
        relink.Click += async (_, _) => await viewModel.PhotoRecovery.RelinkSelectedAsync(replacementPath.Text ?? string.Empty, CancellationToken.None);
        return CreateInspectorExpander(
            "Project recovery and media relink",
            CreateSectionContent(
                projectPath,
                open,
                status,
                new TextBlock { Text = "Missing media" },
                missingList,
                replacementPath,
                relink),
            expanded: false);
    }

    private static Control CreatePhotoEditorPanel(PhotoEditorViewModel editor)
    {
        var originalPreview = new Image
        {
            Height = 160,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(originalPreview, "Photo original preview");
        var originalEmptyState = new TextBlock
        {
            Text = "Open an image to begin",
            FontSize = 12,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(122, 130, 145)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(originalEmptyState, "Photo original preview empty state");
        var editedPreview = new Image
        {
            Height = 160,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(editedPreview, "Photo edit preview");
        var editedEmptyState = new TextBlock
        {
            Text = "Open an image to begin",
            FontSize = 12,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(122, 130, 145)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(editedEmptyState, "Photo preview empty state");
        void RefreshPreview()
        {
            var originalPpm = editor.OriginalPreviewPpm;
            var editedPpm = editor.PreviewPpm;
            originalPreview.Source = CreateBitmap(originalPpm);
            editedPreview.Source = CreateBitmap(editedPpm);
            originalEmptyState.IsVisible = originalPpm is null;
            editedEmptyState.IsVisible = editedPpm is null;
        }
        editor.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(PhotoEditorViewModel.OriginalPreviewPpm) or nameof(PhotoEditorViewModel.PreviewPpm) or nameof(PhotoEditorViewModel.Current))
            {
                RefreshPreview();
            }
        };
        RefreshPreview();
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(status, "Photo editor status");
        status.Bind(TextBlock.TextProperty, new Binding(nameof(PhotoEditorViewModel.StatusText)));
        var open = new Button { Content = "Open image" };
        AutomationProperties.SetName(open, "Open image");
        ToolTip.SetTip(open, "Open a local image for non-destructive editing.");
        open.Classes.Add("primary");
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
        var openProject = new Button { Content = "Open photo project" };
        AutomationProperties.SetName(openProject, "Open photo project");
        ToolTip.SetTip(openProject, "Open a portable non-destructive photo project and restore its layers.");
        openProject.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(openProject);
            if (topLevel is null)
            {
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Frame Theory project") { Patterns = ["*.ftstudio"] }]
            });
            var path = files.SingleOrDefault()?.TryGetLocalPath();
            if (path is not null)
            {
                await editor.OpenProjectAsync(path, CancellationToken.None);
            }
        };
        var saveProject = new Button { Content = "Save photo project" };
        AutomationProperties.SetName(saveProject, "Save photo project");
        ToolTip.SetTip(saveProject, "Save the photo, adjustment layers and media references to a project file.");
        saveProject.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(saveProject);
            if (topLevel is null)
            {
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = "frame-theory-photo.ftstudio",
                FileTypeChoices = [new FilePickerFileType("Frame Theory project") { Patterns = ["*.ftstudio"] }]
            });
            var path = file?.TryGetLocalPath();
            if (path is not null)
            {
                await editor.SaveProjectAsync(path, CancellationToken.None);
            }
        };
        var rotate = new Button { Content = "Rotate 90°" };
        AutomationProperties.SetName(rotate, "Rotate photo 90 degrees clockwise");
        ToolTip.SetTip(rotate, "Add a non-destructive 90-degree clockwise rotation.");
        rotate.Click += (_, _) => editor.RotateRight();
        var undo = new Button { Content = "Undo photo edit" };
        AutomationProperties.SetName(undo, "Undo last photo edit");
        ToolTip.SetTip(undo, "Undo the latest non-destructive photo operation.");
        undo.Click += (_, _) => editor.Undo();
        var overlayOpacity = new Slider { Minimum = 0, Maximum = 1, Value = 1, Width = 120 };
        AutomationProperties.SetName(overlayOpacity, "Photo overlay opacity");
        ToolTip.SetTip(overlayOpacity, "Set the imported overlay opacity from 0% to 100%.");
        var overlayOpacityValue = new TextBlock
        {
            Width = 48,
            Text = "100%",
            TextAlignment = TextAlignment.Right,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(172, 181, 196)),
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(overlayOpacityValue, "Photo overlay opacity value");
        overlayOpacity.ValueChanged += (_, args) => overlayOpacityValue.Text = $"{args.NewValue:P0}";
        var overlayBlend = new ComboBox { ItemsSource = new[] { "Normal", "Multiply", "Screen" }, SelectedIndex = 0, Width = 110 };
        AutomationProperties.SetName(overlayBlend, "Photo overlay blend mode");
        var importOverlay = new Button { Content = "Import overlay" };
        AutomationProperties.SetName(importOverlay, "Import photo overlay");
        importOverlay.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(importOverlay);
            if (topLevel is null)
            {
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Overlay images") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp", "*.tif", "*.tiff"] }]
            });
            var path = files.SingleOrDefault()?.TryGetLocalPath();
            if (path is not null)
            {
                var blendMode = overlayBlend.SelectedIndex switch
                {
                    1 => PhotoBlendMode.Multiply,
                    2 => PhotoBlendMode.Screen,
                    _ => PhotoBlendMode.Normal
                };
                await editor.ImportOverlayAsync(path, 0, 0, (float)overlayOpacity.Value, blendMode, CancellationToken.None);
            }
        };
        var clearOverlays = new Button { Content = "Clear overlays" };
        AutomationProperties.SetName(clearOverlays, "Clear photo overlays");
        clearOverlays.Click += (_, _) => editor.ClearOverlays();
        var maskX = new TextBox { Text = "0", PlaceholderText = "Mask X", Width = 55 };
        var maskY = new TextBox { Text = "0", PlaceholderText = "Mask Y", Width = 55 };
        var maskRadius = new TextBox { Text = "8", PlaceholderText = "Radius", Width = 65 };
        var maskStrength = new TextBox { Text = "1", PlaceholderText = "Strength", Width = 70 };
        AutomationProperties.SetName(maskX, "Photo mask center X");
        AutomationProperties.SetName(maskY, "Photo mask center Y");
        AutomationProperties.SetName(maskRadius, "Photo mask brush radius");
        AutomationProperties.SetName(maskStrength, "Photo mask brush strength");
        var applyMask = new Button { Content = "Mask last overlay" };
        AutomationProperties.SetName(applyMask, "Mask last photo overlay");
        applyMask.Click += (_, _) =>
        {
            if (int.TryParse(maskX.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var centerX) &&
                int.TryParse(maskY.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var centerY) &&
                int.TryParse(maskRadius.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var radius) &&
                float.TryParse(maskStrength.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var strength))
            {
                editor.MaskLastOverlay(centerX, centerY, radius, strength);
            }
        };
        var clearMask = new Button { Content = "Clear last mask" };
        AutomationProperties.SetName(clearMask, "Clear last photo overlay mask");
        clearMask.Click += (_, _) => editor.ClearLastOverlayMask();
        var retouchX = new TextBox { Text = "0", PlaceholderText = "Retouch X", Width = 65 };
        var retouchY = new TextBox { Text = "0", PlaceholderText = "Retouch Y", Width = 65 };
        var retouchRadius = new TextBox { Text = "8", PlaceholderText = "Radius", Width = 65 };
        var retouchOffsetX = new TextBox { Text = "1", PlaceholderText = "Source ΔX", Width = 75 };
        var retouchOffsetY = new TextBox { Text = "0", PlaceholderText = "Source ΔY", Width = 75 };
        var retouchStrength = new TextBox { Text = "1", PlaceholderText = "Strength", Width = 70 };
        AutomationProperties.SetName(retouchX, "Photo retouch center X");
        AutomationProperties.SetName(retouchY, "Photo retouch center Y");
        AutomationProperties.SetName(retouchRadius, "Photo retouch brush radius");
        AutomationProperties.SetName(retouchOffsetX, "Photo retouch source offset X");
        AutomationProperties.SetName(retouchOffsetY, "Photo retouch source offset Y");
        AutomationProperties.SetName(retouchStrength, "Photo retouch brush strength");
        var applyRetouch = new Button { Content = "Apply clone retouch" };
        AutomationProperties.SetName(applyRetouch, "Apply photo clone retouch");
        applyRetouch.Click += (_, _) =>
        {
            if (int.TryParse(retouchX.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var centerX) &&
                int.TryParse(retouchY.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var centerY) &&
                int.TryParse(retouchRadius.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var radius) &&
                int.TryParse(retouchOffsetX.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var offsetX) &&
                int.TryParse(retouchOffsetY.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var offsetY) &&
                float.TryParse(retouchStrength.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var strength))
            {
                editor.ApplyRetouch(centerX, centerY, radius, offsetX, offsetY, strength);
            }
        };
        var clearRetouch = new Button { Content = "Clear retouch" };
        AutomationProperties.SetName(clearRetouch, "Clear photo clone retouch");
        clearRetouch.Click += (_, _) => editor.ClearRetouch();
        var gradeExposure = new TextBox { Text = "1", PlaceholderText = "Exposure EV", Width = 90 };
        AutomationProperties.SetName(gradeExposure, "Photo grade exposure");
        var addGrade = new Button { Content = "Add exposure grade" };
        AutomationProperties.SetName(addGrade, "Add photo exposure grade");
        addGrade.Click += (_, _) =>
        {
            if (float.TryParse(gradeExposure.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var exposure))
            {
                editor.AddAdjustmentLayer(exposure);
            }
        };
        var maskGrade = new Button { Content = "Mask last grade" };
        AutomationProperties.SetName(maskGrade, "Mask last photo grade");
        maskGrade.Click += (_, _) =>
        {
            if (int.TryParse(maskX.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var centerX) &&
                int.TryParse(maskY.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var centerY) &&
                int.TryParse(maskRadius.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var radius) &&
                float.TryParse(maskStrength.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var strength))
            {
                editor.MaskLastAdjustmentLayer(centerX, centerY, radius, strength);
            }
        };
        var clearGradeMask = new Button { Content = "Clear grade mask" };
        AutomationProperties.SetName(clearGradeMask, "Clear last photo grade mask");
        clearGradeMask.Click += (_, _) => editor.ClearLastAdjustmentLayerMask();
        var format = new ComboBox { ItemsSource = new[] { "PNG", "JPEG" }, SelectedIndex = 0, Width = 90 };
        AutomationProperties.SetName(format, "Photo export format");
        var quality = new Slider { Minimum = 1, Maximum = 100, Value = 95, Width = 140 };
        AutomationProperties.SetName(quality, "Photo export quality");
        ToolTip.SetTip(quality, "Set JPEG export quality from 1 to 100.");
        var qualityValue = new TextBlock
        {
            Width = 32,
            Text = "95",
            TextAlignment = TextAlignment.Right,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(172, 181, 196)),
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(qualityValue, "Photo export quality value");
        quality.ValueChanged += (_, args) => qualityValue.Text = $"{args.NewValue:0}";
        var socialPreset = new ComboBox { ItemsSource = new[] { "Original", "16:9 landscape", "1:1 square", "9:16 portrait" }, SelectedIndex = 0, Width = 150 };
        AutomationProperties.SetName(socialPreset, "Photo export social dimensions");
        var export = new Button { Content = "Export image" };
        export.Classes.Add("primary");
        AutomationProperties.SetName(export, "Export photo image");
        ToolTip.SetTip(export, "Render the current photo edit to the selected image format and dimensions.");
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
        var photoPreviewCard = new Border
        {
            Padding = new Thickness(12),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinHeight = 204,
            CornerRadius = new CornerRadius(8),
            Background = Brushes.Black,
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
            BorderThickness = new Thickness(1),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                ColumnSpacing = 10,
                Children =
                {
                    new Border
                    {
                        Padding = new Thickness(8),
                        Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(24, 27, 33)),
                        BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Child = new StackPanel
                        {
                            Spacing = 6,
                            Children =
                            {
                                new TextBlock { Text = "Original", FontSize = 11, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(172, 181, 196)) },
                                new Grid { Children = { originalEmptyState, originalPreview } }
                            }
                        }
                    },
                    new Border
                    {
                        Padding = new Thickness(8),
                        Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(24, 27, 33)),
                        BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Child = new StackPanel
                        {
                            Spacing = 6,
                            Children =
                            {
                                new TextBlock { Text = "Edited", FontSize = 11, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(172, 181, 196)) },
                                new Grid { Children = { editedEmptyState, editedPreview } }
                            }
                        }
                    }
                }
            }
        };
        AutomationProperties.SetName(photoPreviewCard, "Photo before and after preview");
        ToolTip.SetTip(photoPreviewCard, "Compare the original source pixels with the current non-destructive edit.");
        Grid.SetColumn(((Grid)photoPreviewCard.Child).Children[1], 1);
        var photoProjectSection = CreateInspectorExpander(
            "Project",
            CreateSectionContent(CreateFlow(open, openProject, saveProject, rotate, undo)),
            expanded: true);
        var photoLayersSection = CreateInspectorExpander(
            "Layers and blend",
            CreateSectionContent(CreateFlow(importOverlay, clearOverlays, overlayBlend, overlayOpacity, overlayOpacityValue)),
            expanded: false);
        var photoMaskSection = CreateInspectorExpander(
            "Mask and retouch",
            CreateSectionContent(
                CreateFlow(applyMask, maskX, maskY, maskRadius, maskStrength, clearMask),
                CreateFlow(applyRetouch, retouchX, retouchY, retouchRadius, retouchOffsetX, retouchOffsetY, retouchStrength, clearRetouch)),
            expanded: false);
        var photoGradeSection = CreateInspectorExpander(
            "Grade",
            CreateSectionContent(CreateFlow(gradeExposure, addGrade, maskGrade, clearGradeMask)),
            expanded: true);
        var photoExportSection = CreateInspectorExpander(
            "Export",
            CreateSectionContent(CreateFlow(format, quality, qualityValue, socialPreset, export)),
            expanded: true);
        return new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(16),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(29, 32, 39)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 54, 65)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Non-destructive photo editor", FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White },
                    photoPreviewCard,
                    photoProjectSection,
                    photoLayersSection,
                    photoMaskSection,
                    photoGradeSection,
                    photoExportSection,
                    status
                }
            }
        };
    }

    private static Control CreateGeminiSettingsPanel(MainWindowViewModel viewModel)
    {
        var settings = viewModel.GeminiSettings;
        var model = new ComboBox { ItemsSource = settings.Models, SelectedIndex = 0 };
        AutomationProperties.SetName(model, "Gemini model");
        model.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(GeminiSettingsViewModel.SelectedModelId))
        {
            Mode = BindingMode.TwoWay
        });
        var apiKey = new TextBox { PlaceholderText = "Gemini API key (stored securely)", PasswordChar = '•' };
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(status, "Gemini provider status");
        status.Bind(TextBlock.TextProperty, new Binding(nameof(GeminiSettingsViewModel.StatusText)));
        var save = new Button { Content = "Store Gemini key" };
        AutomationProperties.SetName(save, "Store Gemini key securely");
        ToolTip.SetTip(save, "Store the Gemini key in the platform secret store; it is never written to the project.");
        save.Click += async (_, _) =>
        {
            await settings.SaveAsync(apiKey.Text ?? string.Empty, CancellationToken.None);
            apiKey.Text = string.Empty;
        };
        var clear = new Button { Content = "Remove Gemini key" };
        AutomationProperties.SetName(clear, "Remove Gemini key");
        ToolTip.SetTip(clear, "Remove the stored Gemini key and keep manual editing available offline.");
        clear.Click += async (_, _) => await settings.ClearAsync(CancellationToken.None);
        var videoFrames = new CheckBox { Content = "Video frames", IsChecked = true };
        AutomationProperties.SetName(videoFrames, "Gemini video frame scope");
        var audioWaveform = new CheckBox { Content = "Audio waveform" };
        AutomationProperties.SetName(audioWaveform, "Gemini audio waveform scope");
        var photoPreview = new CheckBox { Content = "Photo preview" };
        AutomationProperties.SetName(photoPreview, "Gemini photo preview scope");
        var uploadConsent = new CheckBox { Content = "I approve uploading this exact sample scope to Gemini" };
        AutomationProperties.SetName(uploadConsent, "Gemini upload consent");
        var prepareAnalysis = new Button { Content = "Prepare local analysis samples" };
        AutomationProperties.SetName(prepareAnalysis, "Prepare local Gemini analysis samples");
        ToolTip.SetTip(prepareAnalysis, "Build bounded local samples for the selected media scope before any optional upload.");
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
        var analyzeApprovedSamples = new Button { Content = "Analyze approved samples with Gemini" };
        analyzeApprovedSamples.Classes.Add("primary");
        AutomationProperties.SetName(analyzeApprovedSamples, "Analyze approved samples with Gemini");
        ToolTip.SetTip(analyzeApprovedSamples, "Send only the explicitly approved sample scope to Gemini for a structured analysis.");
        analyzeApprovedSamples.Click += async (_, _) => await viewModel.RequestAiAnalysisAsync(CancellationToken.None);
        var analysisStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(analysisStatus, "Gemini analysis status");
        analysisStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.AnalysisStatusText)));
        var usageStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(usageStatus, "Gemini usage status");
        usageStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.GeminiUsageStatusText)));
        var activityLog = new TextBox { IsReadOnly = true, Height = 100, TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(activityLog, "Gemini activity log");
        activityLog.Bind(TextBox.TextProperty, new Binding(nameof(MainWindowViewModel.GeminiActivityLogText)));
        _ = settings.LoadAsync(CancellationToken.None);
        var providerSection = CreateInspectorExpander(
            "Provider",
            CreateSectionContent(model, apiKey, CreateFlow(save, clear), status),
            expanded: true);
        var consentSection = CreateInspectorExpander(
            "Consent and analysis scope",
            CreateSectionContent(
                new TextBlock { Text = "Samples are prepared locally; upload occurs only after explicit consent.", TextWrapping = TextWrapping.Wrap },
                CreateFlow(videoFrames, audioWaveform, photoPreview),
                uploadConsent,
                CreateFlow(prepareAnalysis, analyzeApprovedSamples)),
            expanded: true);
        var activitySection = CreateInspectorExpander(
            "Activity",
            CreateSectionContent(analysisStatus, usageStatus, activityLog),
            expanded: false);
        return new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(16),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(29, 32, 39)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 54, 65)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Optional Gemini Creative Director", FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White },
                    new TextBlock { Text = "Manual import, editing, preview and export never require a key.", TextWrapping = TextWrapping.Wrap },
                    providerSection,
                    consentSection,
                    activitySection
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
        var emptyActions = new TextBlock
        {
            Text = "No validated actions yet. Validate a reviewed plan to see editable operations.",
            FontSize = 12,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(122, 130, 145)),
            TextWrapping = TextWrapping.Wrap
        };
        AutomationProperties.SetName(emptyActions, "AI proposal empty state");
        void RefreshActions()
        {
            actions.Children.Clear();
            if (viewModel.AiProposal.Actions.Count == 0)
            {
                actions.Children.Add(emptyActions);
                return;
            }

            foreach (var action in viewModel.AiProposal.Actions)
            {
                var approval = new CheckBox
                {
                    Content = new TextBlock { Text = action.Summary, TextWrapping = TextWrapping.Wrap },
                    IsChecked = action.IsApproved,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                AutomationProperties.SetName(approval, $"Approve AI action: {action.Summary}");
                var approvalCard = new Border
                {
                    Margin = new Thickness(0, 0, 0, 4),
                    Padding = new Thickness(8, 6),
                    CornerRadius = new CornerRadius(5),
                    Child = approval
                };
                void RefreshApprovalCard()
                {
                    approvalCard.Background = new SolidColorBrush(action.IsApproved
                        ? Avalonia.Media.Color.FromRgb(35, 53, 84)
                        : Avalonia.Media.Color.FromRgb(24, 27, 33));
                    approvalCard.BorderBrush = new SolidColorBrush(action.IsApproved
                        ? Avalonia.Media.Color.FromRgb(95, 125, 255)
                        : Avalonia.Media.Color.FromRgb(42, 46, 55));
                    approvalCard.BorderThickness = new Thickness(1);
                }
                approval.PropertyChanged += (_, args) =>
                {
                    if (args.Property == ToggleButton.IsCheckedProperty)
                    {
                        action.IsApproved = approval.IsChecked == true;
                        RefreshApprovalCard();
                    }
                };
                RefreshApprovalCard();
                actions.Children.Add(approvalCard);
            }
        }
        RefreshActions();

        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(status, "AI proposal status");
        status.Bind(TextBlock.TextProperty, new Binding("AiProposal.StatusText"));
        var approvalSummary = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(approvalSummary, "AI approval summary");
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
        AutomationProperties.SetName(validate, "Validate AI proposal");
        ToolTip.SetTip(validate, "Schema-check the plan and show its allowlisted editable actions before applying.");
        validate.Click += (_, _) => viewModel.ValidateAiPlan(planJson.Text ?? string.Empty);
        var revisionInstruction = new TextBox { PlaceholderText = "Natural-language revision request", Width = 420 };
        var requestRevision = new Button { Content = "Ask Gemini to revise" };
        AutomationProperties.SetName(requestRevision, "Ask Gemini to revise proposal");
        ToolTip.SetTip(requestRevision, "Request a bounded natural-language revision of the current proposal.");
        requestRevision.Click += async (_, _) => await viewModel.RequestAiRevisionAsync(
            revisionInstruction.Text ?? string.Empty,
            planJson.Text ?? string.Empty,
            CancellationToken.None);
        var revisionStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(revisionStatus, "AI revision status");
        revisionStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.AiRevisionStatusText)));
        var approve = new Button { Content = "Apply approved actions" };
        approve.Classes.Add("primary");
        AutomationProperties.SetName(approve, "Apply approved AI actions");
        ToolTip.SetTip(approve, "Apply only checked, validated actions transactionally to the editable project.");
        approve.Click += (_, _) => viewModel.ApplyApprovedAiPlan();
        var undo = new Button { Content = "Undo last AI apply" };
        AutomationProperties.SetName(undo, "Undo last AI apply");
        ToolTip.SetTip(undo, "Undo the last successful AI edit transaction without changing source media.");
        undo.Click += (_, _) => viewModel.UndoLastAiEdit();
        var clear = new Button { Content = "Clear proposal" };
        AutomationProperties.SetName(clear, "Clear AI proposal");
        clear.Click += (_, _) => viewModel.AiProposal.Clear();
        var recipeName = new TextBox { PlaceholderText = "Recipe name", Width = 180 };
        AutomationProperties.SetName(recipeName, "AI recipe name");
        var saveRecipe = new Button { Content = "Save recipe" };
        AutomationProperties.SetName(saveRecipe, "Save validated AI recipe");
        saveRecipe.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(saveRecipe);
            if (topLevel is null)
            {
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = "frame-theory-recipe.ftrecipe.json",
                FileTypeChoices = [new FilePickerFileType("Frame Theory AI recipe") { Patterns = ["*.ftrecipe.json"] }]
            });
            var path = file?.TryGetLocalPath();
            if (path is not null)
            {
                await viewModel.SaveAiRecipeAsync(path, recipeName.Text ?? string.Empty, CancellationToken.None);
            }
        };
        var loadRecipe = new Button { Content = "Load recipe" };
        AutomationProperties.SetName(loadRecipe, "Load AI recipe");
        loadRecipe.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(loadRecipe);
            if (topLevel is null)
            {
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Frame Theory AI recipe") { Patterns = ["*.ftrecipe.json"] }]
            });
            var path = files.SingleOrDefault()?.TryGetLocalPath();
            if (path is not null)
            {
                await viewModel.LoadAiRecipeAsync(path, CancellationToken.None);
            }
        };
        var recipeStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(recipeStatus, "AI recipe status");
        recipeStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.AiRecipeStatusText)));
        var planSection = CreateInspectorExpander(
            "Plan input and revision",
            CreateSectionContent(planJson, CreateFlow(revisionInstruction, requestRevision), revisionStatus),
            expanded: true);
        var reviewSection = CreateInspectorExpander(
            "Review and apply",
            CreateSectionContent(CreateFlow(validate, approve, undo, clear), approvalSummary, actions, status),
            expanded: true);
        var recipeSection = CreateInspectorExpander(
            "Reusable recipe",
            CreateSectionContent(CreateFlow(recipeName, saveRecipe, loadRecipe), recipeStatus),
            expanded: false);
        return new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(16),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(29, 32, 39)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 54, 65)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "AI proposal review", FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White },
                    new TextBlock { Text = "Plans are untrusted data. Only checked actions are applied, atomically, through editable timeline operations; failures roll back the whole group.", TextWrapping = TextWrapping.Wrap },
                    planSection,
                    reviewSection,
                    recipeSection
                }
            }
        };
    }

    private static Control CreateColorInspectorPanel(MainWindowViewModel viewModel)
    {
        var inspector = viewModel.ColorInspector;
        var beforeImage = new Image
        {
            Width = 240,
            Height = 135,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(beforeImage, "Color grade before preview");
        var afterImage = new Image
        {
            Width = 240,
            Height = 135,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(afterImage, "Color grade after preview");
        var beforeEmptyState = new TextBlock
        {
            Text = "Import media to compare",
            FontSize = 12,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(122, 130, 145)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(beforeEmptyState, "Color before preview empty state");
        var afterEmptyState = new TextBlock
        {
            Text = "Import media to compare",
            FontSize = 12,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(122, 130, 145)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(afterEmptyState, "Color after preview empty state");
        var waveformScope = new Image
        {
            Width = 240,
            Height = 128,
            Stretch = Stretch.Fill,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        AutomationProperties.SetName(waveformScope, "Color waveform scope");
        ToolTip.SetTip(waveformScope, "Luminance distribution calculated from the graded local preview.");
        var vectorscope = new Image
        {
            Width = 240,
            Height = 128,
            Stretch = Stretch.Fill,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        AutomationProperties.SetName(vectorscope, "Color vectorscope");
        ToolTip.SetTip(vectorscope, "Chrominance distribution calculated from the graded local preview.");
        void RefreshPreviews()
        {
            var beforePpm = inspector.BeforePreviewPpm;
            var afterPpm = inspector.AfterPreviewPpm;
            var scopes = inspector.Scopes;
            beforeImage.Source = CreateBitmap(beforePpm);
            afterImage.Source = CreateBitmap(afterPpm);
            beforeEmptyState.IsVisible = beforePpm is null;
            afterEmptyState.IsVisible = afterPpm is null;
            waveformScope.Source = CreateScopeBitmap(scopes, vectorscope: false);
            vectorscope.Source = CreateScopeBitmap(scopes, vectorscope: true);
        }

        inspector.PropertyChanged += (_, _) => RefreshPreviews();
        RefreshPreviews();
        var beforeSummary = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(beforeSummary, "Color grade before summary");
        beforeSummary.Bind(TextBlock.TextProperty, new Binding("ColorInspector.BeforeSummary"));
        var afterSummary = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(afterSummary, "Color grade after summary");
        afterSummary.Bind(TextBlock.TextProperty, new Binding("ColorInspector.AfterSummary"));
        var scopeSummary = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(scopeSummary, "Color scope summary");
        scopeSummary.Bind(TextBlock.TextProperty, new Binding("ColorInspector.ScopeSummary"));
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(status, "Color inspector status");
        status.Bind(TextBlock.TextProperty, new Binding("ColorInspector.StatusText"));
        var controls = new StackPanel { Spacing = 4 };
        controls.Children.Add(CreateGradeControl("Exposure EV", -8, 8, inspector.ExposureEV, value => inspector.ExposureEV = value, inspector, nameof(ColorGradeInspectorViewModel.ExposureEV), () => inspector.ExposureEV));
        controls.Children.Add(CreateGradeControl("Contrast", -1, 1, inspector.Contrast, value => inspector.Contrast = value, inspector, nameof(ColorGradeInspectorViewModel.Contrast), () => inspector.Contrast));
        controls.Children.Add(CreateGradeControl("Saturation", -1, 1, inspector.Saturation, value => inspector.Saturation = value, inspector, nameof(ColorGradeInspectorViewModel.Saturation), () => inspector.Saturation));
        controls.Children.Add(CreateGradeControl("Temperature", -1, 1, inspector.Temperature, value => inspector.Temperature = value, inspector, nameof(ColorGradeInspectorViewModel.Temperature), () => inspector.Temperature));
        controls.Children.Add(CreateGradeControl("Tint", -1, 1, inspector.Tint, value => inspector.Tint = value, inspector, nameof(ColorGradeInspectorViewModel.Tint), () => inspector.Tint));
        var reset = new Button { Content = "Reset grade" };
        AutomationProperties.SetName(reset, "Reset color grade");
        ToolTip.SetTip(reset, "Clear the current grade and return the local preview to the shared default pipeline.");
        reset.Click += (_, _) => inspector.ResetGrade();
        controls.Children.Add(reset);
        var presetStore = new JsonColorPresetStore();
        var presetName = new TextBox { PlaceholderText = "Style name", Width = 160 };
        AutomationProperties.SetName(presetName, "Color style name");
        var presetSelection = new ComboBox { Width = 220 };
        AutomationProperties.SetName(presetSelection, "Loaded color styles");
        var presetStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(presetStatus, "Color style library status");
        var savePreset = new Button { Content = "Save style library" };
        AutomationProperties.SetName(savePreset, "Save color style library");
        ToolTip.SetTip(savePreset, "Save the current grade as a reusable local style library.");
        savePreset.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(presetName.Text))
            {
                presetStatus.Text = "Enter a style name before saving.";
                return;
            }

            var topLevel = TopLevel.GetTopLevel(savePreset);
            if (topLevel is null)
            {
                return;
            }

            try
            {
                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    SuggestedFileName = "frame-theory-styles.json",
                    FileTypeChoices = [new FilePickerFileType("Frame Theory styles") { Patterns = ["*.json"] }]
                });
                var path = file?.TryGetLocalPath();
                if (path is not null)
                {
                    await presetStore.SaveAtomicAsync(path, [inspector.CreatePreset(presetName.Text)], CancellationToken.None);
                    presetStatus.Text = $"Saved style library: {Path.GetFileName(path)}.";
                }
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException)
            {
                presetStatus.Text = $"Style save failed: {exception.Message}";
            }
        };
        var loadPreset = new Button { Content = "Load styles" };
        AutomationProperties.SetName(loadPreset, "Load color style library");
        ToolTip.SetTip(loadPreset, "Load reusable local styles without changing the current grade until applied.");
        loadPreset.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(loadPreset);
            if (topLevel is null)
            {
                return;
            }

            try
            {
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    AllowMultiple = false,
                    FileTypeFilter = [new FilePickerFileType("Frame Theory styles") { Patterns = ["*.json"] }]
                });
                var path = files.SingleOrDefault()?.TryGetLocalPath();
                if (path is not null)
                {
                    var presets = await presetStore.LoadAsync(path, CancellationToken.None);
                    presetSelection.ItemsSource = presets;
                    presetSelection.SelectedIndex = presets.Count == 0 ? -1 : 0;
                    presetStatus.Text = $"Loaded {presets.Count} style(s) from {Path.GetFileName(path)}.";
                }
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException)
            {
                presetStatus.Text = $"Style load failed: {exception.Message}";
            }
        };
        var applyPreset = new Button { Content = "Apply loaded style" };
        applyPreset.Classes.Add("primary");
        AutomationProperties.SetName(applyPreset, "Apply loaded color style");
        ToolTip.SetTip(applyPreset, "Apply the selected reusable style to the local color preview.");
        applyPreset.Click += (_, _) =>
        {
            if (presetSelection.SelectedItem is ColorPreset preset)
            {
                inspector.ApplyPreset(preset);
                presetStatus.Text = $"Applied style '{preset.Name}'.";
            }
            else
            {
                presetStatus.Text = "Load and select a style first.";
            }
        };
        var referenceSource = new ComboBox
        {
            Width = 240,
            ItemTemplate = new FuncDataTemplate<ImportedMediaViewModel>((item, _) =>
            {
                if (item is null)
                {
                    return new TextBlock { Text = "Unavailable media" };
                }

                return new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new Image
                        {
                            Width = 36,
                            Height = 22,
                            Margin = new Thickness(0, 0, 8, 0),
                            Stretch = Stretch.UniformToFill,
                            Source = CreateBitmap(item.ThumbnailPpm)
                        },
                        new TextBlock
                        {
                            Text = item.Name,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        }
                    }
                };
            }, supportsRecycling: false)
        };
        AutomationProperties.SetName(referenceSource, "Reference source preview");
        var referenceTarget = new ComboBox
        {
            Width = 240,
            ItemTemplate = new FuncDataTemplate<ImportedMediaViewModel>((item, _) =>
            {
                if (item is null)
                {
                    return new TextBlock { Text = "Unavailable media" };
                }

                return new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                    Children =
                    {
                        new Image
                        {
                            Width = 36,
                            Height = 22,
                            Margin = new Thickness(0, 0, 8, 0),
                            Stretch = Stretch.UniformToFill,
                            Source = CreateBitmap(item.ThumbnailPpm)
                        },
                        new TextBlock
                        {
                            Text = item.Name,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        }
                    }
                };
            }, supportsRecycling: false)
        };
        AutomationProperties.SetName(referenceTarget, "Reference target preview");
        var referenceConsent = new CheckBox { Content = "I approve uploading these exact previews to Gemini" };
        var analyzeReference = new Button { Content = "Analyze approved visual reference" };
        analyzeReference.Classes.Add("primary");
        AutomationProperties.SetName(analyzeReference, "Analyze approved visual reference");
        ToolTip.SetTip(analyzeReference, "Analyze the explicitly approved source and reference previews for a reviewable grade.");
        var applyReference = new Button { Content = "Apply reviewed reference grade" };
        applyReference.Classes.Add("primary");
        AutomationProperties.SetName(applyReference, "Apply reviewed reference grade");
        ToolTip.SetTip(applyReference, "Apply the reviewed reference grade to the local preview without uploading it again.");
        var referenceStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(referenceStatus, "Reference analysis status");
        referenceStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.ReferenceAnalysisStatusText)));
        void RefreshReferenceSources()
        {
            referenceSource.ItemsSource = viewModel.ImportedMedia;
            referenceTarget.ItemsSource = viewModel.ImportedMedia;
            if (referenceSource.SelectedIndex < 0 && viewModel.ImportedMedia.Count > 0)
            {
                referenceSource.SelectedIndex = 0;
            }

            if (referenceTarget.SelectedIndex < 0 && viewModel.ImportedMedia.Count > 1)
            {
                referenceTarget.SelectedIndex = 1;
            }
        }

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainWindowViewModel.ImportedMedia))
            {
                RefreshReferenceSources();
            }
        };
        RefreshReferenceSources();
        analyzeReference.Click += async (_, _) => await viewModel.AnalyzeColorReferenceAsync(
            referenceSource.SelectedItem as ImportedMediaViewModel,
            referenceTarget.SelectedItem as ImportedMediaViewModel,
            referenceConsent.IsChecked == true,
            CancellationToken.None);
        applyReference.Click += (_, _) => viewModel.ApplyPendingReferenceGrade();
        var beforePane = new Border
        {
            Padding = new Thickness(8),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(24, 27, 33)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = "BEFORE", FontSize = 10, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(172, 181, 196)) },
                    new Grid { Children = { beforeEmptyState, beforeImage } },
                    beforeSummary
                }
            }
        };
        var afterPane = new Border
        {
            Padding = new Thickness(8),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(24, 27, 33)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = "AFTER", FontSize = 10, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(172, 181, 196)) },
                    new Grid { Children = { afterEmptyState, afterImage } },
                    afterSummary
                }
            }
        };
        var colorPreviewCard = new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(16, 18, 22)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
            BorderThickness = new Thickness(1),
            Child = CreateFlow(beforePane, afterPane)
        };
        AutomationProperties.SetName(colorPreviewCard, "Color before and after preview");
        ToolTip.SetTip(colorPreviewCard, "Compare the ungraded source with the current shared color grade.");
        var referenceSection = CreateInspectorExpander(
            "Reference match",
            CreateSectionContent(
                new TextBlock { Text = "Select a source preview and a reference preview. Upload occurs only after explicit consent; the returned grade is reviewable before applying.", TextWrapping = TextWrapping.Wrap },
                CreateFlow(referenceSource, referenceTarget),
                referenceConsent,
                CreateFlow(analyzeReference, applyReference),
                referenceStatus),
            expanded: false);
        var beforeAfterSection = CreateInspectorExpander(
            "Before and after",
            CreateSectionContent(colorPreviewCard),
            expanded: true);
        var scopeCard = new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(16, 18, 22)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
            BorderThickness = new Thickness(1),
            Child = CreateFlow(
                new Border
                {
                    Padding = new Thickness(8),
                    Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(24, 27, 33)),
                    BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Child = new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock { Text = "WAVEFORM", FontSize = 10, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(172, 181, 196)) },
                            waveformScope
                        }
                    }
                },
                new Border
                {
                    Padding = new Thickness(8),
                    Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(24, 27, 33)),
                    BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Child = new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock { Text = "VECTORSCOPE", FontSize = 10, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(172, 181, 196)) },
                            vectorscope
                        }
                    }
                })
        };
        AutomationProperties.SetName(scopeCard, "Color scopes preview");
        ToolTip.SetTip(scopeCard, "Inspect the post-grade waveform and vectorscope generated from the local preview.");
        var scopeSection = CreateInspectorExpander(
            "Scopes",
            CreateSectionContent(scopeCard, scopeSummary),
            expanded: true);
        var gradeSection = CreateInspectorExpander(
            "Grade controls",
            CreateSectionContent(controls),
            expanded: true);
        var styleSection = CreateInspectorExpander(
            "Style library",
            CreateSectionContent(CreateFlow(presetName, savePreset, loadPreset, presetSelection, applyPreset), presetStatus),
            expanded: false);
        return new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(16),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(29, 32, 39)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 54, 65)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Color grade inspector", FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White },
                    status,
                    referenceSection,
                    beforeAfterSection,
                    scopeSection,
                    gradeSection,
                    styleSection
                }
            }
        };
    }

    private static Control CreateTimelinePanel(MainWindowViewModel viewModel)
    {
        var timeline = viewModel.TimelineEditor;
        var timelinePreview = new Image
        {
            Width = 320,
            Height = 180,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(timelinePreview, "Timeline current frame preview");
        var timelinePreviewEmptyState = new TextBlock
        {
            Text = "Preview a frame to inspect the timeline",
            FontSize = 12,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(122, 130, 145)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(timelinePreviewEmptyState, "Timeline preview empty state");
        var timelinePreviewStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(timelinePreviewStatus, "Timeline preview status");
        timelinePreviewStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.TimelinePreviewStatusText)));
        var timelinePerformanceStatus = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(timelinePerformanceStatus, "Timeline preview performance status");
        timelinePerformanceStatus.Bind(TextBox.TextProperty, new Binding(nameof(MainWindowViewModel.TimelinePerformanceStatusText)));
        var analyzeMotion = new Button { Content = "Analyze motion" };
        AutomationProperties.SetName(analyzeMotion, "Analyze timeline motion");
        analyzeMotion.Click += async (_, _) => await viewModel.AnalyzeTimelineMotionAsync(CancellationToken.None);
        var motionAnalysisStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(motionAnalysisStatus, "Timeline motion analysis status");
        motionAnalysisStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.MotionAnalysisStatusText)));
        void RefreshTimelinePreview()
        {
            var previewPpm = viewModel.TimelinePreviewPpm;
            timelinePreview.Source = CreateBitmap(previewPpm);
            timelinePreviewEmptyState.IsVisible = previewPpm is null;
        }
        RefreshTimelinePreview();
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(MainWindowViewModel.TimelinePreviewPpm))
            {
                RefreshTimelinePreview();
            }
        };
        var previewCurrentFrame = new Button { Content = "Preview current frame" };
        AutomationProperties.SetName(previewCurrentFrame, "Preview current timeline frame");
        ToolTip.SetTip(previewCurrentFrame, "Render the frame at the current playhead position.");
        previewCurrentFrame.Click += async (_, _) => await viewModel.PreviewCurrentTimelineFrameAsync(CancellationToken.None);
        var playTimeline = new Button { Content = "Play timeline" };
        playTimeline.Classes.Add("primary");
        AutomationProperties.SetName(playTimeline, "Play timeline preview");
        ToolTip.SetTip(playTimeline, "Play the timeline from the current playhead position.");
        playTimeline.Click += async (_, _) => await viewModel.PlayTimelinePreviewAsync(CancellationToken.None);
        var stopTimeline = new Button { Content = "Stop playback" };
        AutomationProperties.SetName(stopTimeline, "Stop timeline preview");
        ToolTip.SetTip(stopTimeline, "Stop timeline playback and keep the current playhead position.");
        stopTimeline.Click += (_, _) => viewModel.StopTimelinePreview();
        var usePreviewProxy = new CheckBox { Content = "Use bounded preview proxy" };
        usePreviewProxy.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(MainWindowViewModel.UseTimelinePreviewProxy))
        {
            Mode = BindingMode.TwoWay
        });
        AutomationProperties.SetName(usePreviewProxy, "Use bounded timeline preview proxy");
        ToolTip.SetTip(usePreviewProxy, "Limit preview decoding to a bounded local proxy for responsive playback.");
        var playhead = new Slider { Minimum = 0, Maximum = Math.Max(1, timeline.TotalFrames - 1), Value = timeline.CurrentFrame };
        AutomationProperties.SetName(playhead, "Timeline playhead frame");
        ToolTip.SetTip(playhead, "Scrub to an exact timeline frame.");
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
        AutomationProperties.SetName(frameStatus, "Timeline frame status");
        frameStatus.Bind(TextBlock.TextProperty, new Binding("TimelineEditor.CurrentFrame"));
        var positionStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(positionStatus, "Timeline position status");
        positionStatus.Bind(TextBlock.TextProperty, new Binding("TimelineEditor.CurrentTimecode"));
        var monitorTimecode = new TextBlock
        {
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White
        };
        AutomationProperties.SetName(monitorTimecode, "Timeline monitor timecode");
        ToolTip.SetTip(monitorTimecode, "Current program monitor position.");
        monitorTimecode.Bind(TextBlock.TextProperty, new Binding("TimelineEditor.CurrentTimecode"));
        var frameReadout = new Border
        {
            Padding = new Thickness(8, 5),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(16, 18, 22)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    new TextBlock { Text = "FRAME", FontSize = 9, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(126, 143, 190)) },
                    frameStatus
                }
            }
        };
        AutomationProperties.SetName(frameReadout, "Timeline frame readout");
        var positionReadout = new Border
        {
            Padding = new Thickness(8, 5),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(16, 18, 22)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    new TextBlock { Text = "POSITION", FontSize = 9, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(126, 143, 190)) },
                    positionStatus
                }
            }
        };
        AutomationProperties.SetName(positionReadout, "Timeline position readout");
        var ruler = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(ruler, "Timeline ruler");
        ToolTip.SetTip(ruler, "Current frame and timeline position.");
        ruler.Bind(TextBlock.TextProperty, new Binding("TimelineEditor.RulerText"));
        var rulerCanvas = new Canvas { Width = 640, Height = 30 };
        var rulerVisual = new Border
        {
            Padding = new Thickness(8, 4),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(16, 18, 22)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                Content = rulerCanvas
            }
        };
        AutomationProperties.SetName(rulerVisual, "Timeline visual ruler");
        ToolTip.SetTip(rulerVisual, "Click the ruler to seek to an exact timeline frame.");
        rulerCanvas.PointerPressed += (_, args) =>
        {
            timeline.ScrubPixels(Math.Max(0, args.GetPosition(rulerCanvas).X));
            args.Handled = true;
        };
        void RefreshRulerVisual()
        {
            rulerCanvas.Children.Clear();
            var playheadMark = new Border
            {
                Width = 2,
                Height = 30,
                Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 92, 112)),
                IsHitTestVisible = false
            };
            AutomationProperties.SetName(playheadMark, "Timeline ruler playhead");
            Canvas.SetLeft(playheadMark, Math.Max(0, (timeline.CurrentFrame - timeline.ScrollFrame) / timeline.FramesPerPixel));
            Canvas.SetTop(playheadMark, 0);
            rulerCanvas.Children.Add(playheadMark);
            foreach (var tick in timeline.BuildRulerTicks(640))
            {
                var left = Math.Max(0, (tick.Frame - timeline.ScrollFrame) / timeline.FramesPerPixel);
                var tickMark = new Border
                {
                    Width = 1,
                    Height = 8,
                    Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(95, 125, 255))
                };
                Canvas.SetLeft(tickMark, left);
                Canvas.SetTop(tickMark, 0);
                rulerCanvas.Children.Add(tickMark);
                var tickLabel = new TextBlock
                {
                    Text = $"{tick.Frame}  {tick.Position.TotalSeconds:0.##}s",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(172, 181, 196))
                };
                Canvas.SetLeft(tickLabel, left + 4);
                Canvas.SetTop(tickLabel, 9);
                rulerCanvas.Children.Add(tickLabel);
            }
        }
        RefreshRulerVisual();
        timeline.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(TimelineEditorViewModel.RulerText) or nameof(TimelineEditorViewModel.ScrollFrame) or nameof(TimelineEditorViewModel.FramesPerPixel) or nameof(TimelineEditorViewModel.TotalFrames))
            {
                RefreshRulerVisual();
            }
        };
        var selectionContext = new TextBox
        {
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            MinHeight = 0
        };
        AutomationProperties.SetName(selectionContext, "Timeline selection context");
        ToolTip.SetTip(selectionContext, "Selected clip, track and playhead context.");
        selectionContext.Bind(TextBox.TextProperty, new Binding("TimelineEditor.SelectionContextText"));
        var selectionInspector = new Border
        {
            MinHeight = 52,
            Padding = new Thickness(10, 8),
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(20, 23, 29)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 54, 65)),
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Spacing = 3,
                Children =
                {
                    new TextBlock
                    {
                        Text = "SELECTION INSPECTOR",
                        FontSize = 10,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(126, 143, 190))
                    },
                    selectionContext
                }
            }
        };
        AutomationProperties.SetName(selectionInspector, "Timeline selection inspector");
        var tracks = new ListBox
        {
            Height = 124,
            ItemTemplate = new FuncDataTemplate<TimelineTrackRowViewModel>((item, _) =>
            {
                if (item is null)
                {
                    return new TextBlock { Text = "Unavailable track" };
                }

                var maxFrame = item.Clips.Select(clip => clip.StartFrame + clip.DurationFrames).DefaultIfEmpty(timeline.ScrollFrame + 1).Max();
                var clipCanvas = new Canvas
                {
                    Width = Math.Max(320, (maxFrame - timeline.ScrollFrame) / timeline.FramesPerPixel + 24),
                    Height = 52
                };
                AutomationProperties.SetName(clipCanvas, $"{item.Kind} timeline clip canvas");
                foreach (var clip in item.Clips)
                {
                    var clipWidth = Math.Max(128, clip.DurationFrames / timeline.FramesPerPixel);
                    var clipMedia = viewModel.ImportedMedia.FirstOrDefault(media => media.Id?.Value == clip.MediaId);
                    var waveform = CreateWaveformCanvas(clipMedia?.WaveformPeaks, clipWidth - 16, 10);
                    AutomationProperties.SetName(waveform, $"{item.Kind} waveform for {clip.MediaId}");
                    ToolTip.SetTip(waveform, "Imported local waveform peaks for this timeline clip.");
                    var clipButton = new Button
                    {
                        Width = clipWidth,
                        MinWidth = 128,
                        Height = 46,
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Padding = new Thickness(8, 4),
                        Background = new SolidColorBrush(clip.IsSelected
                            ? Avalonia.Media.Color.FromRgb(49, 67, 111)
                            : Avalonia.Media.Color.FromRgb(36, 41, 49)),
                        BorderBrush = new SolidColorBrush(clip.IsSelected
                            ? Avalonia.Media.Color.FromRgb(95, 125, 255)
                            : Avalonia.Media.Color.FromRgb(59, 68, 83)),
                        BorderThickness = new Thickness(1),
                        Content = new StackPanel
                        {
                            Spacing = 2,
                            Children =
                            {
                                new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    Spacing = 6,
                                    Children =
                                    {
                                        new Border
                                        {
                                            Width = 42,
                                            Height = 24,
                                            Background = Brushes.Black,
                                            Child = new Image
                                            {
                                                Source = CreateBitmap(clipMedia?.ThumbnailPpm),
                                                Stretch = Stretch.UniformToFill
                                            }
                                        },
                                        new TextBlock
                                        {
                                            Text = $"{clipMedia?.Name ?? clip.MediaId[..Math.Min(8, clip.MediaId.Length)]}  {clip.StartFrame}–{clip.StartFrame + clip.DurationFrames - 1}",
                                            FontSize = 10,
                                            Foreground = Brushes.White,
                                            TextTrimming = TextTrimming.CharacterEllipsis,
                                            VerticalAlignment = VerticalAlignment.Center
                                        }
                                    }
                                },
                                waveform
                            }
                        }
                    };
                    AutomationProperties.SetName(clipButton, $"Select {item.Kind} clip {clip.MediaId}{(clip.IsSelected ? " (selected)" : string.Empty)}");
                    ToolTip.SetTip(clipButton, $"{item.Kind} clip · frames {clip.StartFrame}–{clip.StartFrame + clip.DurationFrames - 1}{(clip.IsSelected ? " · Selected." : string.Empty)}");
                    clipButton.Click += (_, _) => timeline.SelectMedia(clip.MediaId, item.Kind);
                    Canvas.SetLeft(clipButton, Math.Max(0, (clip.StartFrame - timeline.ScrollFrame) / timeline.FramesPerPixel));
                    Canvas.SetTop(clipButton, 4);
                    clipCanvas.Children.Add(clipButton);
                }
                var playheadIndicator = new Border
                {
                    Width = 2,
                    Height = 52,
                    Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 92, 112)),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(playheadIndicator, Math.Max(0, (timeline.CurrentFrame - timeline.ScrollFrame) / timeline.FramesPerPixel));
                Canvas.SetTop(playheadIndicator, 0);
                clipCanvas.Children.Add(playheadIndicator);
                var clipViewport = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    Content = clipCanvas
                };
                AutomationProperties.SetName(clipViewport, $"{item.Kind} timeline clip viewport");
                var applyingTimelineScroll = false;
                void AlignClipViewport()
                {
                    if (!clipViewport.IsAttachedToVisualTree())
                    {
                        return;
                    }

                    applyingTimelineScroll = true;
                    clipViewport.Offset = new Avalonia.Vector(timeline.ScrollFrame / timeline.FramesPerPixel, 0);
                    applyingTimelineScroll = false;
                }

                void ApplyViewportScroll()
                {
                    if (applyingTimelineScroll || !clipViewport.IsAttachedToVisualTree())
                    {
                        return;
                    }

                    var requestedFrame = Math.Clamp(
                        (long)Math.Round(clipViewport.Offset.X * timeline.FramesPerPixel, MidpointRounding.AwayFromZero),
                        0,
                        Math.Max(0, timeline.TotalFrames - 1));
                    if (requestedFrame != timeline.ScrollFrame)
                    {
                        timeline.ScrollBy(requestedFrame - timeline.ScrollFrame);
                    }
                }

                clipViewport.ScrollChanged += (_, _) => ApplyViewportScroll();
                clipViewport.PropertyChanged += (_, args) =>
                {
                    if (args.Property == ScrollViewer.OffsetProperty)
                    {
                        ApplyViewportScroll();
                    }
                };
                clipViewport.AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(AlignClipViewport);

                var trackLabel = new TextBlock
                {
                    Text = $"{item.Kind}  ·  {item.Id}",
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(186, 193, 207)),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                var trackAccent = new Border
                {
                    Width = 4,
                    CornerRadius = new CornerRadius(2),
                    Background = new SolidColorBrush(item.Kind == TimelineTrackKind.Video
                        ? Avalonia.Media.Color.FromRgb(95, 125, 255)
                        : Avalonia.Media.Color.FromRgb(235, 161, 82))
                };
                var trackGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("4,116,*"),
                    Children = { trackAccent, trackLabel, clipViewport }
                };
                Grid.SetColumn(trackLabel, 1);
                Grid.SetColumn(clipViewport, 2);
                var trackLane = new Border
                {
                    MinHeight = 48,
                    Margin = new Thickness(0, 0, 0, 4),
                    Padding = new Thickness(8, 4),
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(20, 23, 29)),
                    BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 54, 65)),
                    BorderThickness = new Thickness(1),
                    Child = trackGrid
                };
                AutomationProperties.SetName(trackLane, $"{item.Kind} timeline lane");
                ToolTip.SetTip(trackLane, $"{item.Kind} track {item.Id} · {item.Clips.Count} clip(s).");
                return trackLane;
            }, supportsRecycling: false)
        };
        AutomationProperties.SetName(tracks, "Timeline tracks");
        ToolTip.SetTip(tracks, "Select clips in the video and audio edit lanes.");
        tracks.Bind(ItemsControl.ItemsSourceProperty, new Binding("TimelineEditor.TrackRows"));
        var beatMarkers = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(beatMarkers, "Timeline beat markers");
        beatMarkers.Bind(TextBlock.TextProperty, new Binding("TimelineEditor.BeatMarkersText"));
        var beatDetection = new ComboBox { ItemsSource = new[] { "Amplitude peaks", "Energy onsets" }, SelectedIndex = 0, Width = 150 };
        AutomationProperties.SetName(beatDetection, "Beat detection mode");
        ToolTip.SetTip(beatDetection, "Choose the local waveform analysis used for beat markers.");
        beatDetection.SelectionChanged += (_, _) => timeline.BeatDetectionMode = beatDetection.SelectedIndex == 1
            ? BeatDetectionMode.EnergyOnsets
            : BeatDetectionMode.AmplitudePeaks;
        var prepareRoughCut = new Button { Content = "Plan activity rough cut" };
        AutomationProperties.SetName(prepareRoughCut, "Plan waveform activity rough cut");
        prepareRoughCut.Click += (_, _) => viewModel.PrepareRoughCutPlan();
        var applyRoughCut = new Button { Content = "Apply reviewed rough cut" };
        AutomationProperties.SetName(applyRoughCut, "Apply reviewed rough cut");
        applyRoughCut.Click += (_, _) => viewModel.ApplyRoughCutPlan();
        var roughCutStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(roughCutStatus, "Rough cut status");
        roughCutStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.RoughCutStatusText)));
        var snap = new CheckBox { Content = "Snap to clip boundaries" };
        AutomationProperties.SetName(snap, "Snap timeline edits to clip boundaries");
        ToolTip.SetTip(snap, "Keep clip moves, trims and inserts aligned to nearby clip edges.");
        snap.Bind(ToggleButton.IsCheckedProperty, new Binding("TimelineEditor.SnapEnabled") { Mode = BindingMode.TwoWay });
        var zoom = new Slider { Minimum = 0.05, Maximum = 20, Value = timeline.FramesPerPixel };
        AutomationProperties.SetName(zoom, "Timeline zoom");
        ToolTip.SetTip(zoom, "Adjust timeline scale in frames per pixel.");
        var zoomValue = new TextBlock
        {
            Text = $"{timeline.FramesPerPixel:0.00} frames/px",
            Width = 104,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(172, 181, 196)),
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(zoomValue, "Timeline zoom value");
        zoom.ValueChanged += (_, args) =>
        {
            timeline.SetZoom(args.NewValue);
            zoomValue.Text = $"{timeline.FramesPerPixel:0.00} frames/px";
        };
        timeline.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(TimelineEditorViewModel.FramesPerPixel))
            {
                zoom.Value = timeline.FramesPerPixel;
                zoomValue.Text = $"{timeline.FramesPerPixel:0.00} frames/px";
            }
        };
        var back = new Button { Content = "◀ Frame" };
        AutomationProperties.SetName(back, "Previous timeline frame");
        back.Click += (_, _) => timeline.Step(-1);
        var forward = new Button { Content = "Frame ▶" };
        AutomationProperties.SetName(forward, "Next timeline frame");
        forward.Click += (_, _) => timeline.Step(1);
        var select = new Button { Content = "Select at playhead" };
        AutomationProperties.SetName(select, "Select video clip at playhead");
        select.Click += (_, _) => timeline.SelectAt(timeline.CurrentFrame, "Video 1");
        var selectAudio = new Button { Content = "Select audio" };
        AutomationProperties.SetName(selectAudio, "Select audio clip at playhead");
        selectAudio.Click += (_, _) => timeline.SelectAt(timeline.CurrentFrame, "Audio 1");
        var operationStatus = new TextBox
        {
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            MinHeight = 0
        };
        AutomationProperties.SetName(operationStatus, "Timeline operation status");
        ToolTip.SetTip(operationStatus, "Validation or edit result for the last timeline action.");
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
        AutomationProperties.SetName(copy, "Copy selected timeline clip");
        copy.Click += (_, _) =>
        {
            clipboard = timeline.CopySelected();
            operationStatus.Text = clipboard is null ? "Select a clip before copying." : "Clip copied.";
        };
        var paste = new Button { Content = "Paste at playhead" };
        AutomationProperties.SetName(paste, "Paste timeline clip at playhead");
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
        AutomationProperties.SetName(split, "Split selected timeline clip");
        split.Click += (_, _) => RunTimelineAction(() => timeline.SplitSelectedAt(timeline.CurrentFrame));
        var trim = new Button { Content = "Trim right" };
        AutomationProperties.SetName(trim, "Trim selected clip at playhead");
        trim.Click += (_, _) => RunTimelineAction(() => timeline.TrimSelectedEndAt(timeline.CurrentFrame));
        var move = new Button { Content = "Move to playhead" };
        AutomationProperties.SetName(move, "Move selected clip to playhead");
        move.Click += (_, _) => RunTimelineAction(() => timeline.MoveSelected(timeline.CurrentFrame));
        var rippleDelete = new Button { Content = "Ripple delete" };
        AutomationProperties.SetName(rippleDelete, "Ripple delete selected clip");
        rippleDelete.Click += (_, _) => RunTimelineAction(timeline.RippleDeleteSelected);
        var fadeIn = new Button { Content = "Fade in preset" };
        AutomationProperties.SetName(fadeIn, "Apply fade-in animation preset");
        fadeIn.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAnimationPresetToSelected(TimelineAnimationPreset.FadeIn));
        var pushIn = new Button { Content = "Push in preset" };
        AutomationProperties.SetName(pushIn, "Apply push-in animation preset");
        pushIn.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAnimationPresetToSelected(TimelineAnimationPreset.PushIn));
        var clearAnimation = new Button { Content = "Clear animation" };
        AutomationProperties.SetName(clearAnimation, "Clear selected clip animation");
        clearAnimation.Click += (_, _) => RunTimelineAction(timeline.ClearSelectedAnimation);
        var speedRampStart = new TextBox { Text = "1", Width = 55 };
        AutomationProperties.SetName(speedRampStart, "Speed ramp start multiplier");
        var speedRampEnd = new TextBox { Text = "2", Width = 55 };
        AutomationProperties.SetName(speedRampEnd, "Speed ramp end multiplier");
        var applySpeedRamp = new Button { Content = "Apply speed ramp" };
        AutomationProperties.SetName(applySpeedRamp, "Apply speed ramp");
        applySpeedRamp.Click += (_, _) =>
        {
            if (!double.TryParse(speedRampStart.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var startSpeed) ||
                !double.TryParse(speedRampEnd.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var endSpeed))
            {
                operationStatus.Text = "Enter speed values from 0.25 to 4.";
                return;
            }

            RunTimelineAction(() => timeline.ApplySpeedRampToSelected(startSpeed, endSpeed));
        };
        var clearSpeedRamp = new Button { Content = "Clear speed ramp" };
        AutomationProperties.SetName(clearSpeedRamp, "Clear speed ramp");
        clearSpeedRamp.Click += (_, _) => RunTimelineAction(timeline.ClearSelectedSpeedRamp);
        var stabilizationRadius = new TextBox { Text = "8", Width = 55 };
        AutomationProperties.SetName(stabilizationRadius, "Stabilization search radius");
        var applyStabilization = new Button { Content = "Apply stabilization" };
        AutomationProperties.SetName(applyStabilization, "Apply stabilization");
        applyStabilization.Click += (_, _) =>
        {
            if (!int.TryParse(stabilizationRadius.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var radius))
            {
                operationStatus.Text = "Enter a stabilization radius from 1 to 64.";
                return;
            }

            RunTimelineAction(() => timeline.ApplyStabilizationToSelected(radius));
        };
        var clearStabilization = new Button { Content = "Clear stabilization" };
        AutomationProperties.SetName(clearStabilization, "Clear stabilization");
        clearStabilization.Click += (_, _) => RunTimelineAction(timeline.ClearSelectedStabilization);
        var muteAudio = new Button { Content = "Mute audio" };
        AutomationProperties.SetName(muteAudio, "Mute selected audio");
        muteAudio.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAudioSettingsToSelected(0, muted: true));
        var gainAudio = new Button { Content = "Audio +3 dB" };
        AutomationProperties.SetName(gainAudio, "Increase selected audio by 3 dB");
        gainAudio.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAudioSettingsToSelected(3, muted: false));
        var fadeAudio = new Button { Content = "Audio fades" };
        AutomationProperties.SetName(fadeAudio, "Apply selected audio fades");
        fadeAudio.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAudioSettingsToSelected(0, muted: false, fadeInFrames: 2, fadeOutFrames: 2));
        var duckAudio = new Button { Content = "Duck audio -12 dB" };
        AutomationProperties.SetName(duckAudio, "Duck selected audio by 12 dB");
        duckAudio.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAudioDuckingToSelected());
        var clearDucking = new Button { Content = "Clear ducking" };
        AutomationProperties.SetName(clearDucking, "Clear selected audio ducking");
        clearDucking.Click += (_, _) => RunTimelineAction(timeline.ClearAudioDuckingFromSelected);
        var clearAudio = new Button { Content = "Clear audio" };
        AutomationProperties.SetName(clearAudio, "Clear selected audio settings");
        clearAudio.Click += (_, _) => RunTimelineAction(timeline.ClearSelectedAudioSettings);
        var titleText = new TextBox { PlaceholderText = "Title text", Width = 220 };
        AutomationProperties.SetName(titleText, "Timeline title text");
        var addTitle = new Button { Content = "Add title" };
        AutomationProperties.SetName(addTitle, "Add timeline title");
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
        AutomationProperties.SetName(fadeTitle, "Apply fade-in animation to last title");
        fadeTitle.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAnimationPresetToLastTitle(TimelineAnimationPreset.FadeIn));
        var pushTitle = new Button { Content = "Push title in" };
        AutomationProperties.SetName(pushTitle, "Apply push-in animation to last title");
        pushTitle.Click += (_, _) => RunTimelineAction(() => timeline.ApplyAnimationPresetToLastTitle(TimelineAnimationPreset.PushIn));
        var titleAnimationStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(titleAnimationStatus, "Title animation status");
        titleAnimationStatus.Bind(TextBlock.TextProperty, new Binding(nameof(TimelineEditorViewModel.TitleAnimationStatus)));
        var importCaptions = new Button { Content = "Import SRT" };
        AutomationProperties.SetName(importCaptions, "Import SRT captions");
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
        var importTranscript = new Button { Content = "Import verified transcript" };
        AutomationProperties.SetName(importTranscript, "Import verified transcript JSON");
        importTranscript.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(importTranscript);
            if (topLevel is null)
            {
                return;
            }

            var sourceFiles = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Audio or video source") { Patterns = ["*.wav", "*.mp3", "*.m4a", "*.aac", "*.flac", "*.ogg", "*.opus", "*.aif", "*.aiff", "*.avi", "*.mkv", "*.mp4", "*.mov", "*.mpeg", "*.mpg", "*.webm"] }]
            });
            var sourcePath = sourceFiles.SingleOrDefault()?.TryGetLocalPath();
            if (sourcePath is null)
            {
                return;
            }

            var transcriptFiles = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Verified transcript JSON") { Patterns = ["*.transcript.json", "*.json"] }]
            });
            var transcriptPath = transcriptFiles.SingleOrDefault()?.TryGetLocalPath();
            if (transcriptPath is not null)
            {
                await viewModel.ImportTranscriptAsync(sourcePath, transcriptPath, CancellationToken.None);
            }
        };
        var exportCaptions = new Button { Content = "Export SRT" };
        AutomationProperties.SetName(exportCaptions, "Export SRT captions");
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
        var kineticCaptions = new CheckBox { Content = "Kinetic word captions" };
        AutomationProperties.SetName(kineticCaptions, "Kinetic word captions");
        kineticCaptions.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(TimelineEditorViewModel.KineticCaptions))
        {
            Mode = BindingMode.TwoWay
        });
        var captionStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(captionStatus, "Caption status");
        captionStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.CaptionStatusText)));
        var transcriptionConsent = new CheckBox { Content = "I approve uploading this exact local audio file or extracted video audio track to Gemini" };
        AutomationProperties.SetName(transcriptionConsent, "Gemini transcription upload consent");
        var transcribeAudio = new Button { Content = "Transcribe approved audio" };
        AutomationProperties.SetName(transcribeAudio, "Transcribe approved audio");
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
        var transcribeLocalWhisper = new Button { Content = "Transcribe with local Whisper" };
        AutomationProperties.SetName(transcribeLocalWhisper, "Transcribe with local Whisper");
        transcribeLocalWhisper.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(transcribeLocalWhisper);
            if (topLevel is null)
            {
                return;
            }

            var sourceFiles = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Audio or video source") { Patterns = ["*.wav", "*.mp3", "*.m4a", "*.aac", "*.flac", "*.ogg", "*.opus", "*.aif", "*.aiff", "*.avi", "*.mkv", "*.mp4", "*.mov", "*.mpeg", "*.mpg", "*.webm"] }]
            });
            var sourcePath = sourceFiles.SingleOrDefault()?.TryGetLocalPath();
            if (sourcePath is null)
            {
                return;
            }

            var executableFiles = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Local Whisper executable") { Patterns = ["*"] }]
            });
            var executablePath = executableFiles.SingleOrDefault()?.TryGetLocalPath();
            if (executablePath is null)
            {
                return;
            }

            var modelFiles = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Local Whisper model") { Patterns = ["*"] }]
            });
            var modelPath = modelFiles.SingleOrDefault()?.TryGetLocalPath();
            if (modelPath is not null)
            {
                await viewModel.TranscribeWithLocalWhisperAsync(sourcePath, executablePath, modelPath, CancellationToken.None);
            }
        };
        var captionFont = new Button { Content = "Choose caption font" };
        AutomationProperties.SetName(captionFont, "Choose caption font");
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
        export.Classes.Add("primary");
        AutomationProperties.SetName(export, "Export timeline MP4");
        ToolTip.SetTip(export, "Render the editable timeline to an MP4 using the selected aspect ratio.");
        var batchExport = new Button { Content = "Export all social targets" };
        AutomationProperties.SetName(batchExport, "Export all social video targets");
        batchExport.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(batchExport);
            if (topLevel is null)
            {
                return;
            }

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                AllowMultiple = false
            });
            var path = folders.SingleOrDefault()?.TryGetLocalPath();
            if (path is not null)
            {
                await viewModel.ExportTimelineBatchAsync(path, CancellationToken.None);
            }
        };
        var cancelExport = new Button { Content = "Cancel export" };
        AutomationProperties.SetName(cancelExport, "Cancel timeline export");
        cancelExport.Click += (_, _) => viewModel.CancelTimelineExport();
        var saveProject = new Button { Content = "Save timeline project" };
        AutomationProperties.SetName(saveProject, "Save timeline project");
        ToolTip.SetTip(saveProject, "Save the timeline, edits and media references to a portable project file.");
        saveProject.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(saveProject);
            if (topLevel is null)
            {
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = "frame-theory-project.ftstudio",
                FileTypeChoices = [new FilePickerFileType("Frame Theory project") { Patterns = ["*.ftstudio"] }]
            });
            var path = file?.TryGetLocalPath();
            if (path is not null)
            {
                await viewModel.SaveTimelineProjectAsync(path, CancellationToken.None);
            }
        };
        var openProject = new Button { Content = "Open timeline project" };
        AutomationProperties.SetName(openProject, "Open timeline project");
        ToolTip.SetTip(openProject, "Open a Frame Theory project and restore its editable timeline.");
        openProject.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(openProject);
            if (topLevel is null)
            {
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Frame Theory project") { Patterns = ["*.ftstudio"] }]
            });
            var path = files.SingleOrDefault()?.TryGetLocalPath();
            if (path is not null)
            {
                await viewModel.OpenTimelineProjectAsync(path, CancellationToken.None);
            }
        };
        var timelineReplacementPath = new TextBox { PlaceholderText = "Replacement media path within project folder" };
        AutomationProperties.SetName(timelineReplacementPath, "Timeline replacement media path");
        var missingTimelineMedia = new ListBox { Height = 80 };
        AutomationProperties.SetName(missingTimelineMedia, "Missing timeline media");
        missingTimelineMedia.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainWindowViewModel.MissingTimelineMedia)));
        missingTimelineMedia.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(MainWindowViewModel.SelectedMissingTimelineMedia))
        {
            Mode = BindingMode.TwoWay
        });
        var relinkTimelineMedia = new Button { Content = "Relink selected timeline media" };
        AutomationProperties.SetName(relinkTimelineMedia, "Relink selected timeline media");
        relinkTimelineMedia.Click += async (_, _) => await viewModel.RelinkSelectedTimelineMediaAsync(timelineReplacementPath.Text ?? string.Empty, CancellationToken.None);
        var projectStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(projectStatus, "Timeline project status");
        projectStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.TimelineProjectStatusText)));
        var aspectRatio = new ComboBox { ItemsSource = new[] { "16:9 landscape", "1:1 square", "9:16 portrait" }, SelectedIndex = 0, Width = 150 };
        AutomationProperties.SetName(aspectRatio, "Video export aspect ratio");
        ToolTip.SetTip(aspectRatio, "Choose the output framing used by the local video renderer.");
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
        AutomationProperties.SetName(exportStatus, "Timeline export status");
        exportStatus.Bind(TextBlock.TextProperty, new Binding(nameof(MainWindowViewModel.TimelineExportStatusText)));
        var retryExport = new Button { Content = "Retry last export" };
        AutomationProperties.SetName(retryExport, "Retry last timeline export");
        retryExport.Click += async (_, _) => await viewModel.RetryLastTimelineExportAsync(CancellationToken.None);
        var watermarkStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(watermarkStatus, "Watermark status");
        watermarkStatus.Bind(TextBlock.TextProperty, new Binding("TimelineEditor.WatermarkStatusText"));
        var useApprovedWatermark = new Button { Content = "Use Frame Theory watermark" };
        AutomationProperties.SetName(useApprovedWatermark, "Use approved Frame Theory watermark");
        useApprovedWatermark.Click += (_, _) => RunTimelineAction(timeline.SetApprovedWatermark);
        var selectWatermark = new Button { Content = "Select watermark" };
        AutomationProperties.SetName(selectWatermark, "Select custom watermark");
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
        AutomationProperties.SetName(clearWatermark, "Clear watermark");
        clearWatermark.Click += (_, _) => timeline.ClearWatermark();
        var scrollBack = new Button { Content = "Scroll ◀" };
        AutomationProperties.SetName(scrollBack, "Scroll timeline backward");
        scrollBack.Click += (_, _) => timeline.ScrollBy(-30);
        var scrollForward = new Button { Content = "Scroll ▶" };
        AutomationProperties.SetName(scrollForward, "Scroll timeline forward");
        scrollForward.Click += (_, _) => timeline.ScrollBy(30);
        var analysisSection = CreateInspectorExpander(
            "Analysis and rough cut",
            CreateSectionContent(
                CreateFlow(beatDetection, beatMarkers),
                CreateFlow(prepareRoughCut, applyRoughCut),
                roughCutStatus),
            expanded: false);
        var clipEditingSection = CreateInspectorExpander(
            "Clip editing",
            CreateSectionContent(
                CreateFlow(back, forward, select, selectAudio, scrollBack, scrollForward),
                CreateFlow(copy, paste, split, trim, move, rippleDelete),
                operationStatus),
            expanded: true);
        var motionSection = CreateInspectorExpander(
            "Motion and speed",
            CreateSectionContent(
                CreateFlow(fadeIn, pushIn, clearAnimation),
                CreateFlow(new TextBlock { Text = "Speed" }, speedRampStart, new TextBlock { Text = "to" }, speedRampEnd, applySpeedRamp, clearSpeedRamp),
                CreateFlow(new TextBlock { Text = "Stabilization radius" }, stabilizationRadius, applyStabilization, clearStabilization)),
            expanded: false);
        var audioSection = CreateInspectorExpander(
            "Audio",
            CreateSectionContent(CreateFlow(muteAudio, gainAudio, fadeAudio, duckAudio, clearDucking, clearAudio)),
            expanded: false);
        var titleCaptionSection = CreateInspectorExpander(
            "Titles and captions",
            CreateSectionContent(
                CreateFlow(titleText, addTitle, fadeTitle, pushTitle),
                titleAnimationStatus,
                CreateFlow(importCaptions, importTranscript, exportCaptions),
                kineticCaptions,
                CreateFlow(transcribeAudio, transcribeLocalWhisper, transcriptionConsent),
                CreateFlow(captionFont),
                captionStatus),
            expanded: false);
        var projectExportSection = CreateInspectorExpander(
            "Project and export",
            CreateSectionContent(
                CreateFlow(saveProject, openProject),
                missingTimelineMedia,
                timelineReplacementPath,
                relinkTimelineMedia,
                projectStatus,
                CreateFlow(aspectRatio, export, batchExport, cancelExport, retryExport),
                exportStatus,
                CreateFlow(useApprovedWatermark, selectWatermark, clearWatermark),
                watermarkStatus),
            expanded: false);
        var timelineViewSection = CreateInspectorExpander(
            "Timeline view",
            CreateSectionContent(CreateFlow(snap, new TextBlock { Text = "Zoom" }, zoom, zoomValue)),
            expanded: false);
        var previewActions = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = "Preview",
                    FontSize = 13,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Brushes.White
                },
                CreateFlow(previewCurrentFrame, playTimeline, stopTimeline),
                usePreviewProxy,
                analyzeMotion,
                timelinePreviewStatus,
                timelinePerformanceStatus,
                motionAnalysisStatus
            }
        };
        var previewActionsCard = new Border
        {
            Padding = new Thickness(12),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(24, 27, 33)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = previewActions
        };
        AutomationProperties.SetName(previewActionsCard, "Timeline monitor controls");
        var previewCard = new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(16, 18, 22)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
            BorderThickness = new Thickness(1),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("2*,*")
            }
        };
        var previewGrid = (Grid)previewCard.Child;
        var monitorFrame = new Border
        {
            MinHeight = 190,
            Margin = new Thickness(0, 0, 12, 0),
            Background = Brushes.Black,
            Child = new Grid
            {
                Children =
                {
                    timelinePreviewEmptyState,
                    timelinePreview,
                    new Border
                    {
                        Margin = new Thickness(8),
                        Padding = new Thickness(7, 4),
                        CornerRadius = new CornerRadius(4),
                        Background = new SolidColorBrush(Avalonia.Media.Color.FromArgb(210, 13, 15, 18)),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Child = monitorTimecode
                    }
                }
            }
        };
        AutomationProperties.SetName(monitorFrame, "Timeline program monitor");
        previewGrid.Children.Add(new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock
                {
                    Text = "PROGRAM MONITOR",
                    FontSize = 10,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(126, 143, 190))
                },
                monitorFrame
            }
        });
        previewGrid.Children.Add(previewActionsCard);
        Grid.SetColumn(previewActionsCard, 1);
        var timelineNavigation = new Border
        {
            Padding = new Thickness(12),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(24, 27, 33)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 46, 55)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = "TIMELINE NAVIGATION",
                        FontSize = 10,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(126, 143, 190))
                    },
                    CreateFlow(frameReadout, positionReadout),
                    rulerVisual,
                    playhead,
                    ruler
                }
            }
        };
        AutomationProperties.SetName(timelineNavigation, "Timeline navigation controls");
        return new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(16),
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(29, 32, 39)),
            BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 54, 65)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Frame-accurate timeline", FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White },
                    previewCard,
                    timelineNavigation,
                    selectionInspector,
                    tracks,
                    analysisSection,
                    clipEditingSection,
                    motionSection,
                    audioSection,
                    titleCaptionSection,
                    projectExportSection,
                    timelineViewSection
                }
            }
        };
    }

    private static WrapPanel CreateFlow(params Control[] children)
    {
        var panel = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var child in children)
        {
            child.Margin = new Thickness(child.Margin.Left, child.Margin.Top, child.Margin.Right + 6, child.Margin.Bottom + 6);
            panel.Children.Add(child);
        }

        return panel;
    }

    private static Control CreateSectionLabel(string text) => new Border
    {
        Margin = new Thickness(0, 12, 0, 0),
        Padding = new Thickness(0, 10, 0, 6),
        BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 54, 65)),
        BorderThickness = new Thickness(0, 1, 0, 0),
        Child = new TextBlock
        {
            Text = text.ToUpperInvariant(),
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(126, 143, 190))
        }
    };

    private static Expander CreateInspectorExpander(string title, Control content, bool expanded)
    {
        var expander = new Expander
        {
            Header = new Border
            {
                Padding = new Thickness(8, 7),
                MinHeight = 34,
                CornerRadius = new CornerRadius(4),
                Background = Brushes.Transparent,
                Child = new TextBlock
                {
                    Text = title.ToUpperInvariant(),
                    FontSize = 10,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(126, 143, 190)),
                    VerticalAlignment = VerticalAlignment.Center
                }
            },
            Content = content,
            IsExpanded = expanded,
            Margin = new Thickness(0, 8, 0, 0),
            Padding = new Thickness(0, 0, 0, 4),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Top,
            Focusable = true,
            IsTabStop = true
        };
        AutomationProperties.SetName(expander, $"{title} section");
        ToolTip.SetTip(expander, $"Show or hide {title.ToLowerInvariant()} controls.");
        expander.AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(() => NameUnnamedButtons(expander));
        expander.PropertyChanged += (_, args) =>
        {
            if (args.Property == Expander.IsExpandedProperty && expander.IsExpanded)
            {
                Dispatcher.UIThread.Post(() => NameUnnamedButtons(expander));
            }
        };
        return expander;
    }

    private static StackPanel CreateSectionContent(params Control[] children)
    {
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.AddRange(children);
        return panel;
    }

    private static Control? CreateApprovedLogo()
    {
        if (!BrandAssetCatalog.TryGetApprovedLogoPath(out var path))
        {
            return null;
        }

        try
        {
            return new Image
            {
                Source = new Bitmap(path),
                Width = 34,
                Height = 34,
                Stretch = Stretch.Uniform
            };
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static Control CreateGradeControl(
        string label,
        double minimum,
        double maximum,
        float value,
        Action<float> setter,
        ColorGradeInspectorViewModel source,
        string propertyName,
        Func<float> valueReader)
    {
        var slider = new Slider
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = value,
            Width = 180,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(slider, label);
        ToolTip.SetTip(slider, $"Adjust {label} from {minimum:0.00} to {maximum:0.00}.");
        var valueLabel = new TextBlock
        {
            Width = 48,
            Text = value.ToString("0.00", CultureInfo.InvariantCulture),
            TextAlignment = TextAlignment.Right,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(172, 181, 196)),
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(valueLabel, $"{label} value");
        ToolTip.SetTip(valueLabel, $"Current {label} value: {value:0.00}.");
        slider.ValueChanged += (_, args) => setter((float)args.NewValue);
        slider.ValueChanged += (_, args) => valueLabel.Text = args.NewValue.ToString("0.00", CultureInfo.InvariantCulture);
        source.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != propertyName)
            {
                return;
            }

            var nextValue = valueReader();
            if (Math.Abs(slider.Value - nextValue) > 0.0001)
            {
                slider.Value = nextValue;
            }

            valueLabel.Text = nextValue.ToString("0.00", CultureInfo.InvariantCulture);
        };
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { new TextBlock { Text = label, Width = 110 }, slider, valueLabel }
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

    private static Canvas CreateWaveformCanvas(IReadOnlyList<float>? peaks, double width, double height)
    {
        var canvas = new Canvas
        {
            Width = Math.Max(1, width),
            Height = Math.Max(1, height),
            IsHitTestVisible = false
        };
        if (peaks is null || peaks.Count == 0)
        {
            return canvas;
        }

        var sampleCount = Math.Min(peaks.Count, (int)Math.Min(2048, Math.Max(8, Math.Round(width))));
        for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            var peakIndex = Math.Min(peaks.Count - 1, (int)Math.Floor((double)sampleIndex / sampleCount * peaks.Count));
            var amplitude = Math.Clamp(Math.Abs(peaks[peakIndex]), 0, 1);
            var bar = new Border
            {
                Width = 1,
                Height = Math.Max(1, amplitude * Math.Max(1, height - 2)),
                Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(235, 161, 82))
            };
            Canvas.SetLeft(bar, sampleIndex * Math.Max(1, width - 1) / Math.Max(1, sampleCount - 1));
            Canvas.SetTop(bar, (height - bar.Height) / 2);
            canvas.Children.Add(bar);
        }

        return canvas;
    }

    private static Bitmap? CreateScopeBitmap(ColorScopeData? scopes, bool vectorscope)
    {
        if (scopes is null)
        {
            return null;
        }

        const int width = 256;
        const int height = 128;
        var bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Rgba8888,
            AlphaFormat.Opaque);
        var rgba = new byte[width * height * 4];
        for (var pixel = 0; pixel < width * height; pixel++)
        {
            var offset = pixel * 4;
            rgba[offset] = 16;
            rgba[offset + 1] = 18;
            rgba[offset + 2] = 22;
            rgba[offset + 3] = 255;
        }

        for (var grid = 1; grid < 4; grid++)
        {
            var y = grid * height / 4;
            for (var x = 0; x < width; x++)
            {
                SetScopePixel(rgba, width, x, y, 42, 46, 55);
            }
        }

        var bins = vectorscope ? scopes.Vectorscope : scopes.Waveform;
        var maximum = bins.Count == 0 ? 0 : bins.Max();
        if (maximum == 0)
        {
            using var emptyFramebuffer = bitmap.Lock();
            for (var row = 0; row < height; row++)
            {
                Marshal.Copy(rgba, row * width * 4, emptyFramebuffer.Address + row * emptyFramebuffer.RowBytes, width * 4);
            }

            return bitmap;
        }

        for (var y = 0; y < height; y++)
        {
            var sourceY = (height - 1 - y) * 64 / height;
            for (var x = 0; x < width; x++)
            {
                var sourceX = x * 64 / width;
                var value = bins[sourceY * 64 + sourceX];
                if (value == 0)
                {
                    continue;
                }

                var intensity = (byte)(48 + 207 * value / maximum);
                SetScopePixel(
                    rgba,
                    width,
                    x,
                    y,
                    vectorscope ? (byte)205 : intensity,
                    vectorscope ? (byte)184 : intensity,
                    vectorscope ? (byte)255 : intensity);
            }
        }

        using var framebuffer = bitmap.Lock();
        for (var row = 0; row < height; row++)
        {
            Marshal.Copy(rgba, row * width * 4, framebuffer.Address + row * framebuffer.RowBytes, width * 4);
        }

        return bitmap;
    }

    private static void SetScopePixel(byte[] rgba, int width, int x, int y, byte red, byte green, byte blue)
    {
        var offset = (y * width + x) * 4;
        rgba[offset] = red;
        rgba[offset + 1] = green;
        rgba[offset + 2] = blue;
        rgba[offset + 3] = 255;
    }
}
