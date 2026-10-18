## Why

The settings page that configures watched folders, scan depth, discovery mode, ignored patterns, and the
background watcher is labelled "Projects & Paths". The label describes the contents but not the purpose: the page
is where automatic project discovery is configured, and nothing in the current name hints at discovery. Users
looking for "where do projects come from?" do not associate that with paths.

## What Changes

- Rename the settings page from "Projects & Paths" to "Project Discovery" in the sidebar and the page header.
- Keep the internal page key `projects`, so navigation triggers, persistence, and tests that address the page by
  key are unchanged.
- Keep the page description: "Watched directories, discovery rules, and the background watcher."
- Update the specifications, documentation, and code comments that name the page.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `preferences-ui`: The "Settings sections" requirement lists the page as "Project Discovery" instead of
  "Projects & Paths".
- `settings-theme`: The "Cards on the data pages" requirement names the page "Project Discovery".

Requirement identifiers keep their existing names ("Projects and Paths page options", "Projects and Paths
validation") so the unarchived deltas that modify them stay applicable; only the user-facing page name changes.

## Impact

- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — the page catalog title.
- Code comments and XML documentation that name the page.
- `docs/architecture/scanning.md` — page references.
- Specs: `preferences-ui` (settings sections) and `settings-theme` (data-page cards). The unarchived
  `projects-page-live-scanning` and `projects-summary-scan-coverage` deltas are updated to use the new page name
  so archiving in any order leaves the specs consistent.
- No behavior, configuration, or schema changes.
