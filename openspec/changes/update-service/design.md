## Context

See proposal.md - Why. The app is currently run from build output (not a Velopack install), so update checks must degrade gracefully. `Serilog` and DI are in place (`ILogger<T>`, `AddClIHubCoreServices`), and the tray icon (`H.NotifyIcon` `TaskbarIcon`) can show notifications. No Settings window exists yet, so the version and manual check live in `MainWindow`.

## Goals / Non-Goals

**Goals:**
- Detect an available update without blocking startup
- Notify via the tray, including the new version
- Show the current app version and offer a manual check
- Behave correctly when the app is not a Velopack install, offline, or slow
- Keep the logic testable and behind an interface

**Non-Goals**
- Downloading or applying updates (deferred to a packaging change)
- A Settings/About window
- Auto-install, release notes, or update channels
- Building the release feed (packaging concern)

## Decisions

### Decision 1: `IUpdateService` in Core over Velopack `UpdateManager`

```csharp
public enum UpdateStatus { UpToDate, UpdateAvailable, NotInstalled, Failed }
public sealed record UpdateCheckResult(UpdateStatus Status, string CurrentVersion, string? AvailableVersion);

public interface IUpdateService
{
    string GetCurrentVersion();
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}
```

**Rationale:** Wraps Velopack behind a small, testable surface; the UI stays free of Velopack types and can render status/log.

**Alternatives considered:** Calling `UpdateManager` directly from the UI — rejected (untestable, leaks the dependency).

### Decision 2: Detect "not installed" via `CurrentVersion == null`

**Chosen:** If `UpdateManager.CurrentVersion` is null (documented: null when not a Velopack install), return `NotInstalled` immediately, without calling the network.

**Rationale:** Avoids an exception on the common dev path and keeps the check cheap. `NotInstalledException` is still caught as a belt-and-braces measure.

**Alternatives considered:** Relying only on catching `NotInstalledException` — rejected (exception-driven control flow for an expected state).

### Decision 3: GitHub Releases source via `GithubSource`

**Chosen:** `UpdateService` builds `new UpdateManager(new GithubSource(<repositoryUrl>, null, false))` from a constant that must be finalized when packaging is added. Per the Velopack docs, GitHub Releases use `GithubSource` (a plain `github.com` URL is not a Velopack feed).

**Rationale:** Matches the documented source for GitHub-hosted releases; the constant is the single place to set the owner/repo.

**Alternatives considered:** A plain URL string — rejected (only valid for a hosted `releases.{channel}.json` feed, not a GitHub repo).

### Decision 8: Testable manager seam

**Chosen:** Keep the public `UpdateService(ILogger<UpdateService>)` constructor (default GitHub manager, tolerant when the locator is absent), and add an `internal UpdateService(ILogger, UpdateManager)` seam so tests can inject a manager built with `TestVelopackLocator` and a `SimpleFileSource` — the approach the Velopack testing docs recommend. `CLIHub.Core` exposes internals to `CLIHub.Tests` via `InternalsVisibleTo`.

**Rationale:** Exercises the "installed" branch (up-to-date, and later update-available) without a full install, while production code stays simple.

**Alternatives considered:** Testing only the not-installed path — rejected (leaves the real update path untested).

### Decision 4: Bounded, exception-safe check

**Chosen:** A 15-second timeout via a linked `CancellationTokenSource`; catch `NotInstalledException`, `OperationCanceledException`, and general exceptions; always log the outcome.

**Rationale:** The spec requires timeout and failure handling; a background check must never block or crash the app.

**Alternatives considered:** Unbounded await — rejected (a hung feed would leak a task forever).

### Decision 5: Version from Velopack, fallback to the assembly

**Chosen:** `GetCurrentVersion()` returns `UpdateManager.CurrentVersion?.ToString()`; when null, the entry assembly's `AssemblyInformationalVersionAttribute`; else `"unknown"`.

**Rationale:** Shows a real version even when unpackaged.

### Decision 6: Startup check + tray notification; manual check in the window

**Chosen:** `App.OnStartup` fires the check (fire-and-forget with logging); on `UpdateAvailable`, `TrayIconController` shows a notification with the version. `MainWindow` shows the current version and a **Check for updates** button that runs the same check and reports the outcome in the status bar.

**Rationale:** Matches the spec (startup + manual + notify + version display) with the UI available today; no Settings window needed.

**Alternatives considered:** A Settings window (deferred to `preferences-ui`).

### Decision 7: Velopack bootstrap via a custom entry point

**Chosen:** Add `Program.Main` that calls `VelopackApp.Build().Run()` before starting WPF (and set `<StartupObject>CLIHub.Program</StartupObject>`).

**Rationale:** Velopack requires its locator to be initialized before any `UpdateManager` is constructed; calling `new UpdateManager(...)` first throws `InvalidOperationException: No VelopackLocator has been set`. A custom entry point is the documented way to run `VelopackApp.Build().Run()` first. In a non-installed run it installs a "not installed" locator, so the service short-circuits to `NotInstalled` instead of throwing.

**Alternatives considered:** Initializing the locator lazily inside `UpdateService` — rejected; Velopack's guidance is to bootstrap first, and the bootstrap also handles install/uninstall hooks.

## Risks / Trade-offs

**[Risk] A placeholder feed URL yields failed checks once packaged** → Mitigation: URL is a single constant, surfaced in design and code comments; the change is check-only, so a wrong URL only logs failures.

**[Risk] `H.NotifyIcon` notification API differs from expectations** → Mitigation: use `TaskbarIcon.ShowNotification(title, message)`; if unavailable, fall back to a balloon tip; verify at compile/runtime.

**[Risk] Velopack package not compatible with `net8.0`** → Mitigation: Velopack targets `net8.0`; verify restore succeeds before wiring.

**[Trade-off] Check-only** → Benefit: safe, no partial-update states. Cost: users still update manually until packaging lands.

**[Trade-off] No Settings surface** → Benefit: no scope creep. Cost: manual check lives in the main window for now.

## Migration Plan

1. Add the `Velopack` package to `CLIHub.Core`; verify restore
2. Add `IUpdateService`/`UpdateService`; register in `AddClIHubCoreServices`
3. `TrayIconController`: add `NotifyUpdateAvailable(version)`
4. `App.OnStartup`: run the check; on availability, notify
5. `MainWindow`: show the app version and a Check button
6. Tests: not-installed path returns `NotInstalled` without network; version fallback

Rollback: revert code; no persisted state changes.

## Open Questions

- **Download/apply UX** (progress, restart) — deferred to packaging.
- **Release feed and channel** — decided when publishing.

## Reference (Velopack API)

- `VelopackApp.Build().Run()` — bootstrap; must be the **first** call in `Main` (Run contract)
- `new UpdateManager(new GithubSource("<repoUrl>", null, false))` — GitHub Releases source
- `new UpdateManager(source, options, locator)` — locator injection (testing)
- `UpdateManager.CurrentVersion : SemanticVersion?` — null when not installed
- `await UpdateManager.CheckForUpdatesAsync() : UpdateInfo?` — null when up to date
- `UpdateInfo.TargetFullRelease.Version`
- `Velopack.Exceptions.NotInstalledException`
- `TestVelopackLocator(appId, version, packagesDir)` / `SimpleFileSource(DirectoryInfo)` — development/CI testing

Reviewed against https://docs.velopack.io/integrating/overview and https://docs.velopack.io/integrating/testing.
