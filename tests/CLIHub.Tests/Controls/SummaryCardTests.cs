namespace CLIHub.Tests.Controls;

using System.Threading;

using CLIHub.Controls;

public sealed class SummaryCardTests
{
    [Fact]
    public void Constructor_DefaultToneProperties_AreNeutral()
    {
        RunOnSta(() =>
        {
            var card = new SummaryCard();

            Assert.Equal(SummaryCardTone.Neutral, card.IconTone);
            Assert.Equal(SummaryCardTone.Neutral, card.ValueTone);
        });
    }

    [Fact]
    public void Constructor_DefaultAlertVisibility_IsFalse()
    {
        RunOnSta(() =>
        {
            var card = new SummaryCard();

            Assert.False(card.IsAlertVisible);
        });
    }

    private static void RunOnSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw new Xunit.Sdk.XunitException("STA control test failed.", exception);
        }
    }
}
