## Context

See `proposal.md` — Why and What Changes. Current state verified against the tree:

**Item A:**
- File: `src/CLIHub.Core/Agents/AgentReleaseClient.cs:119-120` — uses `updateCheck.Package!` and `updateCheck.Repo!` suppressions.
- Validation: `src/CLIHub.Core/Models/Plugins/UpdateCheckSource.cs:63-107` — `TryResolve(PluginUpdateCheck? updateCheck, out SourceKind kind, out Problem problem)` validates through `updateCheck.HasPackage` (:74) and `updateCheck.HasRepo` (:88).
- Production call sites (verified via grep):
  - `:95` in `AgentReleaseClient.cs:95` (current context: builds `request` with validated URL).
  - `:132` in `AgentUpdateAvailabilityService.cs:132` (discards kind, uses problem).
  - `:145` in `AgentProcessMatcher.cs:145` (reads kind, discards problem).
- Test call sites: 7 assertions in `tests/CLIHub.Core.Tests/Models/Plugins/UpdateCheckSourceTests.cs` (lines 11, 21, 35, 46, 60, 71, 85).

**Item B:**
- File: `src/CLIHub/Views/ChromeWindow.cs:110-137` — `OnWindowMessage(hwnd, msg, wParam, lParam, ref handled)`.
  - Lines 117-127: monitor lookup (`MonitorFromWindow`, `GetMonitorInfo`, `MONITORINFO` creation).
  - Lines 129-133: read `MINMAXINFO` from `lParam`, assign `ptMaxPosition` and `ptMaxSize` (dead writes).
  - Line 134: call `WindowChromeHelper.ApplyMinTrackSize(this, hwnd, lParam)`.
- File: `src/CLIHub/Views/WindowChromeHelper.cs:69-78` — `ApplyMinTrackSize(window, hwnd, lParam)`.
  - Line 74: reads fresh `MINMAXINFO` from `lParam` (independent copy, overwrites the one from line 129).
  - Line 77: marshals only `ptMinTrackSize` changes back to `lParam` (ignores `ptMax*` assignments).
- Usage of P/Invoke: `src/CLIHub/Interop/WindowPositioner.cs:148-157` uses `MonitorFromPoint`, `GetMonitorInfo`, and `MONITORINFO` — these three must remain in `User32.cs`. `MonitorFromWindow` (`User32.cs:55`) has no remaining caller and becomes dead with this change.
- XML doc: lines 96-103 claim the handler "constrains a maximized window to the monitor work area, so the taskbar never hides the window's bottom" — this is untrue; minimum tracking size only.
- Smoke test evidence: backlog entry dated 2026-11-13 documents user-confirmed smoke showing taskbar remains visible; the dead clamp changes nothing.

## Goals / Non-Goals

**Goals:**

- **Item A**: Reshape `TryResolve` so callers receive validated addresses directly and remove all `!` suppressions in call sites.
- **Item B**: Remove dead assignments and the now-unneeded monitor-lookup block; update the misleading XML doc.
- **Item C**: Mark backlog entries resolved with their original evidence intact.
- Preserve all log messages and error handling.
- All tests pass; no behavioral changes to the runtime.

**Non-Goals:**

- Item B: restructuring into a separate method or handler.
- Item B: marshalling the clamped structure back (removal is the chosen decision per backlog and brief).
- Item A: adding other validations to `TryResolve`.
- Removing backlog entries entirely.

## Decisions

### Decision A1: API Shape for UpdateCheckSource.TryResolve (Item A)

**Chosen shape: Add nullable address output with `[NotNullWhen(true)]` annotation.**

```csharp
internal static bool TryResolve(
  PluginUpdateCheck? updateCheck,
  out SourceKind kind,
  [NotNullWhen(true)] out string? address,
  out Problem problem)
```

- `address` = the validated `Package` for npm, the validated `Repo` for githubReleases, `null` otherwise.
- `problem` unchanged in every path (Problem.None on success, diagnostic Problem on failure).

**Rationale:**

- This is the backlog's proposed remedy (docs/backlog.md:92-93).
- Keeps the `Try*` convention and all existing test names (`TryResolve_Null_ReturnsFalse...`) valid — only the signature changes, not the return type.
- The `[NotNullWhen(true)]` attribute tells Roslyn that when the method returns `true`, the `address` parameter is non-null, despite its declared nullable type. This gives the compiler the proof that the attributed `HasPackage`/`HasRepo` properties destroy (the indirection that broke flow analysis in the status quo).
- Callers at `AgentReleaseClient:119` can now use `address` directly without a `!` operator because Roslyn's flow analysis sees the non-nullness guarantee via the attribute.
- Minimal API footprint: no new types, only one new output parameter with an attribute that was designed exactly for this pattern.

**Alternative considered and rejected: Target type (Option B from prior draft)**
- A record like `UpdateCheckResolution(SourceKind Kind, string Address)` would be cleaner in isolation, but it requires renaming every test method that includes `TryResolve` in its name if the return type changes from `bool` to `UpdateCheckResolution?`, and it adds a new type to the codebase for a single use case.

### Decision B1: Remove vs. Marshal Back (Item B)

**Chosen: Remove the dead `ptMax*` assignments and monitor lookup.**

**Rationale:**
- The dead assignments cause no user-visible harm (smoke test verified).
- Marshalling back would mean duplicating the monitor lookup logic inside `OnWindowMessage` while `WindowChromeHelper` makes the same query independently — that is wasteful and error-prone.
- The intended contract is minimum tracking size only; the misleading XML doc will be updated to match.
- Removal is simpler, cleaner, and pairs well with the smoke evidence in the backlog.

**Rejected alternative: Marshal back**
- Requires querying the monitor a second time and passing the clamped values back to lParam after `ApplyMinTrackSize` returns, adding complexity for dead functionality.

### Decision B3: XML Doc Update (Item B)

**Reword lines 96-103 from:**
> "Enforces the window's declared minimum size as the minimum tracking size and constrains a maximized window to the monitor work area, so the taskbar never hides the window's bottom."

**To:**
> "Enforces the window's declared minimum size as the minimum tracking size. Because handling `WM_GETMINMAXINFO` bypasses WPF's own handler, the minimum tracking size is set here so the window cannot be resized below `MinWidth`/`MinHeight`."

**Rationale:** The clamping was intended but never implemented (dead code), so the doc now accurately describes the actual behavior.

### Decision C1: Backlog Entry Status (Item C)

**Status resolution pattern:**
All four entries will use the existing backlog convention: add `**Status: Resolved by <change>** — <summary>.` at the top of each entry, keeping all other text intact.

1. Entry "AgentReleaseClient null-forgiving suppressions…" (line 83):
   - Status: `**Status: Resolved by `followups-quick-wins-2` — API reshaped to return validated addresses directly, suppressions removed from all call sites, test assertions extended to verify resolved address.**`

2. Entry "Maximize work-area clamp writes a copy…" (line 668):
   - Status: `**Status: Resolved by `followups-quick-wins-2` — dead `ptMax*` assignments removed; `MonitorFromWindow`/`GetMonitorInfo` block removed; minimum tracking size behavior unchanged; XML doc updated to accurately describe actual behavior; smoke test evidence recorded.**`

3. Entry "Release-gate test can degrade…" (line 315):
   - Status: `**Status: Resolved by `release-gate-commandline-flake` (2026-11-12, commit `dc78f7f`, archived `openspec/changes/archive/2026-11-12-release-gate-commandline-flake`) — happy path now uses `Timeout.InfiniteTimeSpan` while dedicated 200 ms test (`WindowsAgentProcessInspectorTests.cs:211`) keeps covering degradation; flake no longer reproduces.**`

4. Entry "Stale prose comment quotes the retired 'Detecting…' literal" (line 250):
   - Status: `**Status: Resolved by `quick-wins-sweep` (2026-11-12, commit `ad74bd9`, archived `openspec/changes/archive/2026-11-12-quick-wins-sweep`) — comment updated to `"Detecting"` (state name, not literal string); kept during change for file-scope cleanliness.**`

## Open Questions

### OQ-A1: Null vs. empty string for address on failure paths (Item A)

- When `updateCheck` is null or validation fails, should `address` be set to `null` or to an empty string `""`?
- Current behavior: validation failure returns `false` with `kind = SourceKind.None` and `problem = <diagnostic>`.
- **Tentative:** Set `address = null` on all failure paths (updateCheck is null, source unsupported/blank, missing package/repo) for consistency with the nullable annotation.

### OQ-B1: Smoke evidence transfer (Item B)

- The 2026-11-13 backlog smoke confirmed the taskbar stayed visible when the dead assignments were present. Does this evidence transfer to the post-removal state?
- **Answer (recorded here for clarity):** Yes. The assignments were dead writes that never affected marshalled-back data. Removing them cannot break what was already working, since the structure that gets marshalled back (`ApplyMinTrackSize:77`) never included the `ptMax*` values. The prior smoke is evidence that the removal is safe.

## Constraints and Risks

- **Item A:** All existing log messages and Problem values must stay identical; no new suppressions can be introduced at call sites.
- **Item B:** `GetMonitorInfo`, `MONITORINFO`, and `MonitorFromPoint` P/Invoke declarations must remain in `User32.cs` — `WindowPositioner.cs:148-157` depends on them. `MonitorFromWindow` (`User32.cs:55`) has no remaining caller after this change; it becomes dead and is removed in task 2.6.
- **Item B:** XML doc must be updated alongside the code change to prevent future confusion.
- **All:** No behavior change allowed — same URLs, same return values, same error paths.

## Verification Evidence

- Smoke test (backlog, 2026-11-13): maximized Settings window keeps taskbar visible — dead assignments cause no change.
- Current test suite: 7 `UpdateCheckSourceTests` assertions; all must remain green with the new return type.
- Build: `dotnet build CLIHub.sln` (0 warnings) and `dotnet test CLIHub.sln` (all green).
- Graph analysis: `node .gitnexus/run.cjs detect-changes --scope all --repo .` (pre-edit impact check + post-edit regression check).

## Why skip_specs: true

This change is a pure internal refactor (Item A & B) and documentation cleanup (Item C). No durable capability changes; behavior is identical before and after. Existing specs need no modifications.

## Implementation Deviations

### Dev-1: Task 1.6 — `Uri.EscapeDataString` for npm only, not both branches

Task 1.6 prescribed `Uri.EscapeDataString(address)` for both the npm and the GitHub URL branches. That literal would have escaped the GitHub repository separator (`owner/repository` → `owner%2Frepository`), changing the API route to `https://api.github.com/repos/owner%2Frepository/releases/latest` — which is a different URL and a behavioral change.

The existing test `GetLatestVersion_GitHubBlock_StripsLeadingVFromTagName` (`tests/CLIHub.Core.Tests/Agents/AgentReleaseClientTests.cs:46`) asserts the unescaped form:
```
https://api.github.com/repos/github/copilot-cli/releases/latest
```

The change's constraint is "same URLs" — no behavioral change. The accepted final shape escapes only the npm package name and passes the GitHub repo raw, matching pre-change behavior exactly:

```csharp
var url = kind == UpdateCheckSource.SourceKind.Npm
    ? string.Format(CultureInfo.InvariantCulture, NpmLatestUrl, Uri.EscapeDataString(address))
    : string.Format(CultureInfo.InvariantCulture, GitHubLatestUrl, address);
```

Both `!` suppressions are still removed (the goal of Item A). The npm branch continues to be escaped (scoped packages like `@scope/agent` contain `/`). The GitHub branch preserves the unescaped `owner/repository` separator.

### Dev-2: `MonitorFromWindow` becomes dead with this change

The original plan (design.md constraint, proposal.md Item B note, and tasks.md 2.3) claimed `WindowPositioner.cs:156-157` uses `MonitorFromWindow` and `GetMonitorInfo`, so both must remain. That was half wrong:

- `WindowPositioner.cs:148` uses `MonitorFromPoint` (not `MonitorFromWindow`).
- `WindowPositioner.cs:156-157` uses `GetMonitorInfo` and `MONITORINFO`.

Verified: `MonitorFromWindow` at `User32.cs:55` is the only occurrence in `src/` and `tests/`. No caller remains after the ChromeWindow monitor-lookup block is removed. The declaration is dead and will be removed in task 2.6.
