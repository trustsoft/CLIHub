# Design: add-app-update

## Context

See proposal.md — Why/What. CLIHub is a WPF tray app (`App.xaml.cs` is the
composition root; no custom `Main` today). It already runs background work
at startup (the agent probe round) and owns a tray icon via H.NotifyIcon.
`Config` has typed sections for `probe`/`agents` but none for `update`.
Velopack facts that shape the design (from its docs): the SDK needs
`VelopackApp.Build().Run()` at the start of `Main` (a custom `Main` is
recommended for WPF to avoid loading WPF while an update is applied); the
API is `UpdateManager(feed)` -> `CheckForUpdatesAsync` ->
`DownloadUpdatesAsync` -> `ApplyUpdatesAndRestart`; releases can be hosted
on GitHub Releases; `vpk` builds and uploads them.

## Goals / Non-Goals

**Goals:**

- Update policy (when to check, how to download, when to notify, how to
  apply, when to stay silent) in Core, unit-testable with a fake client.
- Startup background check that never blocks or annoys: silent on failure
  and outside installed builds.
- Tray notification that applies-and-restarts when acted on.

**Non-Goals:**

- Settings window and its manual "Check for updates" button (the service
  exposes a check entry point for it later).
- Release packaging/upload pipeline, CI, code signing, release channels.
- The self-contained-vs-framework-dependent decision (separate packaging
  follow-up).
- Progress UI for downloads; the download is a background no-op until it
  becomes ready.

## Decisions

### D1: Abstraction in Core, Velopack behind it in App

Mirror the existing `IProcessRunner` split: Core defines `IUpdateClient`
with `IsInstalled`, `CheckAsync`, `DownloadAsync`, `ApplyAndRestart`, and
`UpdateService` implements the policy; the App supplies a Velopack-backed
`VelopackUpdateClient`. Rejected: calling Velopack directly from Core —
same reasoning as the process runner: Core stays free of platform side
effects and fully testable.

### D2: Velopack bootstrap via a custom `Main`

Per the Velopack WPF guidance, add a custom `Main` to `App.xaml.cs`
(`App.xaml` build action `Page` + `StartupObject`) that calls
`VelopackApp.Build().SetAutoApplyOnStartup(false).Run()` before WPF starts,
so applying an update does not load WPF unnecessarily. Rejected: calling
the bootstrap inside the `App` constructor — simpler, but pays WPF startup
during update apply. `SetAutoApplyOnStartup(false)` is **required**: Velopack
auto-applies downloaded updates on startup by default, which would install
an update without any user action and contradict the spec.

### D3: Startup check is fire-and-forget background work

`OnStartup` starts the check with `Task.Run`, exactly like the agent probe
round, after reading `update.checkOnStartup`. The result is surfaced by an
event the App marshals to the UI thread (as the probe round already does).

### D4: The adapter holds the pending update between calls

Velopack's `UpdateInfo` does not cross into Core; `VelopackUpdateClient`
keeps it in a field set by `CheckAsync`/`DownloadAsync` and consumed by
`ApplyAndRestart`. This keeps the Core contract simple and fake-friendly.
Rejected: passing an opaque handle through Core.

### D5: Silence is the default, including outside installed builds

`UpdateService` checks `IsInstalled` first and returns without touching the
feed when false (dev `dotnet run`, portable copy). Every failure
(check/download) is swallowed; the next startup retries. No dialogs — a
tray app that pops errors on a flaky network is worse than a delayed
update.

### D6: Tray notification plus an explicit tray-menu action (revised during apply)

After a successful download the App shows a tray notification through
H.NotifyIcon (informational), and reveals a tray context-menu item
("Установить обновление") whose click calls `UpdateService.Apply()`.
Originally the click handler was attached to the notification itself
(`TrayBalloonTipClicked`), but end-to-end verification showed the update
being applied without any user action — a transient notification is a poor
and ambiguous action surface. An explicit menu item is an unambiguous user
action and stays visible until used.

### D7: Config `update` section, default-on

`UpdateConfig { bool? CheckOnStartup }` on `Config`, with
`EffectiveCheckOnStartup => CheckOnStartup ?? true` so a missing section or
absent key means enabled. Round-trips through the existing `ConfigStore`
without touching its mechanics.

### D8: Verification split

Core policy is unit-tested with a fake `IUpdateClient` (enabled/disabled,
update present/absent, download failure, not-installed, silent errors).
Velopack integration cannot be exercised by `dotnet run`; the end-to-end
check builds a local release with `vpk pack`, installs it, points the feed
at a local folder, and verifies check/download/notify/apply by hand. This
manual step is documented in the tasks.

### D9: Feed override for local verification (added during apply)

`VelopackUpdateClient` reads an optional `CLIHUB_UPDATE_FEED` environment
variable: when set, it is a local folder (`SimpleFileSource`) or an HTTP(S)
URL (`SimpleWebSource`); otherwise the GitHub Releases `GithubSource` is
used. Rationale: the GitHub repository has no releases until the packaging
pipeline lands (a separate change), so without an override there is nothing
to check against and the end-to-end path cannot be verified. The override
is a development/QA affordance and does not change any spec-level behavior.

## Risks / Trade-offs

- [Unsigned installers may fail to run] → call out code signing as a
  release prerequisite; a signing change is a follow-up, not part of this
  one.
- [`vpk` and Velopack package versions must match] → pin both in the
  packaging follow-up; a mismatch breaks update compatibility.
- [Restart during apply] → the process exits and starts again; tray and
  hotkey are re-registered on startup, which is already idempotent.
- [Local end-to-end is heavier than unit tests] → keep it as a documented
  manual verification; do not attempt to run Velopack in tests.
- [Feed visibility] → the releases live in the public `trustsoft/CLIHub`
  repository, so no token is needed on the client.

## Migration Plan

None: an optional config section and new code paths; existing installs keep
working, and non-installed builds never hit the feed.

## Open Questions

- Which H.NotifyIcon notification API to use — resolved while implementing
  (does not change the spec).
