# Tasks: Extract Window Action Coordinator

## 1. Create WindowActionCoordinator

### 1.1 Create WindowActionCoordinator class file
- [ ] Create `src/CLIHub/ViewModels/WindowActionCoordinator.cs`
- [ ] Add namespace, usings, XML doc comment
- [ ] Add sealed class declaration

### 1.2 Add dependencies and constructor
- [ ] Add private readonly fields for all dependencies
- [ ] Add state provider fields (Func<AgentItem?>, Func<Project?>, Action)
- [ ] Create constructor with all parameters
- [ ] Add XML doc comments for constructor and parameters

### 1.3 Create window-level commands
- [ ] Add `OpenDataFolderCommand` property
- [ ] Add `OpenSettingsCommand` property
- [ ] Add `ExitCommand` property
- [ ] Initialize commands in constructor
- [ ] Add XML doc comments for command properties

### 1.4 Implement OpenDataFolder logic
- [ ] Create private `OpenDataFolder()` method
- [ ] Use IExternalLauncher to open AppPaths.Root
- [ ] Report success via StatusMessageCoordinator.ReportFolderOpen
- [ ] Report failure via StatusMessageCoordinator.ReportFolderOpen with exception
- [ ] Wrap in try-catch

### 1.5 Create agent action commands
- [ ] Add `LaunchCommand` property
- [ ] Add `ResumeCommand` property
- [ ] Add `InitCommand` property
- [ ] Add `UpdateCommand` property
- [ ] Add `VersionCommand` property
- [ ] Initialize commands in constructor with CanExecute predicates
- [ ] Add XML doc comments for command properties

### 1.6 Create parameterized agent commands
- [ ] Add `LaunchAgentCommand<AgentItem>` property
- [ ] Add `ResumeAgentCommand<AgentItem>` property
- [ ] Initialize commands in constructor with CanExecute predicates
- [ ] Add XML doc comments for command properties

### 1.7 Implement agent command execution
- [ ] Create private `RunAgentCommandAsync(AgentItem?, AgentCommandKind)` method
- [ ] Delegate to LaunchCommandCoordinator.RunAsync
- [ ] Pass current project from state provider
- [ ] Pass StatusMessageCoordinator.ReportCommandOutcome as callback
- [ ] Pass refreshAgents callback
- [ ] Create private `HasSelectedAgent()` helper for CanExecute

## 2. Update LaunchWindowViewModel

### 2.1 Add WindowActionCoordinator dependency
- [ ] Add private readonly field `_windowActions`
- [ ] Add constructor parameter
- [ ] Add XML doc comment for parameter

### 2.2 Create coordinator instance
- [ ] Instantiate WindowActionCoordinator in constructor
- [ ] Pass launchCommandCoordinator dependency
- [ ] Pass statusCoordinator dependency
- [ ] Pass settingsLauncher dependency
- [ ] Pass applicationLifetime dependency
- [ ] Pass externalLauncher dependency
- [ ] Pass `() => SelectedAgent` lambda
- [ ] Pass `() => _projectPane.CurrentProject` lambda
- [ ] Pass `RefreshAgents` method reference

### 2.3 Replace window-level command properties
- [ ] Change OpenDataFolderCommand to pass-through: `=> _windowActions.OpenDataFolderCommand`
- [ ] Change OpenSettingsCommand to pass-through: `=> _windowActions.OpenSettingsCommand`
- [ ] Change ExitCommand to pass-through: `=> _windowActions.ExitCommand`

### 2.4 Replace agent action command properties
- [ ] Change LaunchCommand to pass-through: `=> _windowActions.LaunchCommand`
- [ ] Change ResumeCommand to pass-through: `=> _windowActions.ResumeCommand`
- [ ] Change InitCommand to pass-through: `=> _windowActions.InitCommand`
- [ ] Change UpdateCommand to pass-through: `=> _windowActions.UpdateCommand`
- [ ] Change VersionCommand to pass-through: `=> _windowActions.VersionCommand`
- [ ] Change LaunchAgentCommand to pass-through: `=> _windowActions.LaunchAgentCommand`
- [ ] Change ResumeAgentCommand to pass-through: `=> _windowActions.ResumeAgentCommand`

### 2.5 Remove old command creation logic
- [ ] Remove OpenDataFolderCommand initialization from constructor
- [ ] Remove OpenSettingsCommand initialization from constructor
- [ ] Remove ExitCommand initialization from constructor
- [ ] Remove LaunchCommand initialization from constructor
- [ ] Remove ResumeCommand initialization from constructor
- [ ] Remove InitCommand initialization from constructor
- [ ] Remove UpdateCommand initialization from constructor
- [ ] Remove VersionCommand initialization from constructor
- [ ] Remove LaunchAgentCommand initialization from constructor
- [ ] Remove ResumeAgentCommand initialization from constructor

### 2.6 Remove extracted methods
- [ ] Remove `RunAgentCommandAsync()` private method
- [ ] Remove `OpenDataFolder()` private method
- [ ] Remove `HasSelectedAgent()` private helper

### 2.7 Verify line count reduction
- [ ] Count lines in LaunchWindowViewModel.cs (should be ~300, down from 406)

## 3. Update ServiceRegistration

### 3.1 Register WindowActionCoordinator
- [ ] Add `services.AddSingleton<WindowActionCoordinator>()` in RegisterViewModels
- [ ] Verify DI container configuration

## 4. Add Tests

### 4.1 Create WindowActionCoordinator tests
- [ ] Create `tests/CLIHub.Tests/ViewModels/WindowActionCoordinatorTests.cs`
- [ ] Add test: OpenDataFolderCommand executes and reports success
- [ ] Add test: OpenDataFolderCommand reports failure on exception
- [ ] Add test: OpenSettingsCommand calls ISettingsLauncher
- [ ] Add test: ExitCommand calls IApplicationLifetime.Shutdown

### 4.2 Test agent commands
- [ ] Add test: LaunchCommand delegates to LaunchCommandCoordinator with correct parameters
- [ ] Add test: ResumeCommand delegates to LaunchCommandCoordinator with correct parameters
- [ ] Add test: InitCommand delegates to LaunchCommandCoordinator with correct parameters
- [ ] Add test: UpdateCommand delegates to LaunchCommandCoordinator with correct parameters
- [ ] Add test: VersionCommand delegates to LaunchCommandCoordinator with correct parameters

### 4.3 Test parameterized commands
- [ ] Add test: LaunchAgentCommand executes with item parameter
- [ ] Add test: ResumeAgentCommand executes with item parameter

### 4.4 Test CanExecute logic
- [ ] Add test: Agent commands CanExecute returns true when agent selected
- [ ] Add test: Agent commands CanExecute returns false when no agent selected
- [ ] Add test: Parameterized commands respect item.CanLaunch and item.CanResume

### 4.5 Update LaunchWindowViewModel tests
- [ ] Update test to verify command pass-through to WindowActionCoordinator
- [ ] Verify existing tests still pass

## 5. Verify

### 5.1 Build and test
- [ ] Run `dotnet build CLIHub.sln` (zero errors, zero warnings)
- [ ] Run `dotnet test CLIHub.sln` (100% pass)

### 5.2 Manual verification
- [ ] Launch application
- [ ] Test OpenDataFolder command
- [ ] Test OpenSettings command
- [ ] Test Exit command (from tray, not launch window)
- [ ] Test agent Launch command
- [ ] Test agent Resume command
- [ ] Test agent Init command
- [ ] Test agent Update command
- [ ] Test agent Version command
- [ ] Verify status messages appear correctly
