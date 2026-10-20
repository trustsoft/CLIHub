namespace CLIHub;

using System;
using System.Windows;
using System.Windows.Media;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Windows;
using CLIHub.Core.Logging;
using CLIHub.Core.Models;
using CLIHub.Startup;
using CLIHub.Themes;

/// <summary>
///   Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IApplicationHost? host;

    /// <summary>
    ///   Prepares the environment and delegates application startup to the host.
    /// </summary>
    /// <param name="e"> Startup event arguments. </param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ApplyTheme();
        ApplyAccent();

        this.host = new ApplicationHost();
        this.host.Start(
            action => this.Dispatcher.Invoke(action),
            window => this.MainWindow = (Window)window);
    }

    /// <summary>
    ///   Merges the palette registered for the stored theme into the application resources. Dark is
    ///   the base palette merged by App.xaml, so a null resolution (dark, system-on-dark, unknown)
    ///   keeps that base with no extra merge. Resolved before any window is created, so the choice
    ///   applies on this start.
    /// </summary>
    private static void ApplyTheme()
    {
        var theme = AppThemes.Parse(PreferenceReader.ReadAppTheme(AppPaths.ConfigFile));
        var palettePath = ThemeCatalog.ResolvePalettePath(theme, SystemTheme.IsLight());
        if (palettePath is null || Current is not { } application)
        {
            return;
        }

        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/CLIHub;component{palettePath}", UriKind.Absolute)
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
        this.host?.Shutdown();

        base.OnExit(e);
    }
}
