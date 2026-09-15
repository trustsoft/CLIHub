using CLIHub.App.Interop;
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
    private PopupWindow? _popupWindow;
    private UpdateService? _updateService;
    private System.Windows.Controls.MenuItem? _updateMenuItem;

    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build()
            .SetAutoApplyOnStartup(false)
            .Run();

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
            action => Dispatcher.Invoke(action));
        _popupWindow.DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => _popupWindow?.Hide();
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
                "CLIHub — предупреждения плагинов",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    private void CreateTrayIcon()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        _updateMenuItem = new System.Windows.Controls.MenuItem
        {
            Header = "Установить обновление",
            Visibility = System.Windows.Visibility.Collapsed
        };
        _updateMenuItem.Click += (_, _) => _updateService?.Apply();
        menu.Items.Add(_updateMenuItem);

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Выход" };
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);

        _trayIcon = new H.NotifyIcon.TaskbarIcon
        {
            ToolTipText = "CLIHub",
            Icon = System.Drawing.SystemIcons.Application,
            ContextMenu = menu
        };

        _trayIcon.ForceCreate(false);
    }

    private void RegisterHotkey(string hotkey)
    {
        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.Pressed += () => _popupWindow?.ShowForHotkey();

        if (!_hotkeyManager.TryRegister(hotkey, out var error))
        {
            System.Windows.MessageBox.Show(
                error ?? $"Не удалось зарегистрировать hotkey '{hotkey}'.",
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
        if (_updateMenuItem is not null)
        {
            _updateMenuItem.Header = string.IsNullOrWhiteSpace(version)
                ? "Установить обновление"
                : $"Установить обновление {version}";
            _updateMenuItem.Visibility = System.Windows.Visibility.Visible;
        }

        ShowUpdateNotification(version);
    }

    private void ShowUpdateNotification(string version)
    {
        var message = string.IsNullOrWhiteSpace(version)
            ? "Доступно обновление CLIHub. Нажмите, чтобы установить и перезапустить."
            : $"Доступно обновление CLIHub {version}. Нажмите, чтобы установить и перезапустить.";

        _trayIcon?.ShowNotification("CLIHub", message);
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
