using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using FrameTheory.Application;

namespace FrameTheory.Desktop;

public sealed class App : Avalonia.Application
{
    public App()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        AddStudioStyles();
    }

    private void AddStudioStyles()
    {
        var canvas = new SolidColorBrush(Avalonia.Media.Color.FromRgb(13, 15, 18));
        var surface = new SolidColorBrush(Avalonia.Media.Color.FromRgb(24, 27, 33));
        var input = new SolidColorBrush(Avalonia.Media.Color.FromRgb(31, 35, 42));
        var accent = new SolidColorBrush(Avalonia.Media.Color.FromRgb(95, 125, 255));
        var foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(239, 242, 248));

        Styles.Add(new Style(selector => selector.OfType<Window>())
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, canvas),
                new Setter(TemplatedControl.ForegroundProperty, foreground)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>())
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, input),
                new Setter(TemplatedControl.ForegroundProperty, foreground),
                new Setter(TemplatedControl.BorderBrushProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(53, 59, 70))),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)),
                new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(4)),
                new Setter(TemplatedControl.PaddingProperty, new Thickness(12, 7)),
                new Setter(Layoutable.MinHeightProperty, 32d)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class(":pointerover"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 48, 59))),
                new Setter(TemplatedControl.BorderBrushProperty, accent)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class(":pressed"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(59, 69, 96))),
                new Setter(TemplatedControl.BorderBrushProperty, accent)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class(":focus"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BorderBrushProperty, accent),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("primary"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, accent),
                new Setter(TemplatedControl.ForegroundProperty, Brushes.White),
                new Setter(TemplatedControl.BorderBrushProperty, accent)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("primary").Class(":pointerover"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(118, 143, 255))),
                new Setter(TemplatedControl.BorderBrushProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(150, 168, 255)))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class(":disabled"))
        {
            Setters =
            {
                new Setter(TemplatedControl.ForegroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(119, 125, 138))),
                new Setter(TemplatedControl.BorderBrushProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(43, 47, 55)))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<TextBox>())
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, input),
                new Setter(TemplatedControl.ForegroundProperty, foreground),
                new Setter(TemplatedControl.BorderBrushProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(53, 59, 70))),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)),
                new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(4)),
                new Setter(TemplatedControl.PaddingProperty, new Thickness(10, 6)),
                new Setter(Layoutable.MinHeightProperty, 32d)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<TextBox>().Class(":focus"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BorderBrushProperty, accent)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<TextBox>().Class(":pointerover"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(36, 41, 49))),
                new Setter(TemplatedControl.BorderBrushProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(79, 91, 113)))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<ComboBox>())
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, input),
                new Setter(TemplatedControl.ForegroundProperty, foreground),
                new Setter(TemplatedControl.BorderBrushProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(53, 59, 70))),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)),
                new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(4)),
                new Setter(Layoutable.MinHeightProperty, 32d)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<ComboBox>().Class(":pointerover"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 48, 59))),
                new Setter(TemplatedControl.BorderBrushProperty, accent)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<ComboBox>().Class(":focus"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BorderBrushProperty, accent)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<ComboBoxItem>())
        {
            Setters =
            {
                new Setter(TemplatedControl.PaddingProperty, new Thickness(10, 7)),
                new Setter(TemplatedControl.ForegroundProperty, foreground)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<ComboBoxItem>().Class(":pointerover"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(42, 48, 59)))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<ComboBoxItem>().Class(":selected"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(35, 43, 66))),
                new Setter(TemplatedControl.ForegroundProperty, foreground)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Expander>())
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(20, 23, 29))),
                new Setter(TemplatedControl.ForegroundProperty, foreground),
                new Setter(TemplatedControl.BorderBrushProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(48, 54, 65))),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0, 1, 0, 0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Expander>().Class(":pointerover"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(29, 33, 40)))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Expander>().Class(":focus"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BorderBrushProperty, accent),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0, 1, 0, 0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<TabItem>())
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, canvas),
                new Setter(TemplatedControl.ForegroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(159, 166, 181))),
                new Setter(TemplatedControl.PaddingProperty, new Thickness(16, 12))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<TabItem>().Class(":pointerover"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(29, 33, 40))),
                new Setter(TemplatedControl.ForegroundProperty, foreground)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<TabItem>().Class(":selected"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(35, 43, 66))),
                new Setter(TemplatedControl.ForegroundProperty, foreground),
                new Setter(TemplatedControl.BorderBrushProperty, accent),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0, 0, 3, 0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<TabItem>().Class(":focus"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BorderBrushProperty, accent),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0, 0, 3, 0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<ListBox>())
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, surface),
                new Setter(TemplatedControl.ForegroundProperty, foreground),
                new Setter(TemplatedControl.BorderBrushProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(45, 50, 60))),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)),
                new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(6))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<ListBoxItem>())
        {
            Setters =
            {
                new Setter(TemplatedControl.PaddingProperty, new Thickness(4)),
                new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<ListBoxItem>().Class(":pointerover"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(29, 33, 40)))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<ListBoxItem>().Class(":selected"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(35, 43, 66))),
                new Setter(TemplatedControl.ForegroundProperty, foreground)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<ListBoxItem>().Class(":focus"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BorderBrushProperty, accent),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Slider>())
        {
            Setters =
            {
                new Setter(TemplatedControl.ForegroundProperty, accent)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<CheckBox>())
        {
            Setters =
            {
                new Setter(TemplatedControl.ForegroundProperty, foreground),
                new Setter(TemplatedControl.PaddingProperty, new Thickness(4, 3)),
                new Setter(Layoutable.MinHeightProperty, 28d)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<CheckBox>().Class(":pointerover"))
        {
            Setters =
            {
                new Setter(TemplatedControl.ForegroundProperty, Brushes.White)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<CheckBox>().Class(":focus"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BorderBrushProperty, accent),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1))
            }
        });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var diagnostics = new FileDiagnosticsLog();
            AppDomain.CurrentDomain.UnhandledException += (_, args) => diagnostics.Record("unhandled-exception", args.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                diagnostics.Record("unobserved-task-exception", args.Exception);
                args.SetObserved();
            };
            var services = new DesktopServices(diagnostics);
            var viewModel = services.CreateMainWindowViewModel();
            desktop.MainWindow = new MainWindow(viewModel);
            _ = viewModel.RefreshCapabilitiesAsync(CancellationToken.None);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
