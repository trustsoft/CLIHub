namespace CLIHub.Core.Plugins;

using System.Text.Json;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.FileSystem;

using Microsoft.Extensions.Logging;

/// <summary>
///   File-backed <see cref="IPluginRegistry" /> over <c> %APPDATA%\CLIHub\registry.json </c>:
///   tolerant reads and atomic temp-file-then-rename writes, mirroring the persistence pattern
///   of <c> ConfigurationRepository </c>. When the registry is absent (or unreadable) and the
///   legacy <c> plugin-state.json </c> exists, its entries are adopted into the registry once:
///   origin, content hashes, and installed version are preserved, the inverted legacy
///   <c> disabled </c> flag becomes <c> enabled </c>, and the install timestamp is left null
///   rather than fabricated. A successful adoption deletes the legacy file; a failed write
///   keeps it and retries on the next start. Once a registry exists the legacy file is never
///   read again.
/// </summary>
public sealed class PluginRegistryRepository : IPluginRegistry
{
    private readonly ILogger<PluginRegistryRepository> logger;
    private readonly string registryFilePath;
    private readonly string legacyStateFilePath;
    private bool ignoredLegacyLogged;

    /// <summary>
    ///   Creates the repository.
    /// </summary>
    /// <param name="logger"> The logger. </param>
    /// <param name="registryFilePath">
    ///   Overrides the registry file location; defaults to <c> %APPDATA%\CLIHub\registry.json </c>.
    /// </param>
    /// <param name="legacyStateFilePath">
    ///   Overrides the legacy plugin state file location; defaults to
    ///   <c> %APPDATA%\CLIHub\plugin-state.json </c>.
    /// </param>
    public PluginRegistryRepository(ILogger<PluginRegistryRepository> logger,
                                    string? registryFilePath = null,
                                    string? legacyStateFilePath = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.registryFilePath = string.IsNullOrWhiteSpace(registryFilePath)
            ? AppPaths.RegistryFile
            : registryFilePath;
        this.legacyStateFilePath = string.IsNullOrWhiteSpace(legacyStateFilePath)
            ? AppPaths.LegacyPluginStateFile
            : legacyStateFilePath;
    }

    /// <inheritdoc />
    public PluginRegistryDocument Load()
    {
        if (File.Exists(this.registryFilePath))
        {
            try
            {
                var json = File.ReadAllText(this.registryFilePath);
                var document = JsonSerializer.Deserialize<PluginRegistryDocument>(json, CoreJson.Options);
                if (document is null)
                {
                    throw new InvalidOperationException("the registry document deserialized to null");
                }

                this.Normalize(document);
                this.LogIgnoredLegacyOnce();
                return document;
            }
            catch (Exception ex)
            {
                // A corrupt registry is not a dead end while the legacy file exists: its
                // content is the recovery path.
                if (File.Exists(this.legacyStateFilePath))
                {
                    this.logger.LogWarning(
                        ex,
                        "The plugin registry at {Path} is unreadable; recovering by adopting the legacy plugin state",
                        this.registryFilePath);
                    return this.AdoptLegacyState();
                }

                this.logger.LogWarning(ex, "Failed to load the plugin registry at {Path}; starting from empty", this.registryFilePath);
                return PluginRegistryDocument.Empty();
            }
        }

        if (File.Exists(this.legacyStateFilePath))
        {
            return this.AdoptLegacyState();
        }

        return PluginRegistryDocument.Empty();
    }

    /// <inheritdoc />
    public void Save(PluginRegistryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!this.TrySave(document))
        {
            this.logger.LogWarning("Could not write the plugin registry to {Path}", this.registryFilePath);
        }
    }

    /// <summary>
    ///   Reads the legacy plugin state file tolerantly, maps its entries into registry entries,
    ///   and persists the registry exactly once through <see cref="TrySave" />. On success the
    ///   legacy file is deleted (best effort) so no second source of truth lingers; on failure
    ///   the legacy file stays and the mapped document is served from memory until the next
    ///   load retries the adoption.
    /// </summary>
    /// <returns> The mapped registry document. </returns>
    private PluginRegistryDocument AdoptLegacyState()
    {
        var document = PluginRegistryDocument.Empty();
        try
        {
            var legacyJson = File.ReadAllText(this.legacyStateFilePath);
            var legacy = JsonSerializer.Deserialize<LegacyStateShape>(legacyJson, CoreJson.Options);

            if (legacy?.Plugins is not null)
            {
                foreach (var (pluginId, legacyEntry) in legacy.Plugins)
                {
                    // Map the legacy shape into the registry shape: origin and recorded hashes
                    // and version are preserved, the inverted disabled flag becomes enabled,
                    // and the install time stays null (it is unknowable for legacy entries).
                    document.Plugins[pluginId] = new PluginRegistryEntry(
                        string.IsNullOrWhiteSpace(legacyEntry.Origin) ? PluginOrigin.Unknown : legacyEntry.Origin,
                        !legacyEntry.Disabled)
                    {
                        ContentHash = legacyEntry.ContentHash,
                        LogoHash = legacyEntry.LogoHash,
                        InstalledVersion = legacyEntry.InstalledVersion,
                        InstalledAt = null
                    };
                }
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to read the legacy plugin state at {Path}; starting from empty", this.legacyStateFilePath);
            return PluginRegistryDocument.Empty();
        }

        if (this.TrySave(document))
        {
            this.logger.LogInformation(
                "Adopted the legacy plugin state from {Path} into the registry at {RegistryPath}",
                this.legacyStateFilePath,
                this.registryFilePath);
            try
            {
                File.Delete(this.legacyStateFilePath);
                this.logger.LogInformation("Deleted the legacy plugin state file {Path}", this.legacyStateFilePath);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Could not delete the legacy plugin state file {Path}", this.legacyStateFilePath);
            }
        }
        else
        {
            this.logger.LogWarning(
                "Could not write the registry to {RegistryPath}; the legacy plugin state file {Path} is kept and adoption retries on the next start",
                this.registryFilePath,
                this.legacyStateFilePath);
        }

        return document;
    }

    /// <summary>
    ///   Writes the document atomically and reports success, unlike <see cref="Save" /> which
    ///   swallows failures. Used only by the adoption path, which must know whether the legacy
    ///   file may be deleted.
    /// </summary>
    /// <param name="document"> The registry document to persist. </param>
    /// <returns> Whether the write succeeded. </returns>
    private bool TrySave(PluginRegistryDocument document)
    {
        try
        {
            // Atomic write: write to a temp file next to the target, then rename so a crash
            // mid-write can never leave a truncated registry document behind.
            AtomicFileWriter.Write(this.registryFilePath, JsonSerializer.Serialize(document, CoreJson.Options));
            return true;
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not write the plugin registry to {Path}", this.registryFilePath);
            return false;
        }
    }

    /// <summary>
    ///   Tolerant normalization: a missing plugins map becomes an empty ordinal one, and an
    ///   entry whose origin is absent or blank degrades to unknown.
    /// </summary>
    /// <param name="document"> The document to normalize in place. </param>
    private void Normalize(PluginRegistryDocument document)
    {
        document.Plugins ??= new Dictionary<string, PluginRegistryEntry>(StringComparer.Ordinal);

        foreach (var entry in document.Plugins.Values)
        {
            if (string.IsNullOrWhiteSpace(entry.Origin))
            {
                entry.Origin = PluginOrigin.Unknown;
            }
        }
    }

    /// <summary>
    ///   Logs the presence of an ignored legacy plugin state file once per repository instance.
    ///   The application is single-instance, so a lingering legacy file means a downgraded
    ///   build ran meanwhile; logging on every load would spam.
    /// </summary>
    private void LogIgnoredLegacyOnce()
    {
        if (this.ignoredLegacyLogged || !File.Exists(this.legacyStateFilePath))
        {
            return;
        }

        this.ignoredLegacyLogged = true;
        this.logger.LogInformation(
            "A legacy plugin state file is present at {Path}; it is ignored because the registry exists",
            this.legacyStateFilePath);
    }

    /// <summary>
    ///   The legacy <c> plugin-state.json </c> shape, deserialized only inside the adoption
    ///   path. Unknown fields are ignored.
    /// </summary>
    private sealed record LegacyStateShape
    {
        /// <summary> The legacy schema version; dropped by the adoption mapping. </summary>
        public int SchemaVersion { get; set; }

        /// <summary> The legacy per-plugin entries keyed by plugin id. </summary>
        public Dictionary<string, LegacyEntryShape>? Plugins { get; set; }
    }

    /// <summary>
    ///   One legacy entry: the pre-registry lifecycle state with the inverted
    ///   <c> disabled </c> flag.
    /// </summary>
    private sealed record LegacyEntryShape
    {
        /// <summary> The recorded origin, or null when absent. </summary>
        public string? Origin { get; set; }

        /// <summary> The recorded descriptor content hash, or null when unknown. </summary>
        public string? ContentHash { get; set; }

        /// <summary> The recorded logo content hash, or null when unknown. </summary>
        public string? LogoHash { get; set; }

        /// <summary> The recorded installed version, or null when the descriptor omitted it. </summary>
        public string? InstalledVersion { get; set; }

        /// <summary> The legacy disabled flag, inverted into the registry's enabled flag. </summary>
        public bool Disabled { get; set; }
    }
}
