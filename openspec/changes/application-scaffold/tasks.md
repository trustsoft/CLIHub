## 1. Project Setup

- [x] 1.1 Create WPF application project CLIHub.csproj targeting net8.0-windows and verify project file exists
- [x] 1.2 Add NuGet dependencies (Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Serilog, Serilog.Sinks.File, H.NotifyIcon.Wpf, System.Text.Json) and verify packages restore successfully
- [x] 1.3 Create project directory structure (Models/, Services/, Windows/, Resources/) and verify folders exist

## 2. Core Models

- [x] 2.1 Create Plugin model with id, name, description, commands, logoPath properties and verify model compiles
- [x] 2.2 Create Project model with id, name, path, isFavorite, lastUsed properties and verify model compiles
- [x] 2.3 Create AppConfig model with projects list, preferences, currentProjectId and verify model compiles
- [x] 2.4 Create PluginCommand model with name, executable, arguments properties and verify model compiles

## 3. Configuration Service

- [x] 3.1 Create IConfigService interface with Load, Save, GetCurrentProject, SetCurrentProject methods and verify interface compiles
- [x] 3.2 Implement ConfigService with JSON serialization to %APPDATA%\CLIHub\config.json and verify config file is created on first save
- [x] 3.3 Implement atomic file write (write to temp, rename) and verify config.json remains valid after simulated crash
- [ ] 3.3 Implement atomic file write (write to temp, rename) and verify config.json remains valid after simulated crash
- [ ] 3.4 Add default config generation for first run and verify empty config.json is created with expected structure
- [ ] 3.5 Add schema validation with error handling for invalid JSON and verify invalid config.json falls back to defaults

## 4. Logging Infrastructure

- [ ] 4.1 Configure Serilog with file sink to %APPDATA%\CLIHub\logs\clihub-{Date}.log and verify log file is created
- [ ] 4.2 Set up daily log rotation with 7-day retention and verify old log files are deleted after 7 days
- [ ] 4.3 Configure log format with timestamp, level, message and verify log entries match expected format
- [ ] 4.4 Set log level to Information and filter Microsoft.*/System.* to Warning and verify framework noise is suppressed
- [ ] 4.5 Add ILogger injection support via DI container and verify services receive ILogger<T> instances

## 5. Application Lifecycle

- [ ] 5.1 Implement named mutex single-instance check (Global\CLIHub) in App.xaml.cs and verify second instance exits immediately
- [ ] 5.2 Add named pipe IPC for second instance signaling and verify first instance receives activation signal
- [ ] 5.3 Create %APPDATA%\CLIHub directory structure (logs/, plugins/, cache/) on startup and verify directories are created
- [ ] 5.4 Configure DI container in App.OnStartup with all service registrations and verify services resolve correctly
- [ ] 5.5 Implement graceful shutdown with mutex release and log flush and verify resources are cleaned up on exit

## 6. Project Management Service

- [ ] 6.1 Create IProjectService interface with AddProject, RemoveProject, GetRecentProjects, SetCurrentProject, ToggleFavorite methods and verify interface compiles
- [ ] 6.2 Implement ProjectService with in-memory project list backed by ConfigService and verify projects persist to config.json
- [ ] 6.3 Add project registration with unique ID generation and metadata capture and verify new projects get assigned IDs
- [ ] 6.4 Implement recent projects tracking with lastUsed timestamp updates and verify projects are sorted by lastUsed
- [ ] 6.5 Add favorite projects toggle and retrieval and verify isFavorite flag persists correctly

## 7. Plugin System

- [ ] 7.1 Create IPluginManager interface with LoadPlugins, GetAllPlugins, GetPluginById, ReloadPlugins methods and verify interface compiles
- [ ] 7.2 Implement PluginManager with plugin.json loading from %APPDATA%\CLIHub\plugins\{plugin-id}\ subdirectories and verify plugins are discovered
- [ ] 7.3 Add plugin.json schema validation (required: id, name, commands) and verify invalid plugins are skipped with logged warnings
- [ ] 7.4 Implement duplicate plugin ID detection and rejection and verify second plugin with same ID is rejected
- [ ] 7.5 Add logo.png loading with default placeholder fallback and verify missing logo.png uses placeholder
- [ ] 7.6 Implement plugin reload without restart and verify new plugin subdirectories are detected after reload

## 8. Process Launcher Service

- [ ] 8.1 Create IProcessLauncher interface with LaunchAgent method accepting plugin, project, command parameters and verify interface compiles
- [ ] 8.2 Implement ProcessLauncher with Windows Terminal integration via wt.exe -d and verify terminal session spawns in correct directory
- [ ] 8.3 Add command argument injection from plugin.json and verify arguments are passed to spawned process
- [ ] 8.4 Implement launch error handling for missing executables and permissions and verify errors are logged and user is notified
- [ ] 8.5 Add configurable terminal executable preference (default: wt.exe) and verify custom terminal path works
- [ ] 8.6 Add process launch event logging with agent name, project path, command and verify successful/failed launches are logged

## 9. Hotkey Manager Service

- [ ] 9.1 Create IHotkeyManager interface with RegisterHotkey, UnregisterHotkey methods and verify interface compiles
- [ ] 9.2 Implement HotkeyManager with Win32 RegisterHotKey API and verify hotkey registration succeeds
- [ ] 9.3 Set default hotkey to Ctrl+Shift+A and verify default hotkey is registered on startup
- [ ] 9.4 Add WndProc message handling for WM_HOTKEY and verify hotkey press shows Launch Window
- [ ] 9.5 Implement hotkey unregistration on shutdown and verify hotkey is released on application exit
- [ ] 9.6 Add hotkey validation requiring at least one modifier key and verify invalid hotkeys are rejected

## 10. Update Service

- [ ] 10.1 Create IUpdateService interface with CheckForUpdatesAsync, GetCurrentVersion methods and verify interface compiles
- [ ] 10.2 Implement UpdateService with version checking (placeholder for Velopack integration) and verify check runs without errors
- [ ] 10.3 Add asynchronous update check on application startup and verify check runs in background without blocking UI
- [ ] 10.4 Implement update notification logging when update is available and verify available updates are logged
- [ ] 10.5 Add manual update check trigger and verify Settings Window can initiate check on demand
- [ ] 10.6 Implement network error handling for failed update checks and verify failures are logged gracefully

## 11. System Tray Integration

- [ ] 11.1 Add H.NotifyIcon.Wpf TaskbarIcon to App.xaml with CLIHub icon and tooltip and verify tray icon appears on startup
- [ ] 11.2 Create tray icon context menu with "Launch", "Settings", "Exit" items and verify right-click shows menu
- [ ] 11.3 Wire left-click to show Launch Window and verify window appears and activates
- [ ] 11.4 Add "Current Project" display in context menu and verify selected project name appears
- [ ] 11.5 Add "Recent Projects" submenu with recent project list and verify recently used projects appear
- [ ] 11.6 Add dynamic agent launch menu items based on loaded plugins and verify plugins appear in menu for current project

## 12. Launch Window UI

- [ ] 12.1 Create LaunchWindow.xaml with project selector ComboBox and agent list and verify window displays
- [ ] 12.2 Bind project selector to IProjectService.GetAllProjects and verify projects populate dropdown
- [ ] 12.3 Add agent list display from IPluginManager.GetAllPlugins with logo thumbnails and verify plugins display with logos
- [ ] 12.4 Implement agent launch button click handler calling IProcessLauncher.LaunchAgent and verify button spawns terminal session
- [ ] 12.5 Add window show/hide coordination with tray icon and verify closing window hides it instead of exiting app
- [ ] 12.6 Implement keyboard navigation and Enter key handling for quick launch and verify Enter launches selected agent

## 13. Settings Window UI

- [ ] 13.1 Create SettingsWindow.xaml with preferences section and verify window displays
- [ ] 13.2 Add terminal executable path configuration textbox and verify custom path persists to config.json
- [ ] 13.3 Add hotkey configuration field and verify hotkey changes save to config (restart required)
- [ ] 13.4 Add version display label showing current application version and verify version appears
- [ ] 13.5 Add "Check for Updates" button wired to IUpdateService.CheckForUpdatesAsync and verify manual check triggers
- [ ] 13.6 Add "Reload Plugins" button calling IPluginManager.ReloadPlugins and verify plugins refresh without restart

## 14. Application Resources

- [ ] 14.1 Add CLIHub application icon (.ico) to Resources/ and set as application icon and verify icon appears in taskbar
- [ ] 14.2 Create default plugin logo placeholder image and verify missing plugin logos use placeholder
- [ ] 14.3 Add basic WPF styles for buttons, textboxes, comboboxes and verify UI elements have consistent appearance
- [ ] 14.4 Create DataTemplate for plugin list items with logo and name display and verify plugin list items render correctly

## 15. Integration Testing

- [ ] 15.1 Verify single-instance enforcement by launching app twice and confirming second instance exits
- [ ] 15.2 Verify config persistence by adding a project, restarting, and confirming project remains
- [ ] 15.3 Verify plugin loading by placing a plugin subdirectory with plugin.json and confirming it appears in Launch Window
- [ ] 15.4 Verify process launch by launching an agent and confirming Windows Terminal opens in correct directory
- [ ] 15.5 Verify hotkey activation by pressing Ctrl+Shift+A and confirming Launch Window appears
- [ ] 15.6 Verify logging by checking %APPDATA%\CLIHub\logs\ contains log files with expected entries
