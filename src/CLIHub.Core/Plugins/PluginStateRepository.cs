namespace CLIHub.Core.Plugins;

using System.Text.Json;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.FileSystem;

using Microsoft.Extensions.Logging;

/// <summary>
///   File-backed <see cref="IPluginStateRepository" /> over
///   <c> %APPDATA%\CLIHub\plugin-state.json </c>: tolerant reads and atomic
///   temp-file-then-rename writes, mirroring the persistence pattern of
///   <c> ConfigurationRepository </c>.
/// </summary>
public sealed class PluginStateRepository : IPluginStateRepository
{
    private readonly ILogger<PluginStateRepository> logger;
    private readonly string stateFilePath;

    /// <summary>
    ///   Creates the repository.
    /// </summary>
    /// <param name="logger"> The logger. </param>
    /// <param name="stateFilePath">
    ///   Overrides the state file location; defaults to <c> %APPDATA%\CLIHub\plugin-state.json </c>.
    /// </param>
    public PluginStateRepository(ILogger<PluginStateRepository> logger,
                                 string? stateFilePath = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.stateFilePath = string.IsNullOrWhiteSpace(stateFilePath)
            ? AppPaths.PluginStateFile
            : stateFilePath;
    }

    /// <inheritdoc />
    public PluginStateDocument Load()
    {
        try
        {
            if (!File.Exists(this.stateFilePath))
            {
                return PluginStateDocument.Empty();
            }

            var json = File.ReadAllText(this.stateFilePath);
            var document = JsonSerializer.Deserialize<PluginStateDocument>(json, CoreJson.Options);
            if (document is null)
            {
                this.logger.LogWarning("Plugin state at {Path} is null; starting from empty state", this.stateFilePath);
                return PluginStateDocument.Empty();
            }

            document.Plugins ??= new Dictionary<string, PluginStateEntry>(StringComparer.Ordinal);

            // Tolerant read: an entry whose origin is absent or blank degrades to unknown.
            foreach (var entry in document.Plugins.Values)
            {
                if (string.IsNullOrWhiteSpace(entry.Origin))
                {
                    entry.Origin = PluginOrigin.Unknown;
                }
            }

            return document;
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to load plugin state at {Path}; starting from empty state", this.stateFilePath);
            return PluginStateDocument.Empty();
        }
    }

    /// <inheritdoc />
    public void Save(PluginStateDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        try
        {
            // Atomic write: write to a temp file next to the target, then rename so a crash
            // mid-write can never leave a truncated state file behind.
            AtomicFileWriter.Write(this.stateFilePath, JsonSerializer.Serialize(document, CoreJson.Options));
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not write the plugin state to {Path}", this.stateFilePath);
        }
    }
}
