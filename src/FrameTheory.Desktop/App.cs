using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace FrameTheory.Desktop;

public sealed class App : Avalonia.Application
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
            var viewModel = services.CreateMainWindowViewModel();
            desktop.MainWindow = new MainWindow(viewModel);
            _ = viewModel.RefreshCapabilitiesAsync(CancellationToken.None);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
