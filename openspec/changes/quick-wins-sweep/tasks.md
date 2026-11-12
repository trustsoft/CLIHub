# Quick Wins Sweep — Tasks

## Stage 1: Remove unused glyph constants

- [ ] 1.1 Delete `IconGlyphs.Diagnostic` and `IconGlyphs.Process` from `src/CLIHub/Theming/IconGlyphs.cs` (lines 24-25)
- [ ] 1.2 Build gate: `dotnet build CLIHub.sln` (zero warnings, zero errors)
- [ ] 1.3 Test gate: `dotnet test CLIHub.sln` (all pass)

## Stage 2: Switch Settings maximize button tooltip with WindowState

- [ ] 2.1 In `src/CLIHub/Views/SettingsWindow.xaml`, replace the static `ToolTip="Maximize"` attribute on the maximize/restore button (line 299) with an inline `Button.Style` that includes:
  - `BasedOn="{StaticResource WindowCloseButton}"`
  - `Setter Property="ToolTip" Value="Maximize"` (baseline)
  - `Style.Triggers > DataTrigger` on `WindowState == Maximized` that sets `ToolTip="Restore"`
- [ ] 2.2 Build gate: `dotnet build CLIHub.sln` (zero warnings, zero errors)
- [ ] 2.3 Manual smoke: open Settings, maximize the window, hover the button, confirm tooltip reads "Restore"; restore the window, hover the button, confirm tooltip reads "Maximize"

## Stage 3: Propagate external cancellation in PluginFeedClient

- [ ] 3.1 In `src/CLIHub.Core/Plugins/PluginFeedClient.cs`, add `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }` immediately before the general `catch (Exception ex)` block in `GetFeedAsync` (before line 122)
- [ ] 3.2 In the same file, add the same cancellation catch immediately before the general `catch (Exception ex)` block in `DownloadPackageAsync` (before line 204)
- [ ] 3.3 Build gate: `dotnet build CLIHub.sln` (zero warnings, zero errors)
- [ ] 3.4 Test gate: `dotnet test CLIHub.sln` (all pass)

## Stage 4: Align redirect hop limit message with docs

- [ ] 4.1 In `src/CLIHub.Core/Plugins/PluginFeedClient.cs`, change the error message at line 255 from `$"Too many redirects (more than {MaxRedirectHops} hops)"` to `$"Redirect limit exceeded (maximum {MaxRedirectHops} hops)"`
- [ ] 4.2 Build gate: `dotnet build CLIHub.sln` (zero warnings, zero errors)
- [ ] 4.3 Test gate: `dotnet test CLIHub.sln` (all pass)

## Stage 5: Reword stale comment

- [ ] 5.1 In `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Detection.cs`, reword the comment at line 347 from `"leaving the cards it announced stuck on \"Detecting…\" forever."` to `"leaving the cards it announced stuck in the Detecting state forever."`
- [ ] 5.2 Build gate: `dotnet build CLIHub.sln` (zero warnings, zero errors)
- [ ] 5.3 Test gate: `dotnet test CLIHub.sln` (all pass)

## Final Gates

- [ ] 6.1 Full build: `dotnet build CLIHub.sln` (zero warnings, zero errors)
- [ ] 6.2 Full test suite: `dotnet test CLIHub.sln` (all pass)
- [ ] 6.3 OpenSpec validation: `openspec validate --all` (zero errors)
- [ ] 6.4 Graphify refresh: `graphify update .` (success, no stale graph)

## Notes

- **Stage 1:** Dead code removal; no behavior change.
- **Stage 2:** Cosmetic UI polish; the glyph already flips correctly.
- **Stage 3:** Bug fix; external cancellation now propagates to caller (`BrowsePluginsDialogViewModel`), which already handles it correctly at line 294.
- **Stage 4:** Message wording accuracy; no behavior change.
- **Stage 5:** Comment-only; no code or assertion change.
- **Manual smoke (2.3):** The tooltip change is cosmetic and not covered by existing tests. Manual verification confirms the WPF binding works correctly.
