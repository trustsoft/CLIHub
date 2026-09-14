using CLIHub.App.Interop;
using CLIHub.App.Platform;
using CLIHub.App.ViewModels;
using CLIHub.App.Views;
using CLIHub.Core.Abstractions;
using CLIHub.Core.Services;
using CLIHub.Core.Models;

namespace CLIHub.App;

public partial class App : System.Windows.Application
{
    private HotkeyManager? _hotkeyManager;
    private H.NotifyIcon.TaskbarIcon? _trayIcon;
    private PopupWindow? _popupWindow;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

        var fileSystem = new PhysicalFileSystem();
        var paths = new SystemPathProvider();
        var processRunner = new SystemProcessRunner();

        var configStore = new ConfigStore(fileSystem, paths);
        var pluginLoader = new PluginLoader(fileSystem, paths);
        var registry = new ProjectRegistry(configStore, fileSystem);
        var launcher = new LauncherCore(processRunner);
        var detector = new AgentDetector(configStore, fileSystem, processRunner, new SystemClock());

        var plugins = pluginLoader.Load();

        _popupWindow = new PopupWindow();
        var viewModel = new PopupViewModel(
            registry,
            plugins.Agents,
            detector,
            launcher,
            _popupWindow.PickFolder,
            _popupWindow.Confirm,
            action => Dispatcher.Invoke(action));
        _popupWindow.DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => _popupWindow?.Hide();
        new System.Windows.Interop.WindowInteropHelper(_popupWindow).EnsureHandle();

        CreateTrayIcon();
        RegisterHotkey(registry.Hotkey);

        detector.RoundCompleted += viewModel.OnProbeRoundCompleted;
        _ = System.Threading.Tasks.Task.Run(() => detector.RunStartupRound(plugins.Agents));

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

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
