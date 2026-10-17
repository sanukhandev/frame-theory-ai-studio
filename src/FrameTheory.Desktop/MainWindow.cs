using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

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
}
