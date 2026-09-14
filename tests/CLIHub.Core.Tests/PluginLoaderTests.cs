using CLIHub.Core.Abstractions;
using CLIHub.Core.Services;

namespace CLIHub.Core.Tests;

public sealed class PluginLoaderTests
{
    private static PluginLoader CreateLoader(string applicationDirectory) =>
        new(new PhysicalFileSystem(), new StubPathProvider(applicationDirectory, applicationDirectory));

    private static void WritePlugin(string applicationDirectory, string folder, string json)
    {
        var pluginDirectory = Path.Combine(applicationDirectory, "plugins", "agents", folder);
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(Path.Combine(pluginDirectory, "agent.json"), json);
    }

    [Fact]
    public void Load_ValidManifest_ReturnsAgentAndIgnoresUnknownFields()
    {
        using var temp = new TempDirectory();
        WritePlugin(temp.Path, "claude", """
            {
              "schemaVersion": 1,
              "id": "claude",
              "name": "Claude Code",
              "unknownField": "ignored",
              "actions": { "run": { "command": "claude" } }
            }
            """);

        var result = CreateLoader(temp.Path).Load();

        var agent = Assert.Single(result.Agents);
        Assert.Equal("claude", agent.Id);
        Assert.Equal("Claude Code", agent.Name);
        Assert.Equal("claude", agent.Actions["run"].Command);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Load_DetectProject_ExposesDetectionPaths()
    {
        using var temp = new TempDirectory();
        WritePlugin(temp.Path, "claude", """
            {
              "schemaVersion": 1,
              "id": "claude",
              "name": "Claude Code",
              "actions": { "run": { "command": "claude" } },
              "detect": { "project": [".claude", "CLAUDE.md"], "unknownField": "ignored" }
            }
            """);

        var result = CreateLoader(temp.Path).Load();

        var agent = Assert.Single(result.Agents);
        Assert.NotNull(agent.Detect);
        Assert.Equal(new[] { ".claude", "CLAUDE.md" }, agent.Detect!.Project);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Load_NoDetectSection_LoadsNormally()
    {
        using var temp = new TempDirectory();
        WritePlugin(temp.Path, "bare", """
            { "schemaVersion": 1, "id": "bare", "name": "Bare" }
            """);

        var result = CreateLoader(temp.Path).Load();

        var agent = Assert.Single(result.Agents);
        Assert.Null(agent.Detect);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Load_MissingManifest_SkipsWithWarning()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(temp.Path, "plugins", "agents", "empty"));

        var result = CreateLoader(temp.Path).Load();

        Assert.Empty(result.Agents);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Load_InvalidJson_SkipsWithWarning()
    {
        using var temp = new TempDirectory();
        WritePlugin(temp.Path, "broken", "{ not valid json ");

        var result = CreateLoader(temp.Path).Load();

        Assert.Empty(result.Agents);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Load_UnsupportedSchemaVersion_SkipsWithWarning()
    {
        using var temp = new TempDirectory();
        WritePlugin(temp.Path, "future", """
            { "schemaVersion": 99, "id": "future", "name": "Future" }
            """);

        var result = CreateLoader(temp.Path).Load();

        Assert.Empty(result.Agents);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Load_MissingRequiredFields_SkipsWithWarning()
    {
        using var temp = new TempDirectory();
        WritePlugin(temp.Path, "nameless", """
            { "schemaVersion": 1, "id": "nameless" }
            """);

        var result = CreateLoader(temp.Path).Load();

        Assert.Empty(result.Agents);
        Assert.Single(result.Warnings);
    }
}
