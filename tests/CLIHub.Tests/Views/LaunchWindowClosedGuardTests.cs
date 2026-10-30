namespace CLIHub.Tests.Views;

using System.Threading;
using System.Windows;
using System.Windows.Interop;

using static CLIHub.Tests.Support.TestHelpers;

/// <summary>
///   Regression proof for the launch window closed guard (change
///   <c>launch-window-closed-guard</c>), on the narrowest seam that works in this suite.
///   Constructing a real <see cref="Views.LaunchWindow" /> without an <see cref="Application" />
///   is not possible: its XAML root element resolves <c>StaticResource Sizing.FontFamily.Text</c>
///   from application-level merged dictionaries, so <c>InitializeComponent</c> throws
///   <c>XamlParseException</c> in the Application-less test host (task 2.4 fallback; the
///   window-level guard behavior is covered by the manual smoke in the change's task 3.3). What
///   is proven here is the WPF behavior the guard is built on: a really closed window raises
///   <c>Closed</c> exactly once, and the interop call <c>WindowPositioner.Place</c> makes throws
///   the production <see cref="InvalidOperationException" /> on that closed window while
///   succeeding on an open one.
/// </summary>
public sealed class LaunchWindowClosedGuardTests
{
    [Fact]
    public void EnsureHandle_AfterWindowClosed_ThrowsInvalidOperationException()
    {
        RunOnSta(() =>
        {
            var window = new Window();

            // The interop call WindowPositioner.Place makes succeeds while the window is open:
            // this is the state every normal show request runs in.
            Assert.Null(Record.Exception(() => new WindowInteropHelper(window).EnsureHandle()));

            CloseForReal(window);

            // After the real close — the state the window reaches during application shutdown —
            // the same call throws the production InvalidOperationException that escapes an
            // unguarded show request and kills the process.
            var exception = Record.Exception(() => new WindowInteropHelper(window).EnsureHandle());

            Assert.IsType<InvalidOperationException>(exception);
        }, "STA launch window test failed.");
    }

    [Fact]
    public void Closed_AfterWindowReallyCloses_RunsExactlyOnce()
    {
        RunOnSta(() =>
        {
            var closedCount = 0;
            var window = new Window();
            window.Closed += (_, _) => closedCount++;

            CloseForReal(window);

            // Closed runs exactly once for a real close, which is why LaunchWindow.OnClosed is
            // the single funnel that can mark the window as no longer showable.
            Assert.Equal(1, closedCount);
        }, "STA launch window test failed.");
    }

    /// <summary>
    ///   Closes the window for real: for a directly constructed, never-shown <see cref="Window" />
    ///   <c>Close</c> is synchronous and puts the window into the closed state in which handle
    ///   creation throws.
    /// </summary>
    /// <param name="window"> The window to close. </param>
    private static void CloseForReal(Window window) => window.Close();
}
