# Tasks: Split SettingsViewModel File

Implement in stages with build + test gates after each stage. Do not proceed to the next stage until the
current stage's gate passes.

## Stage 1: Move Secondary Types

Move option records, row class, and navigation types into their own files.

- [x] 1.1 Create `src/CLIHub/ViewModels/Settings/SettingsOptions.cs` with namespace `CLIHub.ViewModels` and
  the eight option records from `SettingsViewModel.cs:26-111` (exact line ranges: `RuntimeOption` :36-42,
  `PathDisplayOption` :49, `ScanDepthOption` :56, `LogLevelOption` :63, `PopupPositionOption` :70,
  `AppThemeOption` :83-89, `AccentChoice` :98-104, `DiscoveryModeOption` :111), preserving exact XML doc,
  parameter names, and computed properties (`DisplayName` on `RuntimeOption`, `AccessibleName` on
  `AccentChoice`). Add `using CLIHub.Core.Models;` (RuntimeKind, PathDisplayStyle, PopupScreenPosition,
  AppTheme, ProjectDiscoveryMode).

- [x] 1.2 Create `src/CLIHub/ViewModels/Settings/WatchedDirectoryRow.cs` with namespace `CLIHub.ViewModels`
  and the `WatchedDirectoryRow` class from `SettingsViewModel.cs:117-218`, preserving exact constructor
  signature, private fields, public properties, computed properties (`DepthChipLabel`, `DiscoveryChipLabel`,
  `DiscoveredSummary`), and `ObservableObject` inheritance. Add `using CLIHub.Core.Models;` for
  `ProjectDiscoveryMode`.

- [x] 1.3 Create `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs` with namespace `CLIHub.ViewModels`,
  the `SettingsPage` record from `:243-252`, and the `SettingsGroup` record from `:259`. No additional
  usings needed.

- [x] 1.4 Remove lines 26-259 from `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` (the 11 secondary
  types now in their own files). The file will still build because no callers outside the file reference
  these types yet.

- [x] 1.5 **Gate**: run `dotnet build CLIHub.sln` and verify zero errors.

- [x] 1.6 **Gate**: run `dotnet test CLIHub.sln` and verify all tests pass.

## Stage 2: Extract Page Catalog

Move the static page catalog into `SettingsPageCatalog` and update the view model to delegate to it.

- [x] 2.1 Add `internal static class SettingsPageCatalog` to `SettingsNavigation.cs` with:
  - A private static readonly `SettingsPage[]` field named `allPages`, initialized with the array from
    `SettingsViewModel.cs:281-352` (the `AllPages` initialization).
  - A private static readonly `SettingsGroup[]` field named `pagesByGroup`, initialized with the projection
    from `SettingsViewModel.cs:407-410` (the `PagesByGroup` initialization).
  - `public static IReadOnlyList<SettingsPage> AllPages => allPages;`
  - `public static IReadOnlyList<SettingsGroup> PageGroups => pagesByGroup;`
  
  Add `using CLIHub.Themes;` to `SettingsNavigation.cs` for `IconGlyphs`. The catalog is `internal`
  (`CLIHub.Tests` has `InternalsVisibleTo`), so no new public API appears.

- [x] 2.2 In `SettingsViewModel.cs`, remove the `AllPages` static field (`:281-352`) and the `PagesByGroup`
  static field (`:407-410`).

- [x] 2.3 In `SettingsViewModel.cs`, update the `Pages` property (`:553`) to:
  ```csharp
  public IReadOnlyList<SettingsPage> Pages => SettingsPageCatalog.AllPages;
  ```

- [x] 2.4 In `SettingsViewModel.cs`, update the `PageGroups` property (`:558`) to:
  ```csharp
  public IReadOnlyList<SettingsGroup> PageGroups => SettingsPageCatalog.PageGroups;
  ```

- [x] 2.5 In `SettingsViewModel.cs`, update the `selectedPage` field initialization (`:444`) to:
  ```csharp
  private SettingsPage selectedPage = SettingsPageCatalog.AllPages[0];
  ```

- [x] 2.6 In `SettingsViewModel.cs`, update the `Load()` method's reset (`:1383`) to:
  ```csharp
  this.selectedPage = SettingsPageCatalog.AllPages[0];
  ```

- [x] 2.7 Update `tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs` method `ReadAllPages()`
  (`:138-151`) to read the catalog through its API instead of reflection, because the catalog is now the
  single source of truth and `CLIHub.Tests` can see it via `InternalsVisibleTo`:
  ```csharp
  private static SettingsPage[] ReadAllPages() => SettingsPageCatalog.AllPages.ToArray();
  ```
  Remove the now-unused `System.Reflection` using if nothing else in the file needs it. Do not change the
  test's assertions.

- [x] 2.8 Update `docs/architecture/ui.md:75` to replace `SettingsViewModel.AllPages` with
  `SettingsPageCatalog.AllPages`:
  ```markdown
  2. Add the `SettingsPage` entry to `SettingsPageCatalog.AllPages` with the key, title, description,
     group, `IsImplemented`, icon, and the optional `HeaderTitleExtraKey`, `HeaderActionKey`, and
     `SummaryRowKey`. The sidebar groups and the page order derive from that array.
  ```

- [x] 2.9 **Gate**: run `dotnet build CLIHub.sln` and verify zero errors.

- [x] 2.10 **Gate**: run `dotnet test CLIHub.sln` and verify all tests pass.

## Stage 3: Simplify DI Registration

Replace the hand-written factory with `AddTransient<SettingsViewModel>()` and update the coupled test.

- [x] 3.1 In `src/CLIHub/Composition/ServiceRegistration.cs`, replace lines 173-186 (the
  `services.AddTransient<SettingsViewModel>(sp => new SettingsViewModel(...)` factory) with:
  ```csharp
  services.AddTransient<SettingsViewModel>();
  ```

- [x] 3.2 In `src/CLIHub/Composition/ServiceRegistration.cs`, update the XML doc above
  `AddSettingsServices()` (`:155-161`) to remove the "with its thirteen dependencies" phrase and simplify:
  ```csharp
  /// <summary>
  ///   Registers the settings window stack (application service, section view models, the settings view
  ///   model, and the window with its accessor) plus the runtime preference-application chain
  ///   (<c> PreferenceApplier </c> with its process, accent, and theme preference targets, and the shared
  ///   palette switcher they use) consumed at startup and on preference change.
  /// </summary>
  ```

- [x] 3.3 In `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs`, update the `settingsViewModel`
  assertion (`:183-184`) to verify transient lifetime and implementation type instead of the factory:
  ```csharp
  Assert.Equal(ServiceLifetime.Transient, settingsViewModel.Lifetime);
  Assert.Equal(typeof(SettingsViewModel), settingsViewModel.ImplementationType);
  ```
  Remove the `Assert.NotNull(settingsViewModel.ImplementationFactory);` line.

- [x] 3.4 **Gate**: run `dotnet build CLIHub.sln` and verify zero errors.

- [x] 3.5 **Gate**: run `dotnet test CLIHub.sln` and verify all tests pass.

## Stage 4: Final Verification

- [x] 4.1 **Gate**: run `dotnet build CLIHub.sln` and confirm 0 warnings, 0 errors.
- [x] 4.2 **Gate**: run `dotnet test CLIHub.sln` and confirm all tests pass.
- [x] 4.3 **Gate**: run `openspec validate --all` and confirm 0 failures.
- [x] 4.4 Run `graphify update .` (graphify-out is gitignored).
- [x] 4.5 **Manual smoke (user-confirmed 2026-10-30)**: open the Settings window and confirm the
  sidebar lists all pages, each page navigates, headers show the correct title/description/icon, and no
  exception is logged.
- [x] 4.6 Verify that `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` is now shorter than 1,800
  lines (expect ~1,700). Run:
  ```powershell
  (Get-Content 'src\CLIHub\ViewModels\Settings\SettingsViewModel.cs').Count
  ```
- [x] 4.7 Do **not** commit. Report the stage status, files, deviations, and exact gate outputs to the
  orchestrator; the user decides about commit and archive.

## Notes

- Do not proceed to the next stage until the current stage's gates pass.
- If any gate fails, diagnose the failure, fix it, re-run the gate, and only then proceed.
- The manual smoke test (4.1) is the final verification that the refactor preserved all behavior.
