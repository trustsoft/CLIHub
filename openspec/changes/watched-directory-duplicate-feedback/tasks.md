# Tasks: watched-directory-duplicate-feedback

## 1. Fix the duplicate branch

- [ ] 1.1 In `src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs`, replace the bare `return true;` at
  the end of `TryAddWatchedDirectory` (currently `:510`, the fallthrough after the `if (!this.WatchedDirectories.Any(...))`
  block at `:491-508`) with code that sets
  `this.WatchedDirectoryError = "That folder is already monitored.";` and returns `false`. Do not change
  any other branch in the method (empty input, non-absolute/missing folder, normalization failure, row
  creation, `ApplyScanningSettings()`, or the apply-failure rollback).

- [ ] 1.2 Re-read the full method after the edit and confirm every other branch is byte-for-byte unchanged
  from the version quoted in `design.md`.

- [ ] 1.3 Gate: run `dotnet build CLIHub.sln` and verify 0 warnings/errors.

## 2. Add caller-level tests

- [ ] 2.1 In `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs`, add a test for
  the typed-path caller: add a watched directory successfully, then attempt to add the same path again via
  `AddWatchedDirectoryCommand`. Assert `HasWatchedDirectoryError` is `true`, `WatchedDirectoryError` equals
  `"That folder is already monitored."`, `NewWatchedDirectoryText` still holds the attempted path
  (unchanged, not cleared), and the watched-root count (`WatchedDirectories.Count`, and/or
  `WatchedRootsSummary`) is unchanged from before the duplicate attempt (still 1, not 2).

- [ ] 2.2 In the same file, add a test for the picker caller: add a watched directory successfully (e.g.
  via `AddWatchedDirectoryCommand` or a first `AddWatchedDirectoryFromPickerCommand` call), then set up
  `fixture.Dialogs` to return the same path again and execute
  `AddWatchedDirectoryFromPickerCommand`. Assert `HasWatchedDirectoryError` is `true`,
  `WatchedDirectoryError` equals `"That folder is already monitored."`, and the watched-root count is
  unchanged from before the duplicate attempt.

- [ ] 2.3 Gate: run `dotnet test CLIHub.sln` and verify all tests pass, including the two new tests, with no
  regression in the existing add-success, missing-folder, apply-failure, and picker-success tests
  (`SettingsViewModelApplicationTests.cs:286`, `:302`, `:316`, `:704`).

## 3. Spec delta

- [ ] 3.1 Create `openspec/changes/watched-directory-duplicate-feedback/specs/preferences-ui/spec.md` with a
  `## MODIFIED Requirements` section for "Projects and Paths page options": restate the requirement text
  verbatim from `openspec/specs/preferences-ui/spec.md:329-331`, keep every existing scenario under it with
  its exact original name and body (`Options offered`, `Stored values loaded`, `Adding a watched directory
  applies immediately`, `Adding a watched directory with apply failure reports error`, `Scanner reload
  failure after a successful scanning apply`, `Removing a watched directory applies immediately`, `Ignored
  pattern changes apply immediately`, `Row chips change only that row`, `Defaults`, `New directory starts
  from the defaults`, `Per-directory scan`, `Row summary shows the per-directory count`, `Watcher status is
  read-only`, `Cancel keeps applied scanning changes`), and add one new scenario, for example:

  ```markdown
  #### Scenario: Adding a duplicate watched directory reports a message

  - **WHEN** the user adds a folder that is already in the watched-directories list (by normalized path)
  - **THEN** no new row is added and the count is unchanged
  - **AND** an explicit message states the folder is already monitored
  - **AND** the input the user entered or selected is not cleared
  ```

- [ ] 3.2 Gate: run `openspec validate --all` and confirm it reports 0 errors; quote the exact output.

## 4. Backlog closure

- [ ] 4.1 In `docs/backlog.md`, mark the item whose heading is
  `### Adding an already-watched folder is a silent no-op reported as success` (near `:236`; locate it by
  title, not by line number) as resolved: change the heading
  `### Adding an already-watched folder is a silent no-op reported as success` to
  `### Adding an already-watched folder is a silent no-op reported as success - **[done <date>]**` (use the
  implementation date) and add a resolution line referencing `watched-directory-duplicate-feedback`,
  following the pattern at `docs/backlog.md:273-276` (the `provenance-mark-contrast` resolution entry). Do
  not add a commit sha; the orchestrator records it after the commit.

## 5. Final verification

- [ ] 5.1 Gate: run `dotnet build CLIHub.sln` and verify 0 warnings/errors.

- [ ] 5.2 Gate: run `dotnet test CLIHub.sln` and verify all tests pass.

- [ ] 5.3 Gate: run `openspec validate --all` and confirm 0 errors; quote the exact output.

- [ ] 5.4 Run `graphify update .` to keep the graph current.
