using CLIHub.Core.Interfaces;
using CLIHub.Core.Services;
using System.IO;

namespace CLIHub;

/// <summary>
/// Simple composition root. Manual wiring for the MVP (DI container is deferred).
/// </summary>
public static class AppServices
{
    public static IConfigService Config { get; } = new ConfigService();

    public static IProjectService Projects { get; } = CreateProjectService();

    public static IPluginManager Plugins { get; } = new PluginManager();

    public static IProcessLauncher Launcher { get; } = new ProcessLauncher();

    private static IProjectService CreateProjectService()
    {
        var service = new ProjectService(Config)
        {
            DefaultLogoPath = Path.Combine(AppContext.BaseDirectory, "default-project.png")
        };
        return service;
    }
}
