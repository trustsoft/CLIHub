namespace CLIHub.Tests.Views;

using static CLIHub.Tests.Support.TestHelpers;

/// <summary>
///   Guards the Browse Plugins dialog wiring: the CLI Agents page entry point (the plugin
/// Actions menu fed by <c>PluginActions</c>, whose Browse entry carries
/// <c>BrowsePluginsCommand</c> per AgentManagementViewModelTests.PluginActions) and the per-row
/// action/status/homepage bindings, closing the gap where the ViewModel was tested before the UI
/// was wired.
/// </summary>
public sealed class BrowsePluginsDialogBindingTests
{
    [Fact]
    public void SettingsAgentsPage_BindsPluginActionsMenu()
    {
        var root = FindRepositoryRoot();
        var pagePath = Path.Combine(root, "src", "CLIHub", "Views", "SettingsAgentsPage.xaml");
        Assert.True(File.Exists(pagePath), $"Expected SettingsAgentsPage.xaml at '{pagePath}'.");

        var content = File.ReadAllText(pagePath);

        // The Browse Plugins entry point is an entry of the data-driven Actions menu: the menu is
        // fed by PluginActions, each entry button binds the entry's Command through the shared
        // entry style, and a click closes the menu after the command runs.
        Assert.Contains("ItemsSource=\"{Binding AgentsState.PluginActions}\"", content);
        Assert.Contains("Style=\"{StaticResource ActionsMenuEntryButton}\"", content);
        Assert.Contains("Command=\"{Binding Command}\"", content);
        Assert.Contains("Click=\"OnMenuActionInvoked\"", content);
    }

    [Fact]
    public void BrowsePluginsDialog_BindsActionStatusAndHomepage()
    {
        var root = FindRepositoryRoot();
        var dialogPath = Path.Combine(root, "src", "CLIHub", "Views", "BrowsePluginsDialog.xaml");
        Assert.True(File.Exists(dialogPath), $"Expected BrowsePluginsDialog.xaml at '{dialogPath}'.");

        var content = File.ReadAllText(dialogPath);

        // The action button is wired to the InstallCommand and collapses for up-to-date plugins.
        Assert.Contains("Command=\"{Binding DataContext.InstallCommand, RelativeSource={RelativeSource AncestorType=ItemsControl}}\"", content);
        Assert.Contains("Content=\"{Binding ActionText}\"", content);
        Assert.Contains("Visibility=\"{Binding ShowActionButton, Converter={StaticResource BoolToVisibility}}\"", content);

        // The status badge renders StatusText; its hard-coded Collapsed visibility is gone.
        var statusIndex = content.IndexOf("Text=\"{Binding StatusText}\"", StringComparison.Ordinal);
        Assert.True(statusIndex >= 0, "Expected StatusText to be bound.");
        var statusWindow = content[(statusIndex - 500)..statusIndex];
        Assert.DoesNotContain("Visibility=\"Collapsed\"", statusWindow, StringComparison.Ordinal);

        // The homepage (and its separator) only render when a homepage is present.
        Assert.Contains("Visibility=\"{Binding HasHomepage, Converter={StaticResource BoolToVisibility}}\"", content);
        Assert.Contains("Text=\"{Binding Homepage}\"", content);
    }
}
