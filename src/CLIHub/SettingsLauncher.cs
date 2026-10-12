namespace CLIHub;

using CLIHub.Views;

/// <summary>
///   Owns the application's single <see cref="SettingsWindow"/> instance so that every entry
///   point (tray menu, launch window) activates the same window instead of creating another.
/// </summary>
public sealed class SettingsLauncher : ISettingsLauncher
{
    private readonly Func<SettingsWindow> factory;
    private SettingsWindow? window;

    /// <summary>
    ///   Creates the launcher with the DI-provided window factory.
    /// </summary>
    /// <param name="factory"> Factory that creates the <see cref="SettingsWindow"/> on demand. </param>
    public SettingsLauncher(Func<SettingsWindow> factory)
    {
        this.factory = factory;
    }

    /// <summary>
    ///   Shows the settings window, creating it on first use.
    /// </summary>
    public void ShowSettings()
    {
        if (this.window is null)
        {
            this.window = this.factory();
            this.window.Closed += (_, _) => this.window = null;
        }

        this.window.ShowSettings();
    }
}
