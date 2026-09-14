# Proposal: add-app-update

## Why

CLIHub has no way to update itself: the vision fixes Velopack as the update
mechanism, and the GitHub repository (`trustsoft/CLIHub`, public) is now the
intended release home, but nothing in the application checks for or applies
new versions. Users must reinstall by hand. This change adds the in-app
update capability, over a Velopack install with a GitHub Releases feed.

## What Changes

- New Core capability **app update**:
  - On startup, when `update.checkOnStartup` is enabled, run a check for a
    newer release in the background without blocking the UI.
  - When a newer release exists, download it in the background; when none
    exists, stay silent.
  - After a successful download, notify through the system tray; the user
    applies the update by acting on that notification, which installs the
    downloaded release and restarts the application.
  - Outside a Velopack-installed build (for example `dotnet run`), checks
    are skipped silently.
  - Check and download failures (offline, feed unreachable) are silent and
    are retried on a later startup — never a blocking dialog.
- New `update` section in `config.json` with `checkOnStartup` (default
  true).
- Application bootstrap gains the Velopack hook so updates can be applied.

## Capabilities

### New Capabilities

- `app-update`: startup update checks, background download, tray
  notification, apply-and-restart, silent no-op outside an installed build,
  and the `update` config section.

### Modified Capabilities

None — the tray icon already exists (`popup`), and the `update` config
section is owned by this new capability, like the probe sections are owned
by `agent-detection`.

## Impact

- `src/CLIHub.Core`: `UpdateService` (check/download/apply policy) behind an
  update-client abstraction; `Config` gains `update { checkOnStartup }`.
- `src/CLIHub.App`: Velopack-backed update client, Velopack bootstrap in a
  custom `Main`, startup wiring, tray notification with an apply action.
- Package: `Velopack` NuGet dependency.
- `tests/CLIHub.Core.Tests`: `UpdateService` coverage with a fake update
  client.
- Deployment: releases are published to GitHub Releases of
  `trustsoft/CLIHub`; the packaging/publishing pipeline and the
  self-contained-vs-framework-dependent decision are separate follow-ups.

Non-goals: the settings window and its manual "Check for updates" button
(the service exposes a check entry point for it later), the release
packaging/publishing pipeline and CI, code signing, and release channels
beyond the default.
