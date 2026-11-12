# Quick Wins Sweep

## Why

Four low-severity items are parked in `docs/backlog.md` (lines 123-134, 322-335, 474-486, 501-508), each awaiting a revisit trigger that has now arrived. Bundling them into one sweep change avoids the overhead of four separate OpenSpec cycles while allowing a single focused verification pass.

**1. Two glyph constants went unused** (backlog lines 474-486): `IconGlyphs.Diagnostic` (E9D9) and `IconGlyphs.Process` (E9F3) have zero references in `src/` and `tests/` as of HEAD `8c3338f`; the backlog trigger ("the next change that touches `IconGlyphs.cs`, or a dead-code sweep") is met by this sweep.

**2. Settings maximize button keeps a static "Maximize" tooltip when maximized** (backlog lines 501-508): `SettingsWindow.xaml:299` sets `ToolTip="Maximize"` statically; after maximizing, the tooltip remains "Maximize" while the button restores. The glyph already flips via a `WindowState` trigger (lines 302-309); the backlog trigger ("an accessibility/tooltip polish pass, or the next change that touches the Settings header") is met by this sweep.

**3. Plugin feed cancellation is reported as a download failure** (backlog lines 322-335): `PluginFeedClient.cs` catches timeout cancellation with `when (!cancellationToken.IsCancellationRequested)` but the subsequent general `catch (Exception)` intercepts external cancellation and reports generic errors instead of propagating `OperationCanceledException` to the caller. The caller (`BrowsePluginsDialogViewModel`) already catches `OperationCanceledException` separately (line 294), so propagation is safe. `FollowRedirectsAsync` also allows 5 HEAD requests (`hop < MaxRedirectHops`) at line 224, so a chain of exactly five redirects is rejected as "more than 5 hops" while the docs say "up to 5 hops". The backlog trigger ("the next change touching `PluginFeedClient`, or a user report of a spurious error dialog when cancelling a feed request") is met by this sweep.

**4. Stale prose comment quotes the retired "Detecting…" literal** (backlog lines 123-134): `AgentManagementViewModelTests.Detection.cs:347` still says `"Detecting…"` while the constant is now `AgentVersionText.Detecting` without ellipsis. The backlog trigger ("any edit to `AgentManagementViewModelTests.Detection.cs`") is met by this sweep.

## What changes

### 1. Remove two unused glyph constants

Delete `IconGlyphs.Diagnostic` and `IconGlyphs.Process` from `src/CLIHub/Theming/IconGlyphs.cs` (lines 24-25). These constants have zero references and remained only as dead weight after the Agent Detection icon swap.

**Justification for deletion:** `IconGlyphs` is a compile-time-checked catalog referenced from XAML via `{x:Static themes:IconGlyphs.Name}`, so an unreferenced member is dead code with no catalog-preservation value. Deliberate reintroduction on future need is straightforward.

### 2. Switch Settings maximize button tooltip with WindowState

Move the maximize/restore button's `ToolTip` (currently `"Maximize"` statically at `SettingsWindow.xaml:299`) into the existing `WindowState` trigger structure that already flips the glyph. The tooltip will read "Maximize" when `WindowState != Maximized` and "Restore" when `WindowState == Maximized`, matching the button's function.

**Implementation approach:** Extend the existing `Style.Triggers > DataTrigger` on `WindowState` (lines 305-307) with a second `Setter Property="ToolTip"` that binds to the button, or move the `ToolTip` attribute into the style baseline and override it in the maximized trigger.

### 3. Propagate external cancellation and align hop limit with docs

In `PluginFeedClient.cs`, add `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }` before the general `catch (Exception)` in both `GetFeedAsync` (line 122) and `DownloadPackageAsync` (line 204), so external cancellation reaches the caller instead of being swallowed and reported as a generic error. Also adjust the redirect hop limit in `FollowRedirectsAsync` (line 224) from `hop < MaxRedirectHops` to `hop <= MaxRedirectHops` or reword the rejection message from "more than 5 hops" to "5 or more hops", aligning the loop bound and the message with the documented "up to 5 hops" limit.

**Caller analysis:** `BrowsePluginsDialogViewModel` is the sole caller of these methods. Its `LoadFeedAsync` catches general `Exception` (line 228) but `InstallPluginAsync` explicitly catches `OperationCanceledException` (line 294) separately from general exceptions (line 298), so propagation is safe and does not crash the UI. The UI's existing cancellation handling is correct and will observe the newly-propagated exception.

**Hop-limit semantics decision:** The loop bound `hop < MaxRedirectHops` (5) allows hops 0–4 (5 requests total) and rejects hop 5, so the intent is "up to 5 hops" (inclusive) while the message says "more than 5 hops" (also inclusive). The simplest fix is to change `hop < MaxRedirectHops` to `hop < MaxRedirectHops` (unchanged) and reword the message to "5 or more hops" to accurately reflect the rejection threshold, preserving the existing 5-request limit without changing behavior.

### 4. Reword stale comment

Reword the prose comment at `AgentManagementViewModelTests.Detection.cs:347` from `"Detecting…"` (with ellipsis) to `"Detecting"` (no ellipsis) or rephrase to describe the state rather than quoting the literal, e.g., "leaving the cards stuck in the Detecting state". The constant is `AgentVersionText.Detecting`, and the comment documents the state, not the literal.

## Impact

- **1. Unused glyphs:** Build-only; no runtime or user-facing impact. The constants are pure dead code with no callers.
- **2. Maximize tooltip:** Cosmetic UI polish; the glyph already flips correctly, so only the tooltip text changes. No functional impact.
- **3. Cancellation propagation:** Observable behavior change for callers that explicitly handle `OperationCanceledException`. The sole caller already does so (verified at `BrowsePluginsDialogViewModel.cs:294`), so this fixes a latent bug where external cancellation was swallowed and reported as a generic error. The hop-limit wording change is accuracy-only; no behavior change.
- **4. Comment wording:** Documentation-only; no code or assertion change.

**Blast radius:** Zero regression risk. Items 1 and 4 are comment/dead-code only; item 2 is cosmetic tooltip text; item 3 is a bug fix where the caller already handles propagated cancellation correctly.

## Non-goals

- Not removing every unused constant in the codebase — only the two flagged by the backlog.
- Not auditing all tooltips for dynamic text — only the one identified by the backlog.
- Not redesigning the `PluginFeedClient` cancellation API — only fixing the current latent bug where external cancellation is swallowed.
- Not changing the redirect hop limit (stays 5 HEAD requests) — only aligning the message with the limit.

## Dependencies

None. This change is self-contained and does not depend on or block any other active change.
