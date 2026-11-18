## Context

See `proposal.md` — Why and What Changes. This change adds a Windows-theme-driven visual layer for
WPF context menus: two small palette dictionaries, one application-level style dictionary, a
palette-apply service, and an always-on personalization follower. No existing behavior moves.

All line numbers below were verified against the clean working tree on 2026-11-18.

### Verified current state

**Tray menu construction and lifecycle.**

- `TrayMenuBuilder.Build` creates a plain `new ContextMenu()` (`src/CLIHub/Tray/TrayMenuBuilder.cs:19-27`);
  items are plain `MenuItem`s (`:44-80`), the top-level separator sits before `Exit` (`:76`), the
  conditional default-agent separator at `:157-160`, and every glyph is a `TextBlock` that sets no
  `Foreground` — only `Text`, `FontFamily` ("Segoe MDL2 Assets"), `FontSize` (14),
  `IsHitTestVisible`, `Focusable` (`:176-185`) — so glyph color inherits from the item.
- `TrayIconController.RefreshMenu` attaches the menu to `taskbarIcon.ContextMenu` once and
  repopulates it in place; the `Opened` handler rebuilds content on every open
  (`src/CLIHub/Tray/TrayIconController.cs:135-146`). The menu instance survives for the whole
  session, so a resource swap repaints it without rebuilding it.
- No `ContextMenu`/`MenuItem`/`Separator` styles exist anywhere in `src/CLIHub` (search for
  `TargetType="(MenuItem|ContextMenu|Separator)"` over `*.xaml`: no matches). WPF therefore renders
  the tray menu with the default Aero2 templates — light surface, system hover color.

**Application merge tree and popup precedent.**

- `App.xaml` merges `Themes/Dark.xaml` (base palette), `Common/SlimScrollBar.xaml` (implicit
  `ScrollBar`), and `Common/SettingsStyles.xaml`, and declares the application-level implicit
  `ToolTip` style (`src/CLIHub/App.xaml:9-31`, `:49-53`). The comments at `:12-17` and `:49-53`
  state the reason that also applies here: popups live outside any window's resource scope, so only
  application-level implicit styles reach them.
- `docs/architecture/ui.md:16-31` documents that merge tree; `:33` states the `StaticResource`
  scoping rule (a loose dictionary resolves only against its own keys and its own merges).
- The Actions menu is the in-app look to mirror: the `ActionsPopover` card is `CornerRadius 8`,
  `PopoverOpenBrush` background, `ActionsBorderBrush` 1px border, `Padding 4`
  (`src/CLIHub/Themes/Common/ActionsMenuStyles.xaml:22-29`); entries are 30-height pills with
  `Sizing.CornerRadius.Chip`, hover via `HoverOverlayBrush`, pressed via `PressedOverlayBrush`,
  disabled via `DisabledTextBrush` plus 0.6 opacity (`:32-63`).
- Sizing tokens: `Sizing.FontFamily.Text` = "Segoe UI Variable Text, Segoe UI"
  (`src/CLIHub/Themes/Common/Sizing.xaml:21`), `Sizing.FontSize.Hint` = 12.5 (`:35`),
  `Sizing.CornerRadius.Card` = 8 (`:48`), `Sizing.CornerRadius.Chip` = 6 (`:49`),
  `Sizing.ActionsMenu.MinWidth` = 176 (`:116`).
- Palettes: `Dark.xaml`/`Light.xaml`/`Blue.xaml` define the identical key set, including
  `MenuItemBrush`, `HoverOverlayBrush` (dark `#0FFFFFFF`, light `#0F000000`), `PressedOverlayBrush`
  (dark `#14000000`), `DisabledTextBrush`, `RowDividerBrush` (`src/CLIHub/Themes/Dark.xaml:13-49`,
  `Light.xaml:13-49`; parity enforced by `ThemePaletteParityTests`).

**Palette switching and startup order.**

- `PaletteSwitcher.Shared.Apply(merged, theme, systemPrefersLight)` resolves the palette path via
  `ThemeCatalog`, builds the dictionary before touching the collection (a load failure logs a warning
  and keeps the previous palette), and replaces the single tracked overlay in place or appends it
  (`src/CLIHub/Theming/PaletteSwitcher.cs:56, 68-116`; the pack-URI factory at `:118-122`).
  Open windows repaint through `DynamicResource` references (`docs/architecture/ui.md:129,138`).
- `App.OnStartup` runs `ApplyTheme()` then `ApplyAccent()` before the host starts
  (`src/CLIHub/App.xaml.cs:24-42`), so the palette is in place before any window — and any menu —
  exists.
- `SystemTheme.IsLight()` reads `HKCU\...\Themes\Personalize\AppsUseLightTheme` and yields dark on
  any failure (`src/CLIHub.Core.Windows/SystemTheme.cs:17-28`).

**Existing follower and its lifecycle.**

- `SystemThemeFollower` reacts to `SystemEvents.UserPreferenceChanged` but early-returns unless the
  stored theme is `AppTheme.System` (`src/CLIHub/Theming/SystemThemeFollower.cs:141-144`); its
  decision logic runs on the system-events thread and only the apply is dispatched
  (`:134-158`); the internal constructor exposes test seams for the stored-theme read, the light
  probe, and the subscribe/unsubscribe pair (`:75-87`); production wiring is the parameterless
  constructor (`:56-64`).
- `ShellCoordinator` owns that follower: constructor injection (`src/CLIHub/Startup/ShellCoordinator.cs:20,48,56`),
  `Start(this.context.Dispatch)` in `InitializeShell` (`:112`), `Dispose()` in `Dispose` (`:155`).
- DI: `services.AddSingleton<SystemThemeFollower>()` and the `ShellCoordinator` factory pass it
  (`src/CLIHub/Composition/ServiceRegistration.cs:78-87`).
- Two tests construct `ShellCoordinator` directly and must follow the constructor change (task
  4.5): `CoordinatorFixture` (`tests/CLIHub.Tests/Services/ShellCoordinatorTests.cs:235-255`,
  seam-built `SystemThemeFollower` with recording delegates and the `ThemeFollowerSystemLight`
  probe) and `SessionFixture` (`tests/CLIHub.Tests/Services/ApplicationSessionTests.cs:156-163`,
  seam follower built at `:146-151`). Neither file asserts constructor arity anywhere; the
  follower assertions are `Assert.Single` over each fixture's theme-follower lists
  (`ShellCoordinatorTests.cs:32-33,163,167`; `ApplicationSessionTests.cs:37,63`) and the
  `RaisePreferenceChanged` helper invokes only the recorded theme-follower handler
  (`ShellCoordinatorTests.cs:269-270`) — all stay green exactly when the menu follower's seam
  records into separate lists or no-ops.

**Test guards that constrain the new files (all verified by reading the tests).**

- `ThemeColorLiteralTests` rejects any `#RRGGBB` literal outside `Themes/(Dark|Light|Blue).xaml` and
  `Themes/Common/PickerData.xaml` (`tests/CLIHub.Tests/Themes/ThemeColorLiteralTests.cs:100-109`),
  with a loud presence assertion for the allowlisted files (`:49`).
- `ThemePaletteParityTests` enumerates `Themes/*.xaml` **non-recursively** and requires an identical
  key set and shape (`tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs:13-21`) — new palette-like
  files must not sit at the `Themes/` root.
- `ApplicationResourceKeyTests` collects palette keys from the same non-recursive root glob and bans
  `StaticResource` references to them (`:53-66`), and walks `App.xaml`'s static `Source=` merges to
  ban duplicate non-palette keys (`:198-280`).
- `XamlResourceReferenceTests` resolves every `StaticResource`/`DynamicResource` reference against
  the keys reachable from its file (`tests/CLIHub.Tests/Themes/XamlResourceReferenceTests.cs:77-252`).
  For a `ResourceDictionary` file, a `DynamicResource` key must additionally be reachable from
  App.xaml's merge tree or the narrow `RuntimeProvidedKeys` set — `AccentBrush`,
  `AccentSelectionBrush`, `PathToImage` (`:59-67`; the dictionary rule at `:213-228`, the
  application set assembled at `:144-150`). The runtime-swapped `SystemMenu.*` brushes are in
  neither place until the set gains the eight keys (task 2.5); until then the test reports every
  `SystemMenu.*` reference in `SystemMenuStyles.xaml` as missing.
- `ArchitectureBoundaryTests.ApplicationWpfReferences_AreLimitedToExplicitUiAdapters` fails for any
  file under `src/CLIHub` that mentions `System.Windows` and is not allow-listed; the allow-list
  includes `Theming/PaletteSwitcher.cs` and `Theming/SystemThemeFollower.cs`
  (`tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:7-26,42-54`).
- Test seams to mirror: `RunOnSta` (`tests/CLIHub.Tests/Support/TestHelpers.cs:26`), the follower
  fixture with captured handlers (`tests/CLIHub.Tests/Themes/SystemThemeFollowerTests.cs:125-169`),
  and `PaletteSwitcherTests`'s factory-seam pattern (`tests/CLIHub.Tests/Themes/PaletteSwitcherTests.cs:20-111`).

**Guard-test audit for the `Themes/` tree (re-checked by reading each test, 2026-11-18).** Every
test that treats the `Themes/` tree specially lives in `tests/CLIHub.Tests/Themes/` (a repository
search for `Themes` path literals in tests finds only the files below). Verdicts:

- `ThemeColorLiteralTests` — recursive walk; new palette files need the allowlist → task 1.3
  (Stage 1, unchanged).
- `ThemePaletteParityTests` — non-recursive `Themes/` root glob (`:12-13`); the `Themes/SystemMenu/`
  subdirectory is not visited. No edit needed.
- `ApplicationResourceKeyTests` — facts 1 and 2 glob `Themes/*.xaml` non-recursively (`:52-53`,
  `:115-121`): the subdirectory is invisible to both, the menu palettes declare no accent keys, and
  the styles never reference the accent keys with `StaticResource`. Fact 3's duplicate-key walk
  visits `SystemMenuStyles.xaml` once it is merged from App.xaml (task 2.2); its only `x:Key` (the
  `MenuItem.SeparatorStyleKey` separator style) is unique in the tree. Only fact 1's dynamic-only
  key set is extended → task 2.3 (Stage 2, unchanged).
- `ThemePreviewResourceKeyTests` — models App.xaml + transitive merges for *declared* keys and
  checks only `ThemePreview*`-prefixed keys for orphans (`:29,50-77`); `SystemMenuStyles.xaml`
  declares no preview keys and the menu palettes are outside the merge tree. No edit needed.
- `XamlResourceReferenceTests` — walks **all** XAML recursively (`:81`), so it visits
  `SystemMenuStyles.xaml`; the eight `SystemMenu.*` `DynamicResource` keys need `RuntimeProvidedKeys`
  entries → task 2.5. Its StaticResource side is already sound: the style dictionary's token
  references resolve against its own merged `Sizing.xaml` (task 2.1).
- `SystemThemeFollowerTests`, `PaletteSwitcherTests` — no `Themes/` tree logic. No edit needed.

**WPF platform facts (verified against the dotnet/wpf reference source, `main`).**

- `ContextMenu.HookupParentPopup` creates the host `Popup` with `AllowsTransparency = true`
  **unconditionally** (`ContextMenu.cs:478-485` under
  `src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/`, dotnet/wpf repo).
  Rounded corners and
  per-pixel alpha on the templated root therefore render — no flag of ours is required.
- `ContextMenu.CoerceHasDropShadow` (`ContextMenu.cs:236-253`): `HasDropShadow` only takes effect
  when the popup allows transparency **and** `SystemParameters.DropShadow` is on, and it enables the
  OS-drawn (rectangular, window-bounds) shadow. The popup's `PopupAnimation` is separately bound to
  `SystemParameters.MenuPopupAnimationKey` (`ContextMenu.cs:498`) — the system fade is unaffected by
  templating.
- `MenuItem.Role` is computed and read-only (`MenuItemRole`: `TopLevelItem`, `TopLevelHeader`,
  `SubmenuItem`, `SubmenuHeader`; `MenuItem.cs:743-787`); templates must adapt through triggers. In a
  `ContextMenu`, items take the `SubmenuHeader`/`SubmenuItem` roles.
- `MenuItem` declares `[TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]` (`MenuItem.cs:59`;
  constant at `:2682`). Current source never fetches it via `GetTemplateChild`, but the declared
  contract and every default theme name the submenu popup `PART_Popup`.
- Separators inside menus are re-keyed, not implicit: `MenuItem.PrepareMenuItem` — reached from both
  `MenuItem.PrepareContainerForItemOverride` (`MenuItem.cs:1301-1304`) and
  `ContextMenu.PrepareContainerForItemOverride` (`ContextMenu.cs:387-391`) — points a child
  `Separator`'s `Style` at the resource keyed `MenuItem.SeparatorStyleKey` and sets
  `DefaultStyleKey = SeparatorStyleKey` (`MenuItem.cs:1330-1338`). A plain implicit `Separator`
  style is therefore **ignored** for menu separators, both top-level and in submenus.
- Keyboard navigation lives in the control (`MenuItem.OnKeyDown`, `MenuItem.cs:1866`), not in
  template parts; a template with a normal layout and an `ItemsPresenter` preserves it. Microsoft's
  ContextMenu template documentation confirms no required named parts and the
  `ItemsPresenter`(-in-`ScrollViewer`) shape.

## Decisions

### Decision 1: Menu palette as a dedicated Windows-driven dictionary pair (DECIDED)

Files `src/CLIHub/Themes/SystemMenu/SystemMenuDark.xaml` and `SystemMenuLight.xaml`, one shared key
set, values chosen to approximate Windows 11 menu neutrals:

| Key | Dark (Windows dark) | Light (Windows light) | Basis |
|---|---|---|---|
| `SystemMenu.SurfaceBrush` | `#FF2C2C2C` | `#FFFBFBFB` | Win11 dark/light menu flyout neutral |
| `SystemMenu.BorderBrush` | `#FF454545` | `#FFE1E1E1` | Win11 1px flyout border |
| `SystemMenu.TextBrush` | `#FFF1F1F1` | `#FF1D1D1D` | near-white / near-black menu text |
| `SystemMenu.HoverOverlayBrush` | `#12FFFFFF` | `#12000000` | ~7% overlay, the app idiom (`Dark.xaml:29` uses 6%) |
| `SystemMenu.PressedOverlayBrush` | `#20000000` | `#1F000000` | ~12% black overlay, app idiom (`Dark.xaml:30`) |
| `SystemMenu.DisabledTextBrush` | `#FF9D9D9D` | `#FFA0A0A6` | mid-gray, dimmer than text, readable on surface |
| `SystemMenu.SeparatorBrush` | `#1AFFFFFF` | `#19000000` | ~10% hairline, app divider idiom |
| `SystemMenu.GestureTextBrush` | `#FFB4B4B4` | `#FF6C6C6C` | muted tone for `InputGestureText` |

Rationale: decision 1 of the brief requires independence from the CLIHub palette, so the colors get
their own files and keys; some values land near the CLIHub palettes because both approximate the
same Windows neutrals, but they are selected only by `SystemTheme.IsLight()`. Hover/pressed are
alpha overlays over the surface — the same trick as `HoverOverlayBrush`/`PressedOverlayBrush` — so
one pair of values works over the rounded card and the pills. All values are tunable in exactly
these two files.

Placement: a `Themes/SystemMenu/` **subdirectory**, deliberately not the `Themes/` root —
`ThemePaletteParityTests` treats every root `Themes/*.xaml` as a CLIHub palette and would fail on
the different key set, and `ApplicationResourceKeyTests`'s palette-key glob has the same shape.
The names `SystemMenuDark`/`SystemMenuLight` avoid matching the filename-only palette regexes in
either test. `ThemeColorLiteralTests` gets these two files added to its allowlist (a new, second
color source by design, documented in the test).

### Decision 2: `SystemMenuTheme` overlay service mirroring `PaletteSwitcher` (DECIDED)

`src/CLIHub/Theming/SystemMenuTheme.cs`: a `Shared` static instance, `Apply(IList<ResourceDictionary>
merged, bool systemPrefersLight)` that resolves `/Themes/SystemMenu/SystemMenuLight.xaml` or
`SystemMenuDark.xaml`, builds the dictionary through an internal factory seam (testable without
pack URIs, exactly like `PaletteSwitcher`'s), and replaces the single tracked overlay in place or
appends it. There is no null resolution — dark is a file, not an absence — so the overlay always
exists once applied. A load failure builds nothing, logs a warning through the static `Log`, keeps
the previous overlay, and returns false (never a half-applied menu palette).

Startup: `App.OnStartup` gains `ApplySystemMenuTheme()` after `ApplyAccent()` (`App.xaml.cs:28-29`
today) — before the host starts, so the palette exists before any menu can open. No static base
merge in `App.xaml`: the style dictionary references the brushes only through `DynamicResource`, and
`PaletteSwitcher` demonstrates that an appended overlay resolves for every consumer.

Rejected alternative: writing brushes straight into `Application.Resources` like `AccentApplier`
(`src/CLIHub/Settings/AccentApplier.cs`). That moves the colors into C# literals, which the
repository deliberately forbids outside palettes (`ThemeColorLiteralTests` exists to keep color in
XAML), and swaps eight keys individually instead of one dictionary. The overlay mechanism is the
established one for "a palette swapped at runtime".

### Decision 3: A separate `SystemMenuThemeFollower`, not a second responsibility in `SystemThemeFollower` (DECIDED)

`src/CLIHub/Theming/SystemMenuThemeFollower.cs` with the same shape as `SystemThemeFollower`
(subscribe/unsubscribe seams, `Start(Action<Action> dispatch)` capturing `lastKnownLight`, dispatch
to the UI thread, `IDisposable`), minus the stored-theme guard: any confirmed light/dark flip
dispatches a `SystemMenuTheme.Shared.Apply` under the `Application.Current` guard. It is registered
as a singleton in `ServiceRegistration` and injected into `ShellCoordinator`, started in
`InitializeShell` beside the existing follower (`ShellCoordinator.cs:112`) and disposed beside it
(`:155`).

Rationale: the two followers have different guard policies by requirement — the palette follower
must stay silent for fixed themes (`SystemThemeFollower.cs:141-144` is its point), the menu follower
must always react. Folding the always-on apply into the existing class would complicate a
well-tested single-responsibility class and its fixture for no gain; two subscriptions to the same
static event are cheap and independently disposable. Reusing `PaletteSwitcher` is wrong outright —
different overlay, different selection input. The new constructor parameter breaks the compilation
of the two direct test construction sites listed in the verified state above; task 4.5 rebuilds
them with seam-built followers (no-op subscribe/unsubscribe delegates, a stub `isSystemLight`
probe, a factory-seam `SystemMenuTheme`), so no real `SystemEvents` subscription enters the tests
and the existing `Assert.Single` follower assertions keep their meaning.

### Decision 4: Style dictionary structure — one `MenuItem` template with `Role` triggers, keyed separator style (DECIDED)

`src/CLIHub/Themes/Common/SystemMenuStyles.xaml`, merged from `App.xaml` (popup scope, per the
scrollbar/tooltip precedent), merging `/Themes/Common/Sizing.xaml` itself for its `StaticResource`
token lookups (the scoping rule in `docs/architecture/ui.md:33`; `PickerData.xaml` and
`SettingsControls.xaml` merge `Sizing.xaml` the same way). Contents:

- **Implicit `ContextMenu` style**: `HasDropShadow=False` (the OS shadow is rectangular and would
  contradict the rounded corners — see `ContextMenu.cs:236-253`), `Foreground` =
  `SystemMenu.TextBrush`, template = root `Grid Margin="7"` (room for the shadow blur) wrapping a
  `Border` with `CornerRadius="{StaticResource Sizing.CornerRadius.Card}"`, `Background`/`BorderBrush`
  from the menu palette, `BorderThickness="1"`, `Padding="4"`, `SnapsToDevicePixels="True"`, a
  `DropShadowEffect BlurRadius="16" ShadowDepth="5" Opacity="0.35"` (near the tooltip precedent at
  `src/CLIHub/Themes/Common/Controls.xaml:96-105`), and inside it a `ScrollViewer
  CanContentScroll="True"` hosting an `ItemsPresenter KeyboardNavigation.DirectionalNavigation="Cycle"`.
  The card also gets `MinWidth` from `Sizing.ActionsMenu.MinWidth` (176) so the tray menu does not
  collapse under its labels.
- **Implicit `MenuItem` style**: `FontFamily` from `Sizing.FontFamily.Text`, `FontSize` from
  `Sizing.FontSize.Hint`, `Foreground` = `SystemMenu.TextBrush`, `FocusVisualStyle = {x:Null}` (the
  hover pill doubles as the keyboard highlight, matching the app's button styles). One
  `ControlTemplate` containing: a root `Grid` with a 30-height pill `Border`
  (`CornerRadius="{StaticResource Sizing.CornerRadius.Chip}"`, `Padding="8,0"`), a `DockPanel` with
  a fixed-width icon slot (`ContentPresenter ContentSource="Icon"`), a check `Path` (collapsed
  unless `IsChecked`; defensive — the app has no checkable items today), the header
  `ContentPresenter ContentSource="Header"`, a right-docked `InputGestureText` `TextBlock`
  (collapsed when empty) in `SystemMenu.GestureTextBrush`, a right-docked chevron `Path` (collapsed
  for leaf roles), and the submenu `Popup x:Name="PART_Popup"` (`AllowsTransparency="True"`,
  `Focusable="False"`, `Placement="Right"`,
  `IsOpen="{Binding IsSubmenuOpen, RelativeSource={RelativeSource TemplatedParent}}"` — a **two-way**
  binding, the default-theme pattern, so a popup that dismisses itself clears the item's
  `IsSubmenuOpen`; a one-way `TemplateBinding` would leave the submenu stuck open) hosting a submenu
  card `Border` (same card look, `MinWidth` from the token) with its own `ItemsPresenter`.
  `ControlTemplate.Triggers`: `Role=SubmenuHeader` shows the chevron; `Role=TopLevelHeader` sets the
  popup `Placement` to `Bottom` and shows a down-chevron (defensive — the app has no `Menu` bar);
  leaf roles keep the popup collapsed; `IsHighlighted` sets the pill background to
  `SystemMenu.HoverOverlayBrush`; `IsPressed` sets `SystemMenu.PressedOverlayBrush`; `IsSubmenuOpen`
  keeps the pill highlighted while the child menu is open; `IsEnabled=False` sets the foreground to
  `SystemMenu.DisabledTextBrush` and pill opacity 0.6 (the Actions-menu disabled idiom,
  `ActionsMenuStyles.xaml:55-58`).
- **Keyed separator style**: `<Style x:Key="{x:Static MenuItem.SeparatorStyleKey}" TargetType="Separator">`
  rendering a 1px `SystemMenu.SeparatorBrush` line inset from the card padding
  (`Height="1"`, `Margin="8,4"`). This — not an implicit `Separator` style — is what WPF applies to
  separators inside `ContextMenu` and submenus (`MenuItem.cs:1330-1338` re-keys every menu child
  `Separator`; `ContextMenu.cs:387-391` routes its children through the same helper). The app has no
  separators outside menus (verified), so no implicit `Separator` style is added.

One template with role triggers instead of the Aero2 approach (four keyed templates selected by
`Style.Triggers`): the application has no `Menu` bar, so `TopLevel*` roles are defensive-only and
duplicating the pill/arrow logic four times buys nothing; a single template keeps every state in one
place. `Role` is read-only and computed (`MenuItem.cs:743-787`), so trigger-based switching is the
only mechanism either way.

**Rounded-popup feasibility (asked by the brief): verified feasible.** The internal popup that
hosts every `ContextMenu` is created with `AllowsTransparency = true` unconditionally
(`ContextMenu.cs:482-485`), so the rounded card with transparent corners renders without any flag
on our side. Two precise consequences are designed in: (1) `HasDropShadow` must be `False` on the
style because that property enables the OS-drawn rectangular shadow (`ContextMenu.cs:244-253`) —
the visible shadow comes from the in-template `DropShadowEffect` instead; (2) the effect extends
past layout bounds and the popup window clips to content, so the template root carries `Margin="7"`
to give the blur room. No square-corner fallback is expected; the residual risk (shadow clipping at
unusual DPI) is listed under Open Questions with the fallback defined.

### Decision 5: Fonts and metrics through the existing tokens (DECIDED)

`Sizing.FontFamily.Text`, `Sizing.FontSize.Hint`, `Sizing.CornerRadius.Card`/`Chip`, and
`Sizing.ActionsMenu.MinWidth` are consumed via `StaticResource` and reached by merging
`Sizing.xaml` from `SystemMenuStyles.xaml` itself (Decision 4). No new `Sizing.*` keys: every value
repeats an existing token (the Sizing rule at `Sizing.xaml:14-15` — a value earns a token when it
repeats; 30/8/4 already exist as the Actions-menu metrics or are one-off literals at their use
site).

### Decision 6: Icons inherit; the builder stays untouched (DECIDED)

The tray glyph `TextBlock`s set no `Foreground` (`TrayMenuBuilder.cs:176-185`), so they inherit the
`MenuItem.Foreground` the style sets; the hover, pressed, and disabled foreground triggers therefore
retint the glyphs with the text, and the hard-coded glyph font/size (with its comment about the tray
popup scope) stays as shipped. The fixed-width icon slot keeps items without icons (the header, the
placeholders) aligned with iconed ones.

### Decision 7: Tests and what only the smoke can confirm (DECIDED)

- `SystemMenuThemeTests` (plain tests + factory seam, mirroring `PaletteSwitcherTests`): light adds
  one overlay for the light path; dark-after-light replaces the same slot (count and index
  preserved); a factory that throws returns false and leaves the collection unchanged; two applies
  track exactly one overlay.
- `SystemMenuThemeFollowerTests` (mirroring `SystemThemeFollowerTests`, including the
  no-`Application.Current` note): a flip dispatches exactly one apply with **no** stored theme in
  play; an unchanged mode dispatches nothing; two flips dispatch twice; `Dispose` unsubscribes and
  later events dispatch nothing; second `Start` is a no-op.
- `SystemMenuStyleTests` (STA, `RunOnSta`): load `Themes/Common/SystemMenuStyles.xaml` from the
  repository file via `FindRepositoryRoot()`; assert the implicit `ContextMenu` and `MenuItem`
  styles exist (resource lookup by type key), the `MenuItem.SeparatorStyleKey` style exists, and the
  two `Themes/SystemMenu/*.xaml` files define the identical key set containing every
  `SystemMenu.*` key the style dictionary references.
- Guard-test extensions land in the same stage as the files they admit:
  `ThemeColorLiteralTests` allowlist + presence assertion; `ApplicationResourceKeyTests` extended to
  treat the `Themes/SystemMenu` keys as dynamic-only; `XamlResourceReferenceTests.RuntimeProvidedKeys`
  gains the eight `SystemMenu.*` keys so the reference resolver models the runtime menu palette (the
  `StaticResource` ban and the reachability model are two different guards — both are needed);
  `ArchitectureBoundaryTests` allow-list gains the two new `Theming/` files. The Stage 4 constructor
  change additionally carries its test-fixture update in the same stage (task 4.5).
- Only the manual smoke can confirm: the actual rendered look (rounded card, shadow, pills, glyph
  tinting), submenu placement and arrow, the live Windows-mode flip in a running app for all four
  stored themes, and the TextBox context menu's gesture column.

## Risks

- **App-global implicit styles**: every future WPF menu inherits them. Intended (full custom
  styling), but any future `Menu` bar gets the submenu-style look; `TopLevel*` role triggers keep it
  sane.
- **Shadow clipping**: the `DropShadowEffect` needs the template's `Margin="7"`; if a smoke run
  shows clipping at some DPI, increase the margin (or drop `ShadowDepth`) — square corners are not
  at risk because popup transparency is unconditional (`ContextMenu.cs:482-485`).
- **Color approximations**: the Win11 values are approximations; tuning happens in exactly two
  files with no code change.
- **Guard tests fail loudly on mistakes**: forgetting the allowlist edits (color literals,
  architecture boundaries, duplicate keys) fails the suite in the same stage — the tasks sequence
  the edits accordingly.
- **No menu scrolling buttons**: the default `MenuScrollViewer` machinery is not reproduced; a menu
  taller than the screen would need it. All current menus are far below that (the tray menu is
  ~10 rows).

## Deviations

- The brief asked for implicit styles for `ContextMenu`, `MenuItem`, **and `Separator`**. The
  `Separator` style is **keyed** (`MenuItem.SeparatorStyleKey`), not implicit: the WPF reference
  source shows menu separators are re-keyed away from the implicit lookup
  (`MenuItem.cs:1330-1338`, reached from `ContextMenu.cs:387-391`), so an implicit style alone would
  never reach them, and the app has no separators outside menus. This is a mechanism correction,
  verified, not a scope reduction.
- The brief left open "extend the existing follower or a new one"; a new follower class was chosen
  (Decision 3). The brief's example name `SystemMenuTheme` was kept for the service.
- `HasDropShadow` is set to `False` while the brief asked for "subtle drop shadow": the shadow comes
  from the template's `DropShadowEffect`; `HasDropShadow=True` would add the OS rectangular shadow
  behind rounded corners (`ContextMenu.cs:236-253`).

## Open Questions

1. **Shadow rendering at the edges (smoke)**: confirm the `Margin="7"` + `BlurRadius="16"` shadow
   renders unclipped at the smoke machine's DPI/scale. Fallback if clipped: enlarge the root margin
   or reduce the blur/depth; rounded corners themselves are guaranteed and no square-corner fallback
   is anticipated.
2. **Color tuning (smoke)**: the Win11 approximations (Decision 1 table) may need adjustment after
   the user sees both modes side by side with the Windows shell menus — a two-file change.
3. **Submenu card shadow**: the proposal gives nested submenu cards the same card+shadow as the
   root (Win11 elevates nested menus too). If the smoke shows the doubled shadow as too heavy, the
   submenu card can drop its `DropShadowEffect` alone — a one-line template tweak.
4. **Follower consolidation (parked)**: whether `SystemThemeFollower` and `SystemMenuThemeFollower`
   should eventually share one `UserPreferenceChanged` subscription is left open; two disposables
   on one static event are acceptable now and keep both classes single-responsibility.
