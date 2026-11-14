namespace CLIHub.Tests.Controls;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

using CLIHub.Controls;

using static CLIHub.Tests.Support.TestHelpers;

public sealed class DecorativeGlyphTests
{
    [Fact]
    public void OnCreateAutomationPeer_DecorativeGlyph_ReturnsNull()
    {
        RunOnSta(() =>
        {
            var glyph = new DecorativeGlyph();

            var peer = UIElementAutomationPeer.CreatePeerForElement(glyph);

            Assert.Null(peer);
        }, "STA control test failed.");
    }

    [Fact]
    public void OnCreateAutomationPeer_PlainTextBlock_ReturnsNonNull()
    {
        RunOnSta(() =>
        {
            var textBlock = new TextBlock();

            var peer = UIElementAutomationPeer.CreatePeerForElement(textBlock);

            Assert.NotNull(peer);
        }, "STA control test failed.");
    }
}
