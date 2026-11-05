# Design: Settings CLI Agents Page — Plugin Actions Menu

## Verified Current State

### The four buttons to combine

`src/CLIHub/Views/SettingsAgentsPage.xaml:99-205` is a `Grid` holding a `controls:SectionHeader
Text="AGENTS"` and a `controls:SpacedPanel` with six `Button` elements, in order:

1. `:112-127` "Detect" — `Command="{Binding AgentsState.DetectCommand}"`, `IsEnabled="{Binding
   AgentsState.CanDetect}"`, glyph `IconGlyphs.Refresh`. **Stays a standalone button.**
2. `:128-143` "Check updates" — `Command="{Binding AgentsState.CheckUpdatesCommand}"`, `IsEnabled=
   "{Binding AgentsState.CanCheckUpdates}"`, glyph `IconGlyphs.Sync`. **Stays a standalone button.**
3. `:144-158` "Install Plugin…" — `Command="{Binding AgentsState.InstallPluginCommand}"`, glyph
   `IconGlyphs.Add`. No `IsEnabled` binding (always enabled). **Moves into the Actions menu.**
4. `:159-173` "Browse Plugins…" — `Command="{Binding AgentsState.BrowsePluginsCommand}"`, glyph
   `IconGlyphs.Search`. No `IsEnabled` binding; disabling is delegated to `Command.CanExecute`.
   **Moves into the Actions menu.**
5. `:174-188` "Open Plugins Folder" — `Command="{Binding AgentsState.OpenPluginsFolderCommand}"`,
   glyph `IconGlyphs.FolderOpen`. **Moves into the Actions menu.**
6. `:189-203` "Reload Catalog" — `Command="{Binding AgentsState.ReloadCatalogCommand}"`, glyph
   `IconGlyphs.SyncFolder`. **Moves into the Actions menu.**

All six use `Style="{StaticResource SectionHeaderChipButton}"` (defined at
`src/CLIHub/Themes/Common/SettingsStyles.xaml:455-480`, based on `IconChipButton` from
`Controls.xaml`).

`src/CLIHub/Views/SettingsAgentsPage.xaml.cs:1-14` is a one-line `UserControl` subclass with only
`InitializeComponent()`; no code-behind handlers exist yet.

### Commands and their source

`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`:
- `:110` `this.InstallPluginCommand = new RelayCommand(this.InstallPlugin);`
- `:111` `this.BrowsePluginsCommand = new RelayCommand(this.BrowsePlugins, this.CanBrowsePlugins);`
- `:112` `this.OpenPluginsFolderCommand = new RelayCommand(this.OpenPluginsFolder);`
- `:113` `this.ReloadCatalogCommand = new RelayCommand(this.ReloadCatalog);`
- Properties: `:271` `InstallPluginCommand`, `:276` `BrowsePluginsCommand`, `:293`
  `OpenPluginsFolderCommand`, `:298` `ReloadCatalogCommand` — all `public RelayCommand ... { get; }`.
- `:1125` `private bool CanBrowsePlugins() => FeedUrlProvider.GetFeedUrl(this.preferencesStore) is not
  null;` — the existing gate; unchanged by this proposal.

None of `InstallPluginCommand`, `BrowsePluginsCommand`, `OpenPluginsFolderCommand`, or
`ReloadCatalogCommand` is declared on `IAgentManagementSection`
(`src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs:1-159` — confirmed by reading the full file;
`DetectCommand` at `:93` and `CheckUpdatesCommand` at `:100` are the only command members on the
interface besides the per-card commands). The XAML already binds to the four plugin commands directly
through `AgentsState` (`SettingsAgentsPage.xaml:145,160,175,190`) even though `SettingsViewModel.
AgentsState` is statically typed `IAgentManagementSection`
(`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:188`). This works today because WPF `Binding`
resolves property paths by runtime reflection on the actual object (`AgentManagementViewModel`), not
through the static interface type — **the interface is not a hard requirement for a binding to work**.
This is a pre-existing pattern this proposal does not have to break.

### The Launch window Actions-menu reference implementation

`src/CLIHub/Views/LaunchWindow.xaml`:
- `:27-101` `Window.Resources` merges only `/Themes/Common/LaunchWindowStyles.xaml` (`:31`) and then
  declares, inline, four `DataTemplate`s: `ActionsMenuActionTemplate` (`:45-60`, a `Button` with
  `Click="OnMenuActionInvoked"`), `ActionsMenuToggleTemplate` (`:62-77`, a checkable `ToggleButton`),
  `ActionsMenuSeparatorTemplate` (`:79-81`), and `ActionsMenuItemTemplate` (`:83-99`, the
  template-switching `ContentControl` that picks one of the first three by `DataTrigger` on
  `IsSeparator`/`IsCheckable`).
- `:126-153` Projects pane header: `ToggleButton x:Name="ProjectsActionsButton"` with
  `Style="{StaticResource PaneActionsToggle}"`, then `Popup x:Name="ProjectsActionsPopup"` bound
  `IsOpen` two-way to the toggle's `IsChecked`, `Opened="OnActionsPopupOpened"`,
  `Placement="Bottom"`, `PlacementTarget={Binding ElementName=ProjectsActionsButton}`,
  `PopupAnimation="Fade"`, `StaysOpen="False"`, `VerticalOffset="-1"`. Inside: a `Grid
  PreviewKeyDown="OnActionsMenuPreviewKeyDown"` containing the `Border Style="{StaticResource
  ActionsPopover}"` with the `ItemsControl` bound to `ProjectsActions`, plus a masking `Rectangle`.
- `:282-309` Agents pane header: the identical shape, bound to `AgentsActions`.

`src/CLIHub/Views/LaunchWindow.xaml.cs`:
- `:254-265` `OnActionsPopupOpened` — measures the popup's child unconstrained, sets
  `popup.HorizontalOffset = targetWidth - child.DesiredSize.Width` to right-align it under the toggle.
- `:140-147` `OnActionsMenuPreviewKeyDown` — Escape inside the popup closes both popups via
  `CloseActionsMenus()`.
- `:121-134` `OnKeyDown` (window-level) — Escape closes an open menu first (via
  `CloseActionsMenus()`), and only hides the window if no menu was open.
- `:153-161` `CloseActionsMenus()` — sets both `Popup.IsOpen = false`; returns whether either was open.
- `:271` `OnMenuActionInvoked` — the shared `Click` handler on menu-entry buttons/toggles; always
  closes both popups after the command runs.

`src/CLIHub/ViewModels/Launch/LaunchWindowActionBuilder.cs:10-73` — `LaunchWindowActionBuilder.Build`
takes the relevant `ICommand`s plus initial toggle state and returns a `LaunchWindowActions` record
with `Projects`, `Agents`, and `FilterAction` built as `new MenuAction[] { new() { Label = ..., Glyph =
IconGlyphs.X, Command = ... }, ... }`. `LaunchWindowViewModel.cs:145,150` exposes
`IReadOnlyList<MenuAction> ProjectsActions` / `AgentsActions` from the builder's result.

`src/CLIHub/ViewModels/Menu/MenuAction.cs:1-50` — `MenuAction : ObservableObject` with `Label`,
`Glyph`, `Command` (nullable `ICommand`), `IsCheckable`, `IsSeparator`, `IsChecked`, and a
`MenuAction.Separator()` factory. No enabled-state property exists: an entry's `Button.Command` already
disables itself through `Command.CanExecute`, exactly matching how `CanBrowsePlugins` must keep
disabling the Browse Plugins entry.

### Styles and where they resolve from

`src/CLIHub/Themes/Common/LaunchWindowStyles.xaml:1-16` is merged **only** from
`LaunchWindow.xaml:31` (`Window.Resources`), and itself merges only `/Themes/Common/Controls.xaml`
(`:14-16`). It is explicitly documented as window-scoped (comment at `:5-12`): "Merged from
LaunchWindow.xaml (not from App.xaml) so that styling is window-scoped and other windows keep their
own look." It defines, among others:
- `:46-53` `ActionsPopover` (`Border`): `MinWidth="{StaticResource Sizing.ActionsMenu.MinWidth}"`,
  `CornerRadius="8,0,8,8"`, `Background="{DynamicResource PopoverOpenBrush}"`,
  `BorderBrush="{DynamicResource ActionsBorderBrush}"`, `BorderThickness="1"`, `Padding="4"`.
- `:56-87` `ActionsMenuEntryButton` (`Button`): custom template, `Foreground=
  {DynamicResource MenuItemBrush}`, hover/pressed/disabled triggers using `HoverOverlayBrush`,
  `PressedOverlayBrush`, `DisabledTextBrush`.
- `:90-124` `ActionsMenuEntryToggle` (`ToggleButton`): same shape plus a checked-state trigger using
  `AccentSelectionBrush`.
- `:128-184` `PaneActionsToggle` (`ToggleButton`): the "ACTIONS" header toggle, using
  `SectionLabelBrush`, `HoverOverlayBrush`, `ChipStrokeBrush`, `PopoverOpenBrush`,
  `ActionsBorderBrush`, `PressedOverlayBrush`, `DisabledTextBrush`.

**Every brush these four styles reference is an application-level palette brush**, verified present
in all three palettes (`Themes/Dark.xaml`, `Themes/Light.xaml`, `Themes/Blue.xaml`):
`PopoverOpenBrush`, `ActionsBorderBrush`, `RowDividerBrush`, `HoverOverlayBrush`,
`PressedOverlayBrush`, `TextMutedBrush`, `MenuItemBrush`, `SectionLabelBrush`, `DisabledTextBrush`,
`ChipStrokeBrush`, `AccentSelectionBrush` (the last one is runtime-provided into
`Application.Resources` by `AccentApplier`, per `XamlResourceReferenceTests.cs:59-67`, not declared in
a palette file — still application-level). `Sizing.ActionsMenu.MinWidth` is declared once, at
`src/CLIHub/Themes/Common/Sizing.xaml:116`, and is reachable from any dictionary that merges
`Sizing.xaml` (directly or transitively). None of these four styles references a `LaunchWindow`-only
resource. This confirms the brief's premise: **the four styles can move to an application-level
dictionary with no loss of any token or brush they need.**

### The application-level merge chain (why this is safe)

`docs/architecture/ui.md:14-31` documents the exact merge order and states explicitly: *"SettingsStyles.
xaml is merged at application level because Settings pages are UserControls built before they attach to
the window tree, so window-scoped resources are unreachable during their XAML load."* The current tree:

```
App.xaml
├─ Dark.xaml                  base palette
├─ Common/SlimScrollBar.xaml
└─ Common/SettingsStyles.xaml
   ├─ Common/Controls.xaml
   │  └─ Common/Sizing.xaml
   ├─ Common/PickerData.xaml
   │  └─ Common/Sizing.xaml
   └─ Common/SettingsControls.xaml
      └─ Common/Sizing.xaml
```

confirmed again directly: `src/CLIHub/Themes/Common/SettingsStyles.xaml:13-25` merges exactly
`Controls.xaml`, `PickerData.xaml`, `SettingsControls.xaml`, in that order; `App.xaml:9-31` merges
`Dark.xaml`, `SlimScrollBar.xaml`, `SettingsStyles.xaml` (plus other app-level pieces not relevant
here). `LaunchWindow.xaml:31` merges `LaunchWindowStyles.xaml` directly into `Window.Resources`,
**separately from this chain** — `LaunchWindow` is not a `UserControl` loaded before attaching to a
window, so it can afford a window-scoped dictionary; `SettingsAgentsPage` (a `UserControl`) cannot reach
one.

### An existing precedent for a Settings popover

`src/CLIHub/Themes/Common/SettingsControls.xaml:581-589` already declares `SettingsChoicePopover`, a
`Border` style explicitly commented "mirrors the launch window actions card", using the very same
`PopoverOpenBrush`/`ActionsBorderBrush` pair but with `CornerRadius="6"` (full rounding, no squared
corner) and `MinWidth="150"`, because that popover is not visually merged under a header toggle the way
the Actions popover is. This is useful evidence that a full-corner popover already exists and reads
correctly in the Settings window's dark surface, but it is a distinct style for a distinct use (a
per-row value picker, not a header Actions menu) and is not reused here — see Decision 2 below.

### Spec and existing scenario text affected

`openspec/specs/preferences-ui/spec.md:461-463` — "Requirement: CLI Agents page management" states (in
part): *"The page SHALL offer an agent detection action labelled "Detect" above the agent cards..."* —
this sentence and the "Detect action label" scenario (`:515-518`) are about the Detect button only and
are unaffected.

`openspec/specs/plugin-installation/spec.md:216-233` — "Requirement: Open plugins folder" (scenario
"Plugins folder opened", `:220-223`) and "Requirement: Manual catalog reload" (scenario "Catalog
reloaded on demand", `:229-233`) describe clicking "Open Plugins Folder" / "Reload Catalog" as actions,
without constraining them to be rendered as top-level buttons. Moving them into a menu entry that is
still invoked by "the user clicks" does not contradict either scenario's wording.

`openspec/specs/plugin-feed/spec.md:278-282` — "Scenario: Feed disabled with empty string" says *"the
Browse Plugins button is grayed out or hidden"*. A disabled `MenuAction` entry (its `Command.
CanExecute` is false, so the rendered `Button` for that entry is disabled/dimmed via the existing
`IsEnabled="False"` trigger in `ActionsMenuEntryButton`, `LaunchWindowStyles.xaml:79-82`) satisfies
"grayed out" under the existing wording without a scenario-text change.

## Decisions

### D1 — Reuse the exact Launch-window mechanism, no new abstraction

Build the Settings page's Actions menu with the same four pieces the Launch window uses: `MenuAction`
(unchanged), a `ToggleButton` + `Popup` + popover `Border` + `ItemsControl`, and page-local item
templates/code-behind handlers. This directly follows the orchestrator's decision #3 and keeps exactly
one menu abstraction in the codebase (`MenuAction`), avoiding a second, parallel menu model.

**Alternative rejected:** A native `WPF ContextMenu`. Already rejected once for this exact problem
(the `2026-10-03-data-driven-actions-menu` archived change replaced `ContextMenu` with this `Popup`
pattern specifically because `ContextMenu` renders in a separate visual tree and cannot inherit
window/control resources, forcing keyed styles and a measure-after-open alignment hack). Reusing
`ContextMenu` here would reintroduce the exact problem already solved once.

### D2 — Style placement: a new application-level dictionary merged by `SettingsStyles.xaml`

Move `ActionsPopover`, `ActionsMenuEntryButton`, `ActionsMenuEntryToggle`, and `PaneActionsToggle` out
of `LaunchWindowStyles.xaml` into a new file, `src/CLIHub/Themes/Common/ActionsMenuStyles.xaml`, merging
only `/Themes/Common/Controls.xaml` (the same single merge `LaunchWindowStyles.xaml` currently declares
for these styles, bringing `Sizing.xaml` transitively). Merge the new file from both:
- `LaunchWindow.xaml:27-32` (`Window.Resources` `MergedDictionaries`, alongside the now-smaller
  `LaunchWindowStyles.xaml`), and
- `src/CLIHub/Themes/Common/SettingsStyles.xaml:13-25` (added to its existing `MergedDictionaries`
  list), which makes the styles reachable at application level and therefore inside
  `SettingsAgentsPage.xaml` (a `UserControl`) before it attaches to any window.

This is a pure relocation: every brush and token the four styles reference is already
application-level (see Verified Current State above), so moving the *style declarations* up one level
in the merge tree changes no resolved value. `LaunchWindow.xaml`'s own merge list keeps reaching all
four styles (now via `ActionsMenuStyles.xaml` instead of `LaunchWindowStyles.xaml` directly), so its
rendered output is unchanged — confirmed logically; the actual pixel check is the manual smoke task in
`tasks.md`.

**Alternative rejected — settings-local duplicate styles.** Copying the four styles into
`SettingsControls.xaml` (or a new settings-only file) under the same keys would collide
(`ApplicationResourceKeyTests.PaletteBrushKeys_...` and the duplicate-key check in
`ApplicationResourceKeyTests.cs` assert every key in the non-palette application merge tree is declared
exactly once); under new keys it would duplicate the templates and need to be kept in sync with the
Launch window's copy by hand. Rejected: violates DRY for no benefit, the brief's preferred option has
no missing-token risk, and a single source of truth is strictly easier to keep in parity with the
Launch window if either is restyled later.

**Why not merge `LaunchWindowStyles.xaml` directly into `SettingsStyles.xaml` instead of extracting a
new file?** `LaunchWindowStyles.xaml` also contains Launch-window-specific styles (`PaneSurface`,
`PaneHeader`, `AgentRowContainer`, `ProjectRowContainer`, `PaneList`, footer button styles, etc., see
`LaunchWindowStyles.xaml:20-386`) that have no business being reachable from Settings pages and are not
needed there. Merging the whole file would pollute the application-level key space and risk a future
accidental dependency. Extracting only the four Actions-menu styles keeps the merged surface minimal
and intention-revealing.

### D3 — Popover corner radius: reuse `ActionsPopover`'s `8,0,8,8`, not a full-radius variant

Use the exact same `ActionsPopover` style (and its `8,0,8,8` corner radius, designed to merge visually
under the open toggle's squared top-right corner) rather than the `SettingsControls.xaml`
`SettingsChoicePopover` style's full `6` radius. The brief's own decision #3 specifies
`PopoverOpenBrush` background, `ActionsBorderBrush` border, `Placement="Bottom"`,
`VerticalOffset="-1"` — the same "merge with the toggle" visual contract the Launch window uses, and
`PaneActionsToggle`'s checked-state trigger (`LaunchWindowStyles.xaml:163-173`) already squares its own
top corners and drops its bottom border specifically to dock with a `CornerRadius="8,0,8,8"` popover.
Reusing `SettingsChoicePopover`'s full radius would leave a visible seam between the toggle's squared
top-right corner and a rounded popover corner. One shared `ActionsPopover` style (via D2) is simplest
and keeps the two "ACTIONS" menus pixel-identical in shape.

### D4 — Item templates and alignment/Escape handlers stay page-local, in `SettingsAgentsPage.xaml`/`.xaml.cs`

Per the Launch window's own documented rationale (`LaunchWindow.xaml:40-44`): an item template that
wires a `Click` handler can only resolve that handler against a type with code-behind, so the four
`DataTemplate`s (`ActionsMenuActionTemplate`, `ActionsMenuSeparatorTemplate`,
`ActionsMenuItemTemplate`, and the checkable one if ever needed) are declared in
`SettingsAgentsPage.xaml`'s own `<UserControl.Resources>`, reusing the shared `ActionsMenuEntryButton`/
`ActionsMenuEntryToggle` styles from D2. `SettingsAgentsPage.xaml.cs` gains:
- `OnActionsPopupOpened` — identical logic to `LaunchWindow.xaml.cs:254-265` (measure, right-align).
- `OnActionsMenuPreviewKeyDown` — Escape closes the popup, mirroring `LaunchWindow.xaml.cs:140-147`.
- `OnMenuActionInvoked` — closes the popup after a click, mirroring `LaunchWindow.xaml.cs:271`.

Because `SettingsAgentsPage` has exactly one Actions popup (unlike `LaunchWindow`'s two), its
`CloseActionsMenus()` equivalent only needs to close the one `Popup` by name; a single-popup
`OnMenuActionInvoked`/`OnActionsMenuPreviewKeyDown` pair (`this.PluginActionsPopup.IsOpen = false;`) is
simpler than `LaunchWindow`'s two-popup `CloseActionsMenus()` helper, so no equivalent "close all"
helper method is introduced — a one-line close in each of the two handlers is sufficient and avoids an
unused abstraction for a single popup.

**Escape and `SettingsWindow`:** `SettingsWindow.xaml.cs:138-147` already closes the whole window on
Escape; because a `Popup` is a separate top-level window, its key events do not reach
`SettingsWindow.OnKeyDown` while the popup has focus, matching the brief's decision #6 exactly — the
page's own `PreviewKeyDown` handler on the popup's content root is required, and `SettingsWindow` needs
no change.

### D5 — `PluginActions` is added to `AgentManagementViewModel` only, not to `IAgentManagementSection`

Add `public IReadOnlyList<MenuAction> PluginActions { get; }` directly on `AgentManagementViewModel`,
built once in its constructor (mirroring `LaunchWindowActionBuilder.Build`'s shape but inline, since
there is only one menu here and not two panes needing a shared builder type). **Do not** add it to
`IAgentManagementSection`.

Rationale: the interface is a UI-facing contract used for mocking in several tests (`Mock<
IAgentManagementSection>` in `ServiceRegistrationTests.cs:60,170,183-184`,
`SettingsViewModelFixture.cs:29,36,38`, `SettingsNavigationTests.cs:147`,
`SettingsViewModelApplicationTests.cs:961`, `SettingsOperationLifetimeTests.cs:48`,
`SharedUpdatePresentationTests.cs:138`). None of those tests currently stub
`InstallPluginCommand`/`BrowsePluginsCommand`/`OpenPluginsFolderCommand`/`ReloadCatalogCommand` — those
four commands are likewise absent from the interface today, confirming the existing precedent that not
every concrete member needs to be promoted. The XAML binding to `AgentsState.PluginActions` resolves by
runtime reflection against the concrete `AgentManagementViewModel` regardless of the interface's
declared shape (see Verified Current State), exactly like the four existing plugin commands it is built
from. Adding `PluginActions` to the interface would force every one of the seven call sites above to
either implement it or fail to compile against a stricter mock, for no behavioral benefit — this
proposal's own `PluginActionsTests` (D6) constructs the concrete `AgentManagementViewModel` through the
existing `AgentManagementViewModelBuilder`, not through the interface, so no test needs the interface
member either.

**Alternative rejected:** Adding `PluginActions` to `IAgentManagementSection`. Rejected because it
widens a seven-call-site interface for a property no consumer other than this one page needs, breaking
the precedent set by the four source commands it is built from.

### D6 — Test coverage

Add one test (new file `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.PluginActions.cs`,
a `partial class AgentManagementViewModelTests`, matching the existing per-concern partial-file split
in that folder) asserting:
- `PluginActions` has exactly four entries, in order: "Install Plugin…" (`IconGlyphs.Add`), "Browse
  Plugins…" (`IconGlyphs.Search`), "Open Plugins Folder" (`IconGlyphs.FolderOpen`), "Reload Catalog"
  (`IconGlyphs.SyncFolder`).
- Each entry's `Command` is `Same` as the corresponding `AgentManagementViewModel` command instance
  (`InstallPluginCommand`, `BrowsePluginsCommand`, `OpenPluginsFolderCommand`, `ReloadCatalogCommand`).
- The Browse Plugins entry's `Command.CanExecute(null)` is `false` when the feed URL preference is the
  disabled empty string (reusing the existing `CanBrowsePlugins`/`FeedUrlProvider` gate through
  `AgentManagementViewModelBuilder`), proving the menu entry surfaces the same disabled state the
  button did.

No XAML/layout automated test is added — the repository has none for layout, consistent with
`docs/testing.md` and every archived UI-only change reviewed (`2026-11-03-theme-accent-copy/tasks.md:40`
relies on a **manual** smoke test for visual verification). The manual smoke task in `tasks.md` covers
right-alignment, hover/disabled visuals, Escape-to-close, and click-to-close/invoke.

## Alternatives Considered (summary)

| Option | Rejected because |
|---|---|
| Keep `ContextMenu` | Already rejected once in this repo (`2026-10-03-data-driven-actions-menu`) for the same separate-tree/keyed-style/alignment-hack problems. |
| Duplicate the four styles into a settings-only file under new keys | Violates DRY; two copies drift; no benefit over extraction. |
| Merge the whole `LaunchWindowStyles.xaml` into `SettingsStyles.xaml` | Pollutes the application-level key space with Launch-window-only styles (`PaneSurface`, `AgentRowContainer`, etc.) that Settings never needs. |
| Reuse `SettingsChoicePopover`'s full `6` corner radius | Breaks the "merge with the open toggle" visual contract the `PaneActionsToggle` checked-state trigger depends on; would show a seam. |
| Add `PluginActions` to `IAgentManagementSection` | Widens a seven-call-site interface for no consumer benefit; breaks the precedent of the four source commands it is built from, which are also absent from the interface. |

## Open Questions

1. **Exact header layout order of the "ACTIONS" toggle relative to "Detect"/"Check updates".** The
   brief places it "at the far right of the AGENTS section header", after Detect and Check updates (in
   that reading order, left to right). This design assumes: `[UpdateCheckSummary] [Detect] [Check
   updates] [ACTIONS ▾]`, i.e. the Actions toggle becomes the last, right-most element inside the
   existing `SpacedPanel`, replacing the four removed buttons in place. **Confirm this ordering is
   correct before implementation**, since no mockup was supplied.
2. **Exact wording of the new spec scenario(s).** `design.md`/`proposal.md` commit to adding a sentence
   plus at least one new scenario to "CLI Agents page management" stating the four plugin actions are
   grouped under an "Actions" menu and Detect/Check updates remain standalone. The implementer should
   confirm the exact scenario wording in the delta (`specs/preferences-ui/spec.md` in this change)
   matches what ships, and adjust if the implementation session reveals a different final menu label or
   position than assumed here.
3. **Automation name for the new toggle.** The Launch window's `PaneActionsToggle` instances carry no
   explicit `AutomationProperties.Name` beyond their `Content="ACTIONS"` text in the reference
   implementation. This design does not add one for parity; if accessibility review during
   implementation finds this insufficient, add `AutomationProperties.Name="Plugin catalog actions"` (or
   similar) as a small follow-up within this same change's tasks.

## Deviations

None identified. The verified code matches the brief's description in every respect checked above
(button order, commands, glyphs, `CanBrowsePlugins` gate, Launch-window mechanism, styles' window
scoping, interface shape). No part of the brief contradicts the current source.

## Implementation deviations

Recorded by the implementation session; none change behavior from what D1–D6 specify.

1. **Popup hosted in the header `Grid`, not inside the `SpacedPanel`** (tasks.md 4.2 markup
   detail). `SpacedPanel` counts every non-collapsed child when applying `Spacing`, and a `Popup`
   — while it has zero measure size — is a non-collapsed child, so hosting it inside the panel
   would add a trailing 8 DIP gap after the `ACTIONS` toggle and push it off the header's right
   edge. The popup is therefore a sibling of the `SpacedPanel` inside the enclosing header
   `Grid` (toggle last inside the panel, right edge flush), which is also exactly how the Launch
   window reference hosts its popups (inside `PaneHeader`, a `Grid`). Placement, binding, and
   behavior are unchanged from D4/tasks.md.
2. **Checkable entry template omitted** (explicitly allowed by tasks.md 4.1). This menu contains
   only plain command entries, so the page-local template switch handles only the action and
   separator cases; the Launch window's `ActionsMenuToggleTemplate` was not copied.
3. **`docs/architecture/ui.md` and the `Sizing.xaml` header comment updated.** Not a tasks.md
   item, but both documents describe the resource merge chain / Sizing consumers that this change
   altered; leaving them stale would contradict the canonical documentation the change itself
   cites. Edits are descriptive only.
4. **`BrowsePluginsDialogBindingTests.SettingsAgentsPage_BindsBrowsePluginsCommand` updated**
   (found by the Stage 6 test gate). The string test asserted the page XAML contains
   `Command="{Binding AgentsState.BrowsePluginsCommand}"` — the direct binding this change
   deliberately replaces with the data-driven menu. The test now guards the new wiring chain
   (`ItemsSource="{Binding AgentsState.PluginActions}"`, the entry template's
   `Style="{StaticResource ActionsMenuEntryButton}"` + `Command="{Binding Command}"`, and
   `Click="OnMenuActionInvoked"`), four assertions where the old test had one; the
   entry-point-to-command identity (the Browse entry carries `BrowsePluginsCommand` and its feed
   gate) is guarded by the new `AgentManagementViewModelTests.PluginActions` tests from Stage 2,
   so the original guard's purpose (UI wired, not just ViewModel tested) is preserved, not
   weakened. The method was renamed to `SettingsAgentsPage_BindsPluginActionsMenu` to match what
   it asserts.

### Open question resolutions (implementation session)

1. **Header order** — settled by the orchestrator before implementation:
   `[UpdateCheckSummary] [Detect] [Check updates] [ACTIONS ▾]`, implemented exactly so.
2. **Spec delta wording** — verified against what ships: the delta's added sentence and three new
   scenarios ("Plugin actions grouped under one Actions menu", "Actions menu entry behavior
   matches the prior standalone button", "Browse Plugins entry disabled state") match the
   implemented labels, order, commands, close-on-invoke, and `CanExecute` gating; every existing
   scenario name and body is preserved verbatim. No rewrite was needed.
3. **Automation name for the toggle** — kept at parity with the Launch window's `PaneActionsToggle`
   instances: no explicit `AutomationProperties.Name` beyond the `Content="ACTIONS"` text, per the
   default this design recommended.
