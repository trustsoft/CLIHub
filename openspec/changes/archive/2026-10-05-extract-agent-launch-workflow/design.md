## Context

The launch window calls `IAgentCommandService.ExecuteAsync` from its private `ExecuteAsync` method after checking the selected project. The tray's Launch Agent menu independently gets the current project and calls the same service for `AgentCommandKind.Launch`, displaying a warning on failure. Core `AgentCommandService` already validates command availability and project directory existence.

## Goals / Non-Goals

**Goals:**

- Provide one application-level workflow for invoking agent commands with a project context.
- Preserve the existing command service contract and result handling.
- Allow the launch window and tray to share invocation logic without sharing their presentation concerns.
- Keep the workflow independent of WPF dialogs and status controls.

**Non-Goals:**

- Change agent command validation, process launching, or plugin models.
- Move status messages or tray warning presentation into Core/workflow.
- Update legacy `MainWindow` in this change.
- Add project selection or agent availability policy to the workflow.

## Decisions

- Add `IAgentCommandWorkflow.ExecuteAsync(Plugin plugin, Project project, AgentCommandKind kind, CancellationToken cancellationToken = default)`.
- Implement the workflow as a thin delegation to `IAgentCommandService.ExecuteAsync(plugin, kind, project.Path, cancellationToken)`.
- The launch ViewModel continues to perform its existing preconditions and maps the result into status text.
- The tray resolves the current project as it does today, then calls the workflow and maps failure to its existing warning dialog.
- Register the workflow as a singleton.

## Risks / Trade-offs

- **The workflow becomes a second validation layer** -> Keep it as delegation only and leave validation in `AgentCommandService`.
- **Tray and ViewModel behavior could drift** -> Add tests for exact plugin/project/kind/path forwarding.
- **Legacy UI remains duplicated** -> Explicitly exclude `MainWindow`; handle its migration separately if still needed.

## Migration Plan

1. Add the workflow contract and implementation.
2. Register it in WPF composition.
3. Replace direct service calls in active launch window and tray paths.
4. Run focused and full tests, then archive.
5. Rollback consists of restoring direct `IAgentCommandService` calls and removing workflow registration/types.

## Open Questions

None.
