# Design

## Context

See `proposal.md` — Why. Current state and constraints that shape the approach:

- `src/CLIHub/Windows/MainWindow.xaml` is a light scaffold: a header border, a grid holding two `GroupBox` panes with a `GridSplitter`, and a status bar that carries the version text and a "Check for updates" button. Agent commands live in a single bottom `WrapPanel` and act on the selected row.
- `MainWindow.xaml.cs` (~280 lines) holds all window state and behavior: project list refresh, agent list construction, version population, filter persistence, add/remove project, command execution, and status text.
- `src/CLIHub/Windows/AgentItem.cs` is the existing `INotifyPropertyChanged` row model for agents.
- `MainWindow` is referenced by `ServiceRegistration` (singleton registration and the hotkey callback), `TrayIconController` (field, constructor, show/toggle), `App.xaml.cs` (startup visibility), and `GlobalHotkeyService` (target window); it is also documented in `docs/architecture.md`.
- `src/CLIHub/ViewModels/` already provides `ObservableObject` and `RelayCommand`, and `SettingsWindow` + `SettingsViewModel` already follow MVVM. Per `AGENTS.md`, views stay thin and code-behind is migrated as a window changes.
- `App.xaml` has an empty `Application.Resources`; there are no shared theme resources, and the only converter is `PathToImageConverter`.
- `TrayIconController` owns the single `SettingsWindow` instance and opens it through a `Func<SettingsWindow>` factory.
- `tests/CLIHub.Tests` references `CLIHub.Core` only, so anything unit-testable must live in Core.
- `ui/mockups/popup-split.png` is the source of truth for the layout; `ui/mockups/README.md` describes it.
- No config schema change is needed: favorites, the availability filter, runtime, and the update preference all already exist in `AppPreferences`/`Project`.

## Goals / Non-Goals

**Goals:**

- Deliver the launch window as a new `LaunchWindow` that takes over `MainWindow`'s role in the application, without rewriting `MainWindow` in place and without removing it.
- Match the mockup's structure and visual hierarchy for the launch window.
- Extract launch-window state, commands, and derived values into a view model, leaving code-behind with window-level concerns only.
- Provide a single place to define the dark palette so a later change can adopt it in other windows without redefining colors.
- Keep the existing service contracts (`IProjectService`, `IPluginManager`, `IAgentCommandService`, `IAgentDetectionService`, `IAgentVersionService`, `IConfigService`, `IUpdateService`) as the only data sources.

**Non-Goals:**

- No removal of the retained `MainWindow`; it stays in the repository as a reference.

- No dynamic/switchable theming, design-token system, or OS-theme following.
- No restyle of the Settings window, no tray-menu redesign, no message-box restyle.
- No new agent commands, no plugin descriptor changes, no config schema change.
- No new architecture layers beyond view models for this window.

## Decisions

**1. Add `LaunchWindow` as a copy of `MainWindow`, then make it the full replacement.**
`MainWindow.xaml`/`.cs` is copied to `LaunchWindow.xaml`/`.cs` as the starting point and stays in the repository untouched, so the old window remains the behavioral reference. In the same change, `LaunchWindow` becomes the only window the application uses: the DI registration, `TrayIconController`'s field and show/toggle members, the `GlobalHotkeyService` target, and the startup-visibility path in `App.xaml.cs` all move to it, and nothing constructs `MainWindow` any more. `MainWindow` is deliberately kept rather than deleted, so the previous window stays available for comparison and as a fallback reference; the only edit it may receive is a `using` directive if a type it references moves namespace (see decision 3). *Alternatives:* deleting `MainWindow` (rejected — the owner wants it retained), evolving `MainWindow` in place (rejected — it forces a half-redesigned window to be the live one throughout the work), or keeping both windows registered (rejected — two windows with overlapping responsibilities and duplicate hide-to-tray behavior).

**2. Introduce `LaunchWindowViewModel` and keep code-behind minimal.**
The view model owns: the project list, the agent list, the selected project/agent, the availability filter state, version resolution (`IAgentVersionService`), and all commands (add project, remove project, toggle favorite, refresh, launch, resume, init, update, version, check for updates, open settings, exit). Commands are `RelayCommand` instances with `CanExecute` so menus and buttons disable correctly (no selection → disabled, rather than an error path). Code-behind keeps only: `InitializeComponent`, constructor wiring, `OnClosing` hide-to-tray, and setting the data context. *Alternative:* keep the code-behind and only restyle — rejected because the window is new code and `AGENTS.md` requires MVVM for window logic.

**3. Project rows bind directly to `Project`; agent rows bind to a view-model row item.**
`Project` already exposes `Name`, `Path`, `LogoPath`, and `IsFavorite`, and the selected-row accent is driven by `ListBoxItem.IsSelected`, so no project wrapper is needed. A favorite toggle refreshes the project list and restores the current selection (the existing refresh pattern). *Alternative:* a `ProjectItemViewModel` with change notification — rejected as unnecessary indirection.
`AgentItem` moves from `Windows/` to `ViewModels/` (namespace `CLIHub.ViewModels`) and gains `CanLaunch` / `CanResume` (derived from the plugin's command set) so inline buttons disable for agents that do not define those commands. It keeps `Plugin`, `Name`, `LogoPath`, `IsAvailable`, `RowOpacity`, and the notify-on-change `Version`. Because the retained `MainWindow.xaml.cs` is the only other consumer of `AgentItem`, it receives a single `using CLIHub.ViewModels;` directive so it keeps compiling; that is the only edit the retained window may receive.

**4. Middle ellipsis as a Core formatter plus a thin WPF converter.**
WPF has no middle `TextTrimming`. A pure, platform-independent formatter goes into `CLIHub.Core` (`MiddleEllipsisFormatter`: input text + maximum width budget in characters → shortened string keeping both ends), unit-tested in `tests/CLIHub.Tests`. The WPF converter in `src/CLIHub/Converters/` adapts it and supplies the budget from the row's available width via a `MultiBinding` on the row container's `ActualWidth`. *Alternatives:* measurement only inside the converter (untestable, and Core-less), or end-truncation (does not match the mockup, hides the meaningful tail of a path).

**5. Dark palette lives in a dedicated resource dictionary merged from `App.xaml`.**
`src/CLIHub/Themes/LaunchTheme.xaml` defines the palette (window and row surfaces, separator, primary/secondary/dimmed text, accent, hover, selection, disabled) plus the implicit styles for the controls the window uses, and `App.xaml` merges it. Window XAML then references only named brushes/styles. *Alternatives:* literal colors inline in the window XAML (duplication, no reuse), or replacing `App.xaml`'s empty resources directly (harder to extend later).

**6. Row visuals use restyled `ListBox` item containers, not `ListView`/`GridView`.**
Each pane stays a `ListBox` with a rich `DataTemplate`; the `ListBoxItem` `ControlTemplate` provides the rounded surface, hover/selection feedback, bottom separator, and the left accent bar driven by `IsSelected`. Inline buttons get `Focusable="False"` and bind their command with the row item as the parameter, so activating an inline action acts on its own row regardless of the selected row. *Alternative:* `ListView` with `GridView` — rejected: cannot express rounded thumbnails, accent bar, or per-row action buttons.

**7. Settings window ownership moves behind a shared launcher.**
The running application instance owns exactly one Settings window. The tray's Settings entry and the footer's gear action both go through a shared launcher that shows and activates the existing window instance; it must never construct a second one. The `Func<SettingsWindow>` instance currently owned by `TrayIconController` is extracted into that launcher, registered in DI and used by both callers. *Alternative:* resolving `SettingsWindow` directly in the launch window — rejected: it constructs a second Settings window instance with its own state alongside the tray-owned one, instead of activating the first.

**8. Remaining commands and the availability filter move into the Agents Actions menu.**
Init, update, version, refresh, and the availability filter become menu entries instead of a bottom button row; the filter entry is a checkable toggle bound to the persisted `ShowOnlyProjectAgents` preference and continues to persist on change, preserving `agent-availability-display` behavior (default off, hide instead of dim when enabled). The `PROJECTS` menu entry is labeled "Remove Project" so the existing `project-management` removal scenario maps to it unchanged.

**9. Footer keeps the update-check control next to the version.**
The footer shows the app name and the version from `IUpdateService.GetCurrentVersion()` as a pill in its left part, with the "Check for updates" button immediately to the right of the pill, then the add-project / Settings / Exit icon buttons. The check reports its outcome as footer status text, so the manual check required by `update-checking` stays reachable from the window instead of moving to Settings only. Transient status and error text is replaced by the next message; blocking failures keep using `MessageBox`.

## Risks / Trade-offs

- **Reference move leaves a window-level concern behind** (hide-to-tray, shutdown path, hotkey target, tray show/toggle, startup visibility) → enumerate every `MainWindow` reference with a search before deleting the old files, move each to `LaunchWindow`, and cover each in the regression pass.
- **Approximate middle ellipsis** (no glyph metrics in Core) → use a conservative character budget, clip the row content, and verify against the sample paths in the mockup; cover the formatter with unit tests including short paths, single-segment paths, long names, and path separators.
- **Inline buttons vs. `ListBox` selection semantics** → commands carry the row item as parameter, buttons are non-focusable, and clicking an unselected row's button is verified explicitly.
- **Dark surfaces may look wrong under OS-level high contrast or forced light chrome** → use explicit palette brushes rather than system colors, and verify text contrast for primary, secondary, and dimmed states.
- **Extracting the Settings launcher can produce a second Settings window instance** → keep the existing tray path as the reference and verify that invoking Settings from the tray and from the footer activates the already open window (brought to the front) and never creates a second instance; the same check applies to the launch window itself when it is activated from the tray, the hotkey, or a repeated application launch.
- **A copied window drifts from the original while both exist** → the copy is kept behaviorally identical until the switchover task completes, and no application code path references `MainWindow` afterwards.
- **The retained `MainWindow` becomes dead code that can rot or mislead** → keep it compiling (the analyzer and style enforcement still cover it), document it in `docs/architecture.md` and `docs/repo-structure.md` as retained but not wired, and state the retained window is not a second entry point to the application.
- **Silent behavior drift while moving logic into the view model** (filter persistence, refresh after command, version placeholder, "no agents" guidance, update-check outcome text) → migrate behavior-by-behavior against the current implementation, and add unit tests for the newly extracted Core logic.

## Migration Plan

The window swap is an in-change replacement, not a data migration: no persisted state or config changes, and hide-to-tray, single-instance enforcement, and the global hotkey keep their behavior.

Order of delivery: copy the window, then switch every application reference to `LaunchWindow` (keeping `MainWindow` in place, unwired), then migrate to the view model, apply the theme, and restyle structure and rows.

Rollback is a git revert of the change; the previous `MainWindow` and its wiring are preserved, and no stored state depends on the new window.

## Open Questions

- Whether the "no agents found" guidance belongs in the footer's message area or as an empty state inside the Agents pane. Deferrable: placement only; the requirement that the user can find plugins is unaffected.
- Whether the agent rows should later gain a per-row overflow menu for init/update/version instead of relying on the selected agent. Deferrable: the Agents Actions menu already satisfies the spec for the selected agent.
