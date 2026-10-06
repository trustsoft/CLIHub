namespace CLIHub.Tests.Services;

using CLIHub.Core.Plugins;

public class PluginDescriptorReaderTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "clihub-plugin-reader-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    [Fact]
    public void Read_MissingDescriptor_ReturnsNoPluginAndNoError()
    {
        Directory.CreateDirectory(_root);

        var result = new PluginDescriptorReader().Read(_root);

        Assert.Null(result.Plugin);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Read_ValidDescriptor_DeserializesPlugin()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "plugin.json"), """
        {
          "id": "opencode",
          "name": "OpenCode",
          "commands": { "launch": { "executable": "opencode" } }
        }
        """);

        var result = new PluginDescriptorReader().Read(_root);

        Assert.Null(result.Error);
        Assert.NotNull(result.Plugin);
        Assert.Equal("opencode", result.Plugin.Id);
        Assert.Equal("opencode", result.Plugin.Commands.Launch!.Executable);
    }

    [Fact]
    public void Read_JsonNull_ReturnsNoPluginAndNoError()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "plugin.json"), "null");

        var result = new PluginDescriptorReader().Read(_root);

        Assert.Null(result.Plugin);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Read_MalformedJson_ReturnsError()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "plugin.json"), "{ not valid json");

        var result = new PluginDescriptorReader().Read(_root);

        Assert.Null(result.Plugin);
        Assert.IsType<System.Text.Json.JsonException>(result.Error);
    }
}
