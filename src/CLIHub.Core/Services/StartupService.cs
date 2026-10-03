namespace CLIHub.Core.Services;

using CLIHub.Core.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
///   Manages the per-user Windows Run registration for the application, so it can start
///   with Windows. Uses the current user's registry hive (no elevation).
/// </summary>
public sealed class StartupService : IStartupService
{
    /// <summary>
    ///   Value name used under the Run key.
    /// </summary>
    public const string ValueName = "CLIHub";

    private readonly IStartupRegistry _registry;
    private readonly ILogger<StartupService> _logger;
    private readonly Func<string?> _executablePathProvider;

    internal StartupService(
        IStartupRegistry registry,
        ILogger<StartupService> logger,
        Func<string?>? executablePathProvider = null)
    {
        _registry = registry;
        _logger = logger;
        _executablePathProvider = executablePathProvider ?? DefaultExecutablePath;
    }

    /// <inheritdoc />
    public bool IsEnabled()
    {
        try
        {
            return !string.IsNullOrWhiteSpace(_registry.GetValue(ValueName));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read the startup registration");
            return false;
        }
    }

    /// <inheritdoc />
    public bool SetEnabled(bool enabled)
    {
        try
        {
            if (enabled)
            {
                var path = _executablePathProvider();
                if (string.IsNullOrWhiteSpace(path))
                {
                    _logger.LogWarning("Cannot enable startup: the executable path is unknown");
                    return false;
                }

                _registry.SetValue(ValueName, Quote(path));
                _logger.LogInformation("Startup registration set to {Path}", path);
            }
            else
            {
                _registry.DeleteValue(ValueName);
                _logger.LogInformation("Startup registration removed");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not update the startup registration");
            return false;
        }
    }

    /// <summary>
    ///   Wraps a path in quotes for the Run value.
    /// </summary>
    internal static string Quote(string path) => $"\"{path.Trim('"')}\"";

    private static string? DefaultExecutablePath() =>
        Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
}
