## Why

`LaunchWindowViewModel` directly creates WPF folder dialogs and message boxes, which couples presentation state to UI infrastructure and prevents focused tests of project actions. Moving these interactions behind narrow services makes the ViewModel testable while preserving the current dialogs, owners, text, and button behavior.

## What Changes

- Add `IProjectDialogService` for folder selection and project-removal confirmation.
- Add `IUserNotificationService` for information and warning dialogs.
- Add WPF implementations and register them through DI.
- Inject the services into `LaunchWindowViewModel` and remove direct `OpenFolderDialog`/`MessageBox` usage there.
- Preserve `PromptState` scopes around modal interactions and preserve existing messages/titles/buttons.
- Leave tray, legacy `MainWindow`, shutdown, and other WPF dialog paths out of scope.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an internal UI-boundary refactor; project-management and application-lifecycle requirements remain unchanged.

## Impact

- Affected runtime code: `LaunchWindowViewModel`, WPF dialog services, and composition registration.
- Affected tests: focused ViewModel dialog-boundary test and full solution tests.
- No change to project persistence, dialog text, modal behavior, or user-visible workflows.
- OpenSpec specs are intentionally skipped because no observable requirement changes.
