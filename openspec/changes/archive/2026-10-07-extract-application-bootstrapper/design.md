## Context

See `proposal.md` for the motivation and `specs/app-lifecycle/spec.md` for the behavior contract. The current `App.OnStartup` builds the service provider and then directly sequences single-instance handling, plugin initialization, preferences, tray and window setup, hotkey registration, release notes, and the update check. Some existing coordinators already isolate release-notes and update workflows, but there is no boundary around the complete startup scenario.

The application is Windows-only and uses WPF for lifecycle and dispatcher integration. Existing service ownership and the single-instance provider lifecycle must remain intact.

## Goals / Non-Goals

**Goals:**

- Introduce one testable bootstrapper abstraction for first-instance startup.
- Keep WPF-specific dispatcher and window operations behind callbacks or narrow application-facing ports.
- Make required versus best-effort startup steps explicit.
- Preserve the current startup order, resource ownership, and user-visible behavior.
- Add composition and orchestration tests without requiring a real WPF window or tray icon.

**Non-Goals:**

- Introducing a new application project or moving `CLIHub.Core` types.
- Adding a shared application cancellation lifetime; that is the next planned change.
- Splitting `LaunchWindowViewModel`, tray actions, or update service contracts.
- Changing logging configuration, update semantics, or configuration persistence.

## Decisions

### Use an application bootstrapper service

Add a single application-facing bootstrapper contract and implementation registered by the composition root. `App` will resolve it and provide only lifecycle callbacks that require WPF, such as shutdown, dispatcher invocation, and launch-window display/notification. This keeps startup sequencing in a class that can be tested with fakes.

An alternative was to keep orchestration in `App` and extract private helper methods. That would reduce file size but would not create an injectable boundary or allow ordering and failure policy to be tested without constructing the WPF application.

### Keep service-specific coordinators intact

The bootstrapper will call the existing plugin, preference, hotkey, release-notes, and update contracts rather than absorbing their behavior. The new boundary coordinates them; it does not become a second implementation of those workflows.

An alternative was to create a larger startup pipeline abstraction with generic step metadata. That adds indirection without solving a current problem and would make the failure policy less visible.

### Classify startup failures at the orchestration boundary

Required infrastructure and UI creation failures are fatal and cause shutdown. Existing optional coordinators retain their best-effort behavior, including logging and continuing where currently defined. The bootstrapper will avoid broad exception swallowing around required steps.

### Preserve provider-owned disposal

The bootstrapper will not dispose the single-instance guard, tray, hotkey, or service provider. `App.OnExit` remains the final lifecycle cleanup boundary, preventing duplicate ownership after the previous single-instance change.

## Risks / Trade-offs

- [Risk] Moving callback wiring can change dispatcher timing or event subscriptions. -> Preserve the existing subscription order and add tests asserting the observable sequence.
- [Risk] A required startup exception may occur before all cleanup fields are assigned. -> Keep cleanup nullable and idempotent in `App.OnExit`, and request shutdown through the existing WPF lifecycle.
- [Risk] The bootstrapper could become a new oversized service. -> Limit it to sequencing and boundary adaptation; keep workflow logic in existing coordinators.

## Migration Plan

1. Add the bootstrapper contract and implementation with tests.
2. Register it in the WPF composition root and replace direct startup orchestration in `App`.
3. Run build, tests, and OpenSpec validation.
4. If startup behavior regresses, revert the composition-root delegation while retaining the isolated tests and contract for follow-up correction.
