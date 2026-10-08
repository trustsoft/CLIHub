## 1. Extract tray responsibilities

- [x] 1.1 Add `TrayStateProjection` and verify state composition, source notifications, and deterministic disposal.
- [x] 1.2 Add `TrayCommandHandlers` and verify project, agent, update, settings, release-notes, and exit actions preserve current behavior.
- [x] 1.3 Reduce `TrayActions` to an adapter over projection and handlers while preserving `ITrayActions`.

## 2. Composition and verification

- [x] 2.1 Register the focused tray services in `ServiceRegistration` and verify the tray graph resolves.
- [x] 2.2 Update architecture documentation to describe tray ownership.
- [x] 2.3 Run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, `openspec validate`, and `git diff --check`.
