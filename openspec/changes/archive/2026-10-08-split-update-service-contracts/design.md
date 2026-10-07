## Context

The existing `IUpdateService` exposes five concerns:

- current version;
- check for updates;
- observable download/update state;
- download;
- apply and restart.

`UpdateService` contains the Velopack manager, timeout and duplicate-download coordination, downloaded asset tracking, and state notifications. These implementation semantics must remain intact while consumers receive narrower contracts.

## Decisions

Define these ports in `CLIHub.Core.Updates`:

- `IUpdateVersionProvider` for `GetCurrentVersion`.
- `IUpdateChecker` for `CheckForUpdatesAsync`.
- `IUpdateStateSource` for `IsDownloading`, `LastKnownAvailableVersion`, and `UpdateStateChanged`.
- `IUpdateDownloader` for `DownloadUpdateAsync`.
- `IUpdateInstaller` for `ApplyDownloadedUpdateAndRestart`.

`IUpdateService` remains a composed compatibility contract inheriting all five ports, and the Velopack-backed `UpdateService` continues implementing it. DI registers each port to the same singleton adapter instance. This keeps existing infrastructure behavior and test seams while preventing application consumers from depending on the aggregate.

Application wiring uses the smallest port set: startup coordinators use checker, version displays use version provider, update controls use version/check/state/download/installer, and download coordination uses download/installer.

## State Adapter Boundary

State notification remains a separate port. `UpdateControlViewModel` and tray/application state consumers subscribe to `IUpdateStateSource`; they do not need download or installation methods merely to observe state. The Velopack adapter remains the only component that owns manager and asset details.

## Risks / Trade-offs

- [Risk] DI aliases might create multiple adapter instances. -> Register `IUpdateService` once and map each narrow port through `GetRequiredService<IUpdateService>()`.
- [Risk] A consumer may accidentally regain aggregate access. -> Change constructors and tests to narrow interfaces and add registration assertions.
- [Risk] Split interfaces could alter duplicate-download or failure behavior. -> Keep all operation bodies in `UpdateService` and retain existing coordinator/service tests.

## Migration Plan

1. Add the five ports and compose `IUpdateService` from them.
2. Update DI aliases and application consumers.
3. Migrate focused tests to narrow mocks while preserving behavior assertions.
4. Run build, full tests, graphify, validate, and archive the change.
