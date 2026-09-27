## MVP Scope - Minimum Viable Product

This change has been scoped down to MVP. The following task groups are deferred to future changes:

**Deferred (not in MVP):**
- Task 4: Logging Infrastructure (5 tasks)
- Task 5: Application Lifecycle DI container (5 tasks)  
- Task 6: Project Management Service (5 tasks)
- Task 9: Hotkey Manager Service (6 tasks)
- Task 10: Update Service (6 tasks)
- Task 12: Launch Window UI (6 tasks)
- Task 13: Settings Window UI (6 tasks)
- Task 14: Application Resources (4 tasks)
- Task 15: Integration Testing (6 tasks)

**MVP includes:** Tasks 1-3, 7-8, 11 (partial: icon + basic menu only)

---

## 1. Project Setup ✅ COMPLETE

- [x] 1.1 Create WPF application project CLIHub.csproj targeting net8.0-windows and verify project file exists
- [x] 1.2 Add NuGet dependencies (Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Serilog, Serilog.Sinks.File, H.NotifyIcon.Wpf, System.Text.Json) and verify packages restore successfully
- [x] 1.3 Create project directory structure (Models/, Services/, Windows/, Resources/) and verify folders exist

## 2. Core Models ✅ COMPLETE

- [x] 2.1 Create Plugin model with id, name, description, commands, logoPath properties and verify model compiles
- [x] 2.2 Create Project model with id, name, path, isFavorite, lastUsed properties and verify model compiles
- [x] 2.3 Create AppConfig model with projects list, preferences, currentProjectId and verify model compiles
- [x] 2.4 Create PluginCommand model with name, executable, arguments properties and verify model compiles

## 3. Configuration Service ✅ COMPLETE

- [x] 3.1 Create IConfigService interface with Load, Save, GetCurrentProject, SetCurrentProject methods and verify interface compiles
- [x] 3.2 Implement ConfigService with JSON serialization to %APPDATA%\CLIHub\config.json and verify config file is created on first save
- [x] 3.3 Implement atomic file write (write to temp, rename) and verify config.json remains valid after simulated crash
- [x] 3.4 Add default config generation for first run and verify empty config.json is created with expected structure
- [x] 3.5 Add schema validation with error handling for invalid JSON and verify invalid config.json falls back to defaults

## 4. Logging Infrastructure (DEFERRED)

## 5. Application Lifecycle (DEFERRED)

## 6. Project Management Service (DEFERRED)

## 7. Plugin System

- [x] 7.1 Create IPluginManager interface with LoadPlugins, GetAllPlugins, GetPluginById methods and verify interface compiles
- [x] 7.2 Implement PluginManager with plugin.json loading from %APPDATA%\CLIHub\plugins\{plugin-id}\ subdirectories and verify plugins are discovered
- [x] 7.3 Add plugin.json schema validation (required: id, name, commands) and verify invalid plugins are skipped with logged warnings
- [x] 7.4 Implement duplicate plugin ID detection and rejection and verify second plugin with same ID is rejected
- [x] 7.5 Add logo.png loading with default placeholder fallback and verify missing logo.png uses placeholder

## 8. Process Launcher Service

- [x] 8.1 Create IProcessLauncher interface with LaunchAgent method accepting plugin, project, command parameters and verify interface compiles
- [x] 8.2 Implement ProcessLauncher with Windows Terminal integration via wt.exe -d and verify terminal session spawns in correct directory
- [x] 8.3 Add command argument injection from plugin.json and verify arguments are passed to spawned process
- [x] 8.4 Implement launch error handling for missing executables and permissions and verify errors are logged and user is notified
- [x] 8.5 Add configurable terminal executable preference (default: wt.exe) and verify custom terminal path works

## 9. Hotkey Manager Service (DEFERRED)

## 10. Update Service (DEFERRED)

## 11. System Tray Integration (MVP SCOPE)

- [x] 11.1 Add H.NotifyIcon.Wpf TaskbarIcon to App.xaml with CLIHub icon and tooltip and verify tray icon appears on startup
- [x] 11.2 Create tray icon context menu with "Launch", "Exit" items and verify right-click shows menu
- [x] 11.3 Wire left-click to show Launch Window and verify window appears and activates
- [ ] 11.4 (DEFERRED) Add "Current Project" display in context menu - depends on Project Management Service (Group 6, deferred)

## 12. Launch Window UI (DEFERRED)

## 13. Settings Window UI (DEFERRED)

## 14. Application Resources (DEFERRED)

## 15. Integration Testing (DEFERRED)