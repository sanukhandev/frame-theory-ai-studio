using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace FrameTheory.Desktop;

public sealed class App : Application
{
    public App()
    {
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new DesktopServices();
            desktop.MainWindow = new MainWindow(services.CreateMainWindowViewModel());
        }

        base.OnFrameworkInitializationCompleted();
    }
}
