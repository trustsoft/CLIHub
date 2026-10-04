namespace CLIHub.Tests.Services;

using CLIHub.Core.Models;
using CLIHub.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class ConfigServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly CapturingLogger _logger = new();

    public ConfigServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "clihub-config-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private string ConfigPath => Path.Combine(_tempDir, "config.json");

    private ConfigService CreateService(string? configPath = null) =>
        new(_logger, configPath ?? ConfigPath);

    [Fact]
    public void Save_Flush_WritesConfigFile()
    {
        var service = CreateService();
        var config = service.Load();
        config.CurrentProjectId = "abc";
        service.Save(config);

        service.Flush();

        Assert.True(File.Exists(ConfigPath));
        Assert.Contains("\"currentProjectId\": \"abc\"", File.ReadAllText(ConfigPath));
    }

    [Fact]
    public void Save_RapidConsecutiveSaves_FlushPersistsLatest()
    {
        var service = CreateService();
        var first = service.Load();
        first.CurrentProjectId = "first";
        service.Save(first);

        var second = service.Load();
        second.CurrentProjectId = "second";
        service.Save(second);

        service.Flush();

        var content = File.ReadAllText(ConfigPath);
        Assert.Contains("\"currentProjectId\": \"second\"", content);
        Assert.DoesNotContain("\"currentProjectId\": \"first\"", content);
    }

    [Fact]
    public void Dispose_FlushesPendingWrites()
    {
        var service = CreateService();
        var config = service.Load();
        config.CurrentProjectId = "pending";
        service.Save(config);

        service.Dispose();

        Assert.True(File.Exists(ConfigPath), "dispose must flush pending changes");
        Assert.Contains("\"currentProjectId\": \"pending\"", File.ReadAllText(ConfigPath));

        service.Dispose();
    }

    [Fact]
    public void Save_FailedWrite_LogsWarningAndDoesNotThrow()
    {
        // The target file path is occupied by a directory, so the final rename must fail.
        Directory.CreateDirectory(ConfigPath);
        var service = CreateService();

        service.Load().CurrentProjectId = "abc";

        var exception = Record.Exception(service.Flush);

        Assert.Null(exception);
        Assert.Contains(_logger.Warnings, w => w.Contains("Could not write the configuration"));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ConfigService(null!, ConfigPath));
    }

    private sealed class CapturingLogger : ILogger<ConfigService>
    {
        public List<string> Warnings { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }
}
