# Design: watched-directory-duplicate-feedback

## Context

`WatchedDirectoryEditor` is the view-model section for the Project Scanning page's watched-directories
list (`src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs`). Adding a watched directory goes through
two commands and one shared validation method.

## Current state with file:line evidence

### The shared validation/add method

`TryAddWatchedDirectory(string value)` (`WatchedDirectoryEditor.cs:465-511`):

```csharp
465: private bool TryAddWatchedDirectory(string value)
466: {
467:     var text = value.Trim();
468:     if (text.Length == 0)
469:     {
470:         this.WatchedDirectoryError = "Enter a folder path or browse for one.";
471:         return false;
472:     }
473:
474:     if (!Path.IsPathFullyQualified(text) || !Directory.Exists(text))
475:     {
476:         this.WatchedDirectoryError = "Enter an existing absolute folder path.";
477:         return false;
478:     }
479:
480:     string path;
481:     try
482:     {
483:         path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(text));
484:     }
485:     catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
486:     {
487:         this.WatchedDirectoryError = "Enter a valid absolute folder path.";
488:         return false;
489:     }
490:
491:     if (!this.WatchedDirectories.Any(existing => string.Equals(existing.Path, path, StringComparison.OrdinalIgnoreCase)))
492:     {
493:         var row = this.CreateWatchedDirectoryRow(
494:             path,
495:             this.SelectedScanDepth.Value,
496:             this.SelectedDiscoveryMode.Mode);
497:
498:         this.WatchedDirectories.Add(row);
499:
500:         if (this.ApplyScanningSettings())
501:         {
502:             return true;
503:         }
504:
504:         // A failed apply must not leave an unpersisted root in the live list.
506:         this.WatchedDirectories.Remove(row);
507:         return false;
508:     }
509:
510:     return true;
511: }
```

Every invalid-input branch (`:467-472`, `:474-478`, `:480-489`) sets `WatchedDirectoryError` and returns
`false`. The apply-failure branch (`:500-507`) also returns `false`, after rolling back the just-added row,
but it does not set `WatchedDirectoryError` itself — it relies on `ApplyScanningSettings()`
(`:394-409`) calling `this.setValidationError(result.Error)`, which reports the failure through the
shared `ValidationError` surface instead. That is an existing, separate error channel and is unchanged by
this proposal.

The duplicate branch (`:491` condition is false, i.e. a row with the same path already exists) has no
`else`; control falls through the `if` block directly to `return true;` at `:510`, with no call to
`WatchedDirectoryError` and no side effect at all.

### Both callers

`AddWatchedDirectoryFromPicker` (`:441-454`):

```csharp
441: private void AddWatchedDirectoryFromPicker()
442: {
443:     var folder = this.dialogs.SelectFolder("Add Watched Directory");
444:     if (string.IsNullOrWhiteSpace(folder))
445:     {
446:         return;
447:     }
448:
449:     if (this.TryAddWatchedDirectory(folder))
450:     {
451:         this.WatchedDirectoryError = null;
452:         this.setValidationError(null);
453:     }
454: }
```

`AddWatchedDirectory` (`:456-463`):

```csharp
456: private void AddWatchedDirectory()
457: {
458:     if (this.TryAddWatchedDirectory(this.NewWatchedDirectoryText))
459:     {
460:         this.NewWatchedDirectoryText = string.Empty;
461:         this.setValidationError(null);
462:     }
463: }
```

**Verified caller behavior on `false`:** Neither caller has an `else` branch. On `false`, both simply do
nothing further:
- `AddWatchedDirectory` does not clear `NewWatchedDirectoryText` — the user's typed input is retained,
  matching every other invalid-input path (empty, non-absolute, normalization failure) which also returns
  `false` and relies on the caller's no-op `else` to keep the text.
- `AddWatchedDirectoryFromPicker` does not call `this.WatchedDirectoryError = null` — whatever
  `TryAddWatchedDirectory` set as `WatchedDirectoryError` during that call stays visible. There is no other
  code path between the `TryAddWatchedDirectory` call and the end of the method that could clear or
  overwrite it.

**Conclusion:** once `TryAddWatchedDirectory`'s duplicate branch sets `WatchedDirectoryError` and returns
`false`, both callers already surface that message correctly with no caller-side change needed. This
matches the brief's instruction to verify rather than assume; the picker path does **not** need an
additional change, and no stale message from a previous failure can linger across a single picker
invocation because `TryAddWatchedDirectory` either sets a fresh message (on any `false` branch) or clears
it implicitly by succeeding (`WatchedDirectoryError = null` in the `true` branch at `:451`). Across
separate invocations, `NewWatchedDirectoryText`'s setter already clears `WatchedDirectoryError` on any text
change (`:98-104`), and the picker's own `BrowseWatchedDirectory`/`AddWatchedDirectoryFromPicker` flow does
not retain state between calls that would cause a stale message to persist incorrectly.

### Existing tests

`tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs`:
- `AddWatchedDirectory_ExistingAbsolutePath_AddsRootAndClearsInput` (`:286-299`) — success path via the
  typed-path command.
- `AddWatchedDirectory_MissingFolder_ReportsError` (`:301-313`) — non-existent folder via the typed-path
  command.
- `AddWatchedDirectory_ApplyFailure_ReportsErrorAndKeepsInput` (`:315-339`) — apply failure via the
  typed-path command, asserting the input is kept.
- `AddWatchedDirectoryFromPicker_SelectedFolder_AddsRoot` (`:703-714`) — success path via the picker
  command; does not assert on the error state.

None of these cover the duplicate-path scenario for either caller.

### Backlog entry

`docs/backlog.md:235-255`:

```markdown
### Adding an already-watched folder is a silent no-op reported as success

- Severity: Low (UI feedback only; no data loss and no wrong persisted state)
- Evidence: `TryAddWatchedDirectory` adds a row only when no row with the same path exists
  (`src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs:491-508`) and then falls through to
  `return true` (`:510`) for the duplicate case. Both callers treat `true` as success:
  `AddWatchedDirectory` clears `NewWatchedDirectoryText` and the validation error (`:456-463`), and
  `AddWatchedDirectoryFromPicker` clears both errors (`:441-454`). So typing or picking a folder that
  is already monitored clears the input and shows nothing, while the list and the summary count stay
  unchanged — the user cannot tell whether the add worked or the folder was already there. No test
  covers the duplicate path: `SettingsViewModelApplicationTests` covers add-success (`:286`),
  missing folder (`:302`), and apply failure (`:316`) only.
- Proposed remedy: make the duplicate case distinguishable from success — either return `false` with
  a dedicated message (for example "That folder is already monitored") so the existing error surface
  and input retention apply, or return a result value the callers map to an informational message.
  Add one test per caller (typed path and picker) asserting the message and that the count is unchanged.
- Note: this quirk was previously mentioned only parenthetically in the refactoring portfolio entry
  ("The parked `TryAddWatchedDirectory` quirk remains out of scope") without its own evidence,
  remedy, or trigger; it is recorded here so the deferred finding is not lost.
- Trigger: a report of an add that "did nothing", the next change touching
  `WatchedDirectoryEditor` add/validation paths, or a redesign of the watched-directory list feedback.
```

This change is the "next change touching `WatchedDirectoryEditor` add/validation paths" trigger; the
implementation session should mark this item `[done]` per the archived-change convention (see
`docs/backlog.md:273` for the `- **[done 2026-10-31]**` heading pattern).

## Goals / Non-Goals

**Goals:**
- Make the duplicate-add case report an explicit, user-visible message on the existing error surface.
- Keep the user's typed input on the duplicate path (matching every other invalid-input branch).
- Close the backlog item.

**Non-Goals:**
- Any change to the stored-list invariant (no duplicate entry stored, existing entry kept) — this is
  already correct per `project-scanning`'s "Reject a duplicate directory" scenario and is not touched.
- Any change to row creation, `ApplyScanningSettings()`, or the apply-failure rollback behavior.
- Any UI redesign (new dialog, toast, etc.) — the existing `WatchedDirectoryError`/`HasWatchedDirectoryError`
  surface already renders under the watched-directory entry; reusing it needs no XAML change in either
  the typed-path row or wherever the picker-path message is bound, since `WatchedDirectoryError` is one
  shared property and both commands are on the same `WatchedDirectoryEditor` instance.

## Decisions

### Decision 1: Duplicate branch sets `WatchedDirectoryError` and returns `false`

**Chosen:** Replace the bare `return true;` at `:510` with a code path that sets
`this.WatchedDirectoryError = "That folder is already monitored."` and returns `false`.

**Rationale:**
- Matches the pattern of every other invalid-input branch in the same method (empty, non-absolute/missing,
  normalization failure) — all set `WatchedDirectoryError` and return `false`.
- Needs no new UI: the error surface (`WatchedDirectoryError` / `HasWatchedDirectoryError`) already exists
  and is already bound wherever the watched-directory entry's error is shown.
- Needs no caller change: both callers already have a correct no-op `else`-equivalent (absence of action)
  on `false`, verified above.

**Alternatives considered:**
- Return a richer result type (enum or result object) the callers map to messages: rejected as
  disproportionate — every other branch in this method already uses the `bool` + settable error-property
  pattern, and introducing a new return shape here would be inconsistent with the rest of the method and
  would require touching both callers for no behavioral gain.
- Have the callers detect the duplicate themselves (e.g., checking `WatchedDirectories` before calling
  `TryAddWatchedDirectory`): rejected — duplicates the normalization logic (`Path.GetFullPath`,
  `Path.TrimEndingDirectorySeparator`, `OrdinalIgnoreCase` comparison) that already lives in
  `TryAddWatchedDirectory`, and risks drifting from it.

### Decision 2: Exact message text

**Chosen:** `"That folder is already monitored."`

**Rationale:** Matches the brief's suggested wording, is consistent in tone and punctuation with the
sibling messages in the same method ("Enter a folder path or browse for one.", "Enter an existing absolute
folder path.", "Enter a valid absolute folder path."), and is unambiguous about why nothing was added.

**Alternatives considered:**
- "This folder is already watched.": rejected only as a style preference — "monitored" matches the
  existing `WatchedRootsSummary` wording ("1 root monitored", "N roots monitored") used elsewhere in the
  same class (`:130-135`), so "monitored" is the established term in this view-model, not "watched".

### Decision 3: Spec delta location and requirement choice

**Chosen:** `MODIFIED Requirements` in `openspec/specs/preferences-ui/spec.md`, under the existing
"Projects and Paths page options" requirement, adding a new scenario alongside the existing, verbatim
scenario "Adding a watched directory with apply failure reports error" (`preferences-ui/spec.md:353-358`).

**Rationale:**
- `preferences-ui`'s "Projects and Paths page options" requirement already owns the UI-feedback contract
  for adding a watched directory, including the sibling failure scenario for apply failures (`:346-358`),
  which has the same shape (error shown, input not cleared) as the duplicate case this change adds.
- `project-scanning`'s "Watched directories configuration" requirement owns a different layer: the
  stored-list invariant ("Reject a duplicate directory": no duplicate entry stored, existing entry kept —
  `project-scanning/spec.md:20-22`). That invariant is already true today and is not changed by this
  proposal; adding a duplicate-message scenario there would misattribute a UI-feedback change to a
  data-invariant requirement.
- The requirement already exists verbatim in the current spec, so `MODIFIED Requirements` applies (not
  `ADDED Requirements`), and the existing scenario names are preserved unchanged per the project's OpenSpec
  rules.

**Alternatives considered:**
- Add the scenario to `project-scanning` instead: rejected per the above — that capability's duplicate
  scenario already describes the correct persisted-state behavior and is not changed by this proposal; the
  new user-visible behavior (a message, an unchanged input) is strictly a `preferences-ui` concern.
- `skip_specs: true`: rejected — the brief requires a spec delta because this is a visible behavior change
  (a new error message appears where none did before), unlike a pure internal refactor.

## Risks / Trade-offs

**[Risk] A caller's `else`-less `false` handling silently relies on `TryAddWatchedDirectory` always setting
an error before returning `false`.** → Mitigation: the implementation task should audit that every `false`
return in `TryAddWatchedDirectory` — including the pre-existing apply-failure branch at `:507`, which does
not set `WatchedDirectoryError` directly — still produces a coherent, visible error state. The apply-failure
branch already relies on `ApplyScanningSettings()` routing through `this.setValidationError(result.Error)`
(a different, shared error surface), which is pre-existing and out of scope; this change only adds a new,
direct `WatchedDirectoryError` assignment to the duplicate branch and does not touch the apply-failure
branch.

**[Trade-off] No new automated UI test for the exact rendered error text in XAML.** → Accepted: the existing
test pattern asserts on the view-model's `WatchedDirectoryError`/`HasWatchedDirectoryError` properties and
the `NewWatchedDirectoryText`/`WatchedDirectories` state, not on rendered XAML, matching every other test in
`SettingsViewModelApplicationTests`.

## Open Questions

None. The duplicate branch's fix, the message text, the caller behavior, and the owning spec are all
settled from the verified evidence above; implementation is mechanical.
