namespace CLIHub.Core.Tests.Plugins;

using CLIHub.Core.Plugins;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class PluginRegistryRepositoryTests : IDisposable
{
    private readonly string registryFilePath;
    private readonly string legacyStateFilePath;

    public PluginRegistryRepositoryTests()
    {
        this.registryFilePath = Path.Combine(
            Path.GetTempPath(),
            "clihub-registry-" + Guid.NewGuid().ToString("N") + ".json");
        this.legacyStateFilePath = Path.Combine(
            Path.GetTempPath(),
            "clihub-legacy-state-" + Guid.NewGuid().ToString("N") + ".json");
    }

    public void Dispose()
    {
        foreach (var path in new[] { this.registryFilePath, this.legacyStateFilePath })
        {
            try { if (File.Exists(path)) { File.Delete(path); } } catch { }
            try { if (File.Exists(path + ".tmp")) { File.Delete(path + ".tmp"); } } catch { }
        }
    }

    private PluginRegistryRepository CreateRepository() =>
        new(NullLogger<PluginRegistryRepository>.Instance, this.registryFilePath, this.legacyStateFilePath);

    [Fact]
    public void Load_MissingRegistryAndLegacy_ReturnsEmptyRegistry()
    {
        var document = this.CreateRepository().Load();

        Assert.Empty(document.Plugins);
    }

    [Fact]
    public void Load_CorruptRegistryWithoutLegacy_ReturnsEmptyAndLogsWarning()
    {
        File.WriteAllText(this.registryFilePath, "{ not valid json");
        var logger = new RecordingLogger();
        var repository = new PluginRegistryRepository(logger, this.registryFilePath, this.legacyStateFilePath);

        var document = repository.Load();

        Assert.Empty(document.Plugins);
        Assert.Contains(logger.Messages, message => message.Contains("Failed to load the plugin registry", StringComparison.Ordinal));
        // The corrupt file is left in place: nothing was recoverable, so nothing was rewritten.
        Assert.True(File.Exists(this.registryFilePath));
    }

    [Fact]
    public void Save_WritesAtomicallyViaTempFileThenRename()
    {
        var repository = this.CreateRepository();
        var document = PluginRegistryDocument.Empty();
        document.Plugins["opencode"] = new PluginRegistryEntry(PluginOrigin.Builtin)
        {
            ContentHash = "abc123",
            LogoHash = "def456"
        };

        repository.Save(document);

        Assert.True(File.Exists(this.registryFilePath), "the registry file must exist after save");
        Assert.False(File.Exists(this.registryFilePath + ".tmp"), "the temp file must be renamed away, not left behind");
        var written = File.ReadAllText(this.registryFilePath);
        Assert.DoesNotContain(".tmp", written, StringComparison.Ordinal);
        // The registry document carries no schema-version field (tolerant reads ignore unknown
        // fields, so the field would be pure noise).
        Assert.DoesNotContain("schemaVersion", written, StringComparison.Ordinal);
    }

    [Fact]
    public void SaveCreatesMissingParentDirectory()
    {
        var nestedPath = Path.Combine(
            Path.GetTempPath(),
            "clihub-registry-nested-" + Guid.NewGuid().ToString("N"),
            "registry.json");
        try
        {
            // The legacy path is explicit too: no test may construct the repository without a
            // legacy path outside %APPDATA%, even when the test only ever saves.
            var repository = new PluginRegistryRepository(
                NullLogger<PluginRegistryRepository>.Instance,
                nestedPath,
                Path.Combine(Path.GetDirectoryName(nestedPath)!, "legacy-plugin-state.json"));

            repository.Save(PluginRegistryDocument.Empty());

            Assert.True(File.Exists(nestedPath));
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(nestedPath)!, recursive: true); } catch { }
        }
    }

    [Fact]
    public void SaveThenLoad_RoundTripPreservesData()
    {
        var repository = this.CreateRepository();
        var installedAt = new DateTimeOffset(2026, 11, 14, 10, 30, 0, TimeSpan.Zero);
        var document = PluginRegistryDocument.Empty();
        document.Plugins["opencode"] = new PluginRegistryEntry(PluginOrigin.Builtin)
        {
            ContentHash = "cafe01",
            LogoHash = null,
            Enabled = false
        };
        document.Plugins["community"] = new PluginRegistryEntry(PluginOrigin.Installed)
        {
            ContentHash = "cafe02",
            LogoHash = "cafe03",
            InstalledVersion = "1.2.3",
            Enabled = true,
            InstalledAt = installedAt
        };

        repository.Save(document);
        var loaded = repository.Load();

        Assert.Equal(2, loaded.Plugins.Count);

        var builtin = loaded.Plugins["opencode"];
        Assert.Equal(PluginOrigin.Builtin, builtin.Origin);
        Assert.Equal("cafe01", builtin.ContentHash);
        Assert.Null(builtin.LogoHash);
        Assert.Null(builtin.InstalledVersion);
        Assert.False(builtin.Enabled);
        Assert.Null(builtin.InstalledAt);

        var installed = loaded.Plugins["community"];
        Assert.Equal(PluginOrigin.Installed, installed.Origin);
        Assert.Equal("cafe02", installed.ContentHash);
        Assert.Equal("cafe03", installed.LogoHash);
        Assert.Equal("1.2.3", installed.InstalledVersion);
        Assert.True(installed.Enabled);
        Assert.Equal(installedAt, installed.InstalledAt);
    }

    [Fact]
    public void Load_MissingFields_DefaultOriginUnknownAndEnabledTrue()
    {
        // A registry entry written before origin/enabled were recorded (or trimmed by a user)
        // must degrade to unknown/enabled rather than fail the load or disable the plugin.
        File.WriteAllText(this.registryFilePath, """
        {
          "plugins": {
            "opencode": { "contentHash": "cafe01" }
          }
        }
        """);

        var document = this.CreateRepository().Load();

        var entry = Assert.Single(document.Plugins.Values);
        Assert.Equal(PluginOrigin.Unknown, entry.Origin);
        Assert.True(entry.Enabled);
        Assert.Equal("cafe01", entry.ContentHash);
    }

    [Fact]
    public void Load_LegacyStateAdopted_WritesRegistryOnceAndDeletesLegacy()
    {
        File.WriteAllText(this.legacyStateFilePath, """
        {
          "schemaVersion": 1,
          "plugins": {
            "opencode": {
              "origin": "builtin",
              "contentHash": "cafe01"
            },
            "community": {
              "origin": "installed",
              "contentHash": "cafe02",
              "logoHash": "cafe03",
              "installedVersion": "1.2.3"
            },
            "parked": {
              "origin": "installed",
              "disabled": true
            }
          }
        }
        """);
        var logger = new RecordingLogger();
        var repository = new PluginRegistryRepository(logger, this.registryFilePath, this.legacyStateFilePath);

        var document = repository.Load();

        Assert.True(File.Exists(this.registryFilePath), "the registry must be written during adoption");
        Assert.False(File.Exists(this.legacyStateFilePath), "the legacy state file must be deleted after a successful adoption");
        Assert.False(File.Exists(this.registryFilePath + ".tmp"), "the adoption write must not leave a temp file behind");
        Assert.Contains(logger.Messages, message => message.Contains("Adopted the legacy plugin state", StringComparison.Ordinal));

        Assert.Equal(3, document.Plugins.Count);

        var builtin = document.Plugins["opencode"];
        Assert.Equal(PluginOrigin.Builtin, builtin.Origin);
        Assert.Equal("cafe01", builtin.ContentHash);
        Assert.True(builtin.Enabled);
        Assert.Null(builtin.InstalledAt);

        var installed = document.Plugins["community"];
        Assert.Equal(PluginOrigin.Installed, installed.Origin);
        Assert.Equal("cafe02", installed.ContentHash);
        Assert.Equal("cafe03", installed.LogoHash);
        Assert.Equal("1.2.3", installed.InstalledVersion);
        Assert.True(installed.Enabled);
        Assert.Null(installed.InstalledAt);

        // The inverted legacy disabled flag becomes the positive enabled flag.
        var disabled = document.Plugins["parked"];
        Assert.Equal(PluginOrigin.Installed, disabled.Origin);
        Assert.False(disabled.Enabled);
        Assert.Null(disabled.InstalledAt);

        // The registry file the adoption wrote is the same mapped content: a reload reads the
        // registry (never the legacy file again) and observes identical entries.
        var reloaded = this.CreateRepository().Load();
        Assert.Equal(3, reloaded.Plugins.Count);
        Assert.False(reloaded.Plugins["parked"].Enabled);
    }

    [Fact]
    public void Load_AdoptionWriteFails_KeepsLegacyAndRetriesNextLoad()
    {
        File.WriteAllText(this.legacyStateFilePath, """
        {
          "plugins": {
            "parked": { "origin": "installed", "disabled": true }
          }
        }
        """);
        // A directory occupying the registry path blocks the adoption write (the temp file
        // cannot be moved over a directory), simulating an unwritable target.
        Directory.CreateDirectory(this.registryFilePath);
        var logger = new RecordingLogger();
        var repository = new PluginRegistryRepository(logger, this.registryFilePath, this.legacyStateFilePath);

        var document = repository.Load();

        // The mapped document is served from memory even though the write failed, the legacy
        // file is kept so adoption can retry, and the failure is logged as a warning.
        var entry = Assert.Single(document.Plugins.Values);
        Assert.Equal(PluginOrigin.Installed, entry.Origin);
        Assert.False(entry.Enabled);
        Assert.True(File.Exists(this.legacyStateFilePath), "the legacy file must survive a failed adoption write");
        Assert.Contains(logger.Messages, message => message.Contains("adoption retries on the next start", StringComparison.Ordinal));

        // A later load over a writable path completes the adoption.
        Directory.Delete(this.registryFilePath);
        var retryDocument = this.CreateRepository().Load();
        Assert.True(File.Exists(this.registryFilePath), "the retried adoption must write the registry");
        Assert.False(File.Exists(this.legacyStateFilePath), "the retried adoption must delete the legacy file");
        Assert.False(Assert.Single(retryDocument.Plugins.Values).Enabled);
    }

    [Fact]
    public void Load_BothFilesPresent_RegistryWinsAndLegacyIgnored()
    {
        // The registry says the plugin is disabled; the legacy file says it was enabled. The
        // registry is the source of truth once it exists.
        File.WriteAllText(this.registryFilePath, """
        {
          "plugins": {
            "opencode": { "origin": "installed", "enabled": false }
          }
        }
        """);
        File.WriteAllText(this.legacyStateFilePath, """
        {
          "plugins": {
            "opencode": { "origin": "installed" }
          }
        }
        """);
        var logger = new RecordingLogger();
        var repository = new PluginRegistryRepository(logger, this.registryFilePath, this.legacyStateFilePath);

        var document = repository.Load();

        var entry = Assert.Single(document.Plugins.Values);
        Assert.False(entry.Enabled);
        Assert.True(File.Exists(this.legacyStateFilePath), "the ignored legacy file must not be touched");
        Assert.Contains(logger.Messages, message => message.Contains("it is ignored because the registry exists", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_CorruptRegistryWithLegacy_AdoptsLegacyContent()
    {
        File.WriteAllText(this.registryFilePath, "{ not valid json");
        File.WriteAllText(this.legacyStateFilePath, """
        {
          "plugins": {
            "community": { "origin": "installed", "installedVersion": "2.0.0", "disabled": true }
          }
        }
        """);
        var logger = new RecordingLogger();
        var repository = new PluginRegistryRepository(logger, this.registryFilePath, this.legacyStateFilePath);

        var document = repository.Load();

        Assert.Contains(logger.Messages, message => message.Contains("unreadable", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("Adopted the legacy plugin state", StringComparison.Ordinal));
        var entry = Assert.Single(document.Plugins.Values);
        Assert.Equal(PluginOrigin.Installed, entry.Origin);
        Assert.Equal("2.0.0", entry.InstalledVersion);
        Assert.False(entry.Enabled);
        Assert.True(File.Exists(this.registryFilePath), "the recovery must leave a readable registry behind");
        Assert.False(File.Exists(this.legacyStateFilePath), "the legacy file is deleted once the recovery wrote the registry");
    }

    private sealed class RecordingLogger : ILogger<PluginRegistryRepository>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            this.Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
