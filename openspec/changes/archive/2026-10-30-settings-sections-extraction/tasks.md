# Tasks: Extract Three Settings Sections from SettingsViewModel

## Overview

This change extracts three focused collaborators from `SettingsViewModel`: `SettingsUpdateCard` record,
`AccentSelection` class, and `WatchedDirectoryEditor` class. It is a behavior-preserving refactor: no specs
change, no features change, no unrelated edits.

**Implementer note**: do NOT commit after stages. The user must explicitly approve a commit. Build and test after
each stage; fix any errors before proceeding.

---

## Stage 1: Extract SettingsUpdateCard Record

### 1.1 Create `SettingsUpdateCard.cs`
- [x] Create `src/CLIHub/ViewModels/Settings/SettingsUpdateCard.cs`
- [x] Add file-scoped namespace `CLIHub.ViewModels`
- [x] Add required usings (`CLIHub.Controls` for `SettingBadgeTone`, `CLIHub.Themes` for `IconGlyphs`)
- [x] Define sealed record with:
  - `string Title` parameter
  - `string Detail` parameter
  - `string Badge` parameter
  - `SettingBadgeTone Tone` parameter
  - Computed `string Glyph` property with switch expression (Success â†’ Checkmark, Accent â†’ Refresh, _ â†’ Info)
- [x] Add XML doc comment for the record and the `Glyph` property

### 1.2 Replace Update Status Fields in SettingsViewModel
- [x] Open `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`
- [x] Keep the `updateMessage` field (it backs the unchanged `UpdateMessage` property)
- [x] Remove the five status fields (lines 123-127): `updateStatusTitle`, `updateStatusDetail`, `updateStatusBadge`, `updateStatusGlyph`, `updateStatusBadgeTone`
- [x] Add field: `private SettingsUpdateCard updateCard = new("Check for updates", string.Empty, "Not checked", SettingBadgeTone.Neutral);` with a safe default
- [x] Remove properties (lines 896-936): `UpdateStatusTitle`, `UpdateStatusDetail`, `UpdateStatusBadge`, `UpdateStatusBadgeTone`, `UpdateStatusGlyph`
- [x] Add property: `public SettingsUpdateCard UpdateCard { get; private set; }` initialized to the default in the field
- [x] Keep `UpdateMessage` property (lines 887-891) unchanged

### 1.3 Update Update Status Methods
- [x] Update `ResetUpdateStatus()` (lines 1485-1504):
  - Replace field assignments with `this.UpdateCard = new SettingsUpdateCard(title, detail, badge, tone);`
  - Keep the `updateWorkflow.LastKnownAvailableVersion` logic
- [x] Update `ApplyUpdateResult(UpdateCheckResult)` (lines 1506-1549):
  - Replace `SetUpdateStatus(...)` calls with inline `this.UpdateCard = new SettingsUpdateCard(...)` assignments
  - Keep the `UpdateMessage` assignments unchanged
- [x] Remove `SetUpdateStatus(string, string, string, SettingBadgeTone)` method (lines 1551-1563)
- [x] Update `CheckForUpdatesAsync(CancellationToken)` (lines 1457-1483):
  - Replace `SetUpdateStatus(...)` calls with `this.UpdateCard = new SettingsUpdateCard(...)` assignments
- [x] Update `RunUpdateCheckAsync(CancellationToken)` (lines 972-982):
  - Replace `SetUpdateStatus(...)` call with `this.UpdateCard = new SettingsUpdateCard(...)` assignment

### 1.4 Update XAML Bindings for Update Status
- [x] Open `src/CLIHub/Views/SettingsWindow.xaml`
- [x] Replace `UpdateStatusBadgeTone` with `UpdateCard.Tone` at lines 44, 49, 63, 67, 80, 84
- [x] Replace `UpdateStatusDetail` with `UpdateCard.Detail` at line 121
- [x] Replace `UpdateStatusGlyph` with `UpdateCard.Glyph` at line 123
- [x] Replace `UpdateStatusBadge` with `UpdateCard.Badge` at line 126

### 1.5 Build and Test Stage 1
- [x] Run `dotnet build CLIHub.sln` â€” must succeed
- [x] Run `dotnet test CLIHub.sln` â€” must succeed
- [x] Verify: no compilation errors, no test failures

---

## Stage 2: Extract AccentSelection Class

### 2.1 Create `AccentSelection.cs`
- [x] Create `src/CLIHub/ViewModels/Settings/AccentSelection.cs`
- [x] Add file-scoped namespace `CLIHub.ViewModels`
- [x] Add required usings (`CLIHub.Themes` for `AccentPalette`, `CLIHub.ViewModels` for `AccentChoice` and `ObservableObject`)
- [x] Define sealed class inheriting `ObservableObject`
- [x] Add fields:
  - `private readonly AccentSelectionMemory accentMemory;`
  - `private string accentText = AccentPalette.DefaultHex;`
  - `private bool customAccentSelected;`
- [x] Add static fields from `SettingsViewModel.cs`:
  - `CustomAccentChoice` (line 72-73)
  - `AccentChoices` (lines 75-78)
- [x] Add constructor taking `AccentSelectionMemory accentMemory`, null-check, assign field
- [x] Add property `public IReadOnlyList<AccentChoice> AccentOptions => AccentChoices;`
- [x] Copy property `AccentText` (lines 543-561) â€” keep setter logic
- [x] Copy property `SelectedAccent` (lines 566-598) â€” keep setter logic
- [x] Copy property `ActiveAccentLabel` (lines 603-610) â€” computed from `accentText`
- [x] Copy method `FindAccentPreset(string)` (lines 1139-1149) as private
- [x] Add method `public void Load(string accentColor)`:
  - Copy logic from `SettingsViewModel.Load()` lines 1024-1028
  - Raise property-changed notifications for `AccentText`, `SelectedAccent`, `ActiveAccentLabel`
- [x] Add XML doc comments for the class, constructor, public properties, and `Load` method

### 2.2 Replace Accent Section in SettingsViewModel
- [x] Open `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`
- [x] Remove fields (lines 136-137): `accentText`, `customAccentSelected`
- [x] Remove static fields (lines 72-78): `CustomAccentChoice`, `AccentChoices`
- [x] Remove properties (lines 538-610): `AccentOptions`, `AccentText`, `SelectedAccent`, `ActiveAccentLabel`
- [x] Remove method `FindAccentPreset(string)` (lines 1139-1149)
- [x] Add field: `private readonly AccentSelection accent;`
- [x] Add property: `public AccentSelection Accent => this.accent;`
- [x] Update constructor:
  - **Keep** the `AccentSelectionMemory accentMemory` parameter (line 178); it is needed to construct the collaborator
  - Remove the `this.accentMemory = ...` field assignment (line 192) and the `accentMemory` field (line 104)
  - Add after `this.runtimeExecutables = ...`: `this.accent = new AccentSelection(accentMemory);`
- [x] Update `Load()` method (lines 1024-1028):
  - Replace the accent seeding block with: `this.accent.Load(draft.AccentColor);`
  - Remove notifications for `AccentText`, `SelectedAccent`, `ActiveAccentLabel` (lines 1090-1092)
- [x] Update `Save()` method (line 1584):
  - Replace `this.AccentText` with `this.Accent.AccentText`

### 2.3 Update XAML Bindings for Accent
- [x] Open `src/CLIHub/Views/SettingsAppearancePage.xaml`
- [x] Replace `ActiveAccentLabel` with `Accent.ActiveAccentLabel` at line 107
- [x] Replace `AccentOptions` with `Accent.AccentOptions` at line 113
- [x] Replace `SelectedAccent` with `Accent.SelectedAccent` at line 114
- [x] Replace `SelectedAccent.IsCustom` with `Accent.SelectedAccent.IsCustom` at line 190
- [x] Replace `AccentText` with `Accent.AccentText` at line 203

### 2.4 Build and Test Stage 2
- [x] Run `dotnet build CLIHub.sln` â€” must succeed
- [x] Run `dotnet test CLIHub.sln` â€” must succeed
- [x] Verify: no compilation errors, no test failures

---

## Stage 3: Extract WatchedDirectoryEditor Class

### 3.1 Create `WatchedDirectoryEditor.cs` â€” Part 1: Structure
- [x] Create `src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs`
- [x] Add file-scoped namespace `CLIHub.ViewModels`
- [x] Add required usings (verify exact usings from `SettingsViewModel.cs` lines 1-24)
- [x] Define sealed class inheriting `ObservableObject`, implementing `IDisposable`
- [x] Add readonly fields:
  - `private readonly SettingsApplicationService settingsApplication;`
  - `private readonly IProjectScanner projectScanner;`
  - `private readonly IProjectDialogService dialogs;`
  - `private readonly Action<string?> setValidationError;` (or choose property approach)
  - `private readonly List<WatchedDirectoryRow> observedWatchedDirectoryRows = new();`
- [x] Add mutable fields (lines 138-149): `scanDepthText`, `selectedDiscoveryMode`, `newIgnoredPatternText`, `newWatchedDirectoryText`, `watchedDirectoryError`, `discoveredProjectCount`, `manualProjectCount`, `scannedDirectoryCount`, `watcherStateLabel`, `watcherDetailText`, `disposed`
- [x] Add static fields from `SettingsViewModel.cs`:
  - `DiscoveryModeOptions` (lines 80-85)
  - `ScanDepthChoices` (lines 87-96)
- [x] Add collections:
  - `public ObservableCollection<WatchedDirectoryRow> WatchedDirectories { get; } = new();`
  - `public ObservableCollection<string> IgnoredPatterns { get; } = new();`
- [x] Add constructor:
  - Parameters: `SettingsApplicationService settingsApplication`, `IProjectScanner projectScanner`, `IProjectDialogService dialogs`, `Action<string?> setValidationError`
  - Null-checks and field assignments
  - Subscribe to `this.WatchedDirectories.CollectionChanged` and `this.projectScanner.StatusChanged`
  - Initialize commands (copy command initialization from `SettingsViewModel` constructor lines 206-213)
- [x] Add XML doc comment for the class and constructor

### 3.2 Copy Properties and Commands
- [x] Copy properties (lines 620-882):
  - `NewWatchedDirectoryText`, `WatchedDirectoryError`, `HasWatchedDirectoryError`, `WatchedRootsSummary`
  - `NewIgnoredPatternText`
  - `ScanDepthText`, `ScanDepthOptions`, `SelectedScanDepth`
  - `DiscoveryModes`, `SelectedDiscoveryMode`
  - `DiscoveredProjectCount`, `ManualProjectCount`, `TotalProjectCount`, `ProjectMixSummary`
  - `ScannedDirectoryCount`, `ScannedCoverageSuffix` (private)
  - `WatcherStateLabel`, `WatcherDetailText`
- [x] Copy commands as public properties:
  - `BrowseWatchedDirectoryCommand`, `AddWatchedDirectoryFromPickerCommand`, `AddWatchedDirectoryCommand`
  - `RemoveWatchedDirectoryCommand`, `ScanWatchedDirectoryCommand`
  - `AddIgnoredPatternCommand`, `RemoveIgnoredPatternCommand`, `ScanNowCommand`
- [x] Add XML doc comments for all public properties and commands

### 3.3 Copy Methods
- [x] Copy private methods (lines 1151-1421):
  - `BrowseWatchedDirectory()`, `AddWatchedDirectoryFromPicker()`, `AddWatchedDirectory()`
  - `TryAddWatchedDirectory(string)` â€” **preserve parked defect** (returns `true` after failed apply)
  - `CreateWatchedDirectoryRow(string, int, ProjectDiscoveryMode)`
  - `RemoveWatchedDirectory(string)`, `ScanWatchedDirectory(string)`
  - `OnWatchedDirectoriesChanged(object?, NotifyCollectionChangedEventArgs)`
  - `SyncObservedWatchedDirectoryRows(NotifyCollectionChangedEventArgs)`
  - `OnWatchedDirectoryRowChanged(object?, PropertyChangedEventArgs)`
  - `OnScannerStatusChanged(object?, ProjectScanStatus)`, `ApplyScannerStatus(ProjectScanStatus?)`
  - `RefreshWatchedDirectoryCounts()`
  - `AddIgnoredPattern()`, `RemoveIgnoredPattern(string)`
  - `OnProjectTotalsChanged()` (lines 791-795)
- [x] Update `TryAddWatchedDirectory`:
  - Replace `this.WatchedDirectoryError = ...` with callback: `this.setValidationError(...)` or expose property
  - Replace `this.ValidationError = ...` (line 1180) with callback or expose property
- [x] Update `AddWatchedDirectoryFromPicker`:
  - Replace `this.ValidationError = null` (line 1171) with callback or expose property
- [x] Copy `ApplyScanningSettings()` method (lines 1313-1338) as public:
  - Replace `this.ValidationError = result.Error;` (line 1336) with callback or expose property
- [x] Add XML doc comments for public methods (`Load`, `CreateInput`, `ApplyScanningSettings`, `Dispose`)

### 3.4 Add Load and CreateInput Methods
- [x] Add `public void Load(SettingsDraft draft)`:
  - Copy logic from `SettingsViewModel.Load()` lines 1033-1060
  - Clear and repopulate `WatchedDirectories` and `IgnoredPatterns`
  - Seed `scanDepthText`, `selectedDiscoveryMode`, entry fields, counts
  - Call `ApplyScannerStatus(this.projectScanner.Status)` at the end
  - Raise property-changed notifications for all loaded properties
- [x] Add `public ScanningSettingsInput CreateInput()`:
  - Return new `ScanningSettingsInput` with current values (copy logic from `SettingsViewModel.Save()` lines 1585-1593)

### 3.5 Add Dispose Method
- [x] Add `public void Dispose()`:
  - Copy logic from `SettingsViewModel.Dispose()` lines 1441-1448
  - Unsubscribe `projectScanner.StatusChanged`
  - Unsubscribe and clear `observedWatchedDirectoryRows`
  - Guard with `if (this.disposed) return;` and set `this.disposed = true;`
- [x] Add XML doc comment

### 3.6 Replace Scanning Section in SettingsViewModel
- [x] Open `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`
- [x] Remove fields (lines 103, 107, 138-149): `dialogs`, `projectScanner`, `observedWatchedDirectoryRows`, scanning-related fields
- [x] Remove static fields (lines 80-96): `DiscoveryModeOptions`, `ScanDepthChoices`
- [x] Remove collections (lines 615, 665): `WatchedDirectories`, `IgnoredPatterns`
- [x] Remove properties (lines 620-882): all scanning properties
- [x] Remove commands (lines 206-213, 847-882): all scanning commands
- [x] Remove methods (lines 1151-1421): all scanning methods except `RefreshAgentSummary` (lines 1423-1428, keep it)
- [x] Add field: `private readonly WatchedDirectoryEditor scanning;`
- [x] Add property: `public WatchedDirectoryEditor Scanning => this.scanning;`
- [x] Update constructor:
  - **Keep** the `IProjectDialogService dialogs` (line 174) and `IProjectScanner projectScanner` (line 175) parameters; they are needed to construct the collaborator
  - Remove the `this.dialogs` and `this.projectScanner` fields/assignments (the editor owns them)
  - Add after `this.accent = ...`: `this.scanning = new WatchedDirectoryEditor(this.settingsApplication, projectScanner, dialogs, error => this.ValidationError = error);`
  - Remove scanner status subscription (lines 216-217)
  - Remove watched directories collection subscription (line 215)
- [x] Update `Load()` method:
  - Replace scanning seeding block (lines 1033-1060) with: `this.scanning.Load(draft);`
  - Remove scanning-related notifications (lines 1093-1109)
- [x] Update `Save()` method:
  - Replace scanning value collection (lines 1585-1593) with reading from `this.scanning.CreateInput()`
  - Integrate the returned `ScanningSettingsInput` into the `SettingsInput` constructor
- [x] Update `Dispose()` method:
  - Replace scanning disposal (lines 1441-1448) with: `this.scanning.Dispose();`
  - Remove scanner status unsubscription, row unsubscription, and clear

### 3.7 Update XAML Bindings for Scanning â€” SettingsWindow.xaml
- [x] Open `src/CLIHub/Views/SettingsWindow.xaml`
- [x] Replace `AddWatchedDirectoryFromPickerCommand` with `Scanning.AddWatchedDirectoryFromPickerCommand` at line 160
- [x] Replace `ScanNowCommand` with `Scanning.ScanNowCommand` at line 171
- [x] Replace `ProjectMixSummary` with `Scanning.ProjectMixSummary` at line 185
- [x] Replace `TotalProjectCount` with `Scanning.TotalProjectCount` at line 189
- [x] Replace `WatcherDetailText` with `Scanning.WatcherDetailText` at lines 196-197
- [x] Replace `WatcherStateLabel` with `Scanning.WatcherStateLabel` at line 200
- [x] Verify: `ProjectRegistry.ProjectMixSummary` at line 208 is unchanged (not scanning-related)

### 3.8 Update XAML Bindings for Scanning â€” SettingsProjectsPage.xaml
- [x] Open `src/CLIHub/Views/SettingsProjectsPage.xaml`
- [x] Replace `WatchedRootsSummary` with `Scanning.WatchedRootsSummary` at line 17
- [x] Replace `WatchedDirectories` with `Scanning.WatchedDirectories` at line 26
- [x] Replace `DataContext.ScanWatchedDirectoryCommand` with `DataContext.Scanning.ScanWatchedDirectoryCommand` at line 92
- [x] Replace `DataContext.RemoveWatchedDirectoryCommand` with `DataContext.Scanning.RemoveWatchedDirectoryCommand` at line 104
- [x] Replace `NewWatchedDirectoryText` with `Scanning.NewWatchedDirectoryText` at line 147
- [x] Replace `BrowseWatchedDirectoryCommand` with `Scanning.BrowseWatchedDirectoryCommand` at line 151
- [x] Replace `AddWatchedDirectoryCommand` with `Scanning.AddWatchedDirectoryCommand` at line 158
- [x] Replace `WatchedDirectoryError` with `Scanning.WatchedDirectoryError` at line 166
- [x] Replace `HasWatchedDirectoryError` with `Scanning.HasWatchedDirectoryError` at line 167
- [x] Replace `ScanDepthOptions` with `Scanning.ScanDepthOptions` at line 176
- [x] Replace `SelectedScanDepth` with `Scanning.SelectedScanDepth` at line 177
- [x] Replace `DiscoveryModes` with `Scanning.DiscoveryModes` at line 193
- [x] Replace `SelectedDiscoveryMode` with `Scanning.SelectedDiscoveryMode` at line 194
- [x] Replace `IgnoredPatterns` with `Scanning.IgnoredPatterns` at line 212
- [x] Replace `DataContext.RemoveIgnoredPatternCommand` with `DataContext.Scanning.RemoveIgnoredPatternCommand` at line 233
- [x] Replace `NewIgnoredPatternText` with `Scanning.NewIgnoredPatternText` at line 266
- [x] Replace `AddIgnoredPatternCommand` with `Scanning.AddIgnoredPatternCommand` at line 269
- [x] Verify: row-scoped bindings (`Path`, `DepthOptions`, `SelectedDepth`, `DiscoveryOptions`, `SelectedDiscovery`, `DiscoveredSummary`) are unchanged

### 3.9 Build and Test Stage 3
- [x] Run `dotnet build CLIHub.sln` â€” must succeed
- [x] Run `dotnet test CLIHub.sln` â€” must succeed
- [x] Verify: no compilation errors, no test failures

---

## Stage 4: Final Verification and Cleanup

### 4.1 Update Test Files
- [x] Identify tests touching the moved members:
  - `tests/CLIHub.Tests/ViewModels/SettingsViewModelApplicationTests.cs`
  - `tests/CLIHub.Tests/ViewModels/SettingsNavigationTests.cs`
  - `tests/CLIHub.Tests/ViewModels/SettingsOperationLifetimeTests.cs`
  - `tests/CLIHub.Tests/Settings/SettingsApplicationServiceTests.cs`
- [x] Update test assertions to reflect through the collaborators (e.g., `vm.Accent.AccentText`, `vm.Scanning.WatchedDirectories`)
- [x] If tests use `InternalsVisibleTo`, verify the collaborators are accessible from `CLIHub.Tests`
- [x] Fix any test failures

### 4.2 Run Full Build and Test Suite
- [x] Run `dotnet build CLIHub.sln` â€” must succeed
- [x] Run `dotnet test CLIHub.sln` â€” all tests must pass
- [x] Verify: no warnings related to the moved code

### 4.3 Run OpenSpec Validation
- [x] Run `openspec validate --all`
- [x] Verify: validation passes (no specs changed, `skip_specs: true`)
- [x] Quote exact validation totals in the report

### 4.4 Refresh the Knowledge Graph
- [x] Run `graphify update .` (graphify-out is gitignored)

### 4.5 Manual Smoke Test (user-side)
- [x] Manual smoke (user-confirmed 2026-10-30): the user runs the app and verifies:
  - Settings → Appearance: preset and custom accent selection, live accent label.
  - Settings → Updates: the status card renders (badge, glyph, tone, detail).
  - Settings → Project Scanning: watched roots, ignored patterns, scan depth, discovery mode, watcher status.
  - Save settings and reopen the window: state is preserved.

### 4.6 Final Report
- [x] Count lines in `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` (expect noticeably below 1,610)
- [x] Report to the orchestrator (do **not** commit):
  - Change ID
  - Files created and modified
  - Line count reduction
  - Constructor parameter count (expected to stay 13; three dependencies now construct collaborators)
  - OpenSpec validation result
  - Any deviations from the plan
  - Open questions resolved

---

## Notes for Implementer

- **Do NOT commit** after any stage. The user must explicitly approve a commit.
- **Preserve the parked defect**: `TryAddWatchedDirectory` returns `true` after a failed apply and clears `ValidationError`. Do NOT fix it.
- **ValidationError reporting**: choose callback (`Action<string?>`) or property (`string? LastError`) approach in Stage 3.1. The design recommends callback; document the choice.
- **Static field migration**: move `CustomAccentChoice`, `AccentChoices`, `DiscoveryModeOptions`, `ScanDepthChoices` to their respective collaborators.
- **Notification chains**: ensure `AccentSelection` and `WatchedDirectoryEditor` inherit `ObservableObject` and raise `PropertyChanged` for all public properties.
- **Disposal**: `SettingsViewModel.Dispose()` must call `Scanning.Dispose()` before its own cleanup.
- **Test updates**: verify tests compile and pass after each stage. If a test reflects on private fields, update it to use public properties or `InternalsVisibleTo`.
