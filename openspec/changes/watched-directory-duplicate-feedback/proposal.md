# Proposal: watched-directory-duplicate-feedback

## Why

Adding a folder that is already a watched root is a silent no-op that looks like success. In
`TryAddWatchedDirectory` (`src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs:465-511`), when a row
with the same normalized path already exists, the method skips the add block and falls through to
`return true` (`:510`) with no message. Both callers treat `true` as success:

- `AddWatchedDirectory` (`:456-463`) clears `NewWatchedDirectoryText` and the validation error.
- `AddWatchedDirectoryFromPicker` (`:441-454`) clears `WatchedDirectoryError` and the shared validation
  error.

So typing or picking an already-watched folder clears the input and shows nothing, while the watched list
and summary count are unchanged. The user cannot tell whether the add worked or the folder was already
there. This is tracked in `docs/backlog.md:235-255` ("Adding an already-watched folder is a silent no-op
reported as success").

## What changes

- `TryAddWatchedDirectory` returns `false` and sets `WatchedDirectoryError` to an explicit message (for
  example `"That folder is already monitored."`) when a row with the same normalized path already exists,
  instead of falling through to `return true`.
- No change to the non-duplicate branches: empty input, non-absolute/missing folder, normalization
  failure, row creation, `ApplyScanningSettings()`, and the rollback-on-apply-failure path all keep their
  current behavior byte-for-byte.
- No change to `AddWatchedDirectory` or `AddWatchedDirectoryFromPicker` themselves. Both already have a
  false-path behavior (do nothing further) that, once `TryAddWatchedDirectory` sets the error on the
  duplicate path, correctly surfaces the new message:
  - `AddWatchedDirectory`'s false path leaves `NewWatchedDirectoryText` untouched (input retained) and
    leaves `WatchedDirectoryError` as the message `TryAddWatchedDirectory` just set.
  - `AddWatchedDirectoryFromPicker`'s false path does the same: it does not call
    `this.WatchedDirectoryError = null`, so the message set by `TryAddWatchedDirectory` is left standing.
- Close the backlog item at `docs/backlog.md:235-255` as done, referencing this change.

## Non-goals

- No redesign of the watched-directory editor UI or a new dialog.
- No change to scan depth or discovery mode handling.
- No change to how an already-added duplicate is handled on the persistence or scanning side (the
  `project-scanning` capability's "Reject a duplicate directory" invariant — no duplicate entry stored,
  existing entry kept — is already correct and stays as-is).
- No change to row creation, `ApplyScanningSettings()`, or the apply-failure rollback path.

## Impact

**Modified code (implementation session):**
- `src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs` — `TryAddWatchedDirectory`'s duplicate branch
  (around `:491-510`).

**Modified specs:**
- `openspec/specs/preferences-ui/spec.md` — `MODIFIED Requirements`: "Projects and Paths page options"
  gains a new scenario for the duplicate-add case, alongside the existing, verbatim-preserved scenarios.

**Modified docs:**
- `docs/backlog.md` — mark the "Adding an already-watched folder is a silent no-op reported as success"
  item as done.

**Tests (implementation session, not written here):**
- Two new tests in `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs`: one for
  the typed-path caller (`AddWatchedDirectoryCommand`), one for the picker caller
  (`AddWatchedDirectoryFromPickerCommand`), both asserting the duplicate message and an unchanged root
  count.

**No breaking changes.** The success path (new folder) and every other validation branch are unchanged.

## Dependencies

None.
