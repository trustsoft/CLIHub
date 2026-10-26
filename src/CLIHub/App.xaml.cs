namespace CLIHub;

using System.Windows;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Windows;
using CLIHub.Core.Logging;
using CLIHub.Core.Models;
using CLIHub.Settings;
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

#if DEBUG
        if (CLIHub.Views.ReadmeImageCapture.TryStart(this))
        {
            return;
        }
#endif
    }

    /// <summary>
    ///   Applies the palette registered for the stored theme through <see cref="PaletteSwitcher" />.
    ///   Dark is the base palette merged by App.xaml, so a null resolution (dark, system-on-dark,
    ///   unknown) keeps that base with no extra merge. Runs before any window is created, so the
    ///   stored theme applies on this start; the same switcher swaps the palette later without a
    ///   restart.
    /// </summary>
    private static void ApplyTheme()
    {
        var theme = AppThemes.Parse(PreferenceReader.ReadAppTheme(AppPaths.ConfigFile));
        if (Current is { } application)
        {
            PaletteSwitcher.Shared.Apply(application.Resources.MergedDictionaries, theme, SystemTheme.IsLight());
        }
    }

    /// <summary>
    ///   Applies the stored accent color by writing the application-level accent brushes before any
    ///   window is created. The stored accent is always applied, the default included: the accent is
    ///   application-scoped and does not depend on the active palette.
    /// </summary>
    private static void ApplyAccent()
    {
        var accent = AccentPalette.Normalize(PreferenceReader.ReadAccentColor(AppPaths.ConfigFile));
        if (Current is { } application)
        {
            AccentApplier.Apply(application.Resources, accent);
        }
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
