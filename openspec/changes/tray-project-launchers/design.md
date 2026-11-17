## Context

See `proposal.md` — Why and What Changes. This design covers the delivered project-first tray
structure **and** the lightweight revision confirmed after the user reviewed the uncommitted first
implementation. The working tree at plan commit `4c9efa0` plus the uncommitted tray changes is the
reference state for every `file:line` below; the revision is applied on top of that state.

### Verified current state (2026-11-17)

**`TrayMenuBuilder` (`src/CLIHub/Tray/TrayMenuBuilder.cs`, uncommitted first implementation).**
`Build` (`:16-24`) creates a `ContextMenu` and delegates to `Populate` (`:33-86`), which clears
`menu.Items` (`:39`) and adds, in order: the disabled header (`:41-45`), one submenu per
`state.FavoriteProjects` labelled `$"{entry.Project.Name} *"` (`:47-52`, label at `:49`), the disabled
`(no favorite projects)` item when the list is empty (`:54-57`), `Add Project...` (`:59-61`),
`Settings` (`:63-65`), `What's New` (`:67-69`), the update item (`:71-75`), a `Separator` (`:77`),
`Show CLIHub` (`:79-81`), and `Exit` (`:83-85`).

`BuildUpdateItem` (`:88-115`) produces four states and never returns null: disabled
`Checking for updates…` (`:90-93`), disabled `Downloading update…` when no version is known
(`:97-100`), clickable `Check for updates` (`:102-104`), disabled `Downloading update…` when a version
is known (`:107-110`), and `Download {version} and restart` (`:112-114`). Because it never returns
null, the `if (updateItem is not null)` guard at `:72` is always true.

`PopulateProjectSubmenu` (`:117-150`) adds the default agent first as `Run {name}` with
`IsEnabled = defaultAgent.Commands?.Launch is not null` (`:120-130`), then a `Separator`, then the
other initialized agents as `Run {name}` with a click that calls
`commands.SelectProjectAndLaunchAgentAsync(project.Id, plugin)` (`:132-144`), and one disabled
`(no agents)` item when there is no default and no initialized agent (`:146-149`). No `MenuItem.Icon`
is set anywhere in the file, and the file references no font or glyph type.

**`TrayMenuState` / `TrayMenuCommands` (`src/CLIHub/Tray/TrayMenuState.cs`).** `TrayProjectAgents`
(`:11-13`) carries `Project`, resolved `Plugin? DefaultAgent`, and `IReadOnlyList<Plugin>`
`InitializedAgents`; `TrayMenuState` (`:23-27`) carries `CurrentProject`, `FavoriteProjects`,
`AvailableUpdateVersion`, `IsDownloadingUpdate`, `IsCheckingForUpdates`. `TrayMenuCommands`
(`:42-51`) has ten positional slots: `SelectProject`, `LaunchAgentAsync`,
`SelectProjectAndLaunchAgentAsync`, `AddProject`, `ShowSettings`, `ShowReleaseNotes`,
`CheckForUpdates`, `RequestUpdateDownload`, `ShowLaunchWindow`, `Exit`. The builder binds all of them
except `SelectProject` and `LaunchAgentAsync`.

**`TrayStateProjection` (`src/CLIHub/Tray/TrayStateProjection.cs`).** Constructor over
`IProjectService`, `IPluginCatalog`, `IUpdateWorkflow`, `IAgentDetectionService`, `IPreferencesStore`
(`:35-50`). `GetState()` (`:60-74`) resolves the default once (`:62`), maps favorites from
`GetAllProjects()` (`:63-66`), and reads the update fields from `IUpdateWorkflow`. `ResolveDefaultAgent`
(`:103-112`) matches `preferences.Load().DefaultAgentId` against the unfiltered catalog.
`ComposeInitializedAgents` (`:120-125`) keeps launch-capable plugins that are installed and available in
the project. `SetCurrentProject(string)` (`:76-80`) is a public pass-through with no caller other than
`TrayActions` passing the method group.

**`TrayCommandHandlers` (`src/CLIHub/Tray/TrayCommandHandlers.cs`).**
`CreateCommands(Action<string> selectProject)` (`:61-71`) forwards `selectProject` into `SelectProject`,
`this.LaunchAgentAsync` into `LaunchAgentAsync`, `this.SelectProjectAndLaunchAgentAsync`, the Settings and
What's New launchers, `this.CheckForUpdates`, and inert defaults for the controller-owned
`RequestUpdateDownload`, `ShowLaunchWindow`, and `Exit` slots (`:69-71`). `AddProject` (`:73-90`) runs
the folder dialog, adds the project and makes it current. `LaunchAgentAsync(Plugin, Project)`
(`:92-99`) runs
`IAgentCommandWorkflow.ExecuteAsync(plugin, project, AgentCommandKind.Launch)` and warns on failure.
`SelectProjectAndLaunchAgentAsync(string, Plugin)` (`:101-111`) calls `SetCurrentProject`, re-reads
`GetCurrentProject()`, returns on a mismatch, and then calls `LaunchAgentAsync`.

**`TrayActions` (`src/CLIHub/Tray/TrayActions.cs:28`)** builds the command set as
`this.handlers.CreateCommands(this.projection.SetCurrentProject)`, which is the only consumer of
`TrayStateProjection.SetCurrentProject`.

**`TrayIconController` (`src/CLIHub/Tray/TrayIconController.cs`).** The left-click handler
(`:48`) calls `ShowLaunchWindow`; the public `ShowLaunchWindow` (`:57-60`) and `ToggleLaunchWindow`
(`:65-75`) remain the app's window entry points (`Startup/WpfApplicationStartupUi.cs:37`; the hotkey
factory in `Composition/ServiceRegistration.cs:143-146`). `RefreshMenu` (`:127-147`) `with`-mutates
`RequestUpdateDownload`, `ShowLaunchWindow`, and `Exit` (`:129-134`), repopulates the one `ContextMenu`
instance in place (`:136-142`), and on first creation attaches `Opened += (_, _) => this.RefreshMenu()`
before assigning the menu (`:144-146`).

**Glyph infrastructure.** `IconGlyphs` is an `internal static` class in `CLIHub.Theming`
(`src/CLIHub/Theming/IconGlyphs.cs`) and contains every constant this design uses: `FavoriteStar`
`\uE734` (`:12`), `Play` `\uE768` (`:14`), `Update` `\uE896` (`:15`), `Page` `\uE7C3` (`:36`), `Sync`
`\uE895` (`:38`), `FolderAdd` `\uE8F4` (`:53`), `Settings` `\uE713` (`:55`), `Exit` `\uE7E8` (`:58`).
The icon font is `Sizing.FontFamily.Icon` = `Segoe MDL2 Assets` (`src/CLIHub/Themes/Common/Sizing.xaml:19`);
glyph sizes are `Sizing.FontSize.Glyph` 16 (`:28`), `GlyphSmall` 14 (`:32`), `GlyphTiny` 12 (`:33`).
The launch-window action menus render entry glyphs at `FontSize = Sizing.FontSize.GlyphSmall` inside a
`Width="16"` `DecorativeGlyph` (`src/CLIHub/Views/LaunchWindow.xaml:56-62,75-81`);
`DecorativeGlyph` is a `TextBlock` with a null automation peer (`src/CLIHub/Controls/DecorativeGlyph.cs:10-17`).
`CLIHub.csproj:25` has `InternalsVisibleTo` for `CLIHub.Tests`, so tests can assert against the
`IconGlyphs` constants.

There is **no** custom `ContextMenu` style in the repository (the only `ContextMenu` usages in `src/`
are the two tray files), so tray items render with the default WPF menu template: `MenuItem.Icon` is
the native icon slot, and the popup is not part of a window's visual tree.

**Tests (uncommitted).** `tests/CLIHub.Tests/Services/TrayActionsTests.cs`: state projection
(`:18-50`), `LaunchAgentCommand_UsesSharedAgentWorkflow` invoking the `LaunchAgentAsync` slot
(`:52-72`), `SelectProjectAndLaunchAgentCommand_WhenInvoked_SelectsProjectThenLaunches` (`:74-99`),
update-check delegation (`:101-125`), disposal (`:127-142`), and
`CreateCommands_ControllerOwnedSlots_AreInertDefaults` (`:144-168`), which creates the commands with a
`selectProject` lambda (`:159`) and invokes `RequestUpdateDownload`, `ShowLaunchWindow`, `Exit`
(`:161-163`). `tests/CLIHub.Tests/Services/TrayMenuBuilderTests.cs` (new, 11 tests) builds commands
positionally with ten arguments (`:282-301`), asserts ` *` headers (`:30-33,268`), `Run <name>` labels
(`:75,98,101,123,126,150,171,188,242,271`), and a nine-item tail ending `…, Show CLIHub, Exit`
(`:209-228`). No test asserts an icon.
`tests/CLIHub.Tests/ViewModels/Updates/SharedUpdatePresentationTests.cs:146-158` constructs the
projection with the five dependencies.

**Spec mapping.** No `tray`/`tray-menu` capability exists in `openspec/specs/` (closest:
`project-management`, `agent-detection`, `agent-commands`). This change adds `specs/tray-menu/spec.md`
with ADDED requirements only, so there is no MODIFIED requirement and no durable scenario-name
preservation obligation. Within the ADDED delta, requirement names are kept from the first revision
where the behavior still holds; scenarios whose asserted behavior changed by this revision are renamed
because keeping a name that no longer states the truth would be worse for review. `skip_specs` stays
unset.

## Decisions

### Decision 1: New immutable, WPF-free tray state shape (DECIDED)

Replace `TrayMenuState` with:

```csharp
public sealed record TrayProjectAgents(Project Project,
                                       Plugin? DefaultAgent,
                                       IReadOnlyList<Plugin> InitializedAgents);

public sealed record TrayMenuState(Project? CurrentProject,
                                   IReadOnlyList<TrayProjectAgents> FavoriteProjects,
                                   string? AvailableUpdateVersion,
                                   bool IsDownloadingUpdate,
                                   bool IsCheckingForUpdates);
```

`TrayProjectAgents` carries the `Project`, the **resolved** default `Plugin?` (already catalog-checked),
and the initialized agents for that project. `TrayMenuState` drops `RecentProjects` and
`LaunchableAgents`.

**Rationale:** the menu needs "per project: default + initialized agents", so the projection resolves the
default and filters agents once and the builder stays a dumb renderer. Keeping only `Project`/`Plugin`
Core models in the record keeps the WPF dependency out of the state, matching the Core/app boundary.

**Alternative — keep a flat `LaunchableAgents` list plus a map of project→agents (rejected):** forces the
builder to re-derive the default and cross-reference projects, duplicating projection logic in the view.

**Alternative — reuse the launch window's `AgentListEntry` (rejected):** it carries dimming/availability
presentation state the tray does not need, and it lives in an app-project namespace oriented to the launch
window.

### Decision 2: One project-scoped launch command slot (DECIDED — revised 2026-11-17)

`TrayMenuCommands` exposes exactly the seven slots the builder binds:

```csharp
public sealed record TrayMenuCommands(Func<string, Plugin, Task> SelectProjectAndLaunchAgentAsync,
                                      Action AddProject,
                                      Action ShowSettings,
                                      Action ShowReleaseNotes,
                                      Action CheckForUpdates,
                                      Action RequestUpdateDownload,
                                      Action Exit);
```

`TrayMenuBuilder` binds each agent item to
`commands.SelectProjectAndLaunchAgentAsync(project.Id, plugin)`. In `TrayCommandHandlers`:

```csharp
private async Task SelectProjectAndLaunchAgentAsync(string projectId, Plugin plugin)
{
    this.projects.SetCurrentProject(projectId);
    var project = this.projects.GetCurrentProject();
    if (project is null || project.Id != projectId)
    {
        return;
    }
    await this.LaunchAgentAsync(plugin, project);
}
```

**Rationale:** `TrayMenuBuilder` is a pure projection with no business logic; making the project current
before running the workflow is an application rule, so it belongs in the handler. The handler reads the
current project back after the setter so the workflow runs against the persisted, stamped record rather
than a stale snapshot.

**Revision:** the first version of this decision kept the existing `SelectProject`, `LaunchAgentAsync`,
and `ShowLaunchWindow` slots alongside the new one. After the redesign nothing binds them, so they are
removed — see Decision 13.

**Alternative — fire `SelectProject(project.Id)` then `LaunchAgentAsync(plugin, project)` (rejected):**
the two slots resolve to different owners, so the ordering coupling leaks the "select first" rule into
the view; it also costs two command slots instead of one.

### Decision 3: Refresh menu content when the context menu opens (DECIDED)

There is exactly **one** `ContextMenu` instance for the tray icon's lifetime. The menu content is
repopulated in place on `Opened`; the instance is never swapped and the handler is never re-attached.

- `TrayMenuBuilder.Build(state, commands)` creates and returns a populated `ContextMenu`. It is used
  **only to create the first instance**.
- `TrayMenuBuilder.Populate(menu, state, commands)` clears `menu.Items` and repopulates that existing
  instance. It must not replace the instance and must not attach or re-attach any event handler.
- `TrayIconController` creates the `ContextMenu` once via `Build`, assigns it to
  `this.taskbarIcon.ContextMenu`, and attaches `Opened += (_, _) => this.RefreshMenu();` **once**
  (`TrayIconController.cs:144-146`).
- `RefreshMenu()` calls `this.menuBuilder.Populate(this.taskbarIcon.ContextMenu, this.actions.GetState(),
  commands)` on that same instance (`:136-142`). `OnStateChanged` and the `Opened` refresh both call
  `RefreshMenu`, so the initial creation, the state-change rebuild, and the on-open rebuild share a
  single refresh path.

On open, `RefreshMenu` reads a **fresh** `GetState()` and rebuilds the content; detection results are
cached (up to the 15-minute probe TTL), so a refresh is a few dictionary lookups in the common case and
re-checks the file system only after the TTL or an `InvalidateFor`.

**Rationale:** `IPreferencesStore` has no change event, so a changed `DefaultAgentId`, a newly favorited
project, or a fresh detection result otherwise never reaches the snapshot. `ToggleFavorite` does raise
`ProjectsChanged`, but opening-time refresh additionally picks up the default-agent change and
TTL-expired detection with no new subscription and no event in Core.

**Alternative — subscription-only refresh (rejected):** leaves a newly enabled default agent invisible
until an unrelated event.

**Alternative — event on `IPreferencesStore` (rejected):** a broader Core change touching every
consumer; out of scope.

**Alternative — swap `ContextMenu` on every refresh (partially rejected):** the `Opened` handler must
move to each new instance and re-entrancy during `Opened` becomes a risk.

### Decision 4: Detection cost and cache semantics (DECIDED — documented cost)

For each favorite project P and each plugin with a launch command, an agent is "initialized" when
`IsInstalledInSystem(plugin)` **and** `IsAvailableInProject(plugin, P.Path)`. Both are cached by
`AgentDetectionService` (`sys:<id>` and `proj:<id>:<path>`, TTL default 15 min,
`AgentProbePolicy.cs:12`), so a rebuild performs at most one file-system probe per (plugin, project) pair
per TTL window. The favorite-only restriction bounds the product to
`favorites × launch-capable plugins` (≈2–10 favorites, ≈2–10 agents → ≈4–100 checks worst case, usually a
warm cache). `IsAvailableInProject` short-circuits on a missing folder
(`AgentDetectionService.cs:100-103`), so a missing project path costs nothing.

**Rationale:** favorites-only bounds the work; no throttling or background pre-computation is needed.
The revision does not change this cost model.

### Decision 5: Default-agent fallback (DECIDED)

The projection resolves `DefaultAgentId` as follows:

- `DefaultAgentId` null/whitespace → no default item, no separator.
- `DefaultAgentId` set but **not present** in `GetAllPlugins()` → no default item, no separator (the id
  is silently ignored; no warning dialog, since the tray is a passive surface).
- `DefaultAgentId` set and present in the catalog → that plugin is the submenu's first item. The default
  need **not** expose a launch command; if it does not, its item is rendered disabled
  (`IsEnabled = false`) but still first, with the separator after it.

The catalog check uses the **unfiltered** `GetAllPlugins()` (not the launch-command filter), so a default
plugin that lacks a launch command still resolves and shows as a disabled first item rather than
vanishing.

**Rationale:** "always the first item, even when it fails the initialization checks" is the confirmed
decision; the only question is the meaning of "not in the catalog", which is decided as silent
ignore — consistent with the menu's other graceful fallbacks (`(no favorite projects)`, `(no agents)`).
Rendering a non-launchable default as a disabled first item keeps it visibly "the configured default"
without offering a launch that cannot work. The revision does not change this rule.

### Decision 6: Label formats (SUPERSEDED by Decision 9)

The first version of this decision kept the existing favorite marker (`<Name> *`) and labelled agent
items `Run <AgentName>`, arguing the prefix stated the action and mirrored the window's "Run" verb.

Both are **superseded**: the delivered menu repeated the marker and the verb on every row and read as
heavy. The final label rules are in Decision 9. The empty-state labels from this decision are kept:
`(no favorite projects)` (list empty) and `(no agents)` (project submenu otherwise empty), both disabled.

### Decision 7: Where the WPF stays (DECIDED)

- `TrayMenuState`, `TrayProjectAgents`, `TrayMenuCommands`, `TrayStateProjection`, and
  `TrayCommandHandlers` stay free of `System.Windows.*`; the projection depends on
  `IAgentDetectionService` and `IPreferencesStore` (Core interfaces).
- `TrayMenuBuilder`, `TrayIconController`, and `TrayActions` remain the only WPF-aware tray types.
  The glyph helper (Decision 12) lives in `TrayMenuBuilder` and is the only new use of
  `System.Windows.Media` in the tray stack.
- No `Core` reference to the app project or vice versa beyond the existing direction.

### Decision 8: Controller-refresh behavior preserved (DECIDED — revised 2026-11-17)

`TrayIconController.RefreshMenu` keeps building from `this.actions.GetState()` and overriding only its
owned slots. After Decision 13 those are `RequestUpdateDownload` and `Exit`; the `ShowLaunchWindow`
override goes away with the slot. The `Opened` refresh calls the same method, so the initial build, the
state-change rebuild, and the on-open rebuild converge on one code path.

### Decision 9: Labels carry no action words and no favorite suffix (DECIDED — revision)

- **Project submenu header:** `entry.Project.Name` only — drop the ` *` suffix written at
  `TrayMenuBuilder.cs:49`. Every project submenu is a favorite by construction, and the `FavoriteStar`
  icon (Decision 12) conveys the favorite state.
- **Agent item (initialized and default):** `plugin.Name` only — drop the `Run ` prefix written at
  `:124` and `:140`. The `Play` icon conveys the launch action.
- **Empty states:** `(no favorite projects)` and `(no agents)`, unchanged and disabled.
- All label formatting stays in `TrayMenuBuilder`; no strings move into the projection.

**Rationale:** the user reviewed the delivered menu and called it too heavy. Repeating `Run ` on every
agent row and ` *` on every project row adds no information — the item's placement and icon already say
"favorite" and "launch" — and widens the popup. Icon-only action conveyance is safe here because every
item inside a project submenu is the same action kind (launch); the item is not ambiguous between
select and launch.

**Alternative — keep `Run ` and put the star in a tooltip (rejected):** tooltips are not discoverable on
a tray menu and the repetition remains.

**Alternative — prefix the icon-free items with the verb and rely on the icon for nothing (rejected):**
contradicts Decision 12, which puts the action in the icon on every item.

**Alternative — `Name ▸` or `Launch Name` (rejected):** still action words, and the window's launch
vocabulary is not the tray's.

### Decision 10: Remove `Show CLIHub` (DECIDED — revision)

The menu drops the `Show CLIHub` item (`TrayMenuBuilder.cs:79-81`) and the `ShowLaunchWindow` command
slot it bound.

**Rationale:** the launch window is already reachable by left-clicking the tray icon
(`TrayIconController.cs:48`) and by the global hotkey (`Composition/ServiceRegistration.cs:143-146` →
`TrayIconController.ToggleLaunchWindow`). A third entry point on the same popup adds one row and one
command slot for no capability. `TrayIconController.ShowLaunchWindow` (`:57-60`) and
`ToggleLaunchWindow` (`:65-75`) stay — they are the surviving entry points
(`Startup/WpfApplicationStartupUi.cs:37`).

The separator that used to sit between the update surfaces and `Show CLIHub` stays: it now separates
the action rows (`Settings`, `Updates`) from `Exit`.

**Alternative — keep `Show CLIHub` for discoverability (rejected):** the tray icon itself is the
affordance and left-click is the conventional gesture; the item duplicates it.

**Alternative — keep the item but disable it (rejected):** a disabled entry that can never act is dead
UI, and it would contradict the new icon rule (an actionable item has an icon, a disabled placeholder
does not).

### Decision 11: Group the update surfaces under `Updates` (DECIDED — revision)

The top-level tail becomes `Settings`, `Updates`, `Separator`, `Exit`. The submenu's header text is
`Updates` exactly: a WPF `MenuItem` that owns children already renders the platform submenu arrow, and
the app defines no custom `MenuItem` style, so the label must not repeat the arrow
(`src/CLIHub/Tray/TrayMenuBuilder.cs:85`). The `Updates` submenu contains, in order:

1. the dynamic update item produced by the existing `BuildUpdateItem` logic and its four states
   (`TrayMenuBuilder.cs:88-115`): `Check for updates`, `Checking for updates…` (disabled),
   `Downloading update…` (disabled), `Download <version> and restart`;
2. `What's New` (`commands.ShowReleaseNotes`), moved out of the top level.

`Updates` is always present and never empty, because `BuildUpdateItem` returns an item in every state
(`:88-115`); the existing `if (updateItem is not null)` guard at `:72` therefore becomes redundant.

**Rationale:** `What's New` is a release/update surface, and grouping it removes one top-level row while
keeping `Check for updates` reachable from the tray — which the spec requires, since the tray is the
manual update entry point outside Settings. The grouping also gives the submenu a single meaning
("updates and release notes") instead of two adjacent unrelated rows.

**Alternative — leave `What's New` top level and put only the dynamic item in `Updates` (rejected):**
keeps two top-level update rows and creates a one-item submenu, so the menu does not actually get
lighter.

**Alternative — move `Check for updates` out of the tray entirely (rejected):** removes a behavior the
tray menu owns today and would break the "update check reachable from the tray" expectation.

**Alternative — flatten the update item and `What's New` into one row (rejected):** they invoke different
workflows and cannot share a label.

### Decision 12: Icons on actionable items through a builder-local glyph helper (DECIDED — revision)

Every item that invokes a command sets `MenuItem.Icon` to a glyph `TextBlock`:

| Item | Glyph |
|---|---|
| Favorite project submenu | `IconGlyphs.FavoriteStar` (`IconGlyphs.cs:12`) |
| Agent item, including the default agent | `IconGlyphs.Play` (`:14`) |
| `Add Project…` | `IconGlyphs.FolderAdd` (`:53`) |
| `Settings` | `IconGlyphs.Settings` (`:55`) |
| `Updates` | `IconGlyphs.Sync` (`:38`) |
| Dynamic update item: `Check for updates`, `Checking for updates…` | `IconGlyphs.Sync` |
| Dynamic update item: `Downloading update…`, `Download <version> and restart` | `IconGlyphs.Update` (`:15`) |
| `What's New` | `IconGlyphs.Page` (`:36`) |
| `Exit` | `IconGlyphs.Exit` (`:58`) |
| Header, `(no favorite projects)`, `(no agents)`, separators | no icon |

Mechanism — a small private static helper inside `TrayMenuBuilder`:

```csharp
private static TextBlock CreateGlyph(string glyph) => new()
{
    Text = glyph,
    // Matches Sizing.FontFamily.Icon / Sizing.FontSize.GlyphSmall; hard-coded because the tray
    // popup has no window resource scope.
    FontFamily = new FontFamily("Segoe MDL2 Assets"),
    FontSize = 14,
    IsHitTestVisible = false,
    Focusable = false
};
```

- `FontFamily` is hard-coded to the value of `Sizing.FontFamily.Icon`
  (`src/CLIHub/Themes/Common/Sizing.xaml:19`) instead of a resource lookup: the tray `ContextMenu` is a
  separate popup tree with no custom style, so the builder must not depend on `StaticResource`
  resolution there.
- `FontSize = 14` is the value of `Sizing.FontSize.GlyphSmall` (`Sizing.xaml:32`), the size the
  launch-window action menus use for entry glyphs (`Views/LaunchWindow.xaml:56-62,75-81`). It is
  hard-coded for the same reason; the comment names the token so a future size change is discoverable.
- `IsHitTestVisible = false` and `Focusable = false` keep the glyph decorative: it can neither take the
  pointer hit from the menu item nor take focus. A `TextBlock` has no text selection, so no additional
  suppression is needed.
- No `Foreground` is set: `TextBlock.Foreground` inherits from the hosting `MenuItem`, so the glyph
  follows the menu's normal, hover, and disabled colors without a palette lookup.
- `IconGlyphs` is `internal` in `CLIHub.Theming` and `TrayMenuBuilder` is in the same assembly, so the
  constants are referenced directly (`using CLIHub.Theming;`); no magic strings appear in the builder.
- The helper stays in `TrayMenuBuilder` (a WPF-allowed file per Decision 7); it is not promoted to a
  shared control or a themed style.

**Rationale:** `MenuItem.Icon` is the native menu icon slot, so the items keep the default WPF menu
template and the app keeps no custom `ContextMenu` styling. One `TextBlock` per item is the cheapest
representation — no `Image`, `Path`, or bitmap.

**Alternative — resolve the font and size from application resources (rejected):** the tray popup is not
part of a window's tree and has no app-level style of its own; a hard-coded value is deterministic and
matches the token.

**Alternative — reuse `Controls.DecorativeGlyph` (considered, not used):** it is a `TextBlock` with a
null automation peer (`Controls/DecorativeGlyph.cs:10-17`) and the app's pattern for glyphs that
duplicate adjacent text. The icon lives in `MenuItem.Icon`, outside the item's header text, so there is
no duplicate accessible name to suppress; a plain `TextBlock` keeps the builder's dependencies at
`System.Windows.Controls` plus `System.Windows.Media`.

**Alternative — draw the icons as `Path` geometries (rejected):** the app already has an MDL2 constant
set; duplicating eight glyphs as geometry would diverge from the rest of the UI.

### Decision 13: Trim the tray command surface to what the menu binds (DECIDED — revision)

After the redesign the builder binds `SelectProjectAndLaunchAgentAsync`, `AddProject`, `ShowSettings`,
`ShowReleaseNotes`, `CheckForUpdates`, `RequestUpdateDownload`, and `Exit`. The following are removed:

- `TrayMenuCommands.SelectProject`, `TrayMenuCommands.LaunchAgentAsync`,
  `TrayMenuCommands.ShowLaunchWindow`, with their XML doc lines (`TrayMenuState.cs:32-33,40,42-43,50`);
- the `selectProject` parameter of `TrayCommandHandlers.CreateCommands`
  (`TrayCommandHandlers.cs:59,61-62`), leaving two inert defaults for the controller-owned slots
  (`RequestUpdateDownload`, `Exit`);
- the `TrayActions` argument pass-through (`TrayActions.cs:28` → `CreateCommands()`);
- `TrayStateProjection.SetCurrentProject` (`TrayStateProjection.cs:76-80`), which loses its last caller
  once `SelectProject` is gone (`IProjectService.SetCurrentProject` is untouched and still used by the
  handlers and the project UI);
- the `ShowLaunchWindow` entry in the controller's `with` mutation
  (`TrayIconController.cs:129-134`).

The private `LaunchAgentAsync(Plugin, Project)` **stays**: `SelectProjectAndLaunchAgentAsync` calls it
(`TrayCommandHandlers.cs:110`), so the shared-workflow behavior and the warning on failure are
unchanged; only the record slot that exposed it disappears.

**Tests.** `LaunchAgentCommand_UsesSharedAgentWorkflow` (`TrayActionsTests.cs:52-72`) is deleted — its
only subject is the removed slot, and the same workflow delegation is asserted through the
project-scoped command by `SelectProjectAndLaunchAgentCommand_WhenInvoked_SelectsProjectThenLaunches`
(`:74-99`). `CreateCommands_ControllerOwnedSlots_AreInertDefaults` (`:144-168`) is rewritten to call
`handlers.CreateCommands()` (no lambda), to invoke `RequestUpdateDownload` and `Exit`, and to assert
those stay inert (no calls reach `IProjectService` or `IUpdateWorkflow`).
`TrayMenuBuilderTests.CreateRecordingCommands` (`:282-301`) drops to the seven parameters.

**Rationale:** a slot the builder cannot reach is dead menu API: it is untested surface, it forces every
future reader to check whether the menu uses it, and it was the only reason `TrayStateProjection`
exposed a pass-through setter. Removing it reduces the record, the wiring, and one test to the actual
menu contract.

**Alternative — keep the slots for a future menu (rejected):** no consumer is planned, and re-adding a
slot with a test is mechanical when one is needed.

**Alternative — keep `SelectProject` for a future recent-projects list (rejected):** that surface is
removed by this change, and the remembered-project behavior is the launch window's job.

### Decision 14: Requirement and scenario naming in the delta spec (DECIDED — revision)

The spec keeps the requirement names from the first revision where the requirement still exists
(`Tray menu structure`, `Favorite project submenus`, `Initialized agents per project submenu`,
`Default agent is first in every project submenu`, `Launching from a project submenu selects the
project`, `Add Project is placed below the project list`, `Menu content refreshes when the menu opens`)
and adds two requirements for the new behavior (`Actionable menu items carry MDL2 icons`,
`Updates submenu groups the update surfaces`). The scenario
`Recent Projects and Launch Agent are gone` keeps its name and gains `Show CLIHub`. Scenarios whose
asserted behavior changed are renamed to match (`Favorite marker in the header` →
`Favorite is conveyed by the icon, not the label`; `Agent item is a Run action` →
`Agent item shows the agent name`; `Settings, What's New, update, Show CLIHub, and Exit keep their
behavior` → `Tail order is Settings, Updates, separator, Exit`).

**Rationale:** the capability is new (ADDED delta), so there is no archived scenario contract to
preserve; a scenario name that asserts a removed label would be actively misleading.

## Deviations

- **State shape.** The confirmed brief did not name a `TrayProjectAgents` record; it is the minimal
  structure that keeps the builder a pure renderer (Decision 1).
- **Superseded Decision 6 retained.** The label decision from the first revision is kept in the log with
  a SUPERSEDED marker instead of being deleted, so the reason the ` *` and `Run ` formats were dropped
  stays reviewable.
- **Hard-coded glyph font and size.** The brief requires a hard-coded icon font; the fixed
  `FontSize = 14` duplicates the `Sizing.FontSize.GlyphSmall` token (`Sizing.xaml:32`) for the same
  resource-scope reason. A comment in the helper names both tokens (Decision 12).
- **ASCII `Add Project...`.** The builder writes three ASCII dots (`TrayMenuBuilder.cs:59`), while the
  spec and menu documents write the ellipsis character `…`. This change does not alter the string; the
  implementation must not "fix" the spelling as part of the rework, and the spec keeps its typographic
  form for readability.
- **Dead null guard.** `BuildUpdateItem` never returns null (`TrayMenuBuilder.cs:88-115`), so the guard
  at `:72` is unreachable. It is left alone or deleted during the rework; either keeps `Updates`
  non-empty (Decision 11).
- **`▸` in this change's prose is submenu notation, not label text (clarified 2026-11-17).** A WPF
  `MenuItem` that owns children renders the platform submenu arrow on the right, and the app defines no
  custom `MenuItem` style (no `TargetType="MenuItem"` style under `src/CLIHub/Themes`), so a literal
  `▸` in a header duplicates the platform arrow. The submenu header is exactly `Updates`
  (`src/CLIHub/Tray/TrayMenuBuilder.cs:85`); earlier revisions of these artifacts wrote `Updates ▸`,
  which a reader — and an implementation following them literally — could mistake for the label.

## Open Questions

1. **Icon-less row alignment.** With the default `MenuItem` template, items without an icon (header,
   disabled placeholders) may be indented differently from icon-bearing rows, because only the latter
   populate the icon column. The brief binds "header and disabled placeholders get no icon", so the
   implementation must accept the default look and record it in the manual smoke (`tasks.md`); if it
   reads badly, raise it with the user instead of adding placeholder or transparent icons.
2. **Glyph vertical centering.** `MenuItem.Icon` is hosted by the default template's icon presenter;
   whether the 14pt glyph needs an explicit `VerticalAlignment`/`Width` to match the header baseline can
   only be judged visually. The helper stays minimal unless the smoke reveals a misalignment.
3. **`TrayIconController` after Stage 5.** Stage 5 removes the `ShowLaunchWindow` override from the
   `with` mutation; the refresh and `Opened` path from the first implementation is otherwise untouched.
   Re-read the file after that edit rather than trusting the line numbers above.

## Consequences

- The tray menu becomes project-first and shorter: the top level is header + favorites + `Add Project…`,
  `Settings`, `Updates`, separator, `Exit` — three rows lighter than the delivered first
  implementation (no `Show CLIHub`, no top-level `What's New`).
- Launching agent A in project B is one click-depth path (project submenu → agent), and the project is
  made current as a side effect.
- Labels are the project and agent names only; the icons carry the action and the favorite state.
- Update surfaces share one submenu, and `Check for updates` stays reachable from the tray.
- The tray command record shrinks from ten slots to seven with no behavior loss; the private launch step
  and the project-scoped handler are unchanged.
- The projection still does bounded detection work when the menu opens; a changed `DefaultAgentId` or a
  new favorite is reflected the next time the menu opens.
- Tests that asserted the removed slots, the ` *`/`Run ` labels, `Show CLIHub`, and the nine-item tail
  must be updated (expected, mechanical); the icon mapping needs new assertions.
