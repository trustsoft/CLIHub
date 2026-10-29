namespace CLIHub.Tests.ViewModels;

using CLIHub.Core.Plugins;
using CLIHub.ViewModels;

public class InvalidAgentCardItemTests
{
    [Fact]
    public void ComposeReason_FromValidationErrors_JoinsWithSemicolon()
    {
        var entry = new InvalidPluginEntry("C:\\plugins\\broken", null, null,
            new PluginValidationResult(["Error1", "Error2"]));

        Assert.Equal("Error1; Error2", InvalidAgentCardItem.ComposeReason(entry));
    }

    [Fact]
    public void ComposeReason_FromReadError_UsesExceptionMessage()
    {
        var entry = new InvalidPluginEntry("C:\\plugins\\broken", null,
            new PluginDescriptorReadResult(null, new InvalidDataException("Bad descriptor")), null);

        Assert.Equal("Bad descriptor", InvalidAgentCardItem.ComposeReason(entry));
    }

    [Fact]
    public void ComposeReason_FallbackToDirectory_WhenNoError()
    {
        var entry = new InvalidPluginEntry("C:\\plugins\\broken", null, null,
            new PluginValidationResult([]));

        Assert.Equal("Plugin directory: C:\\plugins\\broken", InvalidAgentCardItem.ComposeReason(entry));
    }
}
