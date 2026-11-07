namespace CLIHub.Tests.Controls;

using System.Threading;
using System.Windows.Automation;
using System.Windows.Automation.Peers;

using CLIHub.Controls;
using static CLIHub.Tests.Support.TestHelpers;

public sealed class SummaryCardTests
{
    [Fact]
    public void Constructor_DefaultValueTone_IsNeutral()
    {
        RunOnSta(() =>
        {
            var card = new SummaryCard();

            Assert.Equal(SummaryCardTone.Neutral, card.ValueTone);
        }, "STA control test failed.");
    }

    [Fact]
    public void Constructor_DefaultBadgeVisibility_IsFalse()
    {
        RunOnSta(() =>
        {
            var card = new SummaryCard();

            Assert.False(card.IsBadgeVisible);
        }, "STA control test failed.");
    }

    [Fact]
    public void GetName_WithBadgeVisible_IncludesStatusText()
    {
        RunOnSta(() =>
        {
            var card = new SummaryCard
            {
                Label = "PROJECTS",
                Value = "14",
                Detail = "In the launch window",
                IsBadgeVisible = true,
                BadgeTooltip = "1 folder missing · folders that no longer exist",
                BadgeText = "1 missing"
            };

            var peer = UIElementAutomationPeer.CreatePeerForElement(card);

            Assert.NotNull(peer);
            Assert.Equal(
                "PROJECTS 14, In the launch window, 1 folder missing · folders that no longer exist",
                peer.GetName());
        }, "STA control test failed.");
    }

    [Fact]
    public void GetName_WithBadgeHidden_OmitsStatusText()
    {
        RunOnSta(() =>
        {
            var card = new SummaryCard
            {
                Label = "PROJECTS",
                Value = "14",
                Detail = "In the launch window",
                IsBadgeVisible = false,
                BadgeTooltip = "1 folder missing · folders that no longer exist",
                BadgeText = "1 missing"
            };

            var peer = UIElementAutomationPeer.CreatePeerForElement(card);

            Assert.NotNull(peer);
            Assert.Equal("PROJECTS 14, In the launch window", peer.GetName());
        }, "STA control test failed.");
    }

    [Fact]
    public void GetAutomationControlType_Always_ReturnsGroup()
    {
        RunOnSta(() =>
        {
            var card = new SummaryCard();

            var peer = UIElementAutomationPeer.CreatePeerForElement(card);

            Assert.NotNull(peer);
            Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        }, "STA control test failed.");
    }

    [Fact]
    public void GetClassName_Always_ReturnsSummaryCard()
    {
        RunOnSta(() =>
        {
            var card = new SummaryCard();

            var peer = UIElementAutomationPeer.CreatePeerForElement(card);

            Assert.NotNull(peer);
            Assert.Equal("SummaryCard", peer.GetClassName());
        }, "STA control test failed.");
    }
}
