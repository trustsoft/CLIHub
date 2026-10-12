namespace CLIHub.Tests.Logging;

using CLIHub.Core.Logging;

using Serilog;
using Serilog.Events;

public class LoggingSetupTests : IDisposable
{
    private readonly string dir;

    public LoggingSetupTests()
    {
        this.dir = Path.Combine(Path.GetTempPath(), "clihub-log-" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        try { Directory.Delete(this.dir, recursive: true); } catch { }
    }

    private string? ReadSingleLogFile()
    {
        if (!Directory.Exists(this.dir))
        {
            return null;
        }

        var file = Directory.GetFiles(this.dir, "clihub-*.log").FirstOrDefault();
        return file == null ? null : File.ReadAllText(file);
    }

    [Fact]
    public void CreateLogger_WritesEntry_WithExpectedFormat()
    {
        using (var logger = LoggingSetup.CreateLogger(this.dir, LogEventLevel.Information))
        {
            logger.Information("hello {Name}", "world");
            logger.Dispose();
        }

        var content = this.ReadSingleLogFile();

        Assert.NotNull(content);
        Assert.Contains("hello", content);
        Assert.Contains("world", content);
        Assert.Contains("[", content);
        Assert.Contains("INF", content);
    }

    [Fact]
    public void CreateLogger_AtInformation_SuppressesDebug()
    {
        using (var logger = LoggingSetup.CreateLogger(this.dir, LogEventLevel.Information))
        {
            logger.Debug("debug-should-not-appear");
            logger.Information("info-appears");
            logger.Dispose();
        }

        var content = this.ReadSingleLogFile();

        Assert.NotNull(content);
        Assert.Contains("info-appears", content);
        Assert.DoesNotContain("debug-should-not-appear", content);
    }

    [Fact]
    public void CreateLogger_AtDebug_IncludesDebug()
    {
        using (var logger = LoggingSetup.CreateLogger(this.dir, LogEventLevel.Debug))
        {
            logger.Debug("debug-appears");
            logger.Dispose();
        }

        var content = this.ReadSingleLogFile();

        Assert.NotNull(content);
        Assert.Contains("debug-appears", content);
    }

    [Fact]
    public void CreateLogger_WithSizeLimit_RollsToMultipleFiles()
    {
        using (var logger = LoggingSetup.CreateLogger(
            this.dir,
            LogEventLevel.Information,
            retainedFileCountLimit: 7,
            fileSizeLimitBytes: 1024))
        {
            for (var i = 0; i < 200; i++)
            {
                logger.Information("line {Index} {Padding}", i, new string('x', 50));
            }

            logger.Dispose();
        }

        var files = Directory.GetFiles(this.dir, "clihub-*.log");

        Assert.True(files.Length >= 2, $"expected size-based rollover, got {files.Length} files");
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

public class LogFileLocatorTests
{
    [Fact]
    public void CurrentLogFilePath_UsesDatedFileName()
    {
        var path = LogFileLocator.CurrentLogFilePath(@"C:\logs", new DateTime(2026, 10, 10));

        Assert.Equal(Path.Combine(@"C:\logs", "clihub-20261010.log"), path);
    }
}
