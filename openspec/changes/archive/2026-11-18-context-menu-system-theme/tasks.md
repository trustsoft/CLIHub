# Context Menu System Theme: Implementation Tasks

Give WPF context menus a Windows-apps-theme-driven look: two menu palette dictionaries,
application-level implicit menu styles, a palette-apply service, and an always-on personalization
follower. Three ADDED requirements across two spec deltas — two in
`specs/application-theme/spec.md` (the palette rule and the live-apply rule),
one in `specs/tray-menu/spec.md`; no existing requirement text changes. Menu behavior is untouched.

Reference design: `design.md` (Decisions 1-7). Line numbers are from the clean working tree at plan
time (2026-11-18); re-read each file before editing if anything shifted.

The guard-test edits are staged with the files they admit — doing them in a different order fails
the suite in that stage.

## Stage 1: Menu palette dictionaries + color-literal allowlist

- [x] 1.1 Create `src/CLIHub/Themes/SystemMenu/SystemMenuDark.xaml`: a `ResourceDictionary` with
      exactly the eight `SolidColorBrush` keys and dark values from design.md Decision 1
      (`SystemMenu.SurfaceBrush` `#FF2C2C2C`, `BorderBrush` `#FF454545`, `TextBrush` `#FFF1F1F1`,
      `HoverOverlayBrush` `#12FFFFFF`, `PressedOverlayBrush` `#20000000`, `DisabledTextBrush`
      `#FF9D9D9D`, `SeparatorBrush` `#1AFFFFFF`, `GestureTextBrush` `#FFB4B4B4`), with a header
      comment stating: selected by `SystemTheme.IsLight()` only, independent of the CLIHub palette,
      swapped at runtime by `SystemMenuTheme`.
- [x] 1.2 Create `src/CLIHub/Themes/SystemMenu/SystemMenuLight.xaml`: identical key set and file
      shape, light values from Decision 1 (`#FFFBFBFB`, `#FFE1E1E1`, `#FF1D1D1D`, `#12000000`,
      `#1F000000`, `#FFA0A0A6`, `#19000000`, `#FF6C6C6C`).
- [x] 1.3 In `tests/CLIHub.Tests/Themes/ThemeColorLiteralTests.cs`, extend `IsAllowlisted`
      (`:100-109`) with `^Themes/SystemMenu/(SystemMenuDark|SystemMenuLight)\.xaml$` and add a loud
      presence assertion for `Themes/SystemMenu/SystemMenuDark.xaml` beside the PickerData one at
      `:49`, so a rename of the new files fails the test instead of silently passing.

### Gate: build and color-literal guard
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln --filter "FullyQualifiedName~ThemeColorLiteralTests"
```

## Stage 2: Style dictionary + application merge + style tests

- [x] 2.1 Create `src/CLIHub/Themes/Common/SystemMenuStyles.xaml` per design.md Decision 4: merges
      `/Themes/Common/Sizing.xaml`; implicit `ContextMenu` style (`HasDropShadow=False`, root
      `Grid Margin="7"`, card `Border` with `Sizing.CornerRadius.Card`, menu-palette brushes via
      `DynamicResource`, `BorderThickness="1"`, `Padding="4"`, `DropShadowEffect BlurRadius="16"
      ShadowDepth="5" Opacity="0.35"`, `MinWidth` from `Sizing.ActionsMenu.MinWidth`, `ScrollViewer`
      + `ItemsPresenter KeyboardNavigation.DirectionalNavigation="Cycle"`); implicit `MenuItem`
      style (one template with `Role` triggers, 30-height pill with `Sizing.CornerRadius.Chip`,
      fixed-width icon slot, collapsed `InputGestureText` column in `GestureTextBrush`, chevron for
      `SubmenuHeader`, `Popup x:Name="PART_Popup"` with two-way
      `IsOpen="{Binding IsSubmenuOpen, RelativeSource={RelativeSource TemplatedParent}}"`,
      submenu card, `IsHighlighted`/`IsPressed`/`IsSubmenuOpen`/`IsEnabled` triggers); keyed
      `<Style x:Key="{x:Static MenuItem.SeparatorStyleKey}" TargetType="Separator">` (1px
      `SystemMenu.SeparatorBrush`, `Margin="8,4"`).
- [x] 2.2 In `src/CLIHub/App.xaml`, merge `/Themes/Common/SystemMenuStyles.xaml` in
      `ResourceDictionary.MergedDictionaries` (after the `SettingsStyles.xaml` entry at `:30`) with
      a comment in the established style: menus are popups outside any window's resource scope, so
      only application-level implicit styles reach them — same reason as the scrollbar (`:12-17`)
      and tooltip (`:49-53`) entries.
- [x] 2.3 In `tests/CLIHub.Tests/Themes/ApplicationResourceKeyTests.cs`, extend
      `PaletteBrushKeys_AreNeverReferencedWithStaticResource` (`:48-109`) so the key set also
      includes the keys declared by `Themes/SystemMenu/*.xaml` (recursive glob of that folder),
      keeping the existing loud-presence assertions intact — menu brushes must be `DynamicResource`
      so the runtime swap is not frozen at parse time.
- [x] 2.4 Add `tests/CLIHub.Tests/Themes/SystemMenuStyleTests.cs` (STA via `RunOnSta`): load
      `Themes/Common/SystemMenuStyles.xaml` from the repository root
      (`TestHelpers.FindRepositoryRoot()`), assert the implicit `ContextMenu` and `MenuItem` styles
      exist (lookup by type key), assert the `MenuItem.SeparatorStyleKey` style exists, and assert
      both `Themes/SystemMenu/*.xaml` files define the identical key set containing every
      `SystemMenu.*` key referenced by the style dictionary. Test naming
      `MethodOrScenario_Condition_ExpectedResult`.
- [x] 2.5 In `tests/CLIHub.Tests/Themes/XamlResourceReferenceTests.cs`, extend `RuntimeProvidedKeys`
      (`:59-67`) with exactly the eight `SystemMenu.*` keys the style dictionary references:
      `SystemMenu.SurfaceBrush`, `SystemMenu.BorderBrush`, `SystemMenu.TextBrush`,
      `SystemMenu.HoverOverlayBrush`, `SystemMenu.PressedOverlayBrush`,
      `SystemMenu.DisabledTextBrush`, `SystemMenu.SeparatorBrush`, `SystemMenu.GestureTextBrush` —
      with a comment in the set's established style: `SystemMenuTheme` merges the system-menu
      palette into the application resources at startup and re-applies it on each Windows mode
      flip, so no palette XAML merged from App.xaml declares these keys. Without this, the test's
      dictionary rule (`:213-228` — a `DynamicResource` key in a `ResourceDictionary` file must be
      reachable from App.xaml's merge tree or the runtime-provided set, assembled at `:144-150`)
      reports every `SystemMenu.*` reference in `SystemMenuStyles.xaml` as missing. This
      complements 2.3, it does not duplicate it: 2.3 bans `StaticResource` freezes of the same
      keys; 2.5 teaches the reference resolver that they arrive at runtime.

### Gate: build, style tests, resource guards
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln --filter "FullyQualifiedName~SystemMenuStyleTests|FullyQualifiedName~ApplicationResourceKeyTests|FullyQualifiedName~ApplicationLevelMergedDictionaries|FullyQualifiedName~XamlResourceReferenceTests"
```

## Stage 3: `SystemMenuTheme` service + startup apply

- [x] 3.1 Create `src/CLIHub/Theming/SystemMenuTheme.cs` per design.md Decision 2: `sealed`,
      `Shared` static, `Apply(IList<ResourceDictionary> merged, bool systemPrefersLight)` returning
      `bool`, internal factory constructor seam (test seam; production uses pack URIs for
      `/Themes/SystemMenu/SystemMenuLight.xaml` / `SystemMenuDark.xaml`), single tracked overlay
      replaced in place or appended, build-before-touch with a warning log and `false` on load
      failure. File-scoped namespace `CLIHub.Theming`, `this.` qualification, XML docs on public
      members — mirror `PaletteSwitcher.cs` structure and remarks style.
- [x] 3.2 In `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs`, add
      `"Theming" + Path.DirectorySeparatorChar + "SystemMenuTheme.cs"` to
      `WpfAllowedApplicationFiles` (`:7-26`) — required the moment the file mentions
      `System.Windows`.
- [x] 3.3 In `src/CLIHub/App.xaml.cs`, add a private static `ApplySystemMenuTheme()` (XML doc;
      mirrors `ApplyTheme`/`ApplyAccent` at `:51-72`) that calls
      `SystemMenuTheme.Shared.Apply(application.Resources.MergedDictionaries, SystemTheme.IsLight())`
      and invoke it in `OnStartup` after `ApplyAccent()` (`:29`).
- [x] 3.4 Add `tests/CLIHub.Tests/Themes/SystemMenuThemeTests.cs` mirroring
      `PaletteSwitcherTests` (fresh instance, local collection, factory seam):
      `Apply_LightOnEmptyCollection_AddsOneMenuPaletteDictionary`,
      `Apply_DarkAfterLight_KeepsTheCountAndReplacesTheSameSlot`,
      `Apply_CalledTwice_TracksExactlyOneOverlay`,
      `Apply_DictionaryThatCannotLoad_ReturnsFalseAndLeavesTheCollectionUnchanged`.

### Gate: build and theme-service tests
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln --filter "FullyQualifiedName~SystemMenuThemeTests|FullyQualifiedName~ArchitectureBoundaryTests"
```

## Stage 4: Follower + lifecycle wiring

- [x] 4.1 Create `src/CLIHub/Theming/SystemMenuThemeFollower.cs` per design.md Decision 3: same
      shape as `SystemThemeFollower.cs` (subscribe/unsubscribe internal seams, production
      parameterless constructor using `SystemMenuTheme.Shared` and `SystemTheme.IsLight`,
      `Start(Action<Action>)` idempotent, `Dispose` unsubscribes) **without** the stored-theme
      guard: a confirmed light/dark flip always dispatches the apply under the
      `Application.Current` guard; ignore the preference-change category (registry comparison is the
      filter, per the existing follower's remarks).
- [x] 4.2 Add `"Theming" + Path.DirectorySeparatorChar + "SystemMenuThemeFollower.cs"` to
      `WpfAllowedApplicationFiles` in `ArchitectureBoundaryTests.cs` (same edit site as 3.2).
- [x] 4.3 Wire the lifecycle: in `src/CLIHub/Composition/ServiceRegistration.cs` register
      `services.AddSingleton<SystemMenuThemeFollower>();` and pass it as the new constructor
      argument in the `ShellCoordinator` factory (`:78-87`); in
      `src/CLIHub/Startup/ShellCoordinator.cs` add the constructor parameter/field (beside
      `themeFollower` at `:20,48,56`), `menuThemeFollower.Start(this.context.Dispatch)` beside
      `:112`, and `menuThemeFollower.Dispose()` beside `:155`. Update the class XML doc's follower
      sentence to mention both follows.
- [x] 4.4 Add `tests/CLIHub.Tests/Themes/SystemMenuThemeFollowerTests.cs` mirroring
      `SystemThemeFollowerTests` (fixture with captured handlers, no `Application.Current`):
      `UserPreferenceChanged_ModeFlip_DispatchesOneApply` (no stored theme in play — the point of
      this follower), `UserPreferenceChanged_ModeUnchanged_DoesNotDispatch`,
      `UserPreferenceChanged_TwoFlips_DispatchTwice`,
      `Dispose_Unsubscribes_AndLaterEventsDispatchNothing`,
      `Start_SecondCall_IsANoOp`.
- [x] 4.5 Update the two tests that construct `ShellCoordinator` directly so they compile and stay
      hermetic with the new parameter from 4.3: `CoordinatorFixture` in
      `tests/CLIHub.Tests/Services/ShellCoordinatorTests.cs` (`:235-255`) and `SessionFixture` in
      `tests/CLIHub.Tests/Services/ApplicationSessionTests.cs` (`:156-163`, seam follower built at
      `:146-151`). Build the `SystemMenuThemeFollower` through the follower's internal test seam —
      the same subscribe/unsubscribe delegate pattern `SystemThemeFollower` exposes — with no-op
      subscribe/unsubscribe delegates (or delegates recording into new, **separate**
      `MenuThemeFollower*` lists when a dispose assertion is wanted) and a stub `isSystemLight`
      probe (`() => false`), backed by a factory-seam `SystemMenuTheme`
      (`_ => new ResourceDictionary()`): no real `SystemEvents.UserPreferenceChanged` subscription,
      no pack-URI loads, and no `Application.Current` in the test process, so nothing leaks.
      Do not change existing assertions: the theme follower's
      `Assert.Single(fixture.ThemeFollowerSubscribed/Unsubscribed)` spots
      (`ShellCoordinatorTests.cs:32-33,163,167`; `ApplicationSessionTests.cs:37,63`) and the
      `RaisePreferenceChanged` helper (`ShellCoordinatorTests.cs:269-270`) stay valid exactly
      because the menu follower's seam never writes to those lists. Where convenient, add one new
      `Dispose_DisposesMenuThemeFollower`-style assertion using the same delegate-recording pattern
      as `Dispose_DisposesThemeFollower` (`ShellCoordinatorTests.cs:157-173`). Verified at plan
      time: neither file asserts constructor arity anywhere, so these are the only spots to touch.

### Gate: build, follower tests, full app suite
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln --filter "FullyQualifiedName~SystemMenuThemeFollowerTests|FullyQualifiedName~ShellCoordinator|FullyQualifiedName~ApplicationSessionTests"
```

## Stage 5: Documentation + full verification

- [x] 5.1 Update `docs/architecture/ui.md`: extend the merge tree (`:16-31`) with the new
      `Common/SystemMenuStyles.xaml` entry (and its `Sizing.xaml` merge), extend the App.xaml
      layering sentence (`:14`), add a short paragraph after the palette-switching paragraph
      (`:129-138`) describing the Windows-driven menu palette (`Themes/SystemMenu/`, selected by
      `SystemTheme.IsLight()`, swapped by `SystemMenuTheme`, followed live by
      `SystemMenuThemeFollower`, separator style via `MenuItem.SeparatorStyleKey`), and add rows to
      the Theme Data Locations table (`:140-150`).
- [x] 5.2 `dotnet build CLIHub.sln` — expect 0 errors and no new warnings (CI builds with
      `-warnaserror`, so treat any new warning as a failure).
- [x] 5.3 `dotnet test CLIHub.sln` — expect all green.
- [x] 5.4 `openspec validate --all` — expect 0 errors.
- [x] 5.5 `graphify update .`

### Gate: all gates passed
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
openspec validate --all
graphify update .
```

## Manual smoke (user-confirmed, before commit/archive)

- [x] 6.1 Windows dark, app theme Dark: open the tray menu — rounded 8px card, 1px border, subtle
      shadow, hover/pressed pills, dimmed disabled header and placeholders, themed separators,
      glyphs tinted with the text, chevrons on project and Updates submenus, submenu cards styled
      the same. Right/Left/Up/Down arrows, Enter, and Esc behave as before.
- [x] 6.2 Windows light with the app theme still Dark (then repeat with Light/Blue): the menu
      renders the light palette while the windows keep the CLIHub palette — the independence check.
- [x] 6.3 With the app running, flip the Windows apps theme for each stored app theme (Dark, Light,
      Blue, System): the tray menu re-themes without a restart every time; the window palette
      changes only while the stored theme is System.
- [x] 6.4 Right-click a Settings text field (for example the hotkey or feed-URL field): the TextBox
      context menu is themed and the input-gesture column (Ctrl+C etc.) renders in the muted tone.
- [x] 6.5 Confirm the shadow is not clipped at the current display scale (design Open Question 1)
      and report the verdict so any tuning lands in this change, not later.
