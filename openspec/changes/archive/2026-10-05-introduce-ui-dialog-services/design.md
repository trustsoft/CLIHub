## Context

`LaunchWindowViewModel` currently uses `Microsoft.Win32.OpenFolderDialog`, `MessageBox`, and `Application.Current.MainWindow` for Add Project, Remove Project, agent version information, and project-add warning paths. The launch window already uses `PromptState` to prevent focus loss from hiding the shell while modal interactions are active.

## Goals / Non-Goals

**Goals:**

- Isolate WPF dialog construction and ownership from `LaunchWindowViewModel`.
- Preserve current dialog titles, messages, buttons, icons, and owner behavior.
- Keep `PromptState` lifetime scopes in the ViewModel around folder picker and confirmation calls.
- Make project actions and notification calls replaceable in tests.

**Non-Goals:**

- Refactor dialogs in `TrayIconController`, legacy `MainWindow`, Settings, or What's New.
- Change shutdown/lifetime handling or introduce a general navigation service.
- Move project validation or persistence out of Core.

## Decisions

- `IProjectDialogService.SelectProjectFolder()` returns the selected folder path or null on cancel.
- `IProjectDialogService.ConfirmProjectRemoval(string projectName)` owns the existing confirmation message and returns the user's decision.
- `IUserNotificationService` exposes `ShowInformation(message, title)` and `ShowWarning(message, title)`.
- WPF implementations resolve `Application.Current.MainWindow` as the owner when available and fall back to unowned `MessageBox`/dialog calls when not.
- `LaunchWindowViewModel` continues to own status-message updates and `PromptState` scopes; services own only modal UI invocation.
- Register both services as singletons because they are stateless UI adapters.

## Risks / Trade-offs

- **Modal focus behavior could regress** -> Keep `PromptState.Begin()` scopes around service calls and cover cancellation in a ViewModel test.
- **User-facing text could drift** -> Move the exact existing strings into the WPF service or retain message construction at the call site where it carries domain context; verify the resulting messages in code review.
- **ViewModel constructor grows temporarily** -> This change adds two explicit narrow dependencies; later application-boundary work can consolidate them if needed.

## Migration Plan

1. Add the two contracts and WPF implementations.
2. Register them in WPF composition.
3. Inject them into `LaunchWindowViewModel` and replace direct dialog calls.
4. Add a fake-backed ViewModel test for cancelled project-folder selection.
5. Run focused and full tests, then archive the change.

## Open Questions

None.
