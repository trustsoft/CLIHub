## 1. Core Sequence Type

- [x] 1.1 Create `src/CLIHub.Core/UI/BusyGlyphSequence.cs` with a `public static class BusyGlyphSequence` containing: a `private static readonly string[] Frames = ["⠋","⠙","⠹","⠸","⠼","⠴","⠦","⠧","⠇","⠏"];` array, a `public static string GetFrame(int index) => Frames[index % Frames.Length];` method, a `public static int ClampSpeed(int milliseconds) => Math.Clamp(milliseconds, 40, 200);` method, and a `public static string GetReducedMotionGlyph() => "⣿";` method, and verify it compiles with `dotnet build src/CLIHub.Core/CLIHub.Core.csproj`

- [x] 1.2 Create `tests/CLIHub.Core.Tests/UI/BusyGlyphSequenceTests.cs` with tests: `GetFrame_ReturnsCorrectFrame`, `GetFrame_WrapsAroundAtEndOfSequence`, `ClampSpeed_BelowMinimum_ReturnsMinimum` (40), `ClampSpeed_AboveMaximum_ReturnsMaximum` (200), `ClampSpeed_WithinRange_ReturnsValue` (80 returns 80), `GetReducedMotionGlyph_ReturnsFullCellGlyph` (assert "⣿"), and verify all tests pass with `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj`

## 2. BusyMarker Control

- [x] 2.1 Create `src/CLIHub/Controls/BusyMarker.cs` with a `public enum BusyMarkerTone { Busy, Success, Failure }`, a `public sealed class BusyMarker : Control` with dependency properties `IsBusy` (bool, default false), `Speed` (int, default 80), `MarkerTone` (BusyMarkerTone, default Busy), a static constructor calling `DefaultStyleKeyProperty.OverrideMetadata(typeof(BusyMarker), new FrameworkPropertyMetadata(typeof(BusyMarker)))`, and verify it compiles with `dotnet build src/CLIHub/CLIHub.csproj`

- [x] 2.2 In `src/CLIHub/Controls/BusyMarker.cs`, add timer lifecycle: private `DispatcherTimer? timer` field, override `OnApplyTemplate()` to subscribe to `IsVisibleChanged` event and call `UpdateTimerState()`, add `UpdateTimerState()` method that starts the timer when `IsBusy` is true AND `IsVisible` is true (creating it if null, setting `Interval = TimeSpan.FromMilliseconds(BusyGlyphSequence.ClampSpeed(Speed))`), stops/disposes the timer otherwise, and verify it compiles

- [x] 2.3 In `src/CLIHub/Controls/BusyMarker.cs`, add timer tick handler: private `int currentFrameIndex` field, add `OnTimerTick(object? sender, EventArgs e)` method that checks `SystemParameters.ClientAreaAnimation` — if false, sets a "ReducedMotionGlyph" dependency property to `BusyGlyphSequence.GetReducedMotionGlyph()` and stops the timer; otherwise increments `currentFrameIndex`, sets a "CurrentFrame" dependency property to `BusyGlyphSequence.GetFrame(currentFrameIndex)`, subscribe the tick handler in `UpdateTimerState()` when creating the timer, and verify it compiles

- [x] 2.4 In `src/CLIHub/Controls/BusyMarker.cs`, add XML documentation comments for the class, all public properties, and public methods following the existing style from `SummaryCard.cs` (triple-slash `<summary>`, `<param>`, etc.), and verify it compiles with `dotnet build CLIHub.sln`

## 3. BusyMarker Style and Template

- [x] 3.1 In `src/CLIHub/Themes/Common/Controls.xaml`, add an implicit style `<Style TargetType="{x:Type controls:BusyMarker}">` with a `ControlTemplate` containing a `TextBlock` with `TextAlignment="Center"`, `MinWidth="12"` (adjust based on measured glyph width), `Text="{TemplateBinding CurrentFrame}"` (or the property name chosen in 2.3), `Foreground="{DynamicResource TextPrimaryBrush}"`, and template triggers: when `MarkerTone` is `Success`, set `Text` to "✓" and `Foreground` to `{DynamicResource SuccessBrush}`; when `MarkerTone` is `Failure`, set `Text` to "✗" and `Foreground` to `{DynamicResource DangerBrush}`, and verify it compiles with `dotnet build CLIHub.sln`

- [x] 3.2 In the BusyMarker template, add `AutomationProperties.AccessibilityView="Raw"` (or the WPF equivalent to exclude from automation tree) to the TextBlock to mark the glyph as decorative, and verify it compiles

## 4. Update AgentVersionText Constant

- [x] 4.1 In `src/CLIHub/ViewModels/Agents/AgentVersionText.cs` line 13, change `public const string Detecting = "Detecting…";` to `public const string Detecting = "Detecting";` (remove trailing ellipsis), and verify it compiles with `dotnet build CLIHub.sln`

- [x] 4.2 In `tests/CLIHub.Tests/ViewModels/Agents/AgentVersionTextTests.cs` line 12, change `Assert.Equal("Detecting…", AgentVersionText.Detecting);` to `Assert.Equal("Detecting", AgentVersionText.Detecting);`, and verify all tests pass with `dotnet test CLIHub.sln`

## 5. Agent Card BusyMarker Adoption

- [x] 5.1 Locate the XAML file that renders the agent card state chip (the ViewModel is `AgentCardItem`, which exposes `VersionChipText` consumed by Settings → CLI Agents surfaces), and note its path

- [x] 5.2 In `src/CLIHub/ViewModels/Agents/AgentCardItem.cs`, add a `public bool IsProbeRunning => this.InstallState == AgentInstallState.Detecting;` property and call `OnPropertyChanged(nameof(IsProbeRunning))` in the setter of `InstallState` (or wherever `InstallState` changes), and verify it compiles

- [x] 5.3 In the agent card XAML identified in 5.1, compose the state chip with `BusyMarker` inline: replace the chip's text-only binding with a horizontal `StackPanel` or `WrapPanel` containing a `<controls:BusyMarker IsBusy="{Binding IsProbeRunning}" Margin="0,0,4,0" VerticalAlignment="Center"/>` and a `<TextBlock Text="{Binding VersionChipText}" .../>`, ensure the BusyMarker's `IsBusy` binding matches the ViewModel property, and verify it compiles with `dotnet build CLIHub.sln`

## 6. Update UpdateControlViewModel Strings

- [x] 6.1 In `src/CLIHub/ViewModels/Updates/UpdateControlViewModel.cs` line 76, change `"Checking…"` to `"Checking"` (remove trailing ellipsis), and verify it compiles

- [x] 6.2 In `src/CLIHub/ViewModels/Updates/UpdateControlViewModel.cs` lines 77-79, change `$"Downloading {downloading}…"` to `$"Downloading {downloading}"` and `"Downloading…"` to `"Downloading"` (remove trailing ellipses), and verify it compiles

- [x] 6.3 In `src/CLIHub/ViewModels/Updates/UpdateControlViewModel.cs`, add a `public bool IsUpdateControlBusy => this.state is UpdateControlState.Checking or UpdateControlState.Downloading;` property and call `OnPropertyChanged(nameof(IsUpdateControlBusy))` in the `SetState` method (line 239-250), and verify it compiles with `dotnet build CLIHub.sln`

## 7. Update Control BusyMarker Adoption

- [x] 7.1 Locate the XAML file that renders the update control button (the ViewModel is `UpdateControlViewModel`, which exposes `UpdateButtonText` rendered on the launch window footer and Settings → Updates page), and note its path(s)

- [x] 7.2 In the update control button XAML identified in 7.1, replace the button's simple `Content="{Binding UpdateButtonText}"` with a horizontal `StackPanel` or similar containing a `<controls:BusyMarker IsBusy="{Binding IsUpdateControlBusy}" Margin="0,0,4,0" VerticalAlignment="Center"/>` and a `<TextBlock Text="{Binding UpdateButtonText}" VerticalAlignment="Center"/>`, ensure the BusyMarker's `IsBusy` binding matches the ViewModel property, and verify it compiles with `dotnet build CLIHub.sln`

- [x] 7.3 If the update control button appears in multiple XAML files (launch window and Settings), repeat step 7.2 for each file, and verify all compile with `dotnet build CLIHub.sln`

## 8. Build and Test

- [x] 8.1 Build the full solution with `dotnet build CLIHub.sln` and verify 0 errors

- [x] 8.2 Run the full test suite with `dotnet test CLIHub.sln` and verify all tests pass (including the updated `AgentVersionTextTests` and new `BusyGlyphSequenceTests`)

## 9. Spec Validation

- [x] 9.1 Run `openspec validate --all` and verify 0 errors (the spec deltas under `openspec/changes/busy-marker-control/specs/` are valid and the new `busy-marker-control` capability spec is well-formed)

## 10. Manual Smoke

- [x] 10.1 Run the application (`dotnet run --project src/CLIHub/CLIHub.csproj`), open Settings → CLI Agents, trigger a Detect on one agent card, and verify: (a) the state chip shows "Detecting [animated braille glyph]" while the probe runs, (b) the glyph cycles through braille frames at a visible but not distracting speed (~80ms per frame), (c) the chip text has no trailing ellipsis, (d) when the probe completes, the marker disappears and the chip shows the detected version or "Not installed", (e) the layout does not shift noticeably when the marker appears or cycles frames

- [x] 10.2 With the application still running, trigger a manual update check from the launch window footer update button, and verify: (a) the button label shows "Checking [animated braille glyph]" while the check runs, (b) the glyph cycles through braille frames at a visible speed, (c) the button text has no trailing ellipsis, (d) when the check completes, the marker disappears and the button shows the result ("Update to {v}" or the current version), (e) the layout does not shift when the marker appears or cycles

- [x] 10.3 With the application still running, enable Windows reduced motion (Settings → Ease of Access → Display → "Show animations" = Off, or equivalent), close and reopen CLIHub, trigger a Detect on an agent card, and verify: (a) the state chip shows "Detecting ⣿" with a static full-cell braille glyph instead of animating, (b) the glyph does not cycle or change frames, (c) when the probe completes, the static glyph disappears and the chip shows the detected version

- [x] 10.4 With reduced motion still enabled, trigger a manual update check, and verify the update button label shows "Checking ⣿" with the static glyph instead of animating

- [x] 10.5 Re-enable Windows animations, close and reopen CLIHub, and verify the markers animate again on both surfaces

## 11. Final Gate

- [x] 11.1 Run `graphify update .` to refresh the knowledge graph after code changes, and verify 0 errors or warnings
