# Tasks: Shared chromeless-window base (`ChromeWindow` + `WindowHeaderBar`)

Conventions for every edit: file-scoped namespace first, `using` directives after it, nullable
reference types, `camelCase` private fields without underscore prefix, `this.` qualification,
braces required, XML docs on the new types and members, English only. All `file:line` references
are pre-change positions at HEAD `422789f1`; when earlier tasks in the same file shift lines,
anchor by the named symbol. Nothing in `CLIHub.Core` or `CLIHub.Core.Windows` changes. No git
write commands, no `openspec archive`, and no `git commit` in these tasks — commit/archive happen
only after the user approves them following the stage 7 report (repository approval policy).

## Stage 1: `ChromeWindow` base class (design.md D1, D2, D4, D5, D7)

- [ ] 1.1 Create `src/CLIHub/Views/ChromeWindow.cs`: `namespace CLIHub.Views;` then `using`
      directives (`System.Runtime.InteropServices`, `System.Windows`, `System.Windows.Input`,
      `System.Windows.Interop`, `System.Windows.Shell`, `CLIHub.Interop`, ordering per
      `.editorconfig`), then `internal abstract class ChromeWindow : Window` with XML docs stating
      it is the shared base for the chromeless secondary windows (Settings, What's New, Browse
      Plugins) and that the launch window deliberately does not use it; protected parameterless
      constructor setting, in this order: `this.WindowStyle = WindowStyle.None;`,
      `WindowChrome.SetWindowChrome(this, new WindowChrome { CaptionHeight = 0, CornerRadius = new
      CornerRadius(0), GlassFrameThickness = new Thickness(0), ResizeBorderThickness = new
      Thickness(6), UseAeroCaptionButtons = false });` (values from `SettingsWindow.xaml:24-30`),
      `this.SetResourceReference(BackgroundProperty, "ChromeBgBrush");`,
      `this.SetResourceReference(FontFamilyProperty, "Sizing.FontFamily.Text");`
- [ ] 1.2 Add `protected override void OnSourceInitialized(EventArgs e)` to `ChromeWindow`:
      `base.OnSourceInitialized(e);`, `WindowChromeHelper.ApplyRoundedCorners(this);`, then
      `HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle())?.AddHook(this.OnWindowMessage);`
      (shape from `SettingsWindow.xaml.cs:52-57`); add a private `OnWindowMessage(IntPtr hwnd, int
      msg, IntPtr wParam, IntPtr lParam, ref bool handled)` whose body is copied verbatim from
      `SettingsWindow.xaml.cs:66-93` with `window.`→`this.` — `User32.WM_GETMINMAXINFO` guard;
      `User32.MonitorFromWindow` + `User32.GetMonitorInfo` with both early returns leaving
      `handled` unset; the four `ptMaxPosition`/`ptMaxSize` work-area assignments;
      `WindowChromeHelper.ApplyMinTrackSize(this, hwnd, lParam);`; `handled = true;` — with an XML
      doc naming both duties (min-track from `MinWidth`/`MinHeight`; maximized window constrained
      to the monitor work area, adapted from `SettingsWindow.xaml.cs:59-65`)
- [ ] 1.3 Add `protected override void OnKeyDown(KeyEventArgs e)` to `ChromeWindow`:
      `base.OnKeyDown(e);` first, then `if (!e.Handled && e.Key == Key.Escape) { this.Close();
      e.Handled = true; }` (base-first preserves current ordering; the `!e.Handled` guard leaves
      already-handled Escape alone); add `internal void BeginHeaderDrag(MouseButtonEventArgs e)`
      with XML doc (moves the window with the drawn header; a press on a header button never
      reaches it because buttons handle the press) containing only the try/catch from
      `SettingsWindow.xaml.cs:106-113` (`this.DragMove();` wrapped in `catch (InvalidOperationException)`
      with the existing comment) — the left-button guard stays at the caller (task 2.2)
- [ ] 1.4 Gate (base class): `dotnet build CLIHub.sln` — expect 0 errors, 0 warnings (the base is
      not yet referenced; if the markup compiler or analyzers reject `abstract` on a future XAML
      base, drop `abstract` — the type is assembly-internal either way; do not proceed on any
      other error)

## Stage 2: `WindowHeaderBar` control (design.md D3, D6, D7)

- [ ] 2.1 Create `src/CLIHub/Views/WindowHeaderBar.xaml`: internal UserControl
      (`x:Class="CLIHub.Views.WindowHeaderBar"` + `x:ClassModifier="NotPublic"`, same
      `xmlns:d`/`xmlns:mc`/`mc:Ignorable` header style as `WhatsNewWindow.xaml:1-8` plus
      `xmlns:themes="clr-namespace:CLIHub.Theming"`), root visual a `Grid` with
      `Style="{StaticResource WindowHeader}"` and `MouseLeftButtonDown="OnHeaderMouseDown"`
      containing: (a) title area — a `StackPanel` `Margin="12,0,0,0"` `Orientation="Horizontal"`
      `VerticalAlignment="Center"` holding an `Image` `Width="16" Height="16"`
      `RenderOptions.BitmapScalingMode="HighQuality"` `Source="{Binding IconSource,
      RelativeSource={RelativeSource AncestorType=UserControl}}"` plus a `TextBlock`
      `Margin="8,0,0,0" VerticalAlignment="Center" FontSize="13" FontWeight="Medium"
      Foreground="{DynamicResource TextPrimaryBrush}"` bound to `Title` (verbatim shape from
      `SettingsWindow.xaml:280-294`), and a sibling `TextBlock`
      `Style="{StaticResource WindowTitle}"` bound to `Title` (from `WhatsNewWindow.xaml:44`),
      with an `x:Null` DataTrigger on `IconSource` (RelativeSource to the UserControl) toggling
      which of the two is `Visible`; (b) right panel — `StackPanel` `HorizontalAlignment="Right"
      Orientation="Horizontal"` holding a `ContentPresenter` bound to `Actions` (RelativeSource to
      the UserControl), the maximize/restore `Button` block copied verbatim from
      `SettingsWindow.xaml:297-320` (style BasedOn `WindowCloseButton` with the ToolTip
      Maximize/Restore trigger and the `FontSize="10"` Maximize/Restore glyph TextBlock trigger on
      ancestor `Window.WindowState`, `Click="OnMaximizeClick"`), collapsed unless `ShowMaximize`
      (DataTrigger on `ShowMaximize`, RelativeSource to the UserControl), and the close `Button`
      copied verbatim from `WhatsNewWindow.xaml:58-62` (`Style="{StaticResource
      WindowCloseButton}" ToolTip="Close" Click="OnCloseClick"` with the
      `Sizing.FontFamily.Icon` / `Sizing.FontSize.GlyphSmall` / `themes:IconGlyphs.Close`
      TextBlock); (c) separator — a `Rectangle` `Height="1" VerticalAlignment="Bottom"
      Fill="{DynamicResource HairlineBrush}"` (from `SettingsWindow.xaml:329-332`) collapsed when
      `ShowSeparator` is false (DataTrigger on `ShowSeparator`, RelativeSource to the UserControl);
      use `DynamicResource` for every palette brush and `StaticResource` only for the
      application-level keyed styles and `Sizing.*` tokens — no window-scoped keys, no color
      literals (design.md D3 resource discipline)
- [ ] 2.2 Create `src/CLIHub/Views/WindowHeaderBar.xaml.cs`: `internal partial class
      WindowHeaderBar : UserControl` with XML docs; dependency properties registered on the
      control type — `TitleProperty` (string, empty), `IconSourceProperty` (ImageSource, null),
      `ActionsProperty` (object, null), `ShowMaximizeProperty` (bool, false),
      `ShowSeparatorProperty` (bool, true) — each with a CLR property wrapper and a doc comment
      naming which window uses it; bubbling routed events `CloseRequestedEvent`/`MaximizeRequestedEvent`
      with CLR add/remove wrappers and `RaiseCloseRequested()`/`RaiseMaximizeRequested()` raise
      methods; handlers `OnCloseClick`/`OnMaximizeClick` (raise the matching event) and
      `OnHeaderMouseDown` (guard `e.ChangedButton != MouseButton.Left` → return, then
      `(Window.GetWindow(this) as ChromeWindow)?.BeginHeaderDrag(e);` — the drag body itself lives
      in the base, design.md D7)
- [ ] 2.3 Gate (header control): `dotnet build CLIHub.sln` — expect 0 errors, 0 warnings (the
      control is not yet referenced by any window; a `StaticResource` failure appears at runtime,
      not build time — stage 6's smoke is the visual gate)

## Stage 3: Migrate SettingsWindow (design.md D2, D4, D6)

- [ ] 3.1 Edit `src/CLIHub/Views/SettingsWindow.xaml`: change the root tag from `<Window
      x:Class="CLIHub.Views.SettingsWindow"` to `<views:ChromeWindow
      x:Class="CLIHub.Views.SettingsWindow" x:ClassModifier="NotPublic"` (the `views` namespace is
      already declared at `:10`); delete the now base-owned attributes — `Background` (`:14`),
      `FontFamily` (`:15`), `KeyDown` (`:18`), `SourceInitialized` (`:21`), `WindowStyle` (`:23`) —
      and the `<WindowChrome.WindowChrome>` block (`:24-30`); keep `Title`, `Icon`, sizes/min
      sizes, `ResizeMode`, `ShowInTaskbar`, `WindowStartupLocation`; delete the now-unused
      `xmlns:shell` declaration (`:6`)
- [ ] 3.2 Edit `src/CLIHub/Views/SettingsWindow.xaml`: replace the drawn-header `Grid`
      (`:277-327`) and the header hairline `Rectangle` (`:329-332`) with a single element in the
      same grid row — `<views:WindowHeaderBar Title="CLIHub - Settings"
      IconSource="pack://application:,,,/app.ico" ShowMaximize="True" CloseRequested="OnCloseClick"
      MaximizeRequested="OnMaximizeRestoreClick"/>` — keeping the top-highlight `Rectangle`
      (`:274`) and the four-row layout untouched; the header bar's built-in separator replaces the
      deleted hairline at the same pixel line (design.md D6)
- [ ] 3.3 Edit `src/CLIHub/Views/SettingsWindow.xaml.cs`: change the class declaration to
      `internal partial class SettingsWindow : ChromeWindow`; delete `OnSourceInitialized`
      (`:52-57`), `OnWindowMessage` (`:66-93`), `OnHeaderMouseDown` (`:99-114`), and `OnKeyDown`
      (`:121-130`) with their doc comments (all four are base-owned); keep the constructor,
      `ShowSettings` (`:38-50`), `OnMaximizeRestoreClick` (`:116-117`), `OnCloseClick` (`:119`) —
      both reused as the header bar's event handlers — and the `RequestClose`/`Closed` VM wiring;
      remove the now-unused `using` directives after confirming no other use: `System.Runtime.InteropServices`
      (`Marshal`), `System.Windows.Interop` (`HwndSource`/`WindowInteropHelper`), `CLIHub.Interop`
      (`User32`), `System.Windows.Input` (`KeyEventArgs`/`MouseButtonEventArgs`); keep
      `System.Windows` (`Application`, `RoutedEventArgs`)
- [ ] 3.4 Gate (Settings): `dotnet build CLIHub.sln` (0 errors) and `dotnet test CLIHub.sln`
      (0 failed; `SettingsPageMetadataTests` and `ArchitectureBoundaryTests` must pass — a failure
      means the page host or the class accessibility changed beyond the header swap, not a test
      problem)

## Stage 4: Migrate WhatsNewWindow (design.md D4, D6, deviation 1)

- [ ] 4.1 Edit `src/CLIHub/Views/WhatsNewWindow.xaml`: change the root tag to
      `<views:ChromeWindow x:Class="CLIHub.Views.WhatsNewWindow" x:ClassModifier="NotPublic"`
      adding `xmlns:views="clr-namespace:CLIHub.Views"`; delete the base-owned attributes —
      `Background` (`:9`), `FontFamily` (`:10`), `KeyDown` (`:13`), `SourceInitialized` (`:16`),
      `WindowStyle` (`:18`) — and the `WindowChrome.WindowChrome` block (`:19-27`); keep `Title`,
      sizes/min sizes, `ResizeMode`, `ShowInTaskbar`, `WindowStartupLocation`, and
      `Window.Resources` (`:28-31`)
- [ ] 4.2 Edit `src/CLIHub/Views/WhatsNewWindow.xaml`: replace the drawn-header `Grid`
      (`:41-64`) with `<views:WindowHeaderBar Title="What's New" ShowSeparator="False"
      CloseRequested="OnCloseClick">` whose `Actions` property element holds the existing
      Check/Install `StackPanel` content moved verbatim (`:45-57`: the two `NotesInstallButton`
      buttons with their `Click="OnCheckClick"`/`Click="OnInstallClick"`, bindings, styles,
      tooltips, and `BoolToVisibility` visibility) — `Click` handlers resolve against this
      window's partial class because the buttons stay declared in this file; **keep** the
      window-bottom hairline `Rectangle` (`:66-69`) and the ScrollViewer (`:72-75`) untouched (it
      is a bottom-of-window line, not a header separator — design.md deviation 1); delete the
      now-unused `xmlns:themes` declaration (`:6`, its only use was the close glyph now inside the
      header bar)
- [ ] 4.3 Edit `src/CLIHub/Views/WhatsNewWindow.xaml.cs`: change the class declaration to
      `internal partial class WhatsNewWindow : ChromeWindow`; delete `OnSourceInitialized`
      (`:37-38`), `OnHeaderMouseDown` (`:44-59`), and `OnKeyDown` (`:79-88`); keep the
      constructor, `ShowNotes` (`:31-35`), `OnCloseClick` (`:61`), `OnInstallClick` (`:63-69`),
      `OnCheckClick` (`:71-77`); remove `using System.Windows.Input` after confirming
      `KeyEventArgs`/`MouseButtonEventArgs` have no other use; keep `System.Windows`
- [ ] 4.4 Gate (WhatsNew): `dotnet build CLIHub.sln` (0 errors) and `dotnet test CLIHub.sln`
      (0 failed)

## Stage 5: Migrate BrowsePluginsDialog (design.md D4, D5, D6)

- [ ] 5.1 Edit `src/CLIHub/Views/BrowsePluginsDialog.xaml`: change the root tag to
      `<views:ChromeWindow x:Class="CLIHub.Views.BrowsePluginsDialog" x:ClassModifier="NotPublic"`
      adding `xmlns:views="clr-namespace:CLIHub.Views"`; delete the base-owned attributes —
      `Background` (`:11`), `FontFamily` (`:12`), `KeyDown` (`:15`), `SourceInitialized` (`:18`),
      `WindowStyle` (`:20`) — and the `WindowChrome.WindowChrome` block (`:21-27`); keep `Title`,
      `Icon`, sizes/min sizes, `ResizeMode`, `ShowInTaskbar="False"`, `WindowStartupLocation="CenterOwner"`;
      delete the unused `xmlns:shell` (`:6`) and, after the header swap, the unused `xmlns:themes`
      (`:7`)
- [ ] 5.2 Edit `src/CLIHub/Views/BrowsePluginsDialog.xaml`: replace the drawn-header `Grid`
      (`:42-70`) with `<views:WindowHeaderBar Title="Browse Plugins"
      IconSource="pack://application:,,,/app.ico" CloseRequested="OnCloseClick"/>`; delete the
      separate separator `Rectangle` (`:72-75`) — the header bar's built-in separator renders at
      the same pixel line — and change the ScrollViewer's `Margin="0,1,0,0"` (`:79`) to no margin
      (the deleted rectangle was what the +1 compensated); keep the top highlight (`:39`),
      content, and footer untouched
- [ ] 5.3 Edit `src/CLIHub/Views/BrowsePluginsDialog.xaml.cs`: change the class declaration to
      `internal partial class BrowsePluginsDialog : ChromeWindow`; delete `OnSourceInitialized`
      (`:28-33`), `OnWindowMessage` (`:41-51`), `OnHeaderMouseDown` (`:57-72`), and `OnKeyDown`
      (`:79-88`); keep the constructor (including the `Loaded` → `LoadAsync` wiring) and
      `OnCloseClick` (`:74-77`); remove `using System.Windows.Interop`, `using CLIHub.Interop`,
      and `using System.Windows.Input` after confirming no other use; keep `System.Windows`
- [ ] 5.4 Gate (Browse): `dotnet build CLIHub.sln` (0 errors) and `dotnet test CLIHub.sln`
      (0 failed; `BrowsePluginsDialogBindingTests` must pass — its asserted binding strings
      (`BrowsePluginsDialogBindingTests.cs:42-54`) all live in the content area this stage does
      not touch)

## Stage 6: Migration guard test + documentation (design.md D8, D9)

- [ ] 6.1 Create `tests/CLIHub.Tests/Views/ChromeWindowBaseMigrationTests.cs` (xUnit, naming
      `MethodOrScenario_Condition_ExpectedResult`, `FindRepositoryRoot()` from
      `CLIHub.Tests.Support.TestHelpers` — mirror `BrowsePluginsDialogBindingTests.cs:1-30` for
      the file-reading pattern) with tests: (a) `Windows_AreRootedInChromeWindow_WithNotPublicModifier`
      — for each of the three window XAML files assert they contain `views:ChromeWindow
      x:Class="CLIHub.Views.<Name>"` and `x:ClassModifier="NotPublic"`; (b)
      `Windows_DeclareWindowHeaderBar_WithExpectedTitle` — assert each contains
      `views:WindowHeaderBar` with `Title="CLIHub - Settings"`, `Title="What's New"`, and
      `Title="Browse Plugins"` respectively; (c)
      `Windows_DoNotRedeclareBaseOwnedChrome` — for each of the three assert none contains
      `WindowChrome.WindowChrome`, `WindowStyle="None"`, `KeyDown="OnKeyDown"`,
      `SourceInitialized=`, `Background="{DynamicResource ChromeBgBrush}"`, or
      `FontFamily="{StaticResource Sizing.FontFamily.Text}"`; (d)
      `WindowHeaderBar_UsesSharedChromeStyles` — assert `src/CLIHub/Views/WindowHeaderBar.xaml`
      exists and references `WindowHeader`, `WindowTitle`, `WindowCloseButton`, and
      `IconGlyphs.Close`
- [ ] 6.2 Edit `docs/architecture/ui.md` "To add a window" recipe (`:35-39`) to describe the new
      pattern, keeping the existing keyed-styles/tooltips guidance: (1) root the window in
      `views:ChromeWindow` (internal base — the window class is `internal` with
      `x:ClassModifier="NotPublic"`); the chromeless profile (no OS chrome, `WindowChrome`
      metrics, chrome background, text font, rounded corners, minimum-size + maximized work-area
      enforcement, Escape-to-close) comes from the base; (2) render `views:WindowHeaderBar` for
      the drawn header (`Title`, `IconSource` for the icon variant, `Actions` slot,
      `ShowMaximize`, `ShowSeparator`, `CloseRequested`/`MaximizeRequested`); (3) keep
      window-specific sizing, `ResizeMode`, `ShowInTaskbar`, `WindowStartupLocation`, and the
      `Themes/Common/<Window>Styles.xaml` dictionary merge as before; note that the launch window
      is deliberately not on `ChromeWindow` (topmost `SizeToContent` popup, `ResizeMode=NoResize`,
      Escape hides)
- [ ] 6.3 Gate (guards + docs): `dotnet build CLIHub.sln` (0 errors) and `dotnet test CLIHub.sln`
      (0 failed; the suite grows by exactly the new guard tests — any other count delta is a
      stop-and-report signal)

## Stage 7: Final gates, smoke, and handoff (design.md D9)

- [ ] 7.1 Final build gate: `dotnet build CLIHub.sln` — confirm 0 errors, 0 warnings
- [ ] 7.2 Final test gate: `dotnet test CLIHub.sln` — confirm 0 failed, 0 skipped; record the
      passed count and confirm the only delta versus the pre-change baseline is stage 6's guard
      tests
- [ ] 7.3 Run `openspec validate --all` — confirm 0 failed with `✓ change/add-chrome-window-base`
      in the output; fix any structural finding only inside
      `openspec/changes/add-chrome-window-base/`, never in `openspec/specs/`
- [ ] 7.4 Run `node .opencode/scripts/check-model-policy.mjs` if any subagent was dispatched
      during implementation, and `node .opencode/scripts/pipeline-state.mjs add add-chrome-window-base`
      to record the pipeline entry (all stage transitions through the script, never direct edits)
- [ ] 7.5 Run `graphify update .` so the knowledge graph reflects the new base class, header
      control, and rewired windows
- [ ] 7.6 Scope check: `git status --short` lists, for this change, only `src/CLIHub/Views/ChromeWindow.cs`
      (new), `src/CLIHub/Views/WindowHeaderBar.xaml` + `.xaml.cs` (new), the six migrated window
      files, `tests/CLIHub.Tests/Views/ChromeWindowBaseMigrationTests.cs` (new),
      `docs/architecture/ui.md`, and `openspec/changes/add-chrome-window-base/` — no `.csproj`,
      no theme dictionary, no `LaunchWindow` file, no `WindowChromeHelper.cs`, nothing else
- [ ] 7.7 Manual smoke (blocks commit/archive per repository policy — run gates first, then wait
      for the user's result): run the app and verify — Settings: opens centered, header shows icon
      + "CLIHub - Settings", drag moves the window, resize stops at 640x480, maximize fills the
      work area with the footer above the taskbar and the button flips to restore, Esc and the
      drawn close button both close; What's New (tray menu): title without icon, Check/Install
      buttons appear in the header only while applicable and still act, drag works, resize stops
      at 380x280, Esc closes, **no** separator under the header and the window-bottom hairline is
      still present; Browse Plugins (Settings → CLI Agents page action): title with icon, modal,
      no taskbar entry, drag works, resize stops at 520x360, Esc closes, separator directly under
      the header; Launch window: unchanged (Escape hides, rounded corners, no resize borders)
- [ ] 7.8 Report the gate results and smoke outcome to the user, then ask for explicit `git
      commit` and `openspec archive` approval (approval never carries over; nothing is staged
      before it)
