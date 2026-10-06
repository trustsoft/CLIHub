# 0005. UI dialog boundaries

## Status

Accepted — 2026-10-06. Implemented by `introduce-ui-dialog-services`.

## Context

The launch window's view model needs to ask the user things: pick a folder for a new project, confirm a removal, report a failure. The original implementation called `MessageBox.Show` and `OpenFolderDialog` directly from `LaunchWindowViewModel`.

Direct WPF calls in a view model make it impossible to unit-test the surrounding workflow without showing real dialogs, and they couple the view model to the exact modal pattern — any future restyling (custom dialog chrome, different ownership) would require touching business logic.

## Decision

- **Dialog seam:** `IProjectDialogService` wraps the folder picker; `IUserNotificationService` wraps message boxes. View models depend on the interfaces; WPF implementations live beside the views.
- **Preserve the modal experience:** implementations keep the native modal dialogs, owned by the launch window (`Application.Current.MainWindow`) so they stack correctly above it.
- **Scope: dialogs only.** Window launching (Settings, What's New) stays with its own launcher services (`ISettingsLauncher`, `IReleaseNotesLauncher`); this decision does not create a general navigation framework.

Rejected alternatives:

- *Keep dialogs in the view model* — rejected: untestable workflows and a hard WPF coupling in exactly the layer MVVM exists to protect.
- *General window/navigation service for all windows* — deferred: only dialogs were painful; the dedicated launchers already cover window opening, and a generic framework would be speculative.

## Consequences

- Dialog flows are testable: tests inject fake services and assert on the recorded calls instead of dismissing real modals.
- The view model contains no `System.Windows` types; WPF specifics are confined to the implementations.
- Adding a dialog type means extending a service interface — a deliberate, visible API change rather than another inline `MessageBox.Show`.
- The seam covers modal interactions only; anything beyond that (custom-styled dialogs, non-modal notifications beyond the tray) will need a follow-up decision.
