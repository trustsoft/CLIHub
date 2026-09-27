using CLIHub.Core.Logging;
using Serilog;
using Serilog.Events;

namespace CLIHub.Tests.Logging;

public class LoggingSetupTests : IDisposable
{
    private readonly string _dir;

    public LoggingSetupTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "clihub-log-" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    private string? ReadSingleLogFile()
    {
        if (!Directory.Exists(_dir))
            return null;

        var file = Directory.GetFiles(_dir, "clihub-*.log").FirstOrDefault();
        return file == null ? null : File.ReadAllText(file);
    }

    [Fact]
    public void CreateLogger_WritesEntry_WithExpectedFormat()
    {
        using (var logger = LoggingSetup.CreateLogger(_dir, LogEventLevel.Information))
        {
            logger.Information("hello {Name}", "world");
            logger.Dispose();
        }

        var content = ReadSingleLogFile();

        Assert.NotNull(content);
        Assert.Contains("hello", content);
        Assert.Contains("world", content);
        Assert.Contains("[", content);
        Assert.Contains("INF", content);
    }

    [Fact]
    public void CreateLogger_AtInformation_SuppressesDebug()
    {
        using (var logger = LoggingSetup.CreateLogger(_dir, LogEventLevel.Information))
        {
            logger.Debug("debug-should-not-appear");
            logger.Information("info-appears");
            logger.Dispose();
        }

        var content = ReadSingleLogFile();

        Assert.NotNull(content);
        Assert.Contains("info-appears", content);
        Assert.DoesNotContain("debug-should-not-appear", content);
    }

    [Fact]
    public void CreateLogger_AtDebug_IncludesDebug()
    {
        using (var logger = LoggingSetup.CreateLogger(_dir, LogEventLevel.Debug))
        {
            logger.Debug("debug-appears");
            logger.Dispose();
        }

        var content = ReadSingleLogFile();

        Assert.NotNull(content);
        Assert.Contains("debug-appears", content);
    }
}

public class LogLevelParserTests
{
    [Theory]
    [InlineData("Debug", LogEventLevel.Debug)]
    [InlineData("debug", LogEventLevel.Debug)]
    [InlineData("Information", LogEventLevel.Information)]
    [InlineData("Warning", LogEventLevel.Warning)]
    [InlineData("Error", LogEventLevel.Error)]
    [InlineData("  Warning  ", LogEventLevel.Warning)]
    public void Parse_KnownValues(string input, LogEventLevel expected)
    {
        Assert.Equal(expected, LogLevelParser.Parse(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nonsense")]
    public void Parse_UnknownValues_DefaultToInformation(string? input)
    {
        Assert.Equal(LogEventLevel.Information, LogLevelParser.Parse(input));
    }
}
