## Context

See `proposal.md` for motivation and `specs/update-checking/spec.md` for the cross-surface contract. Core `IUpdateChecker`, `IUpdateDownloader`, `IUpdateInstaller`, and `IUpdateStateSource` ports already share one Velopack-backed implementation.

## Goals / Non-Goals

**Goals:**

- Centralize update orchestration and coalesce concurrent checks.
- Expose one checking/availability/download state to all presentation surfaces.
- Keep startup best-effort behavior and existing download/apply policies per entry point.
- Keep Core update contracts and Velopack implementation unchanged.

**Non-Goals:**

- Change the launch-window explicit restart step to automatic restart.
- Change tray/What's New automatic restart to an explicit apply step.
- Change update source, preferences, download result contracts, or release-note behavior.

## Decisions

- Add `IUpdateWorkflow` and `UpdateWorkflow` in `CLIHub`. The workflow implements `IUpdateStateSource`, forwards Core availability/download state, exposes `IsCheckingForUpdates`, coalesces simultaneous checks onto one shared task, and offers shared check, download, apply, and download-and-apply operations.
- Move the existing two-second notification and automatic restart sequence into `UpdateWorkflow.DownloadAndApplyAsync`. It reports downloaded/failed outcomes through application events; `ApplicationSession` forwards those events to the startup UI/tray. This avoids injecting a tray-dependent notifier into services resolved while the tray itself is being constructed.
- Remove `UpdateDownloadCoordinator` and `IUpdateDownloadCoordinator`; `ApplicationSession` invokes the shared workflow for tray and What's New download requests.
- Keep `UpdateStartupCoordinator` as a narrow preference/notification adapter over `IUpdateWorkflow.CheckForUpdatesAsync`, preserving disabled-check and best-effort failure behavior.
- Make `TrayStateProjection`, `TrayCommandHandlers`, `WhatsNewViewModel`, and `UpdateControlViewModel` consume `IUpdateWorkflow` instead of independently injecting checker/downloader/installer/state ports.
- Preserve presentation policy in adapters: tray/What's New call `DownloadAndApplyAsync`; launch window calls `DownloadUpdateAsync` and later `ApplyDownloadedUpdateAndRestart`.
- Register one singleton `IUpdateWorkflow` in `ServiceRegistration` and remove the Core aliases only from application consumers, not from Core composition.
- Supply the workflow to the existing Settings checker port in the composition root. Settings keeps its result messages and narrow constructor contract.
- Reserve the automatic-download operation through its notification delay and apply step, so repeated requests cannot download or restart twice after Core finishes downloading. The launch-window download uses the same exclusion boundary but releases it when the download returns.

## Risks / Trade-offs

- A coalesced check's first caller supplies its cancellation token → every production check is application-lifetime tracked, so shutdown cancellation remains consistent; the initiating call awaits underlying completion, while a joining caller can cancel only its own wait. Tests cover cancellation and later retry.
- Shared workflow update events may arrive off the UI thread → existing WPF adapters continue dispatching state refreshes, and session dispatches tray notifications.
- Centralizing automatic restart could change failure behavior → port the current coordinator's exact notification, delay, cancellation, and exception policy into the shared workflow and retain tests for each result.

## Migration Plan

No data migration is required. Rollback reverts the shared application workflow, its consumers, and tests; Core update contracts and persisted preferences remain unchanged.
