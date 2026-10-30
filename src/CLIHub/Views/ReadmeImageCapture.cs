#if DEBUG
namespace CLIHub.Views;

using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using CLIHub.ViewModels;

/// <summary>
///   Debug-only README image capture. When <c>CLIHUB_CAPTURE_DIR</c> is set, the application renders
///   the launch window and the three screenshot Settings pages at 1.5× and exits. The capture runs
///   in-process so it works on a locked or headless desktop, where screen-copy and PrintWindow
///   return black frames.
/// </summary>
internal static class ReadmeImageCapture
{
    /// <summary>
    ///   Environment variable that switches the debug build into README capture mode.
    /// </summary>
    internal const string DirectoryVariable = "CLIHUB_CAPTURE_DIR";

    private const double Scale = 1.5;
    private const int SettleDelayMilliseconds = 350;

    /// <summary>
    ///   Starts capture mode when the environment variable is set.
    /// </summary>
    /// <param name="application"> The running application. </param>
    /// <returns> True when capture mode was requested and scheduled. </returns>
    public static bool TryStart(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);

        var directory = Environment.GetEnvironmentVariable(DirectoryVariable);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return false;
        }

        application.Dispatcher.BeginInvoke(
            new Action(() => _ = CaptureAsync(application, directory)),
            DispatcherPriority.ApplicationIdle);

        return true;
    }

    private static async Task CaptureAsync(Application application, string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);

            if (application.MainWindow is not LaunchWindow launchWindow
                || launchWindow.DataContext is not LaunchWindowViewModel launchViewModel)
            {
                throw new InvalidOperationException("The launch window is not available for capture.");
            }

            await WaitUntilAsync(
                () => launchViewModel.Projects.Count >= 6 && launchViewModel.Agents.Count > 0,
                TimeSpan.FromSeconds(30),
                "launch window data");

            try
            {
                await WaitUntilAsync(
                    () => launchViewModel.Agents.All(agent => !string.IsNullOrEmpty(agent.Version)),
                    TimeSpan.FromSeconds(30),
                    "agent versions");
            }
            catch (TimeoutException)
            {
                // Capture whatever versions resolved; a missing agent CLI is not a capture failure.
            }

            await Task.Delay(SettleDelayMilliseconds);
            Capture(launchWindow, Path.Combine(directory, "launch-window.png"));

            launchViewModel.OpenSettingsCommand.Execute(null);

            var settingsWindow = await WaitForWindowAsync<SettingsWindow>(TimeSpan.FromSeconds(15));
            if (settingsWindow.DataContext is not SettingsViewModel settingsViewModel)
            {
                throw new InvalidOperationException("The Settings window has no view model.");
            }

            await WaitUntilAsync(
                () => settingsViewModel.AgentsState.Cards.Count > 0 && !settingsViewModel.AgentsState.IsDetecting,
                TimeSpan.FromSeconds(30),
                "Settings agent cards");

            await CapturePageAsync(settingsWindow, settingsViewModel, "appearance", directory, "settings-appearance.png");
            await WaitUntilAsync(
                () => settingsViewModel.Scanning.WatchedDirectories.Count > 0,
                TimeSpan.FromSeconds(15),
                "watched directories");

            try
            {
                await WaitUntilAsync(
                    () => settingsViewModel.Scanning.WatcherStateLabel == "Watching",
                    TimeSpan.FromSeconds(15),
                    "watcher state");
            }
            catch (TimeoutException)
            {
                // An idle watcher is still a valid Settings screenshot.
            }

            await CapturePageAsync(settingsWindow, settingsViewModel, "projects", directory, "settings-discovery.png");
            await CapturePageAsync(settingsWindow, settingsViewModel, "agents", directory, "settings-agents.png");
        }
        catch (Exception exception)
        {
            try
            {
                Directory.CreateDirectory(directory);
                await File.WriteAllTextAsync(
                    Path.Combine(directory, "capture-error.txt"),
                    exception.ToString());
            }
            catch
            {
                // The process still exits; the harness reports the missing images.
            }
        }
        finally
        {
            application.Shutdown();
        }
    }

    private static async Task CapturePageAsync(
        SettingsWindow window,
        SettingsViewModel viewModel,
        string pageKey,
        string directory,
        string fileName)
    {
        viewModel.SelectedPage = viewModel.Pages.Single(page => page.Key == pageKey);
        await Task.Delay(SettleDelayMilliseconds * 2);
        window.UpdateLayout();
        Capture(window, Path.Combine(directory, fileName));
    }

    private static void Capture(Window window, string path)
    {
        window.UpdateLayout();

        var width = (int)Math.Ceiling(window.ActualWidth * Scale);
        var height = (int)Math.Ceiling(window.ActualHeight * Scale);
        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException($"The window has no renderable size: {window.ActualWidth}×{window.ActualHeight}.");
        }

        var bitmap = new RenderTargetBitmap(
            width,
            height,
            96 * Scale,
            96 * Scale,
            PixelFormats.Pbgra32);
        bitmap.Render(window);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static async Task<TWindow> WaitForWindowAsync<TWindow>(TimeSpan timeout)
        where TWindow : Window
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var window = Application.Current.Windows.OfType<TWindow>().FirstOrDefault();
            if (window is not null)
            {
                return window;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"{typeof(TWindow).Name} did not open within {timeout}.");
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout, string description)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Timed out waiting for {description}.");
    }
}
#endif
