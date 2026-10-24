# Low-severity sweep: version sentinels, update-detection test gaps, Git registry InstallPath precedence

## Why

`docs/backlog.md` carries three small, low-severity items the user approved as one batch on
2026-10-24. Each was re-verified against the current tree before inclusion (the backlog cites
state from 2026-10-20; all three claims still hold, with two precision notes recorded in design):

1. **Magic version sentinels** (`docs/backlog.md:115-121`) — the `"…"` pending placeholder and the
   `"unknown"` fallback are checked as bare literals across the agent surfaces.
2. **Update-detection test gaps** (`docs/backlog.md:159-169`) — two spec scenarios are
   code-verified but lack direct tests: the release-request timeout path and the "no indicator
   for a missing installation" card scenario.
3. **Registry InstallPath precedence can shadow a valid install** (`docs/backlog.md:137-146`) —
   `WindowsRuntimeEnvironment.ReadGitForWindowsInstallPath` returns the first non-empty registry
   value, so a stale HKLM value can shadow a valid HKCU/32-bit install, and the class has no
   direct unit test.

The three items are independent (agent UI text, update-detection tests, Windows runtime
resolution); bundling them follows the `hygiene-sweep` precedent and avoids three full
verify-and-archive cycles for individually small edits.

## What Changes

1. **Centralize the version sentinels** — a small `AgentVersionText` static helper in the app
   layer (`src/CLIHub/Helpers/`, beside `PreferenceSyncHelper`) owning the placeholder texts and
   the known/pending checks. Ten live sites across `AgentItem`, `AgentOption`, `AgentCardItem`,
   `AgentManagementViewModel`, and `AgentPaneController` switch to it with byte-identical display
   behavior. The legacy `Views/MainWindow.xaml.cs:206` copy is the documented untouched
   exception. Details in design A1–A3.
2. **Close the two update-detection test gaps** — one handler-stub test in
   `AgentReleaseClientTests` that delays past the configured probe timeout and asserts the
   timeout degrades to `null` (`Unknown` upstream), and one card test in `AgentCardItemTests`
   asserting no update indicator for an uninstalled agent whose plugin declares an `updateCheck`
   block. Test-only; no production code changes. Details in design B1–B3.
3. **Prefer a verifiable Git registry InstallPath** — the `IWindowsRuntimeEnvironment` seam grows
   a plural read (`ReadGitForWindowsInstallPaths`) returning every non-empty `InstallPath`
   candidate across the four registry views in order; `WindowsRuntimeLocator.LocateGitBash`
   probes each candidate's `bin\bash.exe` and uses the first that exists, falling through to the
   well-known and PATH steps when no registry candidate verifies. New tests: hermetic collector
   tests for view order/filtering, fake-seam locator tests for the stale-candidate preference,
   and a guarded HKCU-only real-registry test. The PowerShell 7 registry step is untouched.
   Details in design E1–E5.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Item-by-item justification:

- Item 1 is an internal refactor that preserves every rendered string byte-for-byte. The only
  spec that touches the placeholder text says "the list shows a placeholder (for example,
  `unknown`)" (`openspec/specs/agent-version/spec.md:79-83`) — wording the change keeps true, so
  no `agent-version` delta applies.
- Item 2 adds tests for scenarios that already exist verbatim in the specs: the timeout path is
  inside "Source unreachable" ("a network error or timeout",
  `openspec/specs/agent-update-detection/spec.md:90-93`) and the missing-installation indicator
  is "No indicator for a missing installation"
  (`openspec/specs/agent-ui/spec.md:330-333`). Test-only changes never take deltas.
- Item 3 changes which registry value wins on a machine where earlier views are stale, but the
  owning scenario pins only the source order — "resolved from the `GitForWindows` registry
  `InstallPath`, a well-known install location, or a PATH entry outside the Windows directory,
  in that order" (`openspec/specs/agent-commands/spec.md:99-103`) — and that order is preserved:
  registry candidates are still preferred over well-known and PATH, and "Unresolvable Git Bash
  fails safely" (`spec.md:111-114`) still holds when nothing verifies. The spec names no
  registry view (HKLM/HKCU, 64/32-bit) and no first-non-empty rule, so it does not pin the
  behavior being fixed.

Therefore the change opts out of specs (`skip_specs: true` in `.openspec.yaml`), following
`openspec/changes/archive/2026-10-24-hygiene-sweep/` and
`2026-10-20-fix-settings-open-delay/`. Design E6 records the rejected alternative of adding an
`agent-commands` scenario for the stale-value skip; open question Q1 lets the user override
during review.

## Impact

- Code:
  - `src/CLIHub/Helpers/AgentVersionText.cs` (new, ~30 lines).
  - `src/CLIHub/ViewModels/Agents/AgentItem.cs:107`,
    `src/CLIHub/ViewModels/Agents/AgentCardItem.cs:19,193-194`,
    `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:123,211,304,397,420`,
    `src/CLIHub/ViewModels/Agents/AgentPaneController.cs:182` — literal → helper (display
    behavior unchanged).
  - `src/CLIHub.Core.Windows/WindowsRuntimeLocator.cs` — `IWindowsRuntimeEnvironment` gains the
    plural Git read (`ReadGitForWindowsInstallPath` → `ReadGitForWindowsInstallPaths` +
    `IReadOnlyList<string>`), `WindowsRuntimeEnvironment` collects per-view candidates through a
    testable static collector, and `LocateGitBash`'s registry step becomes a loop over verified
    candidates. `ReadPowerShellCoreInstallPath` and `LocatePowerShell7` are untouched.
- Tests:
  - `tests/CLIHub.Tests/Helpers/AgentVersionTextTests.cs` (new); no existing test edited — the
    sentinel strings stay identical (`AgentCardItemTests.cs:28-29,41`,
    `AgentManagementViewModelTests.cs:224,297`, `AgentPaneControllerTests.cs:179` keep passing).
  - `tests/CLIHub.Core.Tests/Agents/AgentReleaseClientTests.cs` — `StubHandler` gains a
    token-respecting delay mode; one new timeout test.
  - `tests/CLIHub.Tests/ViewModels/Agents/AgentCardItemTests.cs` — one new uninstalled-agent
    indicator test.
  - `tests/CLIHub.Core.Windows.Tests/FakeWindowsRuntimeEnvironment.cs` — single `InstallPath`
    becomes a candidate list (three existing call sites updated mechanically);
    `tests/CLIHub.Core.Windows.Tests/WindowsRuntimeLocatorTests.cs` — two new locator tests;
    `tests/CLIHub.Core.Windows.Tests/WindowsRuntimeEnvironmentTests.cs` (new) — hermetic
    collector tests plus one guarded HKCU real-registry test.
- Docs: `docs/backlog.md` — the three swept entries rewritten to the resolved format, one hunk
  per entry.
- Specs: none (see Capabilities).
- Risk: low. Item 3 is the riskiest edit; it is confined to the registry step of
  `LocateGitBash` (the well-known and PATH steps and their tests are untouched), the
  stale-HKLM-falls-to-well-known behavior that exists today is preserved as
  all-candidates-stale → fall-through, and the DI registration shape (`WindowsRuntimeLocator`
  over the environment seam, `internal` types) is unchanged.

## Non-goals

- No change to the legacy `Views/MainWindow.xaml.cs` version copy (documented exception; the
  window is never constructed).
- No touching the application-update `"unknown"` sentinel domain
  (`src/CLIHub.Core/Updates/UpdateService.cs:132`,
  `src/CLIHub.Core/Updates/IUpdateVersionProvider.cs:9`) — that is CLIHub's own version, not the
  agent-version display this change consolidates.
- No placeholder text redesign, no localization, no new version-command behavior — the rendered
  strings `"…"`, `"unknown"`, `"Installed"`, `"Not installed"` stay byte-identical.
- No change to the PowerShell 7 registry precedence (machine-wide MSI layout, HKLM-only by
  design) and no change to the Git Bash well-known/PATH steps or their spec wording (the PATH
  flavor-validation backlog item stays parked).
- No sub-second probe-timeout preference or timing-infrastructure work just to speed the new
  timeout test up (it runs in ~1 s using the existing 1-second minimum).
- No changes to `docs/backlog.md` beyond the three resolved-format rewrites; the two other
  pre-existing parked entries near them stay untouched.
- No CHANGELOG entry now; entries land under the next release heading.
- No commit, push, or archive in this change's own tasks; those follow the repo approval policy.

## Dependencies

- None on other active changes (`openspec/changes/` holds no other active change).
- Implements the remedies recorded in three `docs/backlog.md` entries: "Magic version sentinels"
  (`:115-121`), "Update-detection test gaps: timeout path and missing-installation indicator"
  (`:159-169`, first recorded in `openspec/changes/archive/2026-10-20-agent-update-detection/`),
  and "Registry InstallPath precedence can shadow a valid install" (`:137-146`, parked from
  `openspec/changes/archive/2026-10-20-git-bash-resolution/`).
- Consistent with the `agent-commands` spec's pinned Git Bash resolution order
  (`spec.md:99-114`) — the change stays inside the registry step and does not reorder the three
  sources.
