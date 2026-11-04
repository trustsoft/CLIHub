namespace CLIHub.Core.Tests.Plugins;

using CLIHub.Core.Plugins;

public class PluginDescriptorReaderTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        "clihub-plugin-reader-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { Directory.Delete(this.root, recursive: true); } catch { }
    }

    [Fact]
    public void Read_MissingDescriptor_ReturnsNoPluginAndNoError()
    {
        Directory.CreateDirectory(this.root);

        var result = new PluginDescriptorReader().Read(this.root);

        Assert.Null(result.Plugin);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Read_ValidDescriptor_DeserializesPlugin()
    {
        Directory.CreateDirectory(this.root);
        File.WriteAllText(Path.Combine(this.root, "plugin.json"), """
        {
          "id": "opencode",
          "name": "OpenCode",
          "commands": { "launch": { "executable": "opencode" } }
        }
        """);

        var result = new PluginDescriptorReader().Read(this.root);

        Assert.Null(result.Error);
        Assert.NotNull(result.Plugin);
        Assert.Equal("opencode", result.Plugin.Id);
        Assert.Equal("opencode", result.Plugin.Commands.Launch!.Executable);
    }

    [Fact]
    public void Read_UnknownUpdateCheckSource_DeserializesWithRawSourceValue()
    {
        Directory.CreateDirectory(this.root);
        File.WriteAllText(Path.Combine(this.root, "plugin.json"), """
        {
          "id": "opencode",
          "name": "OpenCode",
          "commands": { "launch": { "executable": "opencode" } },
          "updateCheck": { "source": "cargo", "package": "some-crate" }
        }
        """);

        var result = new PluginDescriptorReader().Read(this.root);

        Assert.Null(result.Error);
        Assert.NotNull(result.Plugin);
        Assert.Equal("cargo", result.Plugin.UpdateCheck!.Source);
        Assert.Equal("some-crate", result.Plugin.UpdateCheck.Package);
    }

    [Fact]
    public void Read_JsonNull_ReturnsNoPluginAndNoError()
    {
        Directory.CreateDirectory(this.root);
        File.WriteAllText(Path.Combine(this.root, "plugin.json"), "null");

        var result = new PluginDescriptorReader().Read(this.root);

        Assert.Null(result.Plugin);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Read_MalformedJson_ReturnsError()
    {
        Directory.CreateDirectory(this.root);
        File.WriteAllText(Path.Combine(this.root, "plugin.json"), "{ not valid json");

        var result = new PluginDescriptorReader().Read(this.root);

        Assert.Null(result.Plugin);
        Assert.IsType<System.Text.Json.JsonException>(result.Error);
    }

    [Fact]
    public void Read_OptionalMetadataFields_PopulatesPlugin()
    {
        Directory.CreateDirectory(this.root);
        File.WriteAllText(Path.Combine(this.root, "plugin.json"), """
        {
          "id": "custom",
          "name": "Custom Agent",
          "version": "1.2.3",
          "publisher": "Community",
          "homepage": "https://example.com/custom",
          "commands": { "launch": { "executable": "custom" } }
        }
        """);

        var result = new PluginDescriptorReader().Read(this.root);

        Assert.Null(result.Error);
        Assert.NotNull(result.Plugin);
        Assert.Equal("1.2.3", result.Plugin.Version);
        Assert.Equal("Community", result.Plugin.Publisher);
        Assert.Equal("https://example.com/custom", result.Plugin.Homepage);
    }

    [Fact]
    public void Read_OptionalMetadataFieldsAbsent_LeavesPropertiesNull()
    {
        Directory.CreateDirectory(this.root);
        File.WriteAllText(Path.Combine(this.root, "plugin.json"), """
        {
          "id": "custom",
          "name": "Custom Agent",
          "commands": { "launch": { "executable": "custom" } }
        }
        """);

        var result = new PluginDescriptorReader().Read(this.root);

        Assert.Null(result.Error);
        Assert.NotNull(result.Plugin);
        Assert.Null(result.Plugin.Version);
        Assert.Null(result.Plugin.Publisher);
        Assert.Null(result.Plugin.Homepage);
    }

    [Fact]
    public void ReadContent_OptionalMetadataFields_PopulatesPluginWithoutFileSystem()
    {
        var result = new PluginDescriptorReader().ReadContent("""
        {
          "id": "custom",
          "name": "Custom Agent",
          "version": "2.0.0",
          "publisher": "Community",
          "homepage": "https://example.com/custom",
          "commands": { "launch": { "executable": "custom" } }
        }
        """);

        Assert.Null(result.Error);
        Assert.NotNull(result.Plugin);
        Assert.Equal("custom", result.Plugin.Id);
        Assert.Equal("2.0.0", result.Plugin.Version);
        Assert.Equal("Community", result.Plugin.Publisher);
        Assert.Equal("https://example.com/custom", result.Plugin.Homepage);
    }

    [Fact]
    public void ReadContent_MalformedJson_ReturnsError()
    {
        var result = new PluginDescriptorReader().ReadContent("{ not valid json");

        Assert.Null(result.Plugin);
        Assert.IsType<System.Text.Json.JsonException>(result.Error);
    }
}
