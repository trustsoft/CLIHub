# Design: rename the Settings page "Project Discovery" to "Project Scanning"

## Verified current state

All line numbers re-verified against the working tree at HEAD `efd77f0` on 2026-10-25. The working
tree additionally carries uncommitted icon-only edits (see "Protected working-tree state").

### The two pages (source of truth for the page catalog)

- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:297-304` — page key `"projects-registry"`,
  title `"Projects"`, description "Registered projects, their provenance, and manually added
  entries." (**Page A, unchanged**).
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:305-313` — page key `"projects"`, title
  `"Project Discovery"` (`:307`), description "Watched directories, discovery rules, and the
  background watcher." (`:308`), header action key `ProjectsHeaderAction`, summary row key
  `ProjectsSummaryCards` (**Page B, renamed**). The page key stays `"projects"`.

### Occurrence census (page-name references)

Repo-wide grep for `project discovery` (case-insensitive), excluding `openspec/changes/archive/`,
`graphify-out/`, `artifacts/`, untracked scratch HTML, and binary files, returned exactly the
occurrences below. Category **page-name** = references the Settings page (rename); category
**domain** = the process/concept of discovering repositories (keep); category **history** = released
records (keep verbatim).

#### 1. User-visible UI strings (rename)

| # | Location | Current | New |
|---|---|---|---|
| U1 | `SettingsViewModel.cs:307` | `"Project Discovery",` | `"Project Scanning",` |
| U2 | `SettingsViewModel.cs:308` | `"Watched directories, discovery rules, and the background watcher.",` | `"Watched directories, scanning rules, and the background watcher.",` |
| U3 | `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:54` | `Text="Discovered projects are managed by Project Discovery and are read-only here."` | `Text="Discovered projects are managed by Project Scanning and are read-only here."` |
| U4 | `src/CLIHub/Views/SettingsWindow.xaml:225` | `Detail="Managed by Project Discovery"` | `Detail="Managed by Project Scanning"` |

U1/U2 render as the sidebar item label, the pinned page-header title, and the page description.
U3 is the hint under "REGISTERED PROJECTS" on the Projects page. U4 is the DISCOVERED summary card's
Detail on the same page (`RegistrySummaryCards` data template, `SettingsWindow.xaml:207-230`).

#### 2. Test (rename)

| # | Location | Current | New |
|---|---|---|---|
| T1 | `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs:55` | `Assert.Equal("Project Discovery", viewModel.Pages.Single(page => page.Key == "projects").Title);` | `Assert.Equal("Project Scanning", …)` |
| T2 | `SettingsNavigationTests.cs:51` | `public void Catalog_ProjectsPageIsNamedProjectDiscovery()` | `public void Catalog_ProjectsPageIsNamedProjectScanning()` |

T2 is beyond the literal brief (which named only `:55`) but follows the repo test naming pattern
`MethodOrScenario_Condition_ExpectedResult`; leaving "ProjectDiscovery" in the method name would
make the test name false. Verified: no other test asserts the page title or the description string
(grep for `Project Discovery|Project Scanning|discovery rules|Watched directories,` in `tests/`
returns only `:51`/`:55` and unrelated `ProjectDiscoveryMode` uses).

#### 3. Internal comments / XML docs (rename, for accuracy; not user-visible)

| # | Location | Current phrase | New phrase |
|---|---|---|---|
| C1 | `src/CLIHub/Views/SettingsWindow.xaml:152` | `<!-- Project Discovery actions: add a root or rescan the saved configuration. -->` | `<!-- Project Scanning actions: … -->` |
| C2 | `src/CLIHub/Views/SettingsWindow.xaml:182` | `<!-- Project Discovery summary cards, pinned to the page header. -->` | `<!-- Project Scanning summary cards, … -->` |
| C3 | `src/CLIHub/Settings/SettingsApplicationService.cs:133` | `Scanning settings edited on the Project Discovery page.` | `…on the Project Scanning page.` |
| C4 | `SettingsApplicationService.cs:315` | `Applies and persists scanning settings edited on the Project Discovery page and reconfigures the scanner.` | `…on the Project Scanning page…` |
| C5 | `SettingsViewModel.cs:479` | `<param name="projectScanner"> Scanner whose status is shown on the Project Discovery page.` | `…on the Project Scanning page.` |
| C6 | `SettingsViewModel.cs:929` | `Watched root directories edited on the Project Discovery page.` | `…on the Project Scanning page.` |
| C7 | `SettingsViewModel.cs:979` | `The ignored directory-name patterns edited on the Project Discovery page.` | `…on the Project Scanning page.` |
| C8 | `src/CLIHub.Core/Scanning/IProjectScanner.cs:50` | `Used by the per-directory scan action on the Project Discovery page.` | `…on the Project Scanning page.` |
| C9 | `src/CLIHub/ViewModels/Projects/ProjectModel.cs:110` | `discovered projects are managed by Project Discovery.` | `…managed by Project Scanning.` |

#### 4. Specs — page-name occurrences (MODIFIED deltas)

| # | Spec | Requirement (name preserved) | Occurrence |
|---|---|---|---|
| S1 | `openspec/specs/preferences-ui/spec.md:23` | "Settings sections" | body: page list `"Project Discovery"` and grouping sentence "…grouped with 'Project Discovery' under 'Engines & Repos'…" |
| S2 | `preferences-ui/spec.md:39` | "Settings sections" / scenario "Projects page in the sidebar" | THEN: `…next to "Project Discovery"` |
| S3 | `preferences-ui/spec.md:361` | "Projects and Paths page options" | body: "The Project Discovery page SHALL offer…" |
| S4 | `preferences-ui/spec.md:367` | "Projects and Paths page options" / scenario "Options offered" | WHEN: `the Settings window opens on "Project Discovery"` |
| S5 | `preferences-ui/spec.md:439` | "Projects and Paths validation" | body: "The Project Discovery page SHALL validate…" |
| S6 | `openspec/specs/settings-theme/spec.md:183` | "Cards on the data pages" | body: "The Project Discovery and CLI Agents pages…" |
| S7 | `settings-theme/spec.md:187` | "Cards on the data pages" / scenario "Projects page cards" | WHEN: `the Project Discovery page is shown` |
| S8 | `settings-theme/spec.md:260` (×2) | "Persistent page header" | body: `the "Project Discovery" page shows the projects, agents, and watcher cards` and `On the "Project Discovery" page the projects card…` |
| S9 | `settings-theme/spec.md:287` | "Persistent page header" / scenario "Projects summary stays pinned" | WHEN: `the "Project Discovery" page is active…` |
| S10 | `settings-theme/spec.md:294` | same requirement / scenario "Projects card shows the provenance split" | WHEN: `the "Project Discovery" page is shown` |
| S11 | `settings-theme/spec.md:301` | same requirement / scenario "Watcher card shows scan coverage" | WHEN: `the "Project Discovery" page is shown after a scan completed` |
| S12 | `settings-theme/spec.md:308` | same requirement / scenario "Projects content starts below the header" | WHEN: `the "Project Discovery" page is shown` |
| S13 | `settings-theme/spec.md:313` | "Projects page summary cards" | body: "…the discovered card SHALL state that Project Discovery manages them" |
| S14 | `openspec/specs/project-management/spec.md:203` | "Settings project registry page" / scenario "Discovered project is read-only" | AND: `the page states that project discovery manages discovered projects` → `the page states that Project Scanning manages discovered projects` |

`openspec/specs/project-scanning/spec.md` — **no page-name occurrences** (verified by grep; its
"discovery mode"/"Repository discovery mode" wording is the domain concept). No delta needed.
`preferences-ui`'s Purpose (`spec.md:5`) already says "project scanning"; no Purpose edit anywhere.

#### 5. Docs — page-name references (rename at implementation time)

| # | Location | Kind | Change |
|---|---|---|---|
| D1 | `README.md:40` | page ref + description mirror | `*Settings → Project Discovery: watched roots, scan depth and discovery rules, and the background watcher.*` → `*Settings → Project Scanning: watched roots, scan depth and scanning rules, and the background watcher.*` |
| D2 | `README.md:85` | page ref | `…or let CLIHub discover folders under a watched root in Settings → Project Discovery; Settings → Projects…` → `Settings → Project Scanning;` |
| D3 | `README.md:88` | Settings-area ref | `…theme and accent, logging, and project discovery in Settings.` → `…and project scanning in Settings.` |
| D4 | `docs/architecture/scanning.md:48` | page ref | `…refreshes one watched root from the Project Discovery page.` → `Project Scanning page` |
| D5 | `docs/architecture/scanning.md:63` | page ref | `The Project Discovery page uses it for the secondary line…` → `Project Scanning page` |
| D6 | `docs/architecture/scanning.md:82` | page ref | `The Project Discovery summary cards read project provenance…` → `Project Scanning summary cards` |
| D7 | `docs/architecture/scanning.md:90` | page ref | `Scanning settings edited on the Project Discovery page apply immediately…` → `Project Scanning page` |
| D8 | `docs/architecture/ui.md:53` | page ref | `…a summary row above the divider: on Project Discovery, the projects, agents, and watcher summary cards…` → `on Project Scanning,` |
| D9 | `docs/images/README.md:13` | page ref | `Settings → Project Discovery (watched roots, scanning rules, watcher status).` → `Settings → Project Scanning (…)` |
| D10 | `docs/images/README.md:54` | page ref | `…the Appearance, Project Discovery, and CLI Agents settings pages.` → `…Appearance, Project Scanning, and CLI Agents…` |
| D11 | `docs/glossary.md:40` | page ref | `a folder configured on the Project Discovery page.` → `Project Scanning page` |
| D12 | `docs/glossary.md:54` | page ref | `…shown in the watcher summary card on the Project Discovery page.` → `Project Scanning page` |
| D13 | `docs/glossary.md:127` | preference-area ref | `…runtime, agent probing, project discovery, appearance…` → `project scanning` (see Decision 5) |
| D14 | `docs/glossary.md:131` | page ref | `Scanning settings apply as soon as they change on the Project Discovery page;` → `Project Scanning page` |
| D15 | `docs/glossary.md:141` | page ref | `…for example PROJECTS, AGENTS, and DAEMON on Project Discovery.` → `on Project Scanning` |
| D16 | `docs/project-context.md:47` | page ref | `Scanning settings apply immediately from the Project Discovery page.` → `Project Scanning page` (not in the brief's list; see Decision 4) |
| D17 | `README.md:38` | alt-text mirror of the old description | `alt="CLIHub Settings: watched directories and discovery rules"` → `…and scanning rules"` (see Decision 7) |
| D18 | `CHANGELOG.md` (new line under `## 1.0.1 — 2026-10-24` → `### Changed`) | changelog | e.g. `- The Settings page "Project Discovery" is renamed to "Project Scanning" and its description now says "scanning rules"; the page key, configuration keys, and behavior are unchanged (\`preferences-ui\`, \`settings-theme\`, \`project-management\`)` — placed after the existing uncommitted icon line (`:24`), which is left byte-identical |
| D19 | `RELEASE-NOTES.md` (new line in `## 1.0.1` → `### Improved`) | user-facing notes | e.g. `- The Settings page Project Discovery is now called Project Scanning, matching what its controls do` (see Decision 6) |

#### 6. Kept verbatim (domain concept / history / identifiers)

- Domain concept "project discovery" (the process): `docs/glossary.md:38` heading `## Project
  discovery`, `:42` term definition, `:44` "registered by discovery", `:46` "Discovery never
  reclassifies…"; `README.md:49` "Automatic project discovery"; `docs/project-context.md:46`
  "Project discovery watches configured roots…"; every "discovery mode", "discovery chip",
  "Repository discovery" selector, and "discovered repositories/projects" phrase inside specs,
  source, and docs.
- History: `CHANGELOG.md:98,108,110` (0.9.x-era entries), `RELEASE-NOTES.md:59,72` (released 0.9.7
  section; the 1.0.1 section has no page-name reference), `openspec/changes/archive/**`.
- Identifiers and config: page keys `"projects"`/`"projects-registry"`, template keys
  (`ProjectsHeaderAction`, `ProjectsSummaryCards`, `RegistrySummaryCards`), enum/type names
  (`ProjectDiscoveryMode`, `ProjectDiscoveryModes`, `RepositoryDiscovery`, `DiscoveryModeOption`),
  config keys (`discoveryMode`, `watchedDirectories`, `scanDepth`, `ignoredPatterns`,
  `watchedDirectorySettings`), the selector options "Git only"/"Agents only"/"Git and agents".
- Assets: `docs/images/settings-discovery.png` (file name and bytes; see Decision 8).

### Protected working-tree state (uncommitted, NOT part of this change)

- `src/CLIHub/Views/SettingsWindow.xaml:252` — DETECTION summary card icon `IconGlyphs.Refresh`.
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:349` — Updates page icon `IconGlyphs.Sync`.
- `CHANGELOG.md:24` — "Two Settings icons now match their actions…".
- Untracked scratch files at the repo root (`project-discovery-naming.html`,
  `project-pages-naming.html`, `project-pages-naming-shortlist.html`, `settings-icon-audit.html`,
  `detect-icon-options.html`) — artifacts of the naming exploration; untouched and uncommitted.

Implementation stages edit lines in the same three files that carry these icon edits; every task is
worded to change only the named text so the icon hunks survive byte-identical.

## Decisions

1. **Approved pair: Projects + Project Scanning.** Page A stays "Projects" — it is the registry of
   registered projects and already owns the plain name; renaming it (e.g. to "Registry") would
   churn a second user-facing surface for no gain. Page B becomes "Project Scanning" because the
   page configures watched roots, scan depth, ignored patterns, discovery mode, the watcher, and the
   "Scan Now" action — "Scanning" names the controls, while "Discovery" named only the outcome. This
   pair was explicitly approved by the user; alternative pairs were explored in the parent session
   (scratch files listed above) and rejected before approval.
2. **Description wording** — "discovery rules" → "scanning rules" (`SettingsViewModel.cs:308`),
   approved together with the title, and mirrored where docs quote the description (D1, D17).
3. **Test method renamed** (`SettingsNavigationTests.cs:51`) in addition to the assertion (`:55`)
   so the test name keeps telling the truth under the repo's
   `MethodOrScenario_Condition_ExpectedResult` convention.
4. **`docs/project-context.md:47` added to the docs list** (beyond the brief): it says "Scanning
   settings apply immediately from the Project Discovery page" — a live page-name reference. Leaving
   it stale would contradict the compact context every agent reads first.
5. **Glossary `:127` updated** (beyond the brief's `~40`/`~54`): the Preferences bullet lists
   preference areas ("…agent probing, project discovery, appearance…") one line above `:131`, which
   names the page. Renaming `:131` but keeping `:127` would mix terms inside one bullet; the area
   governed by the renamed page is updated to "project scanning". The `## Project discovery` section
   heading and term definition (`:38`, `:42`) stay, per the brief.
6. **`RELEASE-NOTES.md` 1.0.1 gets one `Improved` line** (beyond the brief, which listed only
   CHANGELOG): the rename is user-visible and the 1.0.1 section is still open (uncommitted entries
   are being added to it at HEAD). The released 0.9.7 lines stay untouched. Drop this line without
   losing the change if the user prefers CHANGELOG-only.
7. **README alt text updated** (beyond the brief): `README.md:38` alt="…and discovery rules" quotes
   the old page description; updated to "scanning rules" with D1 so the caption and the image
   description agree.
8. **PNG not recaptured**: `docs/images/settings-discovery.png` will still show the old page title
   until the next screenshot pass. The tasks note this explicitly instead of blocking on it; the
   file name (a stable asset id referenced from `README.md:38`) is not renamed.
9. **MODIFIED deltas, not RENAMED**: requirement and scenario names are preserved byte-identical per
   repo convention, including the two requirement names that carry the historical "Projects and
   Paths" wording — they are history-safe identifiers, not the page's current name.
10. **No `.openspec.yaml`**: the change ships deltas and retires nothing, matching the
    validated-and-archived `2026-10-25-spec-wording-cosmetics` and `2026-10-24-low-sweep-2` changes.

## Alternatives rejected

- **Keep "Project Discovery"** — rejected: names only the outcome; the page is the control surface
  for scanning (watched roots, depth, patterns, mode, watcher, "Scan Now").
- **Rename both pages** (e.g. "Projects" → "Registry" / Page B unchanged or renamed differently) —
  rejected: doubles the user-visible churn; "Projects" is already the right name for the registry
  and the user approved keeping it.
- **Rename Page B to something other than "Project Scanning"** (e.g. "Scanning", "Repositories") —
  rejected in the parent session's shortlist review; "Project Scanning" keeps the noun pair
  symmetry with "Projects" inside "Engines & Repos" and says what the page configures.
- **RENAMED spec requirements** — rejected: OpenSpec rename semantics would change requirement
  names; the brief and repo convention require verbatim preservation with MODIFIED deltas.
- **Rename identifiers/config keys containing "Discovery"** (`ProjectDiscoveryMode`,
  `discoveryMode`, `RepositoryDiscovery`) — rejected: public API and persisted format; the discovery
  mode is a real domain concept (which directories count as repositories), not the page name.
- **Recapture `settings-discovery.png` in this change** — rejected: a screenshot pass needs a
  running build and throwaway configuration; noted as a follow-up instead of a gate.

## Deviations

- None from the approved brief. Additions beyond the brief's explicit lists are Decisions 3–7
  (test-method name, `docs/project-context.md:47`, glossary `:127`/`:131`/`:141`, RELEASE-NOTES
  line, README alt text) — each is a page-name reference or mirror that the census surfaced, and
  each is individually droppable.

## Open questions

None. The naming decision is user-approved; every occurrence is categorized above, and the
beyond-brief additions carry their rationale.
