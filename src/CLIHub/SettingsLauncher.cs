namespace CLIHub;

using CLIHub.Windows;

/// <summary>
/// Owns the application's single <see cref="SettingsWindow"/> instance so that every entry
/// point (tray menu, launch window) activates the same window instead of creating another.
/// </summary>
public sealed class SettingsLauncher : ISettingsLauncher
{
    private readonly Func<SettingsWindow> _factory;
    private SettingsWindow? _window;

    public SettingsLauncher(Func<SettingsWindow> factory)
    {
        _factory = factory;
    }

    public void ShowSettings()
    {
        if (_window is null)
        {
            _window = _factory();
            _window.Closed += (_, _) => _window = null;
        }

        _window.ShowSettings();
    }
}
