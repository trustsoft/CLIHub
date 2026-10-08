## Context

See `proposal.md` for motivation and `specs/` for the behavior contract. Startup ordering is currently characterized by `ApplicationBootstrapperTests`; update availability is already shared through the Core update service state source.

## Goals / Non-Goals

**Goals:**

- Keep startup sequencing and required/best-effort failure policy in the bootstrapper.
- Give application-level event handlers a single explicit owner and deterministic unsubscription point.
- Run manual checks through the shared checker and tracked application operation lifetime.

**Non-Goals:**

- Unify update check, download, and apply workflows.
- Change startup update-check policy or launch-window update behavior.
- Add Core contracts, an event bus, or a new dependency.

## Decisions

- Introduce `ApplicationSession` as a WPF application-layer lifecycle object. It lazily creates the startup UI and update download workflow only after the bootstrapper has confirmed first-instance ownership.
- Keep `ApplicationBootstrapper` responsible for the existing ordered startup sequence and fatal/best-effort policy; it receives the session through constructor injection.
- Dispose session subscriptions in `App.OnExit` before stopping tracked operations and disposing the service provider. The session also disposes its startup UI adapter, which removes its own tray subscription.
- Add manual-check state to the tray projection and What's New ViewModel. Both invoke `IUpdateChecker` through `IApplicationOperationLifetime`, while availability and download state continue to come from the existing shared update-state source.
- Preserve the download-and-restart event path and existing download coordinator.

## Risks / Trade-offs

- A session startup failure after a partial subscription could leave handlers attached → event wiring is localized in `Start`, and normal shutdown disposes the session.
- Update-check completion may occur off the UI thread → tray refresh dispatches through WPF's dispatcher; the What's New ViewModel follows its existing dispatcher update pattern.
- Manual checks do not add a second notification system → current shared availability changes drive the existing tray/window actions, keeping feedback consistent with the established state model.

## Migration Plan

No data or deployment migration is required. The change is backwards-compatible at the user-preference and configuration boundaries. Rollback consists of reverting the application-layer and view changes together with this archived change.
