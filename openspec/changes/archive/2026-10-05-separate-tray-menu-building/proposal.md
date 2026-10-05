## Why

`TrayIconController` currently owns both the lifetime of the WPF `TaskbarIcon` and the complete construction of its context menu. This mixes UI infrastructure with menu composition and application action wiring, making tray changes harder to test and increasing the risk of regressions in lifecycle code.

## What Changes

- Add a `TrayMenuBuilder` service responsible for constructing the tray context menu.
- Move current-project, recent-project, agent-launch, settings, release-notes, update, show-window, and exit menu composition into the builder.
- Keep action callbacks and existing user-visible labels and behavior unchanged.
- Make `TrayIconController` depend on the builder while retaining ownership of `TaskbarIcon` creation, refresh triggering, and disposal/lifecycle behavior.
- Register the builder in the WPF dependency-injection composition as a singleton.

## Capabilities

### New Capabilities

None. This is a structural refactor with no spec-level behavior change.

### Modified Capabilities

None.

## Impact

- Affected WPF code: `TrayIconController`, the new tray menu builder, and service registration.
- The builder will depend on the existing project, plugin, agent workflow, update, launch-window, settings, release-notes, and application-lifetime contracts needed by menu actions.
- No Core APIs, persistence formats, command semantics, or user-visible tray behavior should change.
- Focused composition/menu tests and the existing full solution build/test commands will verify the refactor.
