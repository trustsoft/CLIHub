## Why

The Projects & Paths page mixes a draft editor (Save/Cancel) with a live runtime surface: the watcher status,
the "Scan Now" action, the per-directory scan action, and the summary cards all describe the saved configuration.
Adding a watched directory therefore does not scan it, "Scan Now" walks only the previously saved roots, and the
summary card counts only those roots — until the user saves and closes the window.

The page also has a hidden inconsistency: the per-directory scan action is not filtered through the saved list, so
scanning a freshly added, unsaved row registers the repositories it finds (temporarily polluting the project list)
and uses the saved depth and discovery mode instead of the row's chips.

## What Changes

- Changing any scanning setting on the Projects & Paths page applies it immediately: adding or removing a watched
  directory, changing a row's depth or discovery chip, changing the default depth or discovery mode, and adding or
  removing an ignored pattern each validate, persist atomically, reconfigure the scanner, and trigger a scan.
- A newly added watched directory is scanned without pressing Save; "Scan Now" and the summary card always describe
  the list shown on the page, and the per-directory scan action no longer has an unsaved-root path.
- The page states that scanning changes apply immediately.
- The footer Save and Cancel keep governing the remaining settings; Cancel does not revert scanning changes that
  were already applied.
- Save keeps writing the scanning values (idempotently) and reloading the scanner, so no second source of truth
  remains.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `preferences-ui`: The "Projects and Paths page options" requirement changes so scanning edits apply immediately
  and are rescanned in the background, gains scenarios for adding/removing a root, changing a row chip, and
  changing an ignored pattern, and states that Save/Cancel govern only the remaining settings.

## Impact

- `src/CLIHub/Settings/SettingsApplicationService.cs` — scanning-only validation/persistence entry point that
  reconfigures the scanner.
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — apply scanning changes on root add/remove, row chip
  changes, default selectors, and ignored-pattern changes; suppress applies while loading.
- `src/CLIHub/Views/SettingsProjectsPage.xaml` — note that scanning applies immediately; align the Scan Now tooltip.
- Tests: `SettingsApplicationServiceTests`, `SettingsViewModelApplicationTests`.
- `docs/architecture/scanning.md` — immediate-apply flow.
- No change to the persisted schema, watcher behavior, or scanner API.
