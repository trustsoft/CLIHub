namespace CLIHub.Core.Configuration;

using System.Text.Json;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Models;

/// <summary>
///   Loads and saves the application configuration to config.json. Saves are serialized on the
///   calling thread and written to disk by a single debounced background worker, so callers are
///   never blocked by disk I/O; <see cref="Flush"/> drains pending writes synchronously.
/// </summary>
public class ConfigService : IConfigService
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(250);

    private readonly ILogger<ConfigService> _logger;
    private readonly object _gate = new();
    private readonly object _writeLock = new();
    private readonly string _configFilePath;
    private AppConfig? _cachedConfig;
    private string? _pendingJson;
    private Task _workerTask = Task.CompletedTask;
    private bool _workerRunning;

    internal Action? BeforeWorkerExitForTests { get; set; }

    /// <summary>
    ///   Creates the service.
    /// </summary>
    /// <param name="logger"> The logger. </param>
    /// <param name="configFilePath"> Overrides the config file location; defaults to <c>%APPDATA%\CLIHub\config.json</c>. </param>
    public ConfigService(ILogger<ConfigService> logger, string? configFilePath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
    public ConfigurationSnapshot Load()
    {
        if (_cachedConfig != null)
        {
            return ConfigurationSnapshot.From(_cachedConfig);
        }

        if (!File.Exists(_configFilePath))
        {
            _logger.LogInformation("No config found at {Path}; creating defaults", _configFilePath);
            _cachedConfig = new AppConfig();
            Save(ConfigurationSnapshot.From(_cachedConfig));
            return ConfigurationSnapshot.From(_cachedConfig);
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            var document = JsonSerializer.Deserialize<AppConfigDocument>(json, CoreJson.Options);
            _cachedConfig = document?.ToAppConfig() ?? new AppConfig();
            _logger.LogDebug("Loaded configuration from {Path}", _configFilePath);
            return ConfigurationSnapshot.From(_cachedConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read config at {Path}; using defaults", _configFilePath);
            _cachedConfig = new AppConfig();
            return ConfigurationSnapshot.From(_cachedConfig);
        }
    }

    /// <inheritdoc />
    public void Save(ConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var config = snapshot.ToAppConfig();

        // Serialize here: the config graph is shared and mutated elsewhere, so background
        // serialization could race with the caller. Serializing this small document on the
        // calling thread is far cheaper than the disk write it replaces.
        _cachedConfig = config;
        var json = JsonSerializer.Serialize(AppConfigDocument.From(config), CoreJson.Options);

        lock (_gate)
        {
            _pendingJson = json;
        }

        EnsureWorker();
    }

    /// <inheritdoc />
    public void Flush()
    {
        Task worker;
        lock (_gate)
        {
            worker = _workerTask;
        }

        if (!worker.IsCompleted)
        {
            worker.Wait();
        }

        string? json;
        lock (_gate)
        {
            json = _pendingJson;
            _pendingJson = null;
        }

        if (json is not null)
        {
            WriteConfig(json);
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

    private void EnsureWorker()
    {
        lock (_gate)
        {
            if (_workerRunning)
            {
                return;
            }

            _workerRunning = true;
            _workerTask = Task.Run(ProcessPendingAsync);
        }
    }

    private async Task ProcessPendingAsync()
    {
        try
        {
            while (true)
            {
                await Task.Delay(DebounceDelay).ConfigureAwait(false);

                string? json;
                lock (_gate)
                {
                    json = _pendingJson;
                    _pendingJson = null;
                }

                if (json is null)
                {
                    BeforeWorkerExitForTests?.Invoke();
                    return;
                }

                WriteConfig(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The configuration writer failed");
        }
        finally
        {
            lock (_gate)
            {
                _workerRunning = false;

                // A save that slipped in while this worker was exiting: start a new one.
                if (_pendingJson is not null)
                {
                    _workerRunning = true;
                    _workerTask = Task.Run(ProcessPendingAsync);
                }
            }
        }
    }

    private void WriteConfig(string json)
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
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _configFilePath, overwrite: true);
            }

            _logger.LogDebug("Saved configuration to {Path}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write the configuration to {Path}", _configFilePath);
        }
    }
}
