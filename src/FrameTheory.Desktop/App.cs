using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using FrameTheory.Application;

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
