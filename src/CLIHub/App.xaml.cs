namespace CLIHub;

using System;
using System.Windows;
using System.Windows.Media;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Logging;
using CLIHub.Core.Models;

/// <summary>
///   Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IApplicationHost? _host;

    /// <summary>
    ///   Prepares the environment and delegates application startup to the host.
    /// </summary>
    /// <param name="e"> Startup event arguments. </param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ApplyTheme();
        ApplyAccent();

        _host = new ApplicationHost();
        _host.Start(
            action => Dispatcher.Invoke(action),
            window => MainWindow = (Window)window);
    }

    /// <summary>
    ///   Applies the light palette on top of the base palette when the effective theme is light.
    ///   Resolved before any window is created, so the choice applies on this start.
    /// </summary>
    private static void ApplyTheme()
    {
        var theme = AppThemes.Parse(PreferenceReader.ReadAppTheme(AppPaths.ConfigFile));
        var useLight = theme == AppTheme.Light || (theme == AppTheme.System && SystemTheme.IsLight());
        if (!useLight || Current is not { } application)
        {
            return;
        }

        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/CLIHub;component/Themes/LightTheme.xaml", UriKind.Absolute)
        });
    }

    /// <summary>
    ///   Applies the stored accent color by overriding the application-level accent brushes when it
    ///   differs from the default, before any window is created.
    /// </summary>
    private static void ApplyAccent()
    {
        var accent = AccentPalette.Normalize(PreferenceReader.ReadAccentColor(AppPaths.ConfigFile));
        if (accent == AccentPalette.DefaultHex || Current is not { } application)
        {
            return;
        }

        application.Resources["AccentBrush"] = CreateBrush(AccentPalette.Opaque(accent));
        application.Resources["AccentSelectionBrush"] = CreateBrush(AccentPalette.SelectionTint(accent));
    }

    private static SolidColorBrush CreateBrush(string argbHex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(argbHex));
        brush.Freeze();
        return brush;
    }

    /// <summary>
    ///   Disposes the hotkey, tray icon, and DI container, then flushes logs.
    /// </summary>
    /// <param name="e"> Exit event arguments. </param>
    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Shutdown();

        base.OnExit(e);
    }
}
