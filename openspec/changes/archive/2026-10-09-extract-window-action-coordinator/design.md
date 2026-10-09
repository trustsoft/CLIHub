# Design: Extract Window Action Coordinator

## Overview

Extract window-level and agent command coordination from LaunchWindowViewModel into a focused `WindowActionCoordinator`. This is the fourth coordinator in the launch window architecture, following StatusMessageCoordinator, MenuActionCoordinator, and LaunchCommandCoordinator.

**This is a pure refactoring.** No behavior changes, no new features. Goal: reduce LaunchWindowViewModel from 406 to ~300 lines (~26% reduction) by moving command ownership to a coordinator.

## Current State

LaunchWindowViewModel (406 lines) creates and owns 13 commands:

**Window-level commands** (created in constructor):
- `OpenDataFolderCommand` — opens AppPaths.Root in Explorer
- `OpenSettingsCommand` — calls ISettingsLauncher.ShowSettings()
- `ExitCommand` — calls IApplicationLifetime.Shutdown()

**Agent action commands** (created in constructor):
- `LaunchCommand` — calls RunAgentCommandAsync with AgentCommandKind.Launch
- `ResumeCommand` — calls RunAgentCommandAsync with AgentCommandKind.Resume
- `InitCommand` — calls RunAgentCommandAsync with AgentCommandKind.Init
- `UpdateCommand` — calls RunAgentCommandAsync with AgentCommandKind.Update
- `VersionCommand` — calls RunAgentCommandAsync with AgentCommandKind.Version
- `LaunchAgentCommand<AgentItem>` — calls RunAgentCommandAsync with item parameter
- `ResumeAgentCommand<AgentItem>` — calls RunAgentCommandAsync with item parameter

**Project commands** (already pass-through to ProjectPaneController):
- `AddProjectCommand`, `RemoveProjectCommand`, `ToggleFavoriteCommand`, `RefreshCommand`

The ViewModel also contains:
- `RunAgentCommandAsync()` private method — delegates to LaunchCommandCoordinator
- `OpenDataFolder()` private method — launches folder and reports outcome
- `HasSelectedAgent()` private helper — used by CanExecute predicates

## Desired State

**New WindowActionCoordinator** owns:
- All window-level commands
- All agent action commands
- Command creation logic
- CanExecute predicate logic
- Coordination with LaunchCommandCoordinator for agent execution
- Coordination with StatusMessageCoordinator for folder-open outcomes

**LaunchWindowViewModel** becomes:
- Pass-through properties for coordinator commands
- No command creation logic
- No execution logic
- Pure binding surface

## Architecture

```
LaunchWindowViewModel (thin binding layer)
├── ProjectPaneController (project workflow)
│   └── Commands: Add, Remove, ToggleFavorite, Refresh
├── AgentPaneController (agent workflow)
├── MenuActionCoordinator (menu construction)
├── StatusMessageCoordinator (status messages)
└── WindowActionCoordinator (NEW - window/agent commands)
    ├── Commands: OpenDataFolder, OpenSettings, Exit
    ├── Commands: Launch, Resume, Init, Update, Version
    ├── Commands: LaunchAgent<T>, ResumeAgent<T>
    ├── Delegates execution → LaunchCommandCoordinator
    └── Reports outcomes → StatusMessageCoordinator
```

## WindowActionCoordinator Contract

```csharp
namespace CLIHub.ViewModels;

/// <summary>
///   Coordinates window-level actions and agent commands for the launch window.
///   Owns command instances and delegates execution to specialized coordinators.
/// </summary>
public sealed class WindowActionCoordinator
{
    // Dependencies
    private readonly LaunchCommandCoordinator _launchCoordinator;
    private readonly StatusMessageCoordinator _statusCoordinator;
    private readonly ISettingsLauncher _settingsLauncher;
    private readonly IApplicationLifetime _applicationLifetime;
    private readonly IExternalLauncher _externalLauncher;
    
    // State providers (injected from ViewModel)
    private readonly Func<AgentItem?> _getSelectedAgent;
    private readonly Func<Project?> _getCurrentProject;
    private readonly Action _refreshAgents;

    // Window-level commands
    public RelayCommand OpenDataFolderCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand ExitCommand { get; }

    // Agent action commands
    public RelayCommand LaunchCommand { get; }
    public RelayCommand ResumeCommand { get; }
    public RelayCommand InitCommand { get; }
    public RelayCommand UpdateCommand { get; }
    public RelayCommand VersionCommand { get; }
    public RelayCommand<AgentItem> LaunchAgentCommand { get; }
    public RelayCommand<AgentItem> ResumeAgentCommand { get; }

    public WindowActionCoordinator(
        LaunchCommandCoordinator launchCoordinator,
        StatusMessageCoordinator statusCoordinator,
        ISettingsLauncher settingsLauncher,
        IApplicationLifetime applicationLifetime,
        IExternalLauncher externalLauncher,
        Func<AgentItem?> getSelectedAgent,
        Func<Project?> getCurrentProject,
        Action refreshAgents);
}
```

## Implementation Plan

### Phase 1: Create WindowActionCoordinator
1. Create `src/CLIHub/ViewModels/WindowActionCoordinator.cs`
2. Add constructor with dependencies
3. Create window-level commands (OpenDataFolder, OpenSettings, Exit)
4. Implement OpenDataFolder logic with StatusMessageCoordinator integration
5. Create agent action commands (Launch, Resume, Init, Update, Version)
6. Create parameterized commands (LaunchAgent, ResumeAgent)
7. Implement agent command execution via LaunchCommandCoordinator

### Phase 2: Update LaunchWindowViewModel
1. Add WindowActionCoordinator dependency to constructor
2. Create coordinator instance with state provider lambdas
3. Replace command properties with pass-through to coordinator
4. Remove old command creation logic from constructor
5. Remove `RunAgentCommandAsync()` private method
6. Remove `OpenDataFolder()` private method
7. Remove `HasSelectedAgent()` private helper

### Phase 3: Update ServiceRegistration
1. Register WindowActionCoordinator as singleton
2. Verify DI container configuration

### Phase 4: Add Tests
1. Create `tests/CLIHub.Tests/ViewModels/WindowActionCoordinatorTests.cs`
2. Test window-level commands execute correctly
3. Test agent commands delegate to LaunchCommandCoordinator
4. Test CanExecute logic for agent commands
5. Test folder-open outcome reporting to StatusMessageCoordinator
6. Update LaunchWindowViewModelTests to verify pass-through

### Phase 5: Verify
1. Build solution (zero errors, zero warnings)
2. Run all tests (100% pass)
3. Verify line count reduction (406 → ~300 lines)
4. Manual smoke test of launch window

## State Provider Pattern

WindowActionCoordinator needs access to ViewModel state but shouldn't hold a reference to the ViewModel itself. We use **function providers** injected at construction:

```csharp
// In LaunchWindowViewModel constructor:
_windowActions = new WindowActionCoordinator(
    launchCommandCoordinator,
    statusCoordinator,
    settingsLauncher,
    applicationLifetime,
    externalLauncher,
    () => SelectedAgent,           // State provider
    () => _projectPane.CurrentProject, // State provider
    RefreshAgents                   // Callback
);
```

This pattern:
- Avoids circular dependencies
- Keeps coordinator testable (inject test doubles)
- Makes state dependencies explicit
- Follows functional programming principles

## Success Criteria

- ✅ WindowActionCoordinator owns all window/agent commands
- ✅ LaunchWindowViewModel reduced from 406 to ~300 lines
- ✅ All 13 commands preserve exact behavior
- ✅ CanExecute logic works identically
- ✅ Zero behavior changes (pure refactoring)
- ✅ All tests pass
- ✅ Zero build warnings
- ✅ Code matches coordinator pattern established in codebase
