# Unify the missing-folder labels on the Projects page

## Why

The Settings > Projects page labels the same condition — a registered project whose folder no
longer exists — two different ways:

- The PROJECTS summary card shows the danger-tone line `1 folder missing` / `{n} folders missing`
  (`ProjectRegistryViewModel.cs:78-83`, rendered via `SettingsWindow.xaml:190-194`).
- Each broken project row shows a danger-tone chip reading `Folder not found`
  (`SettingsProjectRegistryPage.xaml:144`, style `ProjectMissingChip` at `:12-25`).

The card already uses "missing"; the row uses "not found". Standardizing both on "missing" makes the
two surfaces read as one consistent status, and the chosen chip text `Folder missing` echoes the
wording the project-management spec already uses ("marked as missing its folder",
`openspec/specs/project-management/spec.md:204`).

## What changes

Change exactly one visible string and its explanatory comment in
`src/CLIHub/Views/SettingsProjectRegistryPage.xaml`:

1. Row chip text: `Folder not found` → `Folder missing` (`:144`).
2. Comment above the `ProjectMissingChip` style: update the quoted chip text from `"Folder not found"`
   to `"Folder missing"` (`:10-11`).

Nothing else about the chip changes: the `IsFolderAvailable` visibility trigger (`:17-19`), the
`DangerBrush` border/foreground (`:14, :24`), the `ProjectMissingChip`/`ProjectMissingChipText`
style keys, and the chip's placement after the provenance mark (`:141-145`) all stay as they are.

## Impact

- UI: `src/CLIHub/Views/SettingsProjectRegistryPage.xaml` (lines 10-11 comment, line 144 chip text).
- No view-model, service, or Core change: the chip text is a hardcoded XAML literal, not a bound
  property.
- No test change: no test asserts the chip text. The change-folder error text
  `Folder not found: {ex.Message}` (`ProjectRegistryViewModel.cs:261`) and its test
  (`ProjectRegistryViewModelTests.cs:383`) are unrelated and untouched.
- No spec delta: no durable spec pins the chip string `Folder not found` (see design.md). Declared
  `skip_specs: true` in `.openspec.yaml`.
- No configuration, layout, or behavior change.

## Non-goals

- Changing the card summary strings `1 folder missing` / `{n} folders missing`
  (`ProjectRegistryViewModel.cs:78-83`) or the `FolderStatusSummary`/`MissingFolderCount`/
  `HasMissingFolders` member names.
- Changing the change-folder error text `Folder not found: {ex.Message}`
  (`ProjectRegistryViewModel.cs:261`) or its test assertion (`ProjectRegistryViewModelTests.cs:383`).
  That is a transient validation error about a path the user just picked, not one of the two
  missing-folder status labels.
- Changing the chip's visibility trigger, danger styling, style keys, or placement.
- Adding a manual smoke task: the change is a one-word XAML literal swap with no binding or
  layout change, and the chip's visibility and styling are covered by the existing
  `IsFolderAvailable` trigger.

## Dependencies

None. This change has no dependency on other open or archived changes.
