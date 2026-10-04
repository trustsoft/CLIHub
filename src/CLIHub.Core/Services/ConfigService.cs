namespace CLIHub.Core.Services;

using CLIHub.Core.Models;
using CLIHub.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
///   Loads and saves the application configuration to config.json. Saves are serialized on the
///   calling thread and written to disk by a single debounced background worker, so callers are
///   never blocked by disk I/O; <see cref="Flush"/> drains pending writes synchronously.
/// </summary>
public class ConfigService : IConfigService
{
    /// <summary>
    ///   Serializer options used for <c>config.json</c>; internal so tests can assert the on-disk key names.
    /// </summary>
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(250);

    private readonly ILogger<ConfigService> _logger;
    private readonly object _gate = new();
    private readonly object _writeLock = new();
    private AppConfig? _cachedConfig;
    private string? _pendingJson;
    private Task _workerTask = Task.CompletedTask;
    private bool _workerRunning;

    /// <inheritdoc />
    public string ConfigFilePath { get; }

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
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var clihubFolder = Path.Combine(appDataPath, "CLIHub");
            Directory.CreateDirectory(clihubFolder);
            ConfigFilePath = Path.Combine(clihubFolder, "config.json");
        }
        else
        {
            ConfigFilePath = configFilePath;
        }
    }

    /// <inheritdoc />
    public AppConfig Load()
    {
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        if (!File.Exists(ConfigFilePath))
        {
            _logger.LogInformation("No config found at {Path}; creating defaults", ConfigFilePath);
            _cachedConfig = new AppConfig();
            Save(_cachedConfig);
            return _cachedConfig;
        }

        try
        {
            var json = File.ReadAllText(ConfigFilePath);
            _cachedConfig = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            _logger.LogDebug("Loaded configuration from {Path}", ConfigFilePath);
            return _cachedConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read config at {Path}; using defaults", ConfigFilePath);
            _cachedConfig = new AppConfig();
            return _cachedConfig;
        }
    }

    /// <inheritdoc />
    public void Save(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        // Serialize here: the config graph is shared and mutated elsewhere, so background
        // serialization could race with the caller. Serializing this small document on the
        // calling thread is far cheaper than the disk write it replaces.
        _cachedConfig = config;
        var json = JsonSerializer.Serialize(config, JsonOptions);

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
            _logger.LogWarning(ex, "Could not flush pending configuration writes to {Path}", ConfigFilePath);
        }
    }

    /// <inheritdoc />
    public Project? GetCurrentProject()
    {
        var config = Load();
        if (config.CurrentProjectId == null)
        {
            return null;
        }

        return config.Projects.FirstOrDefault(p => p.Id == config.CurrentProjectId);
    }

    /// <inheritdoc />
    public void SetCurrentProject(string? projectId)
    {
        var config = Load();
        config.CurrentProjectId = projectId;
        Save(config);
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
            var tempPath = ConfigFilePath + ".tmp";
            var directory = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            lock (_writeLock)
            {
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, ConfigFilePath, overwrite: true);
            }

            _logger.LogDebug("Saved configuration to {Path}", ConfigFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write the configuration to {Path}", ConfigFilePath);
        }
    }
}
