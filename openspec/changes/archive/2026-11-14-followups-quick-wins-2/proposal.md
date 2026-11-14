## Why

Three small followup improvements to code hygiene and test safety:

1. **Item A**: `AgentReleaseClient.cs:119-120` uses null-forgiving suppressions on `updateCheck.Package!` and `updateCheck.Repo!` that cannot be removed via flow analysis today because `UpdateCheckSource.TryResolve` validates through attributed properties (`HasPackage`/`HasRepo`), breaking Roslyn's narrowing. Reshaping the API to return the validated address directly eliminates the suppressions and improves the API contract.

2. **Item B**: `ChromeWindow.cs:110-137` assigns `ptMaxPosition`/`ptMaxSize` to a managed `MINMAXINFO` copy that is never marshalled back because `WindowChromeHelper.ApplyMinTrackSize` re-reads the structure fresh from `lParam` and writes back only `ptMinTrackSize`. The assignments are dead code, verified by runtime smoke test in backlog evidence.

3. **Item C**: Backlog hygiene — mark four resolved entries as such, keeping their evidence intact.

## What Changes

### Item A: Remove UpdateCheckSource suppressions

- Extend `UpdateCheckSource.TryResolve` to return the validated address directly (chosen design: shape TBD, see design.md).
- Callers receive the non-null address and drop the `!` operators.
- All log messages and failure paths remain unchanged.
- Test assertions updated to verify the resolved address: package for npm, repo for githubReleases, null/false on failure.
- Production call sites: 3 (`AgentReleaseClient.cs:95`, `AgentUpdateAvailabilityService.cs:132`, `AgentProcessMatcher.cs:145`).
- Test call sites: 7 in `UpdateCheckSourceTests.cs`.

### Item B: Remove dead maximize clamp writes

- Remove lines 117-127 (monitor lookup, info struct creation, monitor info read) and lines 130-133 (dead `ptMax*` assignments) from `ChromeWindow.OnWindowMessage`.
- Update the XML doc on `OnWindowMessage` (lines 96-103) to remove the claim about constraining to the work area, keeping only the accurate minimum-tracking-size contract.
- Important: **do not** delete or alter `GetMonitorInfo`/`MONITORINFO`/`MonitorFromPoint` P/Invoke declarations in `User32.cs` — they are still used by `WindowPositioner.cs:148-157`. `MonitorFromWindow` (`User32.cs:55`) has no remaining caller and becomes dead with this change; it will be removed.
- No behavioral change: minimum tracking size behavior stays identical, maximum window positioning reverts to default WPF handling (verified by smoke test).

### Item C: Backlog entry status updates

Mark as resolved:
1. "AgentReleaseClient null-forgiving suppressions removable via a TryResolve out-parameter" → resolved by `followups-quick-wins-2`.
2. "Maximize work-area clamp writes a copy that is never marshalled back" → resolved by `followups-quick-wins-2` (removal chosen).
3. "Release-gate test can degrade…" → already fixed by change `release-gate-commandline-flake` (commit `dc78f7f`), now closed.
4. "Stale prose comment quotes the retired 'Detecting…' literal" → already fixed by change `quick-wins-sweep` (commit `ad74bd9`), now closed.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a pure refactor with no spec-level behavior change, so `skip_specs: true` in `.openspec.yaml`.

## Impact

- Code:
  - `src/CLIHub.Core/Models/Plugins/UpdateCheckSource.cs` (modified): extended `TryResolve` return signature.
  - `src/CLIHub.Core/Agents/AgentReleaseClient.cs` (modified): uses new address return.
  - `src/CLIHub.Core/Agents/AgentUpdateAvailabilityService.cs` (modified): uses new address return.
  - `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs` (modified): uses new address return.
  - `src/CLIHub/Views/ChromeWindow.cs` (modified): removes dead assignments and updates XML doc.
- Tests:
  - `tests/CLIHub.Core.Tests/Models/Plugins/UpdateCheckSourceTests.cs` (modified): updates all 7 call sites to assert resolved address.
- Docs:
  - `docs/backlog.md` (modified): mark 4 entries resolved.
- Build: no new package dependencies; no public API changes to `CLIHub.Core`.
- No P/Invoke declarations deleted (Item B constraint).

## Non-Goals

- Extending `UpdateCheckSource` with unrelated validations.
- Converting the `MINMAXINFO` clamping into a helper or separate handler.
- Removing the backlog entries entirely (keep evidence for historical record).

## Dependencies

None. Each item stands alone.
