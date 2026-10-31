## Why

When a project is removed from Settings → Projects, the confirmation prompt is owned by `Application.Current.MainWindow` (the launch window) instead of the Settings window. Settings is a separate, non-modal window also owned by MainWindow, so the prompt can appear behind Settings, and Settings remains interactive while the prompt is open. This allows triggering a second prompt or obscuring the first one, creating a confusing UX where the modal constraint is violated.

## What Changes

- Fix dialog ownership so that when `ProjectDialogService.ConfirmProjectRemoval` is called from Settings, the prompt is owned by (and modal to) the Settings window, appears above it, and disables Settings while open.
- Keep the same ownership when called from the launch window (MainWindow).
- The fix also applies to folder pickers (`SelectFolder`, `SelectProjectFolder`) for consistency, since they share the same owner-resolution path.
- No change to the confirmation text, buttons, removal semantics, or persistence behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an internal UI defect fix within the existing `project-management` spec scenarios (`:115-133`, `:233-243`). The confirmation prompt is already specified (":129: a confirmation prompt is shown"); this change corrects its z-order and modality without altering the spec-level behavior. The change opts out of specs (`skip_specs: true` in `.openspec.yaml`).

## Impact

- `src/CLIHub/Launching/ProjectDialogService.cs` — the private `Show` method and folder-picker calls currently use `Application.Current?.MainWindow` as the owner; replace it with dynamic owner resolution (active window with fallback to MainWindow).
- No changes to `IProjectDialogService`, callers (`ProjectRegistryViewModel`, `ProjectPaneController`), view models, configuration, or tests beyond potentially adding a focused test for the owner-resolution rule.
- The fix is UI-only, WPF-specific, and confined to the dialog service implementation.

## Non-Goals

- Changing the confirmation message, button labels, or removal logic.
- Adding an explicit owner parameter to `IProjectDialogService` methods; the service resolves the owner internally.
- Applying a similar fix to other dialogs (there are no other modal prompts triggered from Settings today).

## Dependencies

None. This change is independent and self-contained.
