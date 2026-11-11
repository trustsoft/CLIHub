# Tasks: Dialog-owner and window-chrome helper consolidation

Conventions for every edit (design.md D3): file-scoped namespace first, `using` directives after it,
no `// --- <file name= …>` header blocks (master has none), XML docs on every helper member,
`internal static class` helpers with `internal` members. All `file:line` references below are the
pre-change positions at HEAD `8ca4c3f`; when earlier tasks in the same file shift lines, anchor by
the named symbol. Stages 1 and 2 touch disjoint files and are independently buildable.

## Stage 1: `MessageBoxHelper` (design.md D1, D2, D3, D6)

- [x] 1.1 Create `src/CLIHub/Launching/MessageBoxHelper.cs`: `namespace CLIHub.Launching;`, then
  `using System.Windows;`, then `internal static class MessageBoxHelper` with four fully documented
  members — (a) `internal const string ApplicationTitle = "CLIHub";` with a doc comment naming it
  the caption for modal dialogs; (b) `internal static Window? Owner` implementing **master's**
  active-window-first walk, body copied verbatim from `ProjectDialogService.ResolveOwner`
  (`src/CLIHub/Launching/ProjectDialogService.cs:55-71`: null `Application.Current` → `null`; first
  `IsActive` window in `application.Windows` → that window; else `application.MainWindow`), live
  re-read per access, doc comment adapted from `:49-54` — do **not** port backup's
  `Application.Current?.MainWindow`-only accessor, it regresses `72b1f25` (design.md D1); (c)
  `internal static void Show(string message, string title, MessageBoxImage image)` branching
  `if (Owner is { } owner) MessageBox.Show(owner, message, title, MessageBoxButton.OK, image); else
  MessageBox.Show(message, title, MessageBoxButton.OK, image);` (branch shape from
  `UserNotificationService.cs:18-30`); (d) `internal static MessageBoxResult Show(string message,
  string title, MessageBoxButton buttons, MessageBoxImage image)` with the same two branches
  returning the result (branch shape from `ProjectDialogService.cs:37-47`)
- [x] 1.2 In `src/CLIHub/Launching/ProjectDialogService.cs`: change `SelectFolder`'s
  `dialog.ShowDialog(ResolveOwner())` (`:23`) to `dialog.ShowDialog(MessageBoxHelper.Owner)`;
  change `ConfirmProjectRemoval` (`:29-35`) to call `MessageBoxHelper.Show(...)` with the message
  text character-for-character unchanged and the title argument `"CLIHub"` (`:32`) replaced by
  `MessageBoxHelper.ApplicationTitle`; delete the private `Show` (`:37-47`) and the private
  `ResolveOwner` with its doc comment (`:49-71`); keep `using System.Windows;` (still needed for
  `MessageBoxButton`/`MessageBoxImage`/`MessageBoxResult`) and `using Microsoft.Win32;`
- [x] 1.3 In `src/CLIHub/Launching/PluginDialogService.cs`: change `PickPluginPackage`'s
  `dialog.ShowDialog(ResolveOwner())` (`:40`) to `MessageBoxHelper.Owner`; change the four
  private-`Show` call sites — `ShowInstallConfirmation` (`:85-88`), `ShowValidationError` (`:100`),
  `ShowInstallOutcome` (`:110` and `:114-119`) — from `Show(...)` to `MessageBoxHelper.Show(...)`
  with every argument unchanged (their titles `"Install Agent Plugin"`, `"Plugin package invalid"`,
  `"Plugin installed"`, `"Plugin installation failed"` stay literal — only the `"CLIHub"` literal
  maps to `ApplicationTitle`); change `ShowBrowsePluginsDialog` (`:123-128`) to
  `dialog.Owner = MessageBoxHelper.Owner;` keeping `dialog.ShowDialog();`; delete the private `Show`
  (`:130-140`) and the private `ResolveOwner` with its doc comment (`:142-164`); keep all five
  using directives
- [x] 1.4 In `src/CLIHub/Launching/UserNotificationService.cs`: change `ShowInformation` (`:11-12`)
  to `MessageBoxHelper.Show(message, title, MessageBoxImage.Information);` (caller-supplied title
  stays), change `ShowWarning` (`:15-16`) to
  `MessageBoxHelper.Show(message, MessageBoxHelper.ApplicationTitle, MessageBoxImage.Warning);`,
  and delete the private `Show` (`:18-30`); keep `using System.Windows;` (still needed for
  `MessageBoxImage`). This deliberately unifies the service's owner on the helper's
  active-window-first policy (design.md D2)
- [x] 1.5 In `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs` add exactly one entry
  to `WpfAllowedApplicationFiles` (`:7-25`) immediately after the
  `"Launching" + Path.DirectorySeparatorChar + "UserNotificationService.cs",` line (`:17`):
  `"Launching" + Path.DirectorySeparatorChar + "MessageBoxHelper.cs",` — same literal style, no
  other line touched; required because the new file references `System.Windows` and
  `ApplicationWpfReferences_AreLimitedToExplicitUiAdapters` (`:41-53`) fails for any non-allow-listed
  file containing that string; `Views/WindowChromeHelper.cs` needs no entry (the `Views/` prefix is
  auto-allowed at `:158`) (design.md D6)
- [x] 1.6 Gate (dialog helper): run `dotnet build CLIHub.sln` (expect 0 errors) and
  `dotnet test CLIHub.sln` (expect 0 failed, 0 skipped; baseline **1563** passed = 839
  `CLIHub.Core.Tests` + 67 `CLIHub.Core.Windows.Tests` + 657 `CLIHub.Tests` — if the concurrent
  `probe-cache-extraction` change has landed, add its documented test additions; the invariant is
  **no count delta attributable to this change**, which adds and removes no tests);
  `ArchitectureBoundaryTests.ApplicationWpfReferences_AreLimitedToExplicitUiAdapters` and
  `Composition.ServiceRegistrationTests` (`:52-53` resolve `IProjectDialogService`/
  `IUserNotificationService`) must pass — a failure means the helper location, the allow-list line,
  or a deleted symbol is wrong, not the test

## Stage 2: `WindowChromeHelper` (design.md D4, D5)

- [x] 2.1 Create `src/CLIHub/Views/WindowChromeHelper.cs` by porting
  `git show backup-before-reset:src/CLIHub/Views/WindowChromeHelper.cs` with exactly three
  adaptations: drop the 4-line `// --- <file name= …>` header block; change `public static class`
  and the three `public` members to `internal`; keep the bodies, names, and XML docs verbatim —
  `namespace CLIHub.Views;` then `using System.Windows;`, `using System.Windows.Interop;`,
  `using CLIHub.Interop;` (ordering per `.editorconfig`), then `internal static class
  WindowChromeHelper` with `EnsureOwnedByLaunchWindow(Window window)` (guarded owner assignment:
  `window.Owner is null && Application.Current?.MainWindow is { } owner &&
  !ReferenceEquals(owner, window)`), `ShowAndActivate(Window window)` (`Show`; `Minimized →
  Normal`; `Activate`), and `ApplyRoundedCorners(Window window)`
  (`new WindowInteropHelper(window).EnsureHandle()` + `DwmApi.TryRoundCorners(handle)`)
  (design.md D3, D4)
- [x] 2.2 In `src/CLIHub/Views/LaunchWindow.xaml.cs`: change the expression-bodied
  `OnSourceInitialized` (`:102-103`) to
  `=> WindowChromeHelper.ApplyRoundedCorners(this);`; remove the two now-unused using directives
  `System.Windows.Interop` (`:7`) and `CLIHub.Interop` (`:13`) after confirming neither
  `WindowInteropHelper` nor `DwmApi` appears elsewhere in the file; make **no other change** — in
  particular `ShowOnPointerMonitor` (`:56-84`) keeps its inline `Show`/restore/`MarkShown`/
  `Activate` sequence because `viewModel.MarkShown()` (`:74`) sits between the restore and
  `Activate`, so `ShowAndActivate` cannot replace it behavior-identically (design.md D5)
- [x] 2.3 In `src/CLIHub/Views/SettingsWindow.xaml.cs`: in `ShowSettings` (`:38-62`) replace the
  comment plus guarded owner assignment (`:42-47`) with `WindowChromeHelper.EnsureOwnedByLaunchWindow(this);`,
  keep the launch-window height sync (`:49-52`) exactly where it is (between the two helper calls),
  and replace the `Show`/restore/`Activate` block (`:54-61`) with
  `WindowChromeHelper.ShowAndActivate(this);`; in `OnSourceInitialized` (`:64-69`) replace the
  `DwmApi.TryRoundCorners(handle);` line (`:67`) with `WindowChromeHelper.ApplyRoundedCorners(this);`
  **before** the existing `var handle = new WindowInteropHelper(this).EnsureHandle();` +
  `HwndSource.FromHwnd(handle)?.AddHook(...)` lines (this is the shape `e141732` shipped;
  `EnsureHandle` is idempotent); leave the `WM_GETMINMAXINFO` hook (`OnWindowMessage`, `:78-110`)
  and all using directives untouched at this stage (stage 3 rewires the hook's min-track fragment
  and re-checks the usings)
- [x] 2.4 In `src/CLIHub/Views/WhatsNewWindow.xaml.cs`: in `ShowNotes` (`:33-50`) replace the
  comment plus guarded owner assignment (`:35-40`) with `WindowChromeHelper.EnsureOwnedByLaunchWindow(this);`
  and the `Show`/restore/`Activate` block (`:42-49`) with
  `WindowChromeHelper.ShowAndActivate(this);`; change the expression-bodied `OnSourceInitialized`
  (`:52-53`) to `=> WindowChromeHelper.ApplyRoundedCorners(this);`; remove the two now-unused using
  directives `System.Windows.Interop` (`:5`) and `CLIHub.Interop` (`:7`)
- [x] 2.5 In `src/CLIHub/Views/BrowsePluginsDialog.xaml.cs`: in `OnSourceInitialized` (`:30-35`)
  apply the same edit as SettingsWindow — `WindowChromeHelper.ApplyRoundedCorners(this);` replacing
  the `DwmApi.TryRoundCorners(handle);` line (`:33`), keeping the `EnsureHandle` + `AddHook` lines
  (`:32`, `:34`); leave the `WM_GETMINMAXINFO` hook (`OnWindowMessage`, `:44-60`) and all using
  directives untouched at this stage (stage 3 rewires the hook and removes the then-unused
  `System.Runtime.InteropServices`) — this
  window gets no `EnsureOwnedByLaunchWindow`/`ShowAndActivate`: its owner is assigned by
  `PluginDialogService.ShowBrowsePluginsDialog` and it is shown via `ShowDialog` (design.md D4)
- [x] 2.6 Gate (chrome helper): run `dotnet build CLIHub.sln` (expect 0 errors, no unused-using
  warnings) and `dotnet test CLIHub.sln` (expect 0 failed, 0 skipped, and no count delta
  attributable to this change — baseline **1563**, see 1.6 for the concurrent-change note);
  grep `src/CLIHub/Views/` for `DwmApi.TryRoundCorners` and `new WindowInteropHelper` — the only
  remaining occurrences must be inside `WindowChromeHelper.cs` (plus the `EnsureHandle` + `AddHook`
  lines in `SettingsWindow.xaml.cs`/`BrowsePluginsDialog.xaml.cs` and `HwndSource` uses)

## Stage 3: `WM_GETMINMAXINFO` min-track consolidation and final gates (design.md D7, D8)

- [x] 3.1 In `src/CLIHub/Views/WindowChromeHelper.cs` (created in 2.1) add
  `using System.Runtime.InteropServices;` (position per `.editorconfig` ordering) and a fourth
  member `internal static void ApplyMinTrackSize(Window window, IntPtr hwnd, IntPtr lParam)`
  whose body is the shared fragment copied verbatim from `BrowsePluginsDialog.OnWindowMessage`
  (`src/CLIHub/Views/BrowsePluginsDialog.xaml.cs:51-57` at pre-change HEAD): `var dpi =
  User32.GetDpiForWindow(hwnd);` / `var scale = dpi == 0 ? 1.0 : dpi / 96.0;` then
  `var minMax = Marshal.PtrToStructure<User32.MINMAXINFO>(lParam);`,
  `minMax.ptMinTrackSize.X = (int)Math.Ceiling(window.MinWidth * scale);` and the `.Y`/
  `MinHeight` twin (`window.` in place of the dialog's `this.`), then
  `Marshal.StructureToPtr(minMax, lParam, false);`; XML doc adapts the dialog's rationale
  (`:37-43`: handling the message bypasses WPF's own size conversion, so layout units are
  converted to physical pixels via the window DPI); no boundary allow-list change (the `Views/`
  prefix is auto-allowed) (design.md D7)
- [x] 3.2 In `src/CLIHub/Views/BrowsePluginsDialog.xaml.cs` rewire `OnWindowMessage`
  (`:44-60` pre-change): keep the `User32.WM_GETMINMAXINFO` guard (`:46-49`) and the trailing
  `handled = true;` / `return IntPtr.Zero;` (`:58-59`); replace the DPI-scale, marshal, and
  `ptMinTrackSize` statements between them (`:51-57`) with the single call
  `WindowChromeHelper.ApplyMinTrackSize(this, hwnd, lParam);`; adapt the hook's doc comment
  (`:37-43`) to state the delegation (the DPI-conversion rationale now lives on the helper);
  then remove `using System.Runtime.InteropServices;` (`:3`) after confirming `Marshal` appears
  nowhere else in the file; keep `System.Windows.Interop` and `CLIHub.Interop`
  (`OnSourceInitialized` still uses `HwndSource`/`WindowInteropHelper`, the guard still uses
  `User32.WM_GETMINMAXINFO`) (design.md D7)
- [x] 3.3 In `src/CLIHub/Views/SettingsWindow.xaml.cs` rewire `OnWindowMessage` (`:78-110`
  pre-change): keep the guard (`:80-83`), the monitor lookup with both early returns that leave
  `handled` unset (`:85-95`), `Marshal.PtrToStructure` (`:100`), and the four
  `ptMaxPosition`/`ptMaxSize` work-area assignments (`:101-104`); delete the DPI-scale pair
  (`:97-98`) and the inline `ptMinTrackSize` pair (`:105-106`); insert
  `WindowChromeHelper.ApplyMinTrackSize(this, hwnd, lParam);` immediately before
  `handled = true;` — the helper's own marshal round trip preserves the `ptMax*` fields just
  written, so the final struct is byte-identical (the one extra 40-byte copy per message is the
  accepted trade-off, design.md D7); keep the hook's doc comment (`:71-77`) and every using
  directive (`Marshal.SizeOf`/`PtrToStructure`/`StructureToPtr` and the `User32` monitor APIs
  are still used) (design.md D7)
- [x] 3.4 Gate (min-track consolidation): run `dotnet build CLIHub.sln` (expect 0 errors, no
  unused-using warnings) and `dotnet test CLIHub.sln` (expect 0 failed, 0 skipped, and no count
  delta attributable to this change — baseline **1563**, see 1.6); grep `src/CLIHub/Views/` for
  `ptMinTrackSize` — occurrences may exist only inside `WindowChromeHelper.cs`; confirm
  `ArchitectureBoundaryTests` still passes with no further allow-list edit
- [x] 3.5 Final build gate: run `dotnet build CLIHub.sln` and confirm 0 errors / 0 warnings
- [x] 3.6 Final test gate: run `dotnet test CLIHub.sln` and confirm 0 failed, 0 skipped, and no
  count delta attributable to this change (baseline **1563** = 839 + 67 + 657; add the concurrent
  `probe-cache-extraction` change's documented additions if it landed first — this change adds,
  removes, and weakens no test); any delta you cannot attribute to a concurrent change means a test
  was touched — stop and report instead of adjusting expectations
- [x] 3.7 Run `openspec validate --all` and confirm **0 failed** with
  `✓ change/dialog-and-chrome-helpers` in the output (32 specs at baseline plus one item per
  active change; observed **35 items** at revision time with this change,
  `probe-cache-extraction`, and `simplify-mechanical-sweep` all active); fix any structural
  finding only inside `openspec/changes/dialog-and-chrome-helpers/`, never in
  `openspec/specs/`
- [x] 3.8 Run `graphify update .` so the knowledge graph reflects the two new helper files and the
  rewired services and windows
- [x] 3.9 Scope check: `git status --short` lists, for this change, only
  `src/CLIHub/Launching/MessageBoxHelper.cs` (new), `src/CLIHub/Views/WindowChromeHelper.cs`
  (new), the three `Launching` services, the four `Views` windows,
  `tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs` (one allow-list line),
  and `openspec/changes/dialog-and-chrome-helpers/` — no other test file, no `.csproj`, no XAML
  (the `SourceInitialized="OnSourceInitialized"` handlers keep their names), and **no
  `docs/backlog.md` edit** (the preparation-time park entry is superseded by including the
  extraction); entries belonging to the concurrent changes (`probe-cache-extraction`'s Core
  agent/test files, `simplify-mechanical-sweep`'s model/converter/XAML-resource files) and the
  ten untracked `backup-port-*.md` files may also appear and must be left untouched; then report
  the result and ask the user for `git commit` and `openspec archive` approval (approval never
  carries over; nothing is staged before it). No manual smoke is expected (design.md Open
  Questions 2: any observed difference in dialog ownership, rounding, z-order, or minimum-size
  enforcement is a stop-and-report condition)
