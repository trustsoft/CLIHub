# Revise the Project Scanning page copy

## Why

The Settings → Project Scanning page carries duplicated and over-long copy. The "default for a new
folder / each row can override" fact appears in two card hints, the discovery hint runs to 154
characters, the ignored-patterns hint repeats its badge, and the page mixes "watched root",
"directory", and "folder" for the same object. A few labels use internal jargon ("DAEMON",
"Register") while the surrounding page is otherwise plain.

The user reviewed an HTML mockup of the revised page and approved the wording item by item. One
proposed change was rejected: `DAEMON` stays, because the card reports the whole background process
(`Watching` / `Scanning` / `Idle` / `Off` / `Error`) and `WATCHER / Watching` would be tautological.

## What changes

- Rewrite the page description, section titles, card titles and hints, button labels, placeholder,
  tooltips, empty-state text, and scan-depth validation messages.
- Standardize user-facing vocabulary on **folder**; the glossary keeps **watched root** as the
  concept name.
- Deduplicate the per-folder default/override rule into one line under the `SCANNING RULES` header.
- Move the immediate-apply note directly under the page header, above the first section, because it
  applies to the whole page.
- Remove the `Globs supported` badge, which repeated the ignored-patterns hint.
- Keep the option names `Git only`, `Agents only`, and `Git and agents`, all defaults, and all
  scanning behavior unchanged.

## Impact

- UI: `src/CLIHub/Views/SettingsProjectsPage.xaml`, `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs`,
  `src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs`,
  `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`,
  `src/CLIHub/Settings/SettingsApplicationService.cs`
- Docs: `README.md`, `docs/architecture/scanning.md`, `docs/images/README.md`
- Spec deltas: `preferences-ui` (the three field labels), `settings-theme` (section header and
  content start)
- No configuration-key, behavior, or layout change beyond moving the note line.
