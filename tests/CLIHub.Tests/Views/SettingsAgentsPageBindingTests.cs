namespace CLIHub.Tests.Views;

using static CLIHub.Tests.Support.TestHelpers;

/// <summary>
///   Guards the disabled-plugin section bindings in SettingsAgentsPage.xaml: verifies that
///   DisabledCards, HasDisabledPlugins, and EnablePluginCommand are bound, closing the gap
///   where only the ViewModel was tested before the UI was wired.
/// </summary>
public sealed class SettingsAgentsPageBindingTests
{
    [Fact]
    public void SettingsAgentsPage_BindsDisabledPluginProperties()
    {
        var root = FindRepositoryRoot();
        var pagePath = Path.Combine(root, "src", "CLIHub", "Views", "SettingsAgentsPage.xaml");
        Assert.True(File.Exists(pagePath), $"Expected SettingsAgentsPage.xaml at '{pagePath}'.");

        var content = File.ReadAllText(pagePath);

        // DisabledCards must be bound in an ItemsControl
        Assert.Contains("ItemsSource=\"{Binding AgentsState.DisabledCards}\"", content);

        // HasDisabledPlugins must be bound to visibility (for the Disabled section and header)
        Assert.Contains("Visibility=\"{Binding AgentsState.HasDisabledPlugins", content);

        // EnablePluginCommand must be bound to a button command
        Assert.Contains("Command=\"{Binding DataContext.AgentsState.EnablePluginCommand", content);
    }

    [Fact]
    public void SettingsWindow_BindsPluginCatalogSummary()
    {
        var root = FindRepositoryRoot();
        var windowPath = Path.Combine(root, "src", "CLIHub", "Views", "SettingsWindow.xaml");
        Assert.True(File.Exists(windowPath), $"Expected SettingsWindow.xaml at '{windowPath}'.");

        var content = File.ReadAllText(windowPath);

        // PluginCatalogSummary must be bound in the AGENTS summary card
        Assert.Contains("Detail=\"{Binding AgentsState.PluginCatalogSummary}\"", content);
    }
}
