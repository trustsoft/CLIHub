## 1. XAML Chrome Upgrade

- [x] 1.1 In `src/CLIHub/Views/BrowsePluginsDialog.xaml`: change `Height="600"` to `Height="480"` (line 13), `Width="800"` to `Width="640"` (line 13), `MinHeight="400"` to `MinHeight="360"` (line 14), `MinWidth="600"` to `MinWidth="520"` (line 14), and verify it compiles (`dotnet build src/CLIHub/CLIHub.csproj`)

- [x] 1.2 In `src/CLIHub/Views/BrowsePluginsDialog.xaml`: verify `ResizeMode="CanResize"` (line 15), `SourceInitialized="OnSourceInitialized"` and `KeyDown="OnKeyDown"` are present on the Window element (after line 18), and `WindowChrome.ResizeBorderThickness="6"` (line 23) — these are already implemented; re-verify they compile

- [x] 1.3 In `src/CLIHub/Views/BrowsePluginsDialog.xaml`: verify `MouseLeftButtonDown="OnHeaderMouseDown"` is present on the header Grid element (line 40) — already implemented; re-verify it compiles

- [x] 1.4 In `src/CLIHub/Views/BrowsePluginsDialog.xaml`: remove the maximize/restore button from the header StackPanel (the `Button` with `Click="OnMaximizeRestoreClick"` and its `TextBlock` child containing the `DataTrigger` for `WindowState`), leaving only the close button (with `Click="OnCloseClick"`, `ToolTip="Close"`, `Style="{StaticResource WindowCloseButton}"`). The right-aligned StackPanel wrapper around the buttons may stay with a single close button child (no need to flatten it to a single Button), and verify it compiles

## 2. Code-Behind Handlers

- [x] 2.1 In `src/CLIHub/Views/BrowsePluginsDialog.xaml.cs`: verify `using System.Runtime.InteropServices;`, `using System.Windows.Input;`, `using System.Windows.Interop;`, `using CLIHub.Interop;` are present (after line 5) — already implemented; re-verify it compiles

- [x] 2.2 In `src/CLIHub/Views/BrowsePluginsDialog.xaml.cs`: verify `OnSourceInitialized(object? sender, EventArgs e)` method exists, gets the window handle via `new WindowInteropHelper(this).EnsureHandle()`, calls `DwmApi.TryRoundCorners(handle)`, and adds an `HwndSource` hook via `HwndSource.FromHwnd(handle)?.AddHook(this.OnWindowMessage)` — already implemented; re-verify it compiles

- [x] 2.3 In `src/CLIHub/Views/BrowsePluginsDialog.xaml.cs`: in the existing `OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)` method, remove the `ptMaxPosition.X`, `ptMaxPosition.Y`, `ptMaxSize.X`, and `ptMaxSize.Y` assignments (the maximize work-area clamp), keeping only the `ptMinTrackSize.X` and `ptMinTrackSize.Y` assignments with DPI scaling: `ptMinTrackSize.X = (int)Math.Ceiling(this.MinWidth * scale)` and `.Y = (int)Math.Ceiling(this.MinHeight * scale)` where `scale = dpi / 96.0` (the method still handles `User32.WM_GETMINMAXINFO` and computes DPI scale, but applies only the minimum tracking size), and verify it compiles

- [x] 2.4 In `src/CLIHub/Views/BrowsePluginsDialog.xaml.cs`: verify `OnHeaderMouseDown(object sender, MouseButtonEventArgs e)` method exists, filters left button, calls `this.DragMove()` in a try-catch, and swallows `InvalidOperationException` — already implemented; re-verify it compiles

- [x] 2.5 In `src/CLIHub/Views/BrowsePluginsDialog.xaml.cs`: verify `OnKeyDown(object sender, KeyEventArgs e)` method exists, filters `Key.Escape`, calls `this.Close()`, and sets `e.Handled = true` — already implemented; re-verify it compiles

- [x] 2.6 In `src/CLIHub/Views/BrowsePluginsDialog.xaml.cs`: remove the `OnMaximizeRestoreClick(object sender, RoutedEventArgs e)` method entirely, and verify it compiles

## 3. Build and Test

- [x] 3.1 Build the full solution with `dotnet build CLIHub.sln` and verify 0 errors/0 warnings

- [x] 3.2 Run the full test suite with `dotnet test CLIHub.sln` and verify all tests pass

## 4. Manual Verification

- [x] 4.1 Run the application (`dotnet run --project src/CLIHub/CLIHub.csproj`), open the Browse Plugins dialog (from CLI Agents), and verify: (a) dragging the header moves the window, (b) dragging the window edges/corners resizes it, (c) dragging cannot resize below MinWidth 520 / MinHeight 360, (d) rounded corners are visible (Windows 11 only; square corners on Windows 10), (e) pressing Escape closes the dialog, (f) the dialog does not appear in the taskbar, (g) the dialog remains modal (owner window is not interactive while the dialog is open), (h) the header contains only the close button (no maximize/restore button), (i) the default size is 640x480

## 5. Spec Update

- [x] 5.1 Run `openspec validate --all` and verify 0 errors (spec delta is valid)

## 6. Final Gate

- [x] 6.1 Run `graphify update .` to refresh the knowledge graph after code changes
