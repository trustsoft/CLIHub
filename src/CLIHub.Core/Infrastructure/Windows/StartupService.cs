namespace CLIHub.Core.Infrastructure.Windows;

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

    private readonly IStartupRegistry registry;
    private readonly ILogger<StartupService> logger;
    private readonly Func<string?> executablePathProvider;

    internal StartupService(
        IStartupRegistry registry,
        ILogger<StartupService> logger,
        Func<string?>? executablePathProvider = null)
    {
        this.registry = registry;
        this.logger = logger;
        this.executablePathProvider = executablePathProvider ?? DefaultExecutablePath;
    }

    /// <inheritdoc />
    public bool IsEnabled()
    {
        try
        {
            return !string.IsNullOrWhiteSpace(this.registry.GetValue(ValueName));
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not read the startup registration");
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
                var path = this.executablePathProvider();
                if (string.IsNullOrWhiteSpace(path))
                {
                    this.logger.LogWarning("Cannot enable startup: the executable path is unknown");
                    return false;
                }

                this.registry.SetValue(ValueName, Quote(path));
                this.logger.LogInformation("Startup registration set to {Path}", path);
            }
            else
            {
                this.registry.DeleteValue(ValueName);
                this.logger.LogInformation("Startup registration removed");
            }

            return true;
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not update the startup registration");
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
