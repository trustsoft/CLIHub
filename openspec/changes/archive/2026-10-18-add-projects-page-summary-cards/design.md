## Context

See `proposal.md` - Why. The Settings window already renders fixed summary cards for Project Discovery from the `ProjectsSummaryCards` template, and `SettingsViewModel` exposes that page's counts from the scanner status. The Projects page is backed by `ProjectRegistryViewModel`, which rebuilds its rows on every `IProjectService.ProjectsChanged` and after its own add/remove actions.

## Goals / Non-Goals

**Goals:**

- Reuse the existing summary-card visual language and the fixed page-header slot.
- Keep the counts owned by the registry section so they update immediately on manual changes, not on the next scan.
- Surface missing folders without adding a fourth card.

**Non-Goals:**

- No new persistence or configuration.
- No changes to the Project Discovery cards or their data source.
- No per-card navigation or click actions.

## Decisions

### Counts live in the registry section

Add `ManualCount`, `DiscoveredCount`, `TotalCount`, `MissingFolderCount`, `ProjectMixSummary`, `FolderStatusSummary`, and `HasMissingFolders` to `IProjectRegistrySection`, computed in `Load()` from the rebuilt rows and raised through `OnPropertyChanged`.

- **Why:** the section already refreshes on registry changes and owns the missing-folder flags; binding the header to it keeps the cards correct after add/remove without subscribing `SettingsViewModel` to the project service.
- **Alternative rejected:** reusing `SettingsViewModel.TotalProjectCount`/`ManualProjectCount`, which come from the scanner status and go stale after a manual add or removal.

### Folder health shown only when a folder is missing

The total card shows `ProjectMixSummary` ("N discovered · M manual") and, only when `HasMissingFolders` is true, a folder-health line (`FolderStatusSummary`) in `DangerBrush`. When every folder exists the line is collapsed and the summary is empty, so the three cards keep their original height.

- **Why:** keeps the requested three cards, avoids a reassuring line that carries no action, and makes stale entries visible at a glance without a fourth card or a new badge style.
- **Alternatives rejected:** always showing "All folders found", which adds noise; a fourth "Missing folders" card, which changes the requested trio; a badge next to the label, which is less explicit about the count.

### Header template keyed by page

Add a `RegistrySummaryCards` `DataTemplate` next to `ProjectsSummaryCards` and extend the summary `ContentControl` style with a trigger on `SelectedPage.Key = "projects-registry"`. The template binds the three cards to `ProjectRegistry.*`.

- **Why:** the summary area is already part of the fixed page header and the trigger pattern is established by the Discovery page.
- **Alternative rejected:** a separate header region per page, which duplicates the fixed-header layout.

### Tone and icon vocabulary

- PROJECTS: `FolderOpen` glyph, accent, muted label.
- MANUAL: `FolderAdd` glyph, accent; hint "Registered manually".
- DISCOVERED: `Sync` glyph, muted; hint "Managed by Project Discovery".
- Folder health: muted "All folders found" or danger "N folders missing".

## Risks / Trade-offs

- **Card height grows with the extra hint line** -> all three header cards stretch to the same height, so the row stays aligned; the other two cards keep their content top-aligned.
- **Counts are derived from the visible rows** -> they are exact for the current registry and inherently consistent with the list.
- **Folder checks are synchronous** -> same cost as the existing row flags; acceptable for a typical registry.

## Migration Plan

No data or configuration migration. Rolling back removes the template, the header trigger, and the section properties.
