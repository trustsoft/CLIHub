## Context

This is a greenfield WPF application with no existing codebase. The repository contains only vision documentation. The application requires Windows 10/11 system tray integration, process spawning capabilities, and a plugin-based architecture for supporting multiple AI agent CLI tools. Target framework is .NET 8 for long-term support and modern C# features.

## Goals / Non-Goals

**Goals:**
- Establish a maintainable WPF application structure with clear service boundaries
- Enable dependency injection for testability and loose coupling
- Provide robust logging for diagnostics and troubleshooting
- Create an extensible plugin system that supports adding new AI agents without code changes
- Ensure reliable single-instance application behavior
- Integrate with Windows Terminal for CLI tool sessions

**Non-Goals:**
- Building a full UI design system (basic functional UI only for this scaffold)
- Implementing actual AI agent plugins (only the loading infrastructure)
- Cross-platform support (Windows-only is explicit)
- In-app terminal emulation (delegate to Windows Terminal)
- Auto-update installation (checking only; Velopack integration deferred)

## Decisions

### Decision 1: WPF over Windows Forms or UWP

**Chosen:** WPF with .NET 8

**Rationale:**
- WPF provides mature system tray support via third-party libraries (H.NotifyIcon.Wpf)
- Better data binding and MVVM support than Windows Forms
- More flexible styling and theming than WinForms
- UWP/WinUI 3 has immature system tray support and deployment complexity
- .NET 8 LTS provides stability through 2026

**Alternatives considered:**
- Windows Forms: Rejected due to limited styling and legacy architecture
- WinUI 3: Rejected due to poor system tray support and packaging overhead
- Avalonia: Rejected to avoid introducing less mature framework for Windows-focused app

### Decision 2: Microsoft.Extensions.DependencyInjection for DI

**Chosen:** Microsoft.Extensions.DependencyInjection

**Rationale:**
- Standard Microsoft framework, consistent with .NET ecosystem
- Lightweight and sufficient for application-level DI
- Built-in support for singleton, transient, and scoped lifetimes
- Integrates naturally with Microsoft.Extensions.Logging

**Alternatives considered:**
- Autofac: Rejected as overkill for this application's DI needs
- No DI container: Rejected to avoid manual dependency wiring and poor testability

### Decision 3: Serilog for structured logging

**Chosen:** Serilog with file sink

**Rationale:**
- Structured logging enables better diagnostics than plain text logs
- File sink with rolling date provides automatic rotation
- Wide ecosystem and proven reliability
- Integrates with Microsoft.Extensions.Logging abstractions

**Alternatives considered:**
- NLog: Similar capability, but Serilog has better structured logging ergonomics
- Plain ILogger without Serilog: Rejected due to lack of file sink and rotation out of the box

### Decision 4: JSON plugin descriptors in subdirectories

**Chosen:** %APPDATA%\CLIHub\plugins\{plugin-id}\plugin.json + logo.png

**Rationale:**
- JSON is human-readable and easily editable by users
- Subdirectory structure keeps each plugin's assets isolated
- Logo files colocated with plugin metadata for easy management
- Simple file-based discovery without database overhead

**Alternatives considered:**
- Single plugins.json manifest: Rejected as harder to add/remove plugins manually
- XML descriptors: Rejected due to verbosity and poor ergonomics
- Compiled plugin DLLs: Rejected to avoid version conflicts and deployment complexity

### Decision 5: Named mutex for single-instance enforcement

**Chosen:** Named mutex with IPC signaling via named pipe

**Rationale:**
- Mutex is OS-level primitive, guaranteed single instance across all sessions
- Named pipe IPC allows second instance to signal first instance to show UI
- No external dependencies or services required

**Alternatives considered:**
- TCP socket: Rejected due to firewall and port allocation complexity
- Memory-mapped file: Rejected as more complex than mutex + pipe

### Decision 6: Windows Terminal integration via wt.exe

**Chosen:** Spawn processes via `wt.exe -d <project-path> <agent-command>`

**Rationale:**
- Windows Terminal is modern, well-maintained, and installed by default on Windows 11
- Simple command-line interface for spawning sessions
- Users see familiar terminal UI rather than in-app emulation
- Avoids complexity of embedding terminal control

**Alternatives considered:**
- ConPTY in-app terminal: Rejected due to complexity and maintenance burden
- CMD.exe: Rejected due to poor UX compared to Windows Terminal
- Configurable terminal: Supported as preference, but wt.exe is default

### Decision 7: H.NotifyIcon.Wpf for system tray

**Chosen:** H.NotifyIcon.Wpf NuGet package

**Rationale:**
- Modern, actively maintained WPF system tray library
- Native WPF integration with data binding support
- Clean API for context menus and icon management
- Better than legacy NotifyIcon from WinForms

**Alternatives considered:**
- Hardcodet.NotifyIcon.Wpf: Older, less actively maintained
- WinForms NotifyIcon: Rejected due to WinForms interop complexity in WPF

## Risks / Trade-offs

**[Risk] Windows Terminal not installed on Windows 10** → Mitigation: Detect wt.exe availability at startup; fall back to cmd.exe with warning, or allow user to configure custom terminal path in settings

**[Risk] Mutex not released on unclean shutdown** → Mitigation: Windows automatically releases mutex on process exit; no manual cleanup required

**[Risk] Plugin JSON schema drift over time** → Mitigation: Add schema version field to plugin.json; implement migration logic in PluginManager

**[Risk] %APPDATA% directory access permissions** → Mitigation: Catch IO exceptions during directory creation; log error and show user-friendly message if write fails

**[Trade-off] File-based config instead of registry** → Benefit: Easier backup and version control. Cost: No system-level persistence if %APPDATA% is cleared

**[Trade-off] No in-process plugin execution** → Benefit: Isolation and simplicity. Cost: Cannot extend UI or app behavior via plugins; plugins are descriptors only

**[Trade-off] Single-threaded UI with async service calls** → Benefit: Simpler threading model. Cost: Long-running operations must be carefully async to avoid freezing UI

## Migration Plan

This is the initial scaffold with no existing users, so no migration is required. Future deployment considerations:

1. **Build**: dotnet publish for self-contained win-x64 executable
2. **Packaging**: Future integration with Velopack for installer generation
3. **First run**: Application auto-creates %APPDATA%\CLIHub\ structure and default config.json
4. **Plugin installation**: Users manually copy plugin subdirectories to %APPDATA%\CLIHub\plugins\ or use future plugin manager UI

## Open Questions

**Q: Should we support portable mode (settings alongside exe)?**
→ Deferred. Default to %APPDATA%; revisit if users request portable deployment

**Q: Should plugins support custom environment variables?**
→ Deferred. Plugin.json schema can be extended later if needed

**Q: Should we validate project paths exist before allowing selection?**
→ Deferred. Initial implementation assumes paths are valid; validation can be added in project management refinement
