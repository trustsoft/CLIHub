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

    private readonly ILogger<ConfigurationRepository> _logger;
    private readonly IConfigMigrationRunner _migrationRunner;
    private readonly object _stateGate = new();
    private readonly object _writerGate = new();
    private readonly object _writeLock = new();
    private readonly string _configFilePath;
    private AppConfig? _cachedConfig;
    private bool _loaded;
    private PendingWrite? _pendingWrite;
    private long _latestWriteGeneration;
    private Task _workerTask = Task.CompletedTask;
    private bool _workerRunning;

    internal Action? BeforeWorkerExitForTests { get; set; }

    /// <summary>
    ///   Creates the service.
    /// </summary>
    /// <param name="logger"> The logger. </param>
    /// <param name="configFilePath"> Overrides the config file location; defaults to <c>%APPDATA%\CLIHub\config.json</c>. </param>
    /// <param name="migrationRunner"> Optional schema migration runner. </param>
    public ConfigurationRepository(
        ILogger<ConfigurationRepository> logger,
        string? configFilePath = null,
        IConfigMigrationRunner? migrationRunner = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _migrationRunner = migrationRunner ?? new ConfigMigrationRunner();

        if (string.IsNullOrWhiteSpace(configFilePath))
        {
            Directory.CreateDirectory(AppPaths.Root);
            _configFilePath = AppPaths.ConfigFile;
        }
        else
        {
            _configFilePath = configFilePath;
        }
    }

    /// <inheritdoc />
    public ConfigurationSnapshot Read()
    {
        lock (_stateGate)
        {
            EnsureLoaded();
            return ConfigurationSnapshot.From(_cachedConfig!);
        }
    }

    /// <inheritdoc />
    public void Update(Action<ConfigurationSnapshot> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        lock (_stateGate)
        {
            EnsureLoaded();

            var snapshot = ConfigurationSnapshot.From(_cachedConfig!);
            update(snapshot);

            var updatedConfig = snapshot.ToAppConfig();
            var json = Serialize(updatedConfig);
            _cachedConfig = updatedConfig;
            QueueWrite(json);
        }
    }

    private void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        if (!File.Exists(_configFilePath))
        {
            _logger.LogInformation("No config found at {Path}; creating defaults", _configFilePath);
            _cachedConfig = new AppConfig();
            _loaded = true;
            QueueWrite(Serialize(_cachedConfig));
            return;
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            var document = JsonSerializer.Deserialize<AppConfigDocument>(json, CoreJson.Options);

            var schemaVersion = document?.SchemaVersion ?? AppConfigDocument.LegacySchemaVersion;
            if (schemaVersion < AppConfigDocument.LegacySchemaVersion ||
                schemaVersion > AppConfigDocument.CurrentSchemaVersion)
            {
                _logger.LogWarning(
                    "Unsupported configuration schema version {SchemaVersion} at {Path}",
                    schemaVersion,
                    _configFilePath);
                _cachedConfig = new AppConfig();
                _loaded = true;
                return;
            }

            var snapshot = ConfigurationSnapshot.From(document?.ToAppConfig() ?? new AppConfig());
            var migrated = !_migrationRunner.HasMigrations &&
                schemaVersion == AppConfigDocument.LegacySchemaVersion
                ? new ConfigurationMigrationResult(snapshot, schemaVersion)
                : _migrationRunner.Migrate(schemaVersion, snapshot, AppConfigDocument.CurrentSchemaVersion);
            _cachedConfig = migrated.Snapshot.ToAppConfig();
            _loaded = true;
            if (migrated.SchemaVersion != schemaVersion)
            {
                QueueWrite(Serialize(_cachedConfig));
            }

            _logger.LogDebug("Loaded configuration from {Path}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load config at {Path}; using defaults", _configFilePath);
            _cachedConfig = new AppConfig();
            _loaded = true;
        }
    }

    private static string Serialize(AppConfig config) =>
        JsonSerializer.Serialize(AppConfigDocument.From(config), CoreJson.Options);

    private void QueueWrite(string json)
    {
        lock (_writerGate)
        {
            _pendingWrite = new PendingWrite(json, ++_latestWriteGeneration);
            EnsureWorkerLocked();
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        while (true)
        {
            Task worker;
            PendingWrite? pendingWrite = null;
            lock (_writerGate)
            {
                worker = _workerTask;
                if (worker.IsCompleted && _pendingWrite is not null)
                {
                    pendingWrite = _pendingWrite;
                    _pendingWrite = null;
                }
            }

            if (!worker.IsCompleted)
            {
                worker.Wait();
                continue;
            }

            if (pendingWrite is not null)
            {
                WriteConfig(pendingWrite);
                continue;
            }

            lock (_writerGate)
            {
                if (!_workerRunning && _pendingWrite is null && _workerTask.IsCompleted)
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
            Flush();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not flush pending configuration writes to {Path}", _configFilePath);
        }
    }

    private void EnsureWorkerLocked()
    {
        if (_workerRunning)
        {
            return;
        }

        _workerRunning = true;
        _workerTask = Task.Run(ProcessPendingAsync);
    }

    private async Task ProcessPendingAsync()
    {
        try
        {
            while (true)
            {
                await Task.Delay(DebounceDelay).ConfigureAwait(false);

                PendingWrite? pendingWrite;
                lock (_writerGate)
                {
                    pendingWrite = _pendingWrite;
                    _pendingWrite = null;
                }

                if (pendingWrite is null)
                {
                    BeforeWorkerExitForTests?.Invoke();
                    return;
                }

                WriteConfig(pendingWrite);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The configuration writer failed");
        }
        finally
        {
            lock (_writerGate)
            {
                _workerRunning = false;

                // A save that slipped in while this worker was exiting: start a new one.
                if (_pendingWrite is not null)
                {
                    _workerRunning = true;
                    _workerTask = Task.Run(ProcessPendingAsync);
                }
            }
        }
    }

    private void WriteConfig(PendingWrite pendingWrite)
    {
        try
        {
            // Atomic write: write to temp file, then rename to avoid corruption
            var tempPath = _configFilePath + ".tmp";
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            lock (_writeLock)
            {
                lock (_writerGate)
                {
                    if (pendingWrite.Generation < _latestWriteGeneration)
                    {
                        return;
                    }
                }

                File.WriteAllText(tempPath, pendingWrite.Json);
                File.Move(tempPath, _configFilePath, overwrite: true);
            }

            _logger.LogDebug("Saved configuration to {Path}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write the configuration to {Path}", _configFilePath);
        }
    }

    private sealed record PendingWrite(string Json, long Generation);
}
