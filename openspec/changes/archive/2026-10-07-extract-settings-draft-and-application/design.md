## Context

See `proposal.md` for motivation and `specs/preferences-ui/spec.md` for the behavior contract. `SettingsViewModel` currently maps persisted strings directly into WPF properties, parses user input during Save, mutates the shared preferences store, and invokes `IPreferenceApplier` methods itself. `PreferenceApplier` remains the existing system boundary for runtime, hotkey, startup registration, and path display changes.

The config file format and existing Settings binding names must remain compatible. The next planned change addresses concrete WPF dependencies in `PreferenceApplier`; this change keeps that boundary intact and focuses on draft/application ownership.

## Goals / Non-Goals

**Goals:**

- Introduce a typed `SettingsDraft` and an application service independent of WPF.
- Centralize loading, parsing/validation, save ordering, and failure/rollback policy.
- Preserve existing validation messages, defaults, Cancel behavior, and binding surface.
- Persist preferences only after required runtime system application succeeds.
- Add direct tests for service and ViewModel behavior without constructing a Settings window.

**Non-Goals:**

- Decoupling `PreferenceApplier` from WPF; that is change 6.
- Changing the JSON schema, migrations, or configuration repository.
- Moving manual update checks out of the ViewModel; they already use the application operation lifetime.
- Adding a general transaction abstraction to all application settings.

## Decisions

### Use separate typed input and draft models

`SettingsInput` represents the editable form values, including text fields that still need parsing. `SettingsDraft` contains typed runtime, hotkey, path style, and nullable probe values. The settings application service converts input to a draft and returns validation errors without touching persistence or system services.

An alternative was to keep parsing in the ViewModel and pass a typed record only to Save. That would leave validation and error policy coupled to WPF and would not provide a directly testable application contract.

### Centralize Save ordering and persistence

The application service captures the previous persisted settings and current startup registration, validates the input, applies system settings through `IPreferenceApplier`, then performs one `IPreferencesStore.Update` callback. It returns a typed result containing success or an error message. The ViewModel only maps that result to `ValidationError` and `RequestClose`.

### Roll back supported system changes

If a later system application or persistence step fails, the service reapplies the previous runtime, path style, hotkey, and startup registration values. Hotkey rollback relies on the existing `ReRegister` contract, which already restores the previous combination when a new registration fails. Rollback failures are logged and included in the save error policy; they do not overwrite the original persisted configuration.

### Keep ViewModel draft fields for binding

The ViewModel retains its public properties and `Load` notifications so XAML does not change. It creates a `SettingsInput` at Save time and delegates validation/application. Cancel continues to raise close without calling the service save path.

## Risks / Trade-offs

- [Risk] A platform preference may change before a later preference fails. -> Capture the previous values and run best-effort rollback before returning the failure result.
- [Risk] Rollback itself can fail due to the same external condition. -> Log rollback failures, keep persisted state unchanged, and leave the Settings window open with an actionable error.
- [Risk] Existing callers depend on exact validation strings. -> Preserve current messages in the application service tests.
- [Risk] Save ordering differs from the current ViewModel. -> Add sequence tests covering startup registration, runtime/path/hotkey application, persistence, and close behavior.

## Migration Plan

1. Add typed settings models and application service with validation, apply, persistence, and rollback tests.
2. Register the service and delegate `SettingsViewModel.Load`/Save to it while retaining binding properties.
3. Add ViewModel tests for Cancel, valid Save, validation failures, and application failures.
4. Run build, tests, graphify update, OpenSpec validation, archive the change, and update `improvements.md`.
