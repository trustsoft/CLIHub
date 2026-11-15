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
    private readonly IPluginRegistry? stateRepository;
    private readonly string pluginsPath;
    private IReadOnlyList<Plugin> plugins = Array.Empty<Plugin>();
    private IReadOnlyList<InvalidPluginEntry> invalidPlugins = Array.Empty<InvalidPluginEntry>();
    private IReadOnlyList<DisabledPluginEntry> disabledPlugins = Array.Empty<DisabledPluginEntry>();
    private IReadOnlyList<UserModifiedPluginEntry> userModifiedPlugins = Array.Empty<UserModifiedPluginEntry>();

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
    /// <param name="stateRepository">
    ///   The plugin registry repository used to skip disabled plugins, verify descriptor
    ///   content, and adopt folders the registry does not know about.
    /// </param>
    public PluginCatalog(ILogger<PluginCatalog> logger,
                         PluginLogoResolver logoResolver,
                         string? pluginsPath = null,
                         IPluginDescriptorReader? descriptorReader = null,
                         IPluginDescriptorValidator? descriptorValidator = null,
                         IPluginDescriptorSource? source = null,
                         ILoggingContextProvider? loggingContext = null,
                         IPluginRegistry? stateRepository = null)
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
        var userModifiedPlugins = new List<UserModifiedPluginEntry>();

        // The registry document is loaded once per scan when a repository is present: the skip
        // check, content verification, and folder adoption all read the same snapshot, and the
        // adoption writes it back at most once after the enumeration.
        var state = this.stateRepository?.Load();
        var stateDirty = false;
        var seenPluginIds = new HashSet<string>(StringComparer.Ordinal);
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
                    seenPluginIds.Add(plugin.Id);

                    PluginRegistryEntry? stateEntry = null;
                    state?.Plugins.TryGetValue(plugin.Id, out stateEntry);
                    var diskHash = PluginHasher.ComputeFileHash(Path.Combine(entry.PluginDirectory, "plugin.json"));

                    // Disabled plugins (enabled: false) are not loaded, not validated, and not
                    // launchable; they surface separately so the UI can offer enabling them
                    // again. Their descriptor content is still verified: a modification is
                    // logged whichever side of the skip the plugin falls on.
                    if (stateEntry is not null && !stateEntry.Enabled)
                    {
                        this.VerifyContentHash(plugin.Id, entry.PluginDirectory, stateEntry, diskHash, userModifiedPlugins);
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

                    if (state is not null)
                    {
                        if (stateEntry is null)
                        {
                            // The registry does not know this folder (hand-created, or its
                            // earlier entry was lost): adopt it as unknown-origin content with
                            // the hash of what is on disk right now.
                            state.Plugins[plugin.Id] = new PluginRegistryEntry(PluginOrigin.Unknown)
                            {
                                ContentHash = diskHash,
                                InstalledVersion = plugin.Version,
                                Enabled = true,
                                InstalledAt = null
                            };
                            stateDirty = true;
                            this.logger.LogInformation("Adopted plugin folder {PluginId} into the registry with unknown origin", plugin.Id);
                        }
                        else
                        {
                            this.VerifyContentHash(plugin.Id, entry.PluginDirectory, stateEntry, diskHash, userModifiedPlugins);
                        }
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

        if (state is not null)
        {
            // Entries whose folder did not show up are kept, not pruned: the seeder's re-seed
            // and disabled-preservation semantics depend on entries surviving missing folders.
            foreach (var pluginId in state.Plugins.Keys)
            {
                if (!seenPluginIds.Contains(pluginId))
                {
                    this.logger.LogInformation("Registry entry {PluginId} has no plugin folder on disk; the entry is kept", pluginId);
                }
            }

            if (stateDirty && !this.stateRepository!.TrySave(state))
            {
                this.logger.LogWarning("Could not write the plugin registry after adopting folders; adopted entries are in memory only");
            }
        }

        this.logger.LogInformation("Loaded {Count} plugin(s)", plugins.Count);
        this.invalidPlugins = invalidPlugins;
        this.disabledPlugins = disabledPlugins;
        this.userModifiedPlugins = userModifiedPlugins;
        return plugins;
    }

    /// <summary>
    ///   Compares the stored content hash with the descriptor hash on disk and records a
    ///   user-modified entry on mismatch. A null stored hash means nothing CLIHub wrote is
    ///   recorded for this plugin: it is "cannot verify", not "modified". Verification never
    ///   rewrites the stored hash, so the modification stays detectable on later scans.
    /// </summary>
    /// <param name="pluginId"> The plugin id. </param>
    /// <param name="pluginDirectory"> The plugin's directory on disk. </param>
    /// <param name="stateEntry"> The registry entry holding the stored hash. </param>
    /// <param name="diskHash"> The hash of the descriptor currently on disk. </param>
    /// <param name="userModifiedPlugins"> The list collecting the scan's user-modified entries. </param>
    private void VerifyContentHash(
        string pluginId,
        string pluginDirectory,
        PluginRegistryEntry stateEntry,
        string? diskHash,
        List<UserModifiedPluginEntry> userModifiedPlugins)
    {
        if (stateEntry.ContentHash is null || diskHash is null ||
            string.Equals(stateEntry.ContentHash, diskHash, StringComparison.Ordinal))
        {
            return;
        }

        this.logger.LogWarning("Plugin descriptor {PluginId} was modified locally; preserving the modified content", pluginId);
        userModifiedPlugins.Add(new UserModifiedPluginEntry(
            pluginId,
            pluginDirectory,
            stateEntry.Origin,
            stateEntry.ContentHash,
            diskHash));
    }

    /// <inheritdoc />
    public IReadOnlyList<Plugin> GetAllPlugins() => this.plugins.Select(plugin => plugin.Copy()).ToArray();

    /// <inheritdoc />
    public IReadOnlyList<InvalidPluginEntry> GetInvalidPlugins() => this.invalidPlugins;

    /// <inheritdoc />
    public IReadOnlyList<DisabledPluginEntry> GetDisabledPlugins() => this.disabledPlugins;

    /// <inheritdoc />
    public IReadOnlyList<UserModifiedPluginEntry> GetUserModifiedPlugins() => this.userModifiedPlugins;

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
