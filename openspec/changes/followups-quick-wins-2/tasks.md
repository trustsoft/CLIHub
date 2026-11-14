## Stage 1: Item A — Remove UpdateCheckSource suppressions

### 1.1 Extend TryResolve signature with address output

- [x] 1.1 Update `src/CLIHub.Core/Models/Plugins/UpdateCheckSource.cs`, method `TryResolve` signature from `internal static bool TryResolve(PluginUpdateCheck? updateCheck, out SourceKind kind, out Problem problem)` to `internal static bool TryResolve(PluginUpdateCheck? updateCheck, out SourceKind kind, [NotNullWhen(true)] out string? address, out Problem problem)`.
- [x] 1.2 Update the method body to set `address = updateCheck.Package` when npm validation succeeds (line 81-82 region) and `address = updateCheck.Repo` when githubReleases validation succeeds (line 95-96 region); set `address = null` on all validation failure paths (null declaration, missing package, missing repo, unsupported/blank source).
- [x] 1.3 Add `using System.Diagnostics.CodeAnalysis;` at the top of the file if not already present (required for `[NotNullWhen(true)]`).
- [x] 1.4 Run `dotnet build CLIHub.sln` — confirm 0 warnings, 0 errors.

### 1.2 Update production call site: AgentReleaseClient

- [x] 1.5 Update `src/CLIHub.Core/Agents/AgentReleaseClient.cs:95` from `if (!UpdateCheckSource.TryResolve(updateCheck, out var kind, out var problem))` to `if (!UpdateCheckSource.TryResolve(updateCheck, out var kind, out var address, out var problem))`.
- [x] 1.6 Replace lines 118-120 (the `kind == UpdateCheckSource.SourceKind.Npm ? updateCheck.Package! : updateCheck.Repo!` ternary inside `string.Format`) with `var url = string.Format(CultureInfo.InvariantCulture, kind == UpdateCheckSource.SourceKind.Npm ? NpmLatestUrl : GitHubLatestUrl, Uri.EscapeDataString(address));` — no `!` operator needed.
- [x] 1.7 Run `dotnet build CLIHub.sln` — confirm 0 warnings, 0 errors, no `!` suppressions remain in `AgentReleaseClient.cs`.

### 1.3 Update production call site: AgentUpdateAvailabilityService

- [x] 1.8 Update `src/CLIHub.Core/Agents/AgentUpdateAvailabilityService.cs:132` from `if (UpdateCheckSource.TryResolve(updateCheck, out _, out var problem))` to `if (UpdateCheckSource.TryResolve(updateCheck, out _, out _, out var problem))` (add discard for the new `address` parameter).
- [x] 1.9 Run `dotnet build CLIHub.sln` — confirm 0 warnings, 0 errors.

### 1.4 Update production call site: AgentProcessMatcher

- [x] 1.10 Update `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs:145` from `UpdateCheckSource.TryResolve(plugin.UpdateCheck, out var kind, out _) && kind == UpdateCheckSource.SourceKind.Npm` to `UpdateCheckSource.TryResolve(plugin.UpdateCheck, out var kind, out _, out _) && kind == UpdateCheckSource.SourceKind.Npm` (add discard for the new `address` parameter).
- [x] 1.11 Run `dotnet build CLIHub.sln` — confirm 0 warnings, 0 errors.

### 1.5 Update test call sites

- [x] 1.12 Update all 7 test methods in `tests/CLIHub.Core.Tests/Models/Plugins/UpdateCheckSourceTests.cs` to use the new `TryResolve` signature with the `address` output parameter:
  - `TryResolve_Null_ReturnsFalseWithNone` (line 11): add `out var address` parameter; assert `address is null` and `kind == SourceKind.None && problem == Problem.None`.
  - `TryResolve_NpmWithPackage_ReturnsTrueWithNpm` (line 21): add `out var address`; assert `address == "@scope/agent"` and `kind == SourceKind.Npm && problem == Problem.None`.
  - `TryResolve_NpmWithBlankPackage_ReturnsFalseWithMissingPackage` (line 35): add `out var address`; assert `address is null` and `problem == Problem.MissingPackage`.
  - `TryResolve_GitHubReleasesWithRepo_ReturnsTrueWithGitHubReleases` (line 46): add `out var address`; assert `address == "owner/repository"` and `kind == SourceKind.GitHubReleases && problem == Problem.None`.
  - `TryResolve_GitHubReleasesWithBlankRepo_ReturnsFalseWithMissingRepo` (line 60): add `out var address`; assert `address is null` and `problem == Problem.MissingRepo`.
  - `TryResolve_UnknownSource_ReturnsFalseWithUnknownSource` (line 71): add `out var address`; assert `address is null` and `problem == Problem.UnknownSource`.
  - `TryResolve_BlankSource_ReturnsFalseWithUnknownSource` (line 85): add `out var address`; assert `address is null` and `problem == Problem.UnknownSource`.
- [x] 1.13 Run `dotnet test CLIHub.sln` — confirm all tests pass, including the 7 updated assertions.
- [x] 1.14 Run `dotnet build CLIHub.sln` — confirm 0 warnings, 0 errors.

## Stage 2: Item B — Remove dead maximize clamp writes

### 2.1 Remove dead assignments and monitor lookup from ChromeWindow

- [x] 2.1 Edit `src/CLIHub/Views/ChromeWindow.cs`, method `OnWindowMessage` (lines 110-137):
  - Delete lines 117-127 (monitor lookup: `var monitor = ...`, `if (monitor == ...)`, `var info = new ...`, `if (!GetMonitorInfo(...))`, all three return statements).
  - Delete lines 130-133 (dead assignments: `minMax.ptMaxPosition.X/Y` and `minMax.ptMaxSize.X/Y`).
  - Keep line 134 (`WindowChromeHelper.ApplyMinTrackSize(this, hwnd, lParam);`).
- [x] 2.2 Update XML doc (lines 96-103) from "constrains a maximized window to the monitor work area, so the taskbar never hides" to "Enforces the window's declared minimum size as the minimum tracking size. Because handling `WM_GETMINMAXINFO` bypasses WPF's own handler, the minimum tracking size is set here so the window cannot be resized below `MinWidth`/`MinHeight`."
- [x] 2.3 Verify `src/CLIHub/Interop/User32.cs` still contains `GetMonitorInfo`, `MONITORINFO`, and `MonitorFromPoint` declarations (they are used by `WindowPositioner.cs:148-157`). `MonitorFromWindow` (`User32.cs:55`) has no remaining caller and becomes dead.
- [x] 2.4 Run `dotnet build CLIHub.sln` — confirm 0 warnings, 0 errors.

### 2.2 Verification

- [x] 2.5 Run `dotnet test CLIHub.sln` — confirm all tests pass (no test directly covers the removed code; existing window tests verify the remaining minimum tracking size behavior).
- [x] 2.6 Remove the now-unused `MonitorFromWindow` `[LibraryImport]` declaration from `src/CLIHub/Interop/User32.cs` (lines 51-55: XML doc comment, attribute, and method signature). Gate on `dotnet build CLIHub.sln` (0 warnings) and `dotnet test CLIHub.sln` (all pass). Verify that no `MonitorFromWindow` reference remains in `src/` or `tests/`.

**Optional sanity check (non-blocking):** On the next manual run, launch the application, open Settings, maximize the window, and verify the taskbar remains visible and the window is not clipped. (This confirms the prior 2026-11-13 smoke evidence transfers to the post-removal state: the dead assignments were never involved in the marshalled-back data, so removing them preserves the behavior.) **After task 2.6:** confirm no `MonitorFromWindow` reference remains in `src/` or `tests/`.

## Stage 3: Item C — Update backlog entries

### 3.1 Mark entries resolved

- [x] 3.1 Edit `docs/backlog.md`:
  - Line 83, "AgentReleaseClient null-forgiving suppressions…": add at the top of the entry `**Status: Resolved by `followups-quick-wins-2` — API reshaped to return validated addresses directly, suppressions removed from all call sites, test assertions extended to verify resolved address.**`.
  - Line 668, "Maximize work-area clamp…": add `**Status: Resolved by `followups-quick-wins-2` — dead `ptMax*` assignments removed; monitor lookup block removed; minimum tracking size behavior unchanged; XML doc updated to accurately describe actual behavior; smoke test evidence recorded.**`.
  - Line 315, "Release-gate test can degrade…": add `**Status: Resolved by `release-gate-commandline-flake` (2026-11-12, commit `dc78f7f`, archived `openspec/changes/archive/2026-11-12-release-gate-commandline-flake`) — happy path now uses `Timeout.InfiniteTimeSpan` while dedicated 200 ms test keeps covering degradation.**`.
  - Line 250, "Stale prose comment quotes…": add `**Status: Resolved by `quick-wins-sweep` (2026-11-12, commit `ad74bd9`, archived `openspec/changes/archive/2026-11-12-quick-wins-sweep`) — comment now reads "Detecting" (state name).**`.

## Stage 4: Verification and cleanup

### 4.1 Build and test gates

- [x] 4.1 Run `dotnet build CLIHub.sln` — confirm 0 warnings / 0 errors (`EnforceCodeStyleInBuild=true`).
- [x] 4.2 Run `dotnet test CLIHub.sln` — confirm all pass (baseline: 1793 tests total; 0 failed, 0 skipped).
- [x] 4.3 Run `openspec validate --all` — confirm 0 errors.

### 4.2 Graph analysis

- [x] 4.4 Run `node .gitnexus/run.cjs detect-changes --scope all --repo .` — confirm no unexpected blast radius or issues reported.

### 4.3 Final cleanup

- [x] 4.5 Run `graphify update .` — refresh the knowledge graph to reflect the refactored code.

## Build / Test Gates

Each stage ends with a build gate. Failures stop the stage:
1. After 1.1–1.4 (UpdateCheckSource refactor and all call sites): `dotnet build CLIHub.sln` (0 warnings, 0 errors).
2. After 1.5 (test updates): `dotnet test CLIHub.sln` (all green).
3. After 2.1–2.2 (ChromeWindow cleanup): `dotnet build CLIHub.sln` (0 warnings, 0 errors); the optional maximize sanity check stays non-blocking.
4. After 3.1 (backlog updates): `openspec validate --all` (0 errors).
5. Final: `node .gitnexus/run.cjs detect-changes --scope all --repo .` and `graphify update .`.
