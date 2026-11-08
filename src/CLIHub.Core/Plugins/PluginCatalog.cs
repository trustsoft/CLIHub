namespace CLIHub.Core.Plugins;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Infrastructure.Logging;
using CLIHub.Core.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
///   Discovers and stores AI agent CLI tool plugins.
/// </summary>
public sealed class PluginCatalog : IPluginCatalog
{
    private readonly ILogger<PluginCatalog> logger;
    private readonly PluginLogoResolver logoResolver;
    private readonly IPluginDescriptorReader descriptorReader;
    private readonly IPluginDescriptorValidator descriptorValidator;
    private readonly IPluginDescriptorSource source;
    private readonly ILoggingContextProvider loggingContext;
    private readonly IPluginStateRepository? stateRepository;
    private readonly string pluginsPath;
    private IReadOnlyList<Plugin> plugins = Array.Empty<Plugin>();
    private IReadOnlyList<InvalidPluginEntry> invalidPlugins = Array.Empty<InvalidPluginEntry>();
    private IReadOnlyList<DisabledPluginEntry> disabledPlugins = Array.Empty<DisabledPluginEntry>();

    /// <inheritdoc />
    public event EventHandler? PluginsChanged;

    /// <summary>
    ///   Creates the plugin catalog for the given plugins root.
    /// </summary>
    /// <param name="logger"> The logger. </param>
    /// <param name="logoResolver"> The resolver producing final plugin logo paths. </param>
    /// <param name="pluginsPath"> Overrides the plugins root; defaults to <c> %APPDATA%\CLIHub\plugins </c>. </param>
    /// <param name="descriptorReader"> The reader for plugin.json descriptors. </param>
    /// <param name="descriptorValidator"> The validator for plugin descriptors. </param>
    /// <param name="source"> The source of plugin descriptors. </param>
    /// <param name="loggingContext"> The provider for operation logging scopes. </param>
    /// <param name="stateRepository"> The plugin lifecycle state repository used to skip disabled plugins. </param>
    public PluginCatalog(ILogger<PluginCatalog> logger,
                         PluginLogoResolver logoResolver,
                         string? pluginsPath = null,
                         IPluginDescriptorReader? descriptorReader = null,
                         IPluginDescriptorValidator? descriptorValidator = null,
                         IPluginDescriptorSource? source = null,
                         ILoggingContextProvider? loggingContext = null,
                         IPluginStateRepository? stateRepository = null)
    {
        this.logger = logger;
        this.logoResolver = logoResolver ?? throw new ArgumentNullException(nameof(logoResolver));
        this.pluginsPath = pluginsPath ?? AppPaths.PluginsDirectory;
        this.descriptorReader = descriptorReader ?? new PluginDescriptorReader();
        this.descriptorValidator = descriptorValidator ?? new PluginDescriptorValidator();
        this.source = source ?? new LocalFolderPluginSource(this.pluginsPath, this.descriptorReader);
        this.loggingContext = loggingContext ?? new LoggingContextProvider(NullLogger<LoggingContextProvider>.Instance);
        this.stateRepository = stateRepository;
    }

    /// <inheritdoc />
    public void LoadPlugins()
    {
        this.plugins = this.LoadPluginsCore();
    }

    /// <inheritdoc />
    public void ReloadPlugins()
    {
        using var scope = this.loggingContext.BeginOperation("PluginCatalogReload");
        this.plugins = this.LoadPluginsCore();
        this.PluginsChanged?.Invoke(this, EventArgs.Empty);
    }

    private IReadOnlyList<Plugin> LoadPluginsCore()
    {
        var plugins = new List<Plugin>();
        var invalidPlugins = new List<InvalidPluginEntry>();
        var disabledPlugins = new List<DisabledPluginEntry>();
        PluginStateDocument? state = null;
        using var scope = this.loggingContext.BeginOperation("PluginCatalogLoad");

        foreach (var entry in this.source.EnumerateDescriptors())
        {
            try
            {
                if (entry.ReadError is not null)
                {
                    var readResult = new PluginDescriptorReadResult(null, entry.ReadError);
                    invalidPlugins.Add(new InvalidPluginEntry(entry.PluginDirectory, null, readResult, null));
                    this.logger.LogWarning(entry.ReadError, "Failed to load plugin.json from {Directory}", entry.PluginDirectory);
                    continue;
                }

                var plugin = entry.Plugin;

                if (plugin is not null)
                {
                    // Disabled plugins are not loaded, not validated, and not launchable; they
                    // surface separately so the UI can offer enabling them again.
                    state ??= this.stateRepository?.Load();
                    if (state is not null && state.Plugins.TryGetValue(plugin.Id, out var stateEntry) && stateEntry.Disabled)
                    {
                        disabledPlugins.Add(new DisabledPluginEntry(
                            plugin.Id,
                            entry.PluginDirectory,
                            stateEntry.Origin,
                            plugin.Version ?? stateEntry.InstalledVersion));
                        this.logger.LogInformation("Skipped disabled plugin {PluginId} in {Directory}", plugin.Id, entry.PluginDirectory);
                        continue;
                    }

                    var validationResult = this.descriptorValidator.Validate(plugin);
                    if (!validationResult.IsValid)
                    {
                        invalidPlugins.Add(new InvalidPluginEntry(entry.PluginDirectory, plugin, null, validationResult));
                        this.logger.LogWarning(
                            "Invalid plugin in {Directory}: {Errors}",
                            entry.PluginDirectory,
                            string.Join("; ", validationResult.Errors));
                        continue;
                    }

                    var loadedDirectory = FindLoadedPluginDirectory(plugins, plugin.Id, entry.PluginDirectory);
                    if (loadedDirectory is not null)
                    {
                        this.logger.LogWarning(
                            "Duplicate plugin ID {PluginId} in {Directory}; skipped; loaded from {LoadedDirectory}",
                            plugin.Id,
                            entry.PluginDirectory,
                            loadedDirectory);
                        continue;
                    }

                    plugin.PluginDirectory = entry.PluginDirectory;
                    plugin.LogoPath = this.logoResolver.Resolve(plugin.Id, entry.PluginDirectory);

                    plugins.Add(plugin);
                    this.logger.LogInformation("Loaded plugin {PluginId} ({PluginName})", plugin.Id, plugin.Name);
                }
            }
            catch (Exception ex)
            {
                invalidPlugins.Add(new InvalidPluginEntry(entry.PluginDirectory, entry.Plugin, new PluginDescriptorReadResult(entry.Plugin, ex), null));
                this.logger.LogWarning(ex, "Failed to load plugin.json from {Directory}", entry.PluginDirectory);
            }
        }

        this.logger.LogInformation("Loaded {Count} plugin(s)", plugins.Count);
        this.invalidPlugins = invalidPlugins;
        this.disabledPlugins = disabledPlugins;
        return plugins;
    }

    /// <inheritdoc />
    public IReadOnlyList<Plugin> GetAllPlugins() => this.plugins.Select(plugin => plugin.Copy()).ToArray();

    /// <inheritdoc />
    public IReadOnlyList<InvalidPluginEntry> GetInvalidPlugins() => this.invalidPlugins;

    /// <inheritdoc />
    public IReadOnlyList<DisabledPluginEntry> GetDisabledPlugins() => this.disabledPlugins;

    private static string? FindLoadedPluginDirectory(IReadOnlyList<Plugin> plugins,
                                                     string pluginId,
                                                     string currentPluginDirectory)
    {
        return plugins
            .Where(plugin => plugin.Id == pluginId && plugin.PluginDirectory != currentPluginDirectory)
            .Select(plugin => plugin.PluginDirectory)
            .FirstOrDefault();
    }
}
