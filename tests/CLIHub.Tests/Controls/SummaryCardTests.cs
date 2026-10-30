namespace CLIHub.Tests.Controls;

using System.Threading;

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
    public void Constructor_DefaultAlertVisibility_IsFalse()
    {
        RunOnSta(() =>
        {
            var card = new SummaryCard();

            Assert.False(card.IsAlertVisible);
        }, "STA control test failed.");
    }
}
