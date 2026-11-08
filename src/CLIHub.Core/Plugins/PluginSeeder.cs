namespace CLIHub.Core.Plugins;

using CLIHub.Core.Infrastructure.FileSystem;

using Microsoft.Extensions.Logging;

/// <summary>
///   Maintains the built-in plugins folder from descriptors and logos embedded in this
///   assembly, using three-hash refresh logic per file: the hash of the embedded content
///   (<c> embeddedHash </c>), the hash recorded in the plugin state file of the content CLIHub
///   last wrote (<c> storedHash </c>), and the hash of the current on-disk file
///   (<c> diskHash </c>). Unmodified on-disk content is refreshed when the application update
///   ships different embedded content; content that drifted from what CLIHub last wrote is
///   preserved as a user modification. Installed plugins are never touched, and disabled
///   built-ins never have their files rewritten.
/// </summary>
public class PluginSeeder : IPluginSeeder
{
    private const string DescriptorFileName = "plugin.json";
    private const string LogoFileName = "logo.png";

    private readonly ILogger<PluginSeeder> logger;
    private readonly IPluginStateRepository stateRepository;
    private readonly string pluginsPath;

    /// <summary>
    ///   Creates the seeder for the given plugins root.
    /// </summary>
    /// <param name="logger"> The logger. </param>
    /// <param name="stateRepository"> The plugin lifecycle state repository. </param>
    /// <param name="pluginsPath"> Overrides the plugins root; defaults to <c> %APPDATA%\CLIHub\plugins </c>. </param>
    public PluginSeeder(ILogger<PluginSeeder> logger,
                        IPluginStateRepository stateRepository,
                        string? pluginsPath = null)
    {
        this.logger = logger;
        this.stateRepository = stateRepository ?? throw new ArgumentNullException(nameof(stateRepository));
        this.pluginsPath = pluginsPath ?? AppPaths.PluginsDirectory;
    }

    /// <inheritdoc />
    public int EnsureBuiltinsExist()
    {
        try
        {
            Directory.CreateDirectory(this.pluginsPath);

            var assembly = typeof(PluginSeeder).Assembly;

            // Embedded-resource names sanitize characters (for example '-' becomes '_'),
            // so the plugin id is taken from the descriptor content. The sanitized key
            // links a logo resource back to its descriptor.
            var descriptors = EmbeddedPluginResources.EnumerateDescriptors(assembly);
            var logosByKey = EmbeddedPluginResources.EnumerateLogos(assembly)
                .ToDictionary(logo => logo.Key, StringComparer.Ordinal);

            var state = this.stateRepository.Load();
            var stateDirty = false;
            var descriptorsWritten = 0;

            foreach (var descriptor in descriptors)
            {
                if (string.IsNullOrWhiteSpace(descriptor.Id))
                {
                    this.logger.LogWarning("Skipped seed descriptor {Resource}: missing id", descriptor.ResourceName);
                    continue;
                }

                var id = descriptor.Id;
                var directory = Path.Combine(this.pluginsPath, id);
                var descriptorPath = Path.Combine(directory, DescriptorFileName);
                var logoPath = Path.Combine(directory, LogoFileName);

                state.Plugins.TryGetValue(id, out var entry);
                var origin = entry?.Origin ?? PluginOrigin.Unknown;

                // Installed plugins are never touched by seeding, whatever their hashes.
                if (origin == PluginOrigin.Installed)
                {
                    continue;
                }

                // A disabled built-in keeps its files exactly as they are.
                if (entry is { Disabled: true })
                {
                    continue;
                }

                var embeddedHash = PluginHasher.ComputeHash(descriptor.Json);
                var diskHash = PluginHasher.ComputeFileHash(descriptorPath);
                var storedHash = entry?.ContentHash;
                var hadEntry = entry is not null;
                entry ??= new PluginStateEntry(PluginOrigin.Unknown);
                state.Plugins[id] = entry;

                if (diskHash is null)
                {
                    // No descriptor on disk (first run, deleted plugin, or an unknown-origin
                    // plugin whose user content is gone): (re-)seed the embedded content.
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(descriptorPath, descriptor.Json);
                    entry.Origin = PluginOrigin.Builtin;
                    entry.ContentHash = embeddedHash;
                    stateDirty = true;
                    descriptorsWritten++;
                    this.logger.LogInformation("Seeded plugin descriptor {PluginId}", id);
                }
                else if (diskHash == embeddedHash)
                {
                    // The on-disk content matches this build: adopt it as a built-in (no-op
                    // when the state already says so).
                    if (entry.Origin != PluginOrigin.Builtin || entry.ContentHash != embeddedHash)
                    {
                        entry.Origin = PluginOrigin.Builtin;
                        entry.ContentHash = embeddedHash;
                        stateDirty = true;
                    }
                }
                else if (entry.Origin == PluginOrigin.Builtin && diskHash == storedHash && embeddedHash != storedHash)
                {
                    // Unmodified by the user (disk still holds what CLIHub last wrote) while the
                    // application update ships different content: refresh it.
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(descriptorPath, descriptor.Json);
                    entry.ContentHash = embeddedHash;
                    stateDirty = true;
                    descriptorsWritten++;
                    this.logger.LogInformation("Refreshed built-in plugin descriptor {PluginId} after an application update", id);
                }
                else if (entry.Origin == PluginOrigin.Builtin && diskHash != storedHash)
                {
                    // The user edited the descriptor since CLIHub wrote it: preserve the edit
                    // and keep the stored hash so the modification stays detectable.
                    this.logger.LogWarning("Built-in plugin descriptor {PluginId} was modified locally; preserving the modified content", id);
                }
                else
                {
                    if (!hadEntry)
                    {
                        // First observation of unrecognized content: record it as
                        // unknown-origin user content with its current hash.
                        entry.Origin = PluginOrigin.Unknown;
                        entry.ContentHash = diskHash;
                        stateDirty = true;
                    }

                    // An already-unknown-origin plugin keeps its user content and its state
                    // entry untouched.
                    this.logger.LogWarning("Plugin {PluginId} was not recognized as a built-in descriptor; preserved as user content with unknown origin", id);
                }

                // Logo refresh follows the same three-hash pattern against the logo hashes.
                if (logosByKey.TryGetValue(descriptor.Key, out var logoResource))
                {
                    var embeddedLogo = EmbeddedPluginResources.ReadLogo(assembly, logoResource);
                    if (embeddedLogo is not null)
                    {
                        var embeddedLogoHash = PluginHasher.ComputeHash(embeddedLogo);
                        var diskLogoHash = PluginHasher.ComputeFileHash(logoPath);
                        var storedLogoHash = entry.LogoHash;

                        if (diskLogoHash is null)
                        {
                            Directory.CreateDirectory(directory);
                            File.WriteAllBytes(logoPath, embeddedLogo);
                            entry.LogoHash = embeddedLogoHash;
                            stateDirty = true;
                            this.logger.LogInformation("Seeded logo for {PluginId}", id);
                        }
                        else if (diskLogoHash == embeddedLogoHash)
                        {
                            if (storedLogoHash != embeddedLogoHash)
                            {
                                entry.LogoHash = embeddedLogoHash;
                                stateDirty = true;
                            }
                        }
                        else if (storedLogoHash is not null && diskLogoHash == storedLogoHash && embeddedLogoHash != storedLogoHash)
                        {
                            File.WriteAllBytes(logoPath, embeddedLogo);
                            entry.LogoHash = embeddedLogoHash;
                            stateDirty = true;
                            this.logger.LogInformation("Refreshed built-in logo for {PluginId} after an application update", id);
                        }
                        else if (storedLogoHash is not null && diskLogoHash != storedLogoHash)
                        {
                            this.logger.LogWarning("Built-in logo for {PluginId} was modified locally; preserving the modified logo", id);
                        }
                        else if (storedLogoHash is null && entry.Origin == PluginOrigin.Builtin)
                        {
                            // A built-in with an unattributed on-disk logo: record what is
                            // there so later refreshes can distinguish drift from updates.
                            entry.LogoHash = diskLogoHash;
                            stateDirty = true;
                        }
                    }
                }
            }

            if (stateDirty)
            {
                this.stateRepository.Save(state);
            }

            if (descriptorsWritten > 0)
            {
                this.logger.LogInformation("Seeded or refreshed {Count} plugin descriptor(s)", descriptorsWritten);
            }

            return descriptorsWritten;
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Plugin seeding failed; continuing without seeding");
            return 0;
        }
    }
}
