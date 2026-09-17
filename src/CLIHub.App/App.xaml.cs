using CLIHub.App.Interop;
using System.Reflection;
using CLIHub.App.Platform;
using CLIHub.App.ViewModels;
using CLIHub.App.Views;
using CLIHub.Core.Abstractions;
using CLIHub.Core.Services;
using CLIHub.Core.Models;
using Velopack;

namespace CLIHub.App;

public partial class App : System.Windows.Application
{
    private HotkeyManager? _hotkeyManager;
    private H.NotifyIcon.TaskbarIcon? _trayIcon;
    private System.Windows.Forms.ContextMenuStrip? _trayMenu;
    private PopupWindow? _popupWindow;
    private SettingsWindow? _settingsWindow;
    private SettingsStore? _settingsStore;
    private UpdateService? _updateService;
    private System.Windows.Forms.ToolStripMenuItem? _updateMenuItem;
    private string? _readyVersion;

    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build()
            .SetAutoApplyOnStartup(false)
            .Run();

        System.Windows.Forms.Application.EnableVisualStyles();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

        var fileSystem = new PhysicalFileSystem();
        var paths = new SystemPathProvider();
        var processRunner = new SystemProcessRunner();

        var settingsStore = new SettingsStore(fileSystem, paths);
        _settingsStore = settingsStore;
        var projectsStore = new JsonDocumentStore<ProjectsDocument>(fileSystem, paths, ProjectsDocument.FileName);
        var agentsStore = new JsonDocumentStore<AgentsDocument>(fileSystem, paths, AgentsDocument.FileName);
        var pluginLoader = new PluginLoader(fileSystem, paths);
        var registry = new ProjectRegistry(projectsStore, fileSystem);
        var launcher = new LauncherCore(processRunner);
        var detector = new AgentDetector(settingsStore, agentsStore, fileSystem, processRunner, new SystemClock());
        var logoResolver = new LogoResolver(fileSystem);
        var logoImages = new LogoImageService();

        var plugins = pluginLoader.Load();

        _popupWindow = new PopupWindow();
        var viewModel = new PopupViewModel(
            registry,
            settingsStore,
            plugins.Agents,
            detector,
            launcher,
            logoResolver,
            logoImages,
            _popupWindow.PickFolder,
            _popupWindow.Confirm,
            action => Dispatcher.Invoke(action),
            ResolveAppVersion(),
            () => processRunner.StartDetached("explorer.exe", $"\"{paths.ConfigDirectory}\"", string.Empty),
            ShowSettings);
        _popupWindow.DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => _popupWindow?.Hide();
        viewModel.ExitRequested += (_, _) => Shutdown();
        new System.Windows.Interop.WindowInteropHelper(_popupWindow).EnsureHandle();

        CreateTrayIcon();
        RegisterHotkey(settingsStore.Hotkey);
        RegisterUpdateCheck(settingsStore);

        detector.RoundCompleted += viewModel.OnProbeRoundCompleted;
        _ = System.Threading.Tasks.Task.Run(() => detector.RunStartupRound(
            plugins.Agents.Select(plugin => plugin.Manifest).ToArray()));

        if (plugins.Warnings.Count > 0)
        {
            System.Windows.MessageBox.Show(
                string.Join(Environment.NewLine, plugins.Warnings),
                "CLIHub — plugin warnings",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    internal static string ResolveAppVersion()
    {
        try
        {
            var manager = VelopackUpdateClient.CreateManager();
            if (manager.IsInstalled)
            {
                var current = manager.CurrentVersion?.ToString();
                if (!string.IsNullOrEmpty(current))
                {
                    return current;
                }
            }
        }
        catch (Exception)
        {
        }

        var informational = System.Reflection.Assembly.GetEntryAssembly()?
            .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        return string.IsNullOrWhiteSpace(informational)
            ? "dev"
            : informational.Split('+')[0];
    }

    private void CreateTrayIcon()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();

        _updateMenuItem = new System.Windows.Forms.ToolStripMenuItem("Install update")
        {
            Visible = false
        };
        _updateMenuItem.Click += (_, _) => _updateService?.Apply();
        menu.Items.Add(_updateMenuItem);

        var settingsItem = new System.Windows.Forms.ToolStripMenuItem("Settings");
        settingsItem.Click += (_, _) => ShowSettings();
        menu.Items.Add(settingsItem);

        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);

        _trayMenu = menu;

        _trayIcon = new H.NotifyIcon.TaskbarIcon
        {
            ToolTipText = "CLIHub",
            Icon = System.Drawing.SystemIcons.Application
        };
        _trayIcon.TrayRightMouseUp += (_, _) => ShowTrayMenu();

        _trayIcon.ForceCreate(false);
    }

    private void ShowTrayMenu()
    {
        if (_trayMenu is null)
        {
            return;
        }

        _trayMenu.Show(System.Windows.Forms.Cursor.Position);
        NativeMethods.SetForegroundWindow(_trayMenu.Handle);
    }

    private void ShowSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        var window = new SettingsWindow();
        var viewModel = new SettingsViewModel(
            _settingsStore ?? throw new InvalidOperationException("Settings are not initialized."),
            TryApplyHotkey,
            () => _updateService!.CheckNowAsync(),
            action => Dispatcher.Invoke(action));
        if (_readyVersion is not null)
        {
            viewModel.OnUpdateReady(_readyVersion);
        }

        window.DataContext = viewModel;
        window.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow = window;
        window.Show();
    }

    private string? TryApplyHotkey(string hotkey)
    {
        if (_hotkeyManager is null || _settingsStore is null)
        {
            return "Hotkey is not initialized yet.";
        }

        if (hotkey == _settingsStore.Hotkey)
        {
            return null;
        }

        _hotkeyManager.Unregister();
        if (_hotkeyManager.TryRegister(hotkey, out var error))
        {
            return null;
        }

        _hotkeyManager.TryRegister(_settingsStore.Hotkey, out _);
        return error ?? $"Failed to register hotkey '{hotkey}'.";
    }

    private void RegisterHotkey(string hotkey)
    {
        if (_hotkeyManager is null)
        {
            _hotkeyManager = new HotkeyManager();
            _hotkeyManager.Pressed += () => _popupWindow?.ShowForHotkey();
        }

        if (!_hotkeyManager.TryRegister(hotkey, out var error))
        {
            System.Windows.MessageBox.Show(
                error ?? $"Failed to register hotkey '{hotkey}'.",
                "CLIHub — hotkey",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    private void RegisterUpdateCheck(SettingsStore settingsStore)
    {
        var updateService = new UpdateService(new VelopackUpdateClient(), settingsStore);
        _updateService = updateService;
        updateService.UpdateReady += version => Dispatcher.Invoke(() => OnUpdateReady(version));

        _ = System.Threading.Tasks.Task.Run(() => updateService.RunStartupCheckAsync());
    }

    private void OnUpdateReady(string version)
    {
        _readyVersion = string.IsNullOrWhiteSpace(version) ? "" : version;

        if (_settingsWindow?.DataContext is ViewModels.SettingsViewModel settingsViewModel)
        {
            settingsViewModel.OnUpdateReady(version);
        }

        if (_updateMenuItem is not null)
        {
            _updateMenuItem.Text = string.IsNullOrWhiteSpace(version)
                ? "Install update"
                : $"Install update {version}";
            _updateMenuItem.Visible = true;
        }

        ShowUpdateNotification(version);
    }

    private void ShowUpdateNotification(string version)
    {
        var message = string.IsNullOrWhiteSpace(version)
            ? "A CLIHub update is available. Click to install and restart."
            : $"CLIHub {version} is available. Click to install and restart.";

        _trayIcon?.ShowNotification("CLIHub", message);
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        _trayMenu?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
