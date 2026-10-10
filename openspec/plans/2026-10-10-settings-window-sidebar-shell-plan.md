# GitNexus Engineering Plan

> Task: Restructure the CLIHub Settings window into a sidebar-navigated shell (3 groups / 8 pages) without changing preference behavior.
> Evidence verified at commit 569030ccfe7b17856e510d27bd47318d01cb5e14; GitNexus index 4 commits behind HEAD, refresh skipped: UI-only change, source-weighted.
> Evidence provenance schema 2; global dirty digest f71f0a9beef3265303d74182be8ad0f2b8f586832f464b23e3d13a15394feb81; cited-path manifest 7 sorted entries; exact generated plan path excluded.

## Objective (§1)

Follow OpenSpec change `settings-window-sidebar-shell`: replace the single scrolling Settings page with a sidebar-navigated shell of three groups (General; Engines & Repos; System) and eight pages, re-home existing controls to their pages, and show an empty state on pages without implemented settings. No preference is added, removed, or renamed; `config.json` and `CLIHub.Core` are untouched.

## Current Behaviour (§2–3)

- `SettingsViewModel` (`src/CLIHub/ViewModels/SettingsViewModel.cs:31-364`) owns every field (startup toggles, runtime, path display, hotkey text/parts, probe TTL/timeout, update check), validation, and the Save/Cancel/CheckForUpdates commands. No navigation state exists.
- `SettingsWindow.xaml` renders all controls in one `StackPanel` inside a `ScrollViewer` (`:62-182`) under a drawn chrome header (`:44-60`) and footer (`:185-211`).
- `SettingsWindow.xaml.cs` handles header drag, Escape-to-close, and hotkey capture; `ShowSettings()` sizes the window height to the launch window.
- `SettingsStyles.xaml` holds window-scoped dark styles and merges `Controls.xaml` for the palette.

## Findings (§4–5) — load-bearing only

- [verified] Tests instantiate `SettingsViewModel` directly (`tests/CLIHub.Tests/ViewModels/SettingsViewModelApplicationTests.cs`: `Cancel_DoesNotPersistAndRequestsClose`, `Save_ValidDraft_PersistsAndRequestsClose`, `Save_InvalidDraft_DoesNotPersistOrClose`; `SettingsOperationLifetimeTests.cs`: `CheckForUpdatesCommand_WhenApplicationCancels_ReportsCancellation`). XAML restructure does not touch them.
- [graph] `context(SettingsViewModel)` — incoming calls: `ServiceRegistration.AddClIHubServices`, tests; outgoing: `extends ObservableObject`; `epistemic: lower-bound` (dispatch boundary 7); no processes. Blast radius is composition + tests only.
- [verified] `SettingsStyles.xaml` already provides `FieldLabel`, `HintText`, `SettingsInput`, `HotkeyField`/`HotkeyChip`, `SettingsCheck`, `SegmentGroup`/`SegmentItem`, `AccentButton`, `NeutralButton`, `FooterStrip`. Palette brushes come transitively from `Controls.xaml`; no new colors needed.
- [verified] `SettingsWindow.xaml.cs` code-behind is chrome/hotkey only; navigation is a view-model concern.
- [assumed] No other view consumes `SettingsWindow` besides `SettingsLauncher`/`ISettingsLauncher`; re-verify at implementation.

## Proposed Changes (§6)

- `src/CLIHub/ViewModels/SettingsViewModel.cs` — add `SettingsGroup` and `SettingsPage` records (group label, page key, title, implemented flag), a static 3-group/8-page catalog, and `SelectedPage` (default "General & Startup"). Existing properties/commands unchanged.
- `src/CLIHub/Views/SettingsWindow.xaml` — replace the flat content with a shell grid: existing header + a left sidebar `ListBox` bound to the catalog + a `ContentControl` host (page `DataTemplate` selected by page key, shared empty-state template) + unchanged footer.
- New `src/CLIHub/Views/Pages/*.xaml` UserControls, one per implemented page: `SettingsGeneralPage` (startup toggles), `SettingsHotkeysPage` (hotkey field + chips), `SettingsTerminalPage` (runtime segments), `SettingsAgentsPage` (probe fields), `SettingsAppearancePage` (path display segments), `SettingsUpdatesPage` (update toggle + check + status). Pages bind to the shared `SettingsViewModel`.
- `src/CLIHub/Themes/SettingsStyles.xaml` — add `SidebarGroupLabel`, `SidebarItem` (+ selected accent), `PageHeader` styles reusing existing brushes.
- `src/CLIHub/Views/SettingsWindow.xaml.cs` — unchanged except the hotkey handlers move with the hotkey page if page markup needs them (keep handlers in the window and resolve by name, or move to the page code-behind; prefer keeping them on the window).

## Implementation Sequence (§7)

1. Add catalog records + `SelectedPage` to `SettingsViewModel`; build.
2. Rework `SettingsWindow.xaml` shell (sidebar + content host + footer); keep header/footer markup; window still builds, existing pages empty.
3. Extract the six implemented pages into UserControls and move the bindings; verify each control still binds to the same `SettingsViewModel` property.
4. Add the shared empty-state template and route the two placeholder pages (Projects & Paths; Telemetry & Logs) to it.
5. Add sidebar/page styles to `SettingsStyles.xaml`; widen default window and `MinWidth`.
6. Add navigation tests; run build + tests.

## Test Strategy (§8)

- New `tests/CLIHub.Tests/ViewModels/SettingsNavigationTests.cs`: (a) catalog has exactly 3 groups and 8 pages in the specified order; (b) default `SelectedPage` is "General & Startup"; (c) changing `SelectedPage` raises `PropertyChanged`. Input → action → assertion.
- Keep existing `SettingsViewModelApplicationTests` and `SettingsOperationLifetimeTests` green (no `SettingsViewModel` API removals).
- Verification commands: `dotnet build CLIHub.sln`; `dotnet test CLIHub.sln`; `openspec validate settings-window-sidebar-shell --strict`.

## Implementation Context (§11) — mini-pack

```json
{
  "change": "settings-window-sidebar-shell",
  "open_spec_change": "openspec/changes/settings-window-sidebar-shell",
  "roadmap": "openspec/plans/2026-10-10-settings-window-roadmap.md",
  "primary_symbols": ["SettingsViewModel", "SettingsWindow"],
  "page_map": {
    "General & Startup": "StartWithWindows, ShowWindowOnStartup",
    "Hotkeys & Launchers": "HotkeyText, HotkeyParts, capture handlers",
    "Terminal Profiles": "SelectedRuntime",
    "CLI Agents": "ProbeTtlText, ProbeTimeoutText",
    "Appearance": "SelectedPathDisplay",
    "Updates": "CheckForUpdatesOnStartup, CheckForUpdatesCommand, UpdateMessage",
    "Projects & Paths": "placeholder",
    "Telemetry & Logs": "placeholder"
  },
  "evidence_provenance": {
    "schema_version": 2,
    "head_commit": "569030ccfe7b17856e510d27bd47318d01cb5e14",
    "generated_plan_path": "docs/plans/2026-10-10-gitnexus-plan-settings-window-sidebar-shell.md",
    "global_dirty_digest": {
      "algorithm": "sha256",
      "canonicalization": "gitnexus-evidence-provenance-v2 NUL-framed UTF-8 records",
      "value": "f71f0a9beef3265303d74182be8ad0f2b8f586832f464b23e3d13a15394feb81"
    },
    "cited_path_manifest": [
      "src/CLIHub/ServiceRegistration.cs",
      "src/CLIHub/Themes/SettingsStyles.xaml",
      "src/CLIHub/ViewModels/SettingsViewModel.cs",
      "src/CLIHub/Views/SettingsWindow.xaml",
      "src/CLIHub/Views/SettingsWindow.xaml.cs",
      "tests/CLIHub.Tests/ViewModels/SettingsOperationLifetimeTests.cs",
      "tests/CLIHub.Tests/ViewModels/SettingsViewModelApplicationTests.cs"
    ]
  }
}
```

## Assumptions and Open Questions (§12)

- [assumed] `SettingsWindow` has no other consumer besides the launcher service (verify at step 2).
- GitNexus index is 4 commits behind HEAD; findings are source-weighted. Re-index is optional for this UI-only change.
- Exact default window dimensions/sidebar width: settle against the mockup; no spec impact.
- The six implemented pages could later gain per-page view models; not in this change.

## Definition of Done (§13)

- Settings opens on "General & Startup" with a sidebar showing 3 groups / 8 pages; existing controls appear on their mapped pages and behave as before.
- Pages without implemented settings show a neutral empty state.
- `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` pass; new navigation tests pass.
- `openspec validate settings-window-sidebar-shell --strict` is valid.
- Save/Cancel/Escape/single-window/version-footer behavior unchanged.
