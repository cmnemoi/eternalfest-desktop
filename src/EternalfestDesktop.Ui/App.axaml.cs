using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EternalfestDesktop.Infrastructure.FileSystem;
using EternalfestDesktop.Ui.Views;

namespace EternalfestDesktop.Ui;

public sealed partial class App : Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var composition = new Composition(
                AppFolders.ForThisUser,
                new SocketsHttpHandler(),
                () => (desktop.MainWindow as MainWindow)?.ScreenAvailableToTheGame());
            composition.ApplyLanguage();
            composition.AddToApplicationsMenu();
            var main = composition.MainWindow();
            desktop.MainWindow = new MainWindow { DataContext = main };
            desktop.Exit += (_, _) => composition.Dispose();
            _ = main.Library.Load(CancellationToken.None);
        }
        base.OnFrameworkInitializationCompleted();
    }
}
