## Why

The MVP treats each plugin as a flat list of arbitrary commands and can only "launch the first one". The vision calls for a richer agent model: a named command set (launch, resume last session, version, update, initialize in project) plus detection of whether an agent is installed on the host and used in a given project. This change delivers that model and executes the commands.

## What Changes

- Replace the flat `Plugin.Commands` list with a structured `AgentCommands` object holding named commands: `launch`, `resume`, `version`, `update`, `init`
- Add `AgentDetection` to a plugin: `systemPaths` (host install markers under `%USERPROFILE%`) and `projectIndicators` (folders/files marking use in a project)
- **BREAKING**: the `plugin.json` schema changes from `"commands": [ ... ]` to `"commands": { "launch": {...}, ... }` plus a `"detection"` object. Existing plugin descriptors must be updated
- Validate that a plugin defines at least a `launch` command
- Execute commands in the current project's folder: launch/resume/init/update open in Windows Terminal; version runs the executable and captures its output
- Detect agent availability: installed in the system, and available in a given project
- Surface agent availability and the new commands in the UI

## Capabilities

### New Capabilities

- `agent-commands`: the named command model and its execution (launch, resume, version, update, init) in a project context
- `agent-detection`: determining whether an agent is installed on the host and used within a project

### Modified Capabilities

<!-- None: no existing main-spec requirement changes. -->

## Impact

**New code:**
- `src/CLIHub.Core/Models/AgentCommands.cs`, `AgentDetection.cs`, `AgentCommandKind.cs`
- `src/CLIHub.Core/Interfaces/IAgentCommandService.cs`, `IAgentDetectionService.cs`
- `src/CLIHub.Core/Services/AgentCommandService.cs`, `AgentDetectionService.cs`

**Modified code:**
- `src/CLIHub.Core/Models/Plugin.cs` — `Commands` becomes `AgentCommands`; add `Detection`
- `src/CLIHub.Core/Interfaces/IProcessLauncher.cs` + `ProcessLauncher.cs` — add output capture for version
- `src/CLIHub.Core/Services/PluginManager.cs` — parse/validate the new schema
- `src/CLIHub.Core/ServiceCollectionExtensions.cs` — register the new services
- `src/CLIHub/Windows/MainWindow.xaml(.cs)` — show availability and command actions
- `src/CLIHub/TrayIconController.cs` — launch the current project's agents

**Data:**
- `%APPDATA%\CLIHub\plugins\<id>\plugin.json` — schema change (commands object + detection); sample descriptors updated

**No UI-breaking changes beyond the plugin schema.**
