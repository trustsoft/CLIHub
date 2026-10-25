# Proposal: rename the Settings page "Project Discovery" to "Project Scanning"

## Why

The Settings page titled "Project Discovery" (`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:307`,
page key `"projects"`) configures the machinery of scanning, not just its outcome: watched root
directories, per-root and default scan depth, ignored directory patterns, the repository discovery
mode, the background watcher, and the page-header "Scan Now" action
(`src/CLIHub/Views/SettingsWindow.xaml:170-174`). "Discovery" names only the outcome — the moment a
repository is found — while every control on the page governs how folders are scanned. The sibling
page "Projects" (`SettingsViewModel.cs:299`, key `"projects-registry"`) shows the resulting registry,
so the pair reads as "the list" and "the outcome" rather than "the list" and "the controls".

The user approved the pair **Projects + Project Scanning**: Page A stays "Projects" (the
registered-project registry) and Page B becomes "Project Scanning", with its description changing
from "Watched directories, discovery rules, and the background watcher."
(`SettingsViewModel.cs:308`) to "Watched directories, scanning rules, and the background watcher.".
"Scanning" names what the page's controls do; the registry page keeps the plain, unambiguous name.

## What Changes

- **Page title** — `SettingsViewModel.cs:307` `"Project Discovery"` → `"Project Scanning"`
  (sidebar item, page header, window title binding; user-visible).
- **Page description** — `SettingsViewModel.cs:308` "…discovery rules…" → "…scanning rules…"
  (user-visible).
- **Registry-page hint** — `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:54` "Discovered
  projects are managed by Project Discovery and are read-only here." → "…managed by Project
  Scanning…" (user-visible).
- **Registry summary card** — `src/CLIHub/Views/SettingsWindow.xaml:225` `Detail="Managed by
  Project Discovery"` → `Detail="Managed by Project Scanning"` (user-visible).
- **Navigation test** — `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs:55`
  asserts the new title; the test method is renamed to match
  (`Catalog_ProjectsPageIsNamedProjectDiscovery` → `Catalog_ProjectsPageIsNamedProjectScanning`,
  `:51`).
- **Internal comments / XML docs** that name the page, for accuracy: `SettingsWindow.xaml:152,182`;
  `src/CLIHub/Settings/SettingsApplicationService.cs:133,315`;
  `SettingsViewModel.cs:479,929,979`; `src/CLIHub.Core/Scanning/IProjectScanner.cs:50`;
  `src/CLIHub/ViewModels/Projects/ProjectModel.cs:110`.
- **Spec deltas** (MODIFIED requirements only, all requirement and scenario names preserved
  verbatim): `preferences-ui` ("Settings sections", "Projects and Paths page options", "Projects and
  Paths validation"), `settings-theme` ("Cards on the data pages", "Persistent page header",
  "Projects page summary cards"), `project-management` ("Settings project registry page").
  `project-scanning` was checked and contains no page-name references; it needs no delta.
- **Docs** — user/architecture references to the page name: `README.md`, `docs/architecture/
  scanning.md`, `docs/architecture/ui.md`, `docs/images/README.md`, `docs/glossary.md` (page
  references only), `docs/project-context.md`, plus one `Changed` line in `CHANGELOG.md` under
  `## 1.0.1 — 2026-10-24` and one user-facing `Improved` line in `RELEASE-NOTES.md` 1.0.1 (see
  design "Decision 6"). The domain concept "project discovery" (the process) keeps its name
  everywhere, including the glossary section `## Project discovery`.

Unchanged by design: the page key `"projects"`, the capability names (`project-scanning`,
`project-management`, `preferences-ui`, `settings-theme`), all configuration keys
(`discoveryMode`, `watchedDirectories`, `scanDepth`, `ignoredPatterns`, `watchedDirectorySettings`),
the public identifiers built on the discovery-mode concept (`ProjectDiscoveryMode`,
`ProjectDiscoveryModes`, `RepositoryDiscovery`), the "Repository discovery" selector label and its
options ("Git only", "Agents only", "Git and agents"), the "discovery chip" control wording, and the
provenance term "discovered".

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `preferences-ui` — MODIFIED "Settings sections" (page list, grouping sentence, and the "Projects
  page in the sidebar" scenario THEN), "Projects and Paths page options" (requirement body and the
  "Options offered" scenario WHEN), and "Projects and Paths validation" (requirement body). All
  requirement and scenario names are byte-identical to the main spec.
- `settings-theme` — MODIFIED "Cards on the data pages" (body and "Projects page cards" scenario
  WHEN), "Persistent page header" (body twice, and the WHEN lines of "Projects summary stays pinned",
  "Projects card shows the provenance split", "Watcher card shows scan coverage", "Projects content
  starts below the header"), and "Projects page summary cards" (body: who manages the discovered
  projects). All names preserved.
- `project-management` — MODIFIED "Settings project registry page" (the "Discovered project is
  read-only" scenario AND: the page states that Project Scanning manages discovered projects). All
  names preserved.

No `.openspec.yaml` is included: the change carries deltas (so `skip_specs` does not apply) and no
capability is retired, following `openspec/changes/archive/2026-10-25-spec-wording-cosmetics/` and
`.../2026-10-24-low-sweep-2/`, which shipped deltas with no `.openspec.yaml` and validated clean.

## Impact

- **Specs**: three delta files under this change; `openspec archive` applies them to the main specs.
  No Purpose changes (`preferences-ui`'s Purpose already says "project scanning"; verified).
- **Code**: four source files touched — one title/description pair, one hint string, one summary-card
  Detail string, and eight comment/XML-doc lines. No behavior, layout, binding, or identifier change.
- **Tests**: one test file; the assertion string and the test method name change. No other test
  references the page title or description (verified by grep).
- **Docs**: the files listed above; historical entries (`CHANGELOG.md` 0.9.x–1.0.0 lines,
  `RELEASE-NOTES.md` 0.9.7 lines 59 and 72, `openspec/changes/archive/**`) keep the wording of their
  time. `docs/images/settings-discovery.png` is not recaptured in this change; it may show the old
  page title until the next screenshot pass (noted in `docs/images/README.md`).
- **Risk**: minimal — string-only, user-visible; the two spec scenario sets that quote the page name
  are guarded by `openspec validate --all` (MODIFIED name preservation).

## Non-goals

- No behavior, layout, styling, or icon change of any page. In particular, the uncommitted icon
  edits in the working tree are out of scope and must stay untouched: `SettingsWindow.xaml:252`
  (`IconGlyphs.Refresh` on the DETECTION card), `SettingsViewModel.cs:349` (`IconGlyphs.Sync` on the
  Updates page), and the uncommitted `CHANGELOG.md` "Two Settings icons now match their actions…"
  line.
- No rename of the "Projects" page (page key `"projects-registry"`), and no change to any page key.
- No capability renames and no requirement/scenario renames — MODIFIED deltas only, names verbatim
  (the historical "Projects and Paths" wording inside two requirement names stays).
- No configuration-key, persistence-format, or public-identifier change; `ProjectDiscoveryMode` and
  friends keep their names.
- No change to the domain concept "project discovery" — the glossary section/term, the
  "Repository discovery" selector, the discovery chip, the discovery-mode options, and the
  "discovered" provenance term keep their wording.
- No rewrite of historical records (`CHANGELOG.md` pre-1.0.1 entries, `RELEASE-NOTES.md` released
  sections, archived changes, historical plans).
- No recapture or renaming of `docs/images/settings-discovery.png`.
- No `git commit`, `git push`, or `openspec archive` in this change's own tasks; those follow the
  repo approval policy (and must wait for the manual smoke confirmation).

## Dependencies

None. `openspec/changes/` holds no other active change (only `archive/`). The working tree at HEAD
`efd77f0` carries unrelated uncommitted icon edits that this change must neither depend on nor
disturb.
