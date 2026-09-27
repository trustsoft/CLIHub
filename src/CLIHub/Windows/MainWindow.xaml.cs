using CLIHub.Core.Services;
using CLIHub.Core.Interfaces;
using System.Windows;
using System.IO;

namespace CLIHub.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IPluginManager _pluginManager;
    private readonly IProcessLauncher _processLauncher;
    private readonly IConfigService _configService;

    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize core services
        _pluginManager = new PluginManager();
        _processLauncher = new ProcessLauncher();
        _configService = new ConfigService();
        
        // Load plugins on startup
        LoadPlugins();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Hide to tray instead of closing the application
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }

    private void LoadPlugins()
    {
        try
        {
            _pluginManager.LoadPlugins();
            var plugins = _pluginManager.GetAllPlugins().ToList();
            
            if (plugins.Count > 0)
            {
                StatusText.Text = $"Loaded {plugins.Count} plugin(s) - Ready to launch AI agents";
                PluginListText.Text = string.Join("\n", plugins.Select(p => $"- {p.Name}"));
            }
            else
            {
                StatusText.Text = "No plugins found. Check %APPDATA%\\CLIHub\\plugins\\";
                PluginListText.Text = "No plugins loaded.";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error loading plugins: {ex.Message}";
            PluginListText.Text = "Error loading plugins.";
        }
    }

    private void LaunchPlugin_Click(object sender, RoutedEventArgs e)
    {
        // For MVP: Simple plugin launch demonstration
        var plugins = _pluginManager.GetAllPlugins().ToList();
        if (plugins.Count > 0)
        {
            var plugin = plugins.First();
            var command = plugin.Commands?.FirstOrDefault();
            
            if (command != null)
            {
                var currentDir = Directory.GetCurrentDirectory();
                var success = _processLauncher.LaunchProcess(command, currentDir);
                
                StatusText.Text = success 
                    ? $"Launched: {plugin.Name} - {command.Name}" 
                    : $"Failed to launch {plugin.Name}";
            }
        }
        else
        {
            StatusText.Text = "No plugins available to launch";
        }
    }
}