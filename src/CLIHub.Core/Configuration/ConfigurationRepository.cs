namespace CLIHub.Core.Configuration;

using System.Text.Json;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Models;

/// <summary>
///   Owns configuration snapshots, migrations, and persistence to config.json.
/// </summary>
public sealed class ConfigurationRepository : IConfigurationRepository
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(250);

    private readonly ILogger<ConfigurationRepository> logger;
    private readonly IConfigMigrationRunner migrationRunner;
    private readonly object stateGate = new();
    private readonly object writerGate = new();
    private readonly object writeLock = new();
    private readonly string configFilePath;
    private ConfigurationSnapshot? cachedSnapshot;
    private bool loaded;
    private PendingWrite? pendingWrite;
    private long latestWriteGeneration;
    private Task workerTask = Task.CompletedTask;
    private bool workerRunning;

    internal Action? BeforeWorkerExitForTests { get; set; }

    /// <summary>
    ///   Creates the service.
    /// </summary>
    /// <param name="logger"> The logger. </param>
    /// <param name="configFilePath">
    ///   Overrides the config file location; defaults to <c> %APPDATA%\CLIHub\config.json </c>.
    /// </param>
    /// <param name="migrationRunner"> Optional schema migration runner. </param>
    public ConfigurationRepository(ILogger<ConfigurationRepository> logger,
                                   string? configFilePath = null,
                                   IConfigMigrationRunner? migrationRunner = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.migrationRunner = migrationRunner ?? new ConfigMigrationRunner();

        if (string.IsNullOrWhiteSpace(configFilePath))
        {
            Directory.CreateDirectory(AppPaths.Root);
            this.configFilePath = AppPaths.ConfigFile;
        }
        else
        {
            this.configFilePath = configFilePath;
        }
    }

    /// <inheritdoc />
    public ConfigurationSnapshot Read()
    {
        lock (this.stateGate)
        {
            this.EnsureLoaded();
            return this.cachedSnapshot!.Clone();
        }
    }

    /// <inheritdoc />
    public void Update(Action<ConfigurationSnapshot> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        lock (this.stateGate)
        {
            this.EnsureLoaded();

            var snapshot = this.cachedSnapshot!.Clone();
            update(snapshot);

            var json = Serialize(snapshot);
            this.cachedSnapshot = snapshot;
            this.QueueWrite(json);
        }
    }

    private void EnsureLoaded()
    {
        if (this.loaded)
        {
            return;
        }

        if (!File.Exists(this.configFilePath))
        {
            this.logger.LogInformation("No config found at {Path}; creating defaults", this.configFilePath);
            this.cachedSnapshot = new ConfigurationSnapshot();
            this.loaded = true;
            this.QueueWrite(Serialize(this.cachedSnapshot));
            return;
        }

        try
        {
            var json = File.ReadAllText(this.configFilePath);
            var document = JsonSerializer.Deserialize<AppConfigDocument>(json, CoreJson.Options);

            var schemaVersion = document?.SchemaVersion ?? AppConfigDocument.LegacySchemaVersion;
            if (schemaVersion < AppConfigDocument.LegacySchemaVersion ||
                schemaVersion > AppConfigDocument.CurrentSchemaVersion)
            {
                this.logger.LogWarning(
                    "Unsupported configuration schema version {SchemaVersion} at {Path}",
                    schemaVersion,
                    this.configFilePath);
                this.cachedSnapshot = new ConfigurationSnapshot();
                this.loaded = true;
                return;
            }

            var snapshot = document?.ToSnapshot() ?? new ConfigurationSnapshot();
            var migrated = !this.migrationRunner.HasMigrations &&
                schemaVersion == AppConfigDocument.LegacySchemaVersion
                ? new ConfigurationMigrationResult(snapshot, schemaVersion)
                : this.migrationRunner.Migrate(schemaVersion, snapshot, AppConfigDocument.CurrentSchemaVersion);
            this.cachedSnapshot = migrated.Snapshot;
            this.loaded = true;
            if (migrated.SchemaVersion != schemaVersion)
            {
                this.QueueWrite(Serialize(this.cachedSnapshot));
            }

            this.logger.LogDebug("Loaded configuration from {Path}", this.configFilePath);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to load config at {Path}; using defaults", this.configFilePath);
            this.cachedSnapshot = new ConfigurationSnapshot();
            this.loaded = true;
        }
    }

    private static string Serialize(ConfigurationSnapshot snapshot) =>
        JsonSerializer.Serialize(AppConfigDocument.FromSnapshot(snapshot), CoreJson.Options);

    private void QueueWrite(string json)
    {
        lock (this.writerGate)
        {
            this.pendingWrite = new PendingWrite(json, ++this.latestWriteGeneration);
            this.EnsureWorkerLocked();
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        while (true)
        {
            Task worker;
            PendingWrite? pendingWrite = null;
            lock (this.writerGate)
            {
                worker = this.workerTask;
                if (worker.IsCompleted && this.pendingWrite is not null)
                {
                    pendingWrite = this.pendingWrite;
                    this.pendingWrite = null;
                }
            }

            if (!worker.IsCompleted)
            {
                worker.Wait();
                continue;
            }

            if (pendingWrite is not null)
            {
                this.WriteConfig(pendingWrite);
                continue;
            }

            lock (this.writerGate)
            {
                if (!this.workerRunning && this.pendingWrite is null && this.workerTask.IsCompleted)
                {
                    return;
                }
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            this.Flush();
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not flush pending configuration writes to {Path}", this.configFilePath);
        }
    }

    private void EnsureWorkerLocked()
    {
        if (this.workerRunning)
        {
            return;
        }

        this.workerRunning = true;
        this.workerTask = Task.Run(this.ProcessPendingAsync);
    }

    private async Task ProcessPendingAsync()
    {
        try
        {
            while (true)
            {
                await Task.Delay(DebounceDelay).ConfigureAwait(false);

                PendingWrite? pendingWrite;
                lock (this.writerGate)
                {
                    pendingWrite = this.pendingWrite;
                    this.pendingWrite = null;
                }

                if (pendingWrite is null)
                {
                    this.BeforeWorkerExitForTests?.Invoke();
                    return;
                }

                this.WriteConfig(pendingWrite);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "The configuration writer failed");
        }
        finally
        {
            lock (this.writerGate)
            {
                this.workerRunning = false;

                // A save that slipped in while this worker was exiting: start a new one.
                if (this.pendingWrite is not null)
                {
                    this.workerRunning = true;
                    this.workerTask = Task.Run(this.ProcessPendingAsync);
                }
            }
        }
    }

    private void WriteConfig(PendingWrite pendingWrite)
    {
        try
        {
            // Atomic write: write to temp file, then rename to avoid corruption
            var tempPath = this.configFilePath + ".tmp";
            var directory = Path.GetDirectoryName(this.configFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            lock (this.writeLock)
            {
                lock (this.writerGate)
                {
                    if (pendingWrite.Generation < this.latestWriteGeneration)
                    {
                        return;
                    }
                }

                File.WriteAllText(tempPath, pendingWrite.Json);
                File.Move(tempPath, this.configFilePath, overwrite: true);
            }

            this.logger.LogDebug("Saved configuration to {Path}", this.configFilePath);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not write the configuration to {Path}", this.configFilePath);
        }
    }

    private sealed record PendingWrite(string Json, long Generation);
}
