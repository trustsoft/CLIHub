namespace CLIHub.Core.Tests.Plugins;

using CLIHub.Core.Plugins;

public class PluginHasherTests
{
    [Fact]
    public void ComputeHash_SameContent_ProducesDeterministicHash()
    {
        var first = PluginHasher.ComputeHash("""{ "id": "opencode" }""");
        var second = PluginHasher.ComputeHash("""{ "id": "opencode" }""");

        Assert.Equal(first, second);
    }

    [Fact]
    public void ComputeHash_DifferentContent_ProducesDifferentHash()
    {
        var first = PluginHasher.ComputeHash("""{ "id": "opencode" }""");
        var second = PluginHasher.ComputeHash("""{ "id": "pi" }""");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ComputeHash_TextAndUtf8Bytes_ProduceSameHash()
    {
        var content = """{ "id": "opencode" }""";

        Assert.Equal(PluginHasher.ComputeHash(content), PluginHasher.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content)));
    }

    [Fact]
    public void ComputeHash_IsHexEncodedSha256()
    {
        var hash = PluginHasher.ComputeHash("clihub");

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void ComputeFileHash_MissingFile_ReturnsNull()
    {
        Assert.Null(PluginHasher.ComputeFileHash(Path.Combine(Path.GetTempPath(), "clihub-missing-" + Guid.NewGuid().ToString("N") + ".json")));
    }

    [Fact]
    public void ComputeFileHash_ExistingFile_MatchesContentHash()
    {
        var path = Path.Combine(Path.GetTempPath(), "clihub-hash-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            const string content = """{ "id": "opencode" }""";
            File.WriteAllText(path, content);

            Assert.Equal(PluginHasher.ComputeHash(content), PluginHasher.ComputeFileHash(path));
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }
}
