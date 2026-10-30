# Design: Extract Three Settings Sections from SettingsViewModel

## Verified Current State

**`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`** — 1,610 lines (verified with `Get-Content | Count`):

### Update Status Section
- **Fields** (lines 122-127):
  - `updateMessage` (122)
  - `updateStatusTitle` (123)
  - `updateStatusDetail` (124)
  - `updateStatusBadge` (125)
  - `updateStatusGlyph` (126)
  - `updateStatusBadgeTone` (127)
- **Properties** (lines 887-936):
  - `UpdateMessage` (887-891)
  - `UpdateStatusTitle` (896-900)
  - `UpdateStatusDetail` (905-909)
  - `UpdateStatusBadge` (914-918)
  - `UpdateStatusBadgeTone` (923-927)
  - `UpdateStatusGlyph` (932-936)
- **Methods**:
  - `ResetUpdateStatus()` (1485-1504)
  - `ApplyUpdateResult(UpdateCheckResult)` (1506-1549)
  - `SetUpdateStatus(string, string, string, SettingBadgeTone)` (1551-1563)
  - `CheckForUpdatesAsync(CancellationToken)` (1457-1483) — **stays on the VM**
  - `RunUpdateCheckAsync(CancellationToken)` (972-982) — **stays on the VM**

### Accent Section
- **Fields** (lines 136-137):
  - `accentText` (136)
  - `customAccentSelected` (137)
- **Dependency**: `accentMemory` field (104), constructor parameter (178, 192)
- **Static data**: `CustomAccentChoice` (72-73), `AccentChoices` (75-78)
- **Properties**:
  - `AccentOptions` (538) — returns `AccentChoices`
  - `AccentText` (543-561)
  - `SelectedAccent` (566-598)
  - `ActiveAccentLabel` (603-610)
- **Methods**:
  - `FindAccentPreset(string)` (1139-1149)
- **Load seeding** (lines 1024-1028):
  ```csharp
  this.accentText = AccentPalette.Normalize(draft.AccentColor);
  this.customAccentSelected = this.FindAccentPreset(this.accentText) is null;
  if (this.customAccentSelected && this.accentMemory.LastCustomAccent is null)
  {
      this.accentMemory.LastCustomAccent = this.accentText;
  }
  ```
- **Save reading** (line 1584): `this.AccentText`

### Scanning Section
- **Fields** (lines 103, 107, 138-149):
  - `dialogs` (102) — constructor parameter
  - `projectScanner` (103) — constructor parameter, used at 189, 213, 216-217, 259, 1401, 1344, 1441
  - `observedWatchedDirectoryRows` (107)
  - `scanDepthText` (138)
  - `selectedDiscoveryMode` (139)
  - `newIgnoredPatternText` (140)
  - `newWatchedDirectoryText` (141)
  - `watchedDirectoryError` (142)
  - `discoveredProjectCount` (143)
  - `manualProjectCount` (144)
  - `scannedDirectoryCount` (145)
  - `watcherStateLabel` (148)
  - `watcherDetailText` (149)
- **Collections**:
  - `WatchedDirectories` (615) — `ObservableCollection<WatchedDirectoryRow>`
  - `IgnoredPatterns` (665) — `ObservableCollection<string>`
- **Properties** (lines 615-882):
  - `WatchedDirectories` (615)
  - `NewWatchedDirectoryText` (620-630)
  - `WatchedDirectoryError` (635-645)
  - `HasWatchedDirectoryError` (650)
  - `WatchedRootsSummary` (655-660)
  - `IgnoredPatterns` (665)
  - `NewIgnoredPatternText` (670-674)
  - `ScanDepthText` (679-689)
  - `ScanDepthOptions` (694) — returns static `ScanDepthChoices`
  - `SelectedScanDepth` (699-712)
  - `DiscoveryModes` (717) — returns static `DiscoveryModeOptions`
  - `SelectedDiscoveryMode` (722-732)
  - `DiscoveredProjectCount` (737-747)
  - `ManualProjectCount` (752-762)
  - `TotalProjectCount` (767) — computed: `DiscoveredProjectCount + ManualProjectCount`
  - `ProjectMixSummary` (772-773)
  - `ScannedDirectoryCount` (778-782)
  - `ScannedCoverageSuffix` (784-789) — private helper
  - `WatcherStateLabel` (829-833)
  - `WatcherDetailText` (838-842)
- **Commands** (lines 206-213, 847-882):
  - `BrowseWatchedDirectoryCommand` (206, 847)
  - `AddWatchedDirectoryFromPickerCommand` (207, 852)
  - `AddWatchedDirectoryCommand` (208, 857)
  - `RemoveWatchedDirectoryCommand` (209, 862)
  - `ScanWatchedDirectoryCommand` (210, 867)
  - `AddIgnoredPatternCommand` (211, 872)
  - `RemoveIgnoredPatternCommand` (212, 877)
  - `ScanNowCommand` (213, 882)
- **Methods** (lines 1151-1422):
  - `BrowseWatchedDirectory()` (1151-1158)
  - `AddWatchedDirectoryFromPicker()` (1160-1173)
  - `AddWatchedDirectory()` (1175-1182)
  - `TryAddWatchedDirectory(string)` (1184-1229) — **preserves parked defect**: returns `true` after failed apply
  - `CreateWatchedDirectoryRow(string, int, ProjectDiscoveryMode)` (1231-1238)
  - `RemoveWatchedDirectory(string)` (1240-1257)
  - `ScanWatchedDirectory(string)` (1259)
  - `OnWatchedDirectoriesChanged(object?, NotifyCollectionChangedEventArgs)` (1261-1266)
  - `SyncObservedWatchedDirectoryRows(NotifyCollectionChangedEventArgs)` (1268-1298)
  - `OnWatchedDirectoryRowChanged(object?, PropertyChangedEventArgs)` (1300-1306)
  - `ApplyScanningSettings()` (1313-1338) — applies immediately
  - `RefreshWatchedDirectoryCounts()` (1340-1346)
  - `AddIgnoredPattern()` (1348-1367)
  - `RemoveIgnoredPattern(string)` (1369-1383)
  - `OnScannerStatusChanged(object?, ProjectScanStatus)` (1385-1395)
  - `ApplyScannerStatus(ProjectScanStatus?)` (1397-1421)
  - `RefreshAgentSummary()` (1423-1428) — **not moved** (agents section)
  - `OnProjectTotalsChanged()` (791-795) — raises notifications
- **Dispose** (lines 1433-1451):
  - Unsubscribes `projectScanner.StatusChanged` (1441)
  - Unsubscribes and clears `observedWatchedDirectoryRows` (1443-1448)
- **Load seeding** (lines 1033-1060):
  - Clears and repopulates `WatchedDirectories` (1033-1046)
  - Clears and repopulates `IgnoredPatterns` (1048-1052)
  - Seeds `scanDepthText`, `selectedDiscoveryMode`, entry fields, counts (1054-1060)
- **Save reading** (lines 1585-1593):
  - `this.WatchedDirectories.Select(row => row.Path).ToList()`
  - `this.ScanDepthText`
  - `this.IgnoredPatterns.ToList()`
  - `this.SelectedDiscoveryMode.Mode`
  - `this.WatchedDirectories.ToDictionary(...)` for per-directory settings

### Constructor (lines 168-218)
Currently takes 13 parameters (after slice 1):
- `SettingsApplicationService settingsApplication` (168, 182) — **stays**
- `IUpdateVersionProvider versionProvider` (169, 183) — **stays**
- `IUpdateWorkflow updateWorkflow` (170, 184) — **stays**
- `IReleaseNotesLauncher releaseNotesLauncher` (171, 185) — **stays**
- `IApplicationOperationLifetime operationLifetime` (172, 186) — **stays**
- `IExternalLauncher externalLauncher` (173, 187) — **stays**
- `IProjectDialogService dialogs` (174, 188) — **stays; passed to `WatchedDirectoryEditor`**
- `IProjectScanner projectScanner` (175, 189) — **stays; passed to `WatchedDirectoryEditor`**
- `IAgentManagementSection agentsState` (176, 190) — **stays**
- `IProjectRegistrySection projectRegistry` (177, 191) — **stays**
- `AccentSelectionMemory accentMemory` (178, 192) — **stays; passed to `AccentSelection`**
- `IRuntimeExecutableLocator runtimeExecutables` (179, 193) — **stays**
- `ILogger<SettingsViewModel> logger` (180, 194) — **stays**

After extraction: 13 parameters remain. The three dependencies above are still needed to construct the two
collaborators (the editor's validation callback is bound to this view model, so DI-injecting the
collaborators would add complexity without reducing the constructor). The constructor body shrinks because
the field assignments and event subscriptions move into the collaborators.

## XAML Binding Map

### `SettingsWindow.xaml` — Update Status Card (lines 44-126)
Before | After | Line
---|---|---
`UpdateStatusBadgeTone` | `UpdateCard.Tone` | 44, 49, 63, 67, 80, 84
`UpdateStatusDetail` | `UpdateCard.Detail` | 121
`UpdateStatusGlyph` | `UpdateCard.Glyph` | 123
`UpdateStatusBadge` | `UpdateCard.Badge` | 126

**Not bound**: `UpdateStatusTitle` and `UpdateMessage` (used in code-behind or not bound).

### `SettingsWindow.xaml` — Project Scanning Header (lines 160-200)
Before | After | Line
---|---|---
`AddWatchedDirectoryFromPickerCommand` | `Scanning.AddWatchedDirectoryFromPickerCommand` | 160
`ScanNowCommand` | `Scanning.ScanNowCommand` | 171
`ProjectMixSummary` | `Scanning.ProjectMixSummary` | 185
`TotalProjectCount` | `Scanning.TotalProjectCount` | 189
`WatcherDetailText` | `Scanning.WatcherDetailText` | 196-197
`WatcherStateLabel` | `Scanning.WatcherStateLabel` | 200

**Unchanged**: `ProjectRegistry.ProjectMixSummary` (line 208) — not scanning-related.

### `SettingsAppearancePage.xaml` — Accent (lines 107-203)
Before | After | Line
---|---|---
`ActiveAccentLabel` | `Accent.ActiveAccentLabel` | 107
`AccentOptions` | `Accent.AccentOptions` | 113
`SelectedAccent` (Mode=TwoWay) | `Accent.SelectedAccent` | 114
`SelectedAccent.IsCustom` | `Accent.SelectedAccent.IsCustom` | 190
`AccentText` (UpdateSourceTrigger=PropertyChanged) | `Accent.AccentText` | 203

### `SettingsProjectsPage.xaml` — Scanning Section (lines 17-269)
Before | After | Line
---|---|---
`WatchedRootsSummary` | `Scanning.WatchedRootsSummary` | 17
`WatchedDirectories` | `Scanning.WatchedDirectories` | 26
`DataContext.ScanWatchedDirectoryCommand` (RelativeSource) | `DataContext.Scanning.ScanWatchedDirectoryCommand` | 92
`DataContext.RemoveWatchedDirectoryCommand` (RelativeSource) | `DataContext.Scanning.RemoveWatchedDirectoryCommand` | 104
`NewWatchedDirectoryText` | `Scanning.NewWatchedDirectoryText` | 147
`BrowseWatchedDirectoryCommand` | `Scanning.BrowseWatchedDirectoryCommand` | 151
`AddWatchedDirectoryCommand` | `Scanning.AddWatchedDirectoryCommand` | 158
`WatchedDirectoryError` | `Scanning.WatchedDirectoryError` | 166
`HasWatchedDirectoryError` | `Scanning.HasWatchedDirectoryError` | 167
`ScanDepthOptions` | `Scanning.ScanDepthOptions` | 176
`SelectedScanDepth` (Mode=TwoWay) | `Scanning.SelectedScanDepth` | 177
`DiscoveryModes` | `Scanning.DiscoveryModes` | 193
`SelectedDiscoveryMode` (Mode=TwoWay) | `Scanning.SelectedDiscoveryMode` | 194
`IgnoredPatterns` | `Scanning.IgnoredPatterns` | 212
`DataContext.RemoveIgnoredPatternCommand` (RelativeSource) | `DataContext.Scanning.RemoveIgnoredPatternCommand` | 233
`NewIgnoredPatternText` | `Scanning.NewIgnoredPatternText` | 266
`AddIgnoredPatternCommand` | `Scanning.AddIgnoredPatternCommand` | 269

**Row-scoped bindings** (stay on `WatchedDirectoryRow`): `Path`, `DepthOptions`, `SelectedDepth`, `DiscoveryOptions`, `SelectedDiscovery`, `DiscoveredSummary`.

## Collaborator APIs

### `SettingsUpdateCard` Record

```csharp
namespace CLIHub.ViewModels;

/// <summary>
///   Update status card shown in the Settings Updates page header.
/// </summary>
/// <param name="Title"> Status heading. </param>
/// <param name="Detail"> Version and last-check detail. </param>
/// <param name="Badge"> Badge text; empty hides the badge. </param>
/// <param name="Tone"> Badge visual tone. </param>
public sealed record SettingsUpdateCard(
    string Title,
    string Detail,
    string Badge,
    SettingBadgeTone Tone)
{
    /// <summary>
    ///   The glyph shown in the card's leading icon block, derived from the tone.
    /// </summary>
    public string Glyph => this.Tone switch
    {
        SettingBadgeTone.Success => IconGlyphs.Checkmark,
        SettingBadgeTone.Accent => IconGlyphs.Refresh,
        _ => IconGlyphs.Info
    };
}
```

**File**: `src/CLIHub/ViewModels/Settings/SettingsUpdateCard.cs`

### `AccentSelection` Class

```csharp
namespace CLIHub.ViewModels;

/// <summary>
///   Accent color selection section: presets, custom entry, and memory.
/// </summary>
public sealed class AccentSelection : ObservableObject
{
    private readonly AccentSelectionMemory accentMemory;
    private string accentText = AccentPalette.DefaultHex;
    private bool customAccentSelected;

    /// <summary>
    ///   Creates the accent selection section.
    /// </summary>
    /// <param name="accentMemory"> Process-wide memory of the last custom accent. </param>
    public AccentSelection(AccentSelectionMemory accentMemory)
    {
        this.accentMemory = accentMemory ?? throw new ArgumentNullException(nameof(accentMemory));
    }

    /// <summary>
    ///   The accent color choices offered by the selector: the presets and the custom entry.
    /// </summary>
    public IReadOnlyList<AccentChoice> AccentOptions => /* static catalog */;

    /// <summary>
    ///   The accent color text as <c> #RRGGBB </c>.
    /// </summary>
    public string AccentText { get; set; } // setter normalizes, detects custom, writes memory, raises notifications

    /// <summary>
    ///   The selected accent choice: the matching preset, or the custom entry for any other value.
    /// </summary>
    public AccentChoice SelectedAccent { get; set; } // setter applies preset or custom logic

    /// <summary>
    ///   The accent shown next to the selector title, e.g. "Active: Blue".
    /// </summary>
    public string ActiveAccentLabel { get; } // computed from accentText

    /// <summary>
    ///   Loads the accent from a draft.
    /// </summary>
    public void Load(string accentColor);

    private AccentChoice? FindAccentPreset(string value);
}
```

**File**: `src/CLIHub/ViewModels/Settings/AccentSelection.cs`

### `WatchedDirectoryEditor` Class

```csharp
namespace CLIHub.ViewModels;

/// <summary>
///   Watched directory and scanning settings editor: immediate-apply settings,
///   project/watcher status, and disposal.
/// </summary>
public sealed class WatchedDirectoryEditor : ObservableObject, IDisposable
{
    private readonly SettingsApplicationService settingsApplication;
    private readonly IProjectScanner projectScanner;
    private readonly IProjectDialogService dialogs;
    private readonly Action<string?> setValidationError; // or: the VM reads a property after apply
    private readonly List<WatchedDirectoryRow> observedWatchedDirectoryRows = new();
    /* ... other fields ... */

    /// <summary>
    ///   Creates the scanning editor.
    /// </summary>
    public WatchedDirectoryEditor(
        SettingsApplicationService settingsApplication,
        IProjectScanner projectScanner,
        IProjectDialogService dialogs,
        Action<string?> setValidationError /* or expose a property */);

    // Collections
    public ObservableCollection<WatchedDirectoryRow> WatchedDirectories { get; }
    public ObservableCollection<string> IgnoredPatterns { get; }

    // Properties (string, int, etc.)
    public string NewWatchedDirectoryText { get; set; }
    public string? WatchedDirectoryError { get; }
    public bool HasWatchedDirectoryError { get; }
    public string WatchedRootsSummary { get; }
    public string NewIgnoredPatternText { get; set; }
    public string ScanDepthText { get; set; }
    public IReadOnlyList<ScanDepthOption> ScanDepthOptions { get; }
    public ScanDepthOption SelectedScanDepth { get; set; }
    public IReadOnlyList<DiscoveryModeOption> DiscoveryModes { get; }
    public DiscoveryModeOption SelectedDiscoveryMode { get; set; }
    public int DiscoveredProjectCount { get; }
    public int ManualProjectCount { get; }
    public int TotalProjectCount { get; }
    public string ProjectMixSummary { get; }
    public int ScannedDirectoryCount { get; }
    public string WatcherStateLabel { get; }
    public string WatcherDetailText { get; }

    // Commands
    public RelayCommand BrowseWatchedDirectoryCommand { get; }
    public RelayCommand AddWatchedDirectoryFromPickerCommand { get; }
    public RelayCommand AddWatchedDirectoryCommand { get; }
    public RelayCommand<string> RemoveWatchedDirectoryCommand { get; }
    public RelayCommand<string> ScanWatchedDirectoryCommand { get; }
    public RelayCommand AddIgnoredPatternCommand { get; }
    public RelayCommand<string> RemoveIgnoredPatternCommand { get; }
    public RelayCommand ScanNowCommand { get; }

    /// <summary>
    ///   Loads the scanning settings from a draft.
    /// </summary>
    public void Load(SettingsDraft draft);

    /// <summary>
    ///   Creates the input for persistence and scanner reconfiguration.
    /// </summary>
    public ScanningSettingsInput CreateInput();

    /// <summary>
    ///   Applies the scanning settings currently shown and rescans in the background.
    /// </summary>
    /// <returns> True when the settings were persisted and the scanner was reconfigured. </returns>
    public bool ApplyScanningSettings();

    /// <summary>
    ///   Detaches scanner status subscriptions.
    /// </summary>
    public void Dispose();

    private bool TryAddWatchedDirectory(string value); // preserves parked defect
    private WatchedDirectoryRow CreateWatchedDirectoryRow(string path, int scanDepth, ProjectDiscoveryMode discoveryMode);
    private void OnWatchedDirectoriesChanged(object? sender, NotifyCollectionChangedEventArgs e);
    private void SyncObservedWatchedDirectoryRows(NotifyCollectionChangedEventArgs e);
    private void OnWatchedDirectoryRowChanged(object? sender, PropertyChangedEventArgs e);
    private void OnScannerStatusChanged(object? sender, ProjectScanStatus status);
    private void ApplyScannerStatus(ProjectScanStatus? status);
    private void RefreshWatchedDirectoryCounts();
    private void OnProjectTotalsChanged();
}
```

**File**: `src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs`

## Ownership Table

Member | Before | After
---|---|---
Update status fields (6) | `SettingsViewModel` | Replaced by `SettingsUpdateCard` record instance
`UpdateMessage` | `SettingsViewModel` | `SettingsViewModel` (not moved)
`CheckForUpdatesAsync` | `SettingsViewModel` | `SettingsViewModel` (not moved)
`RunUpdateCheckAsync` | `SettingsViewModel` | `SettingsViewModel` (not moved)
Accent fields (2) | `SettingsViewModel` | `AccentSelection`
`AccentOptions`, `AccentText`, `SelectedAccent`, `ActiveAccentLabel` | `SettingsViewModel` | `AccentSelection`
`AccentSelectionMemory` dependency | `SettingsViewModel` constructor | used to construct `AccentSelection`
Scanning fields (13) | `SettingsViewModel` | `WatchedDirectoryEditor`
`WatchedDirectories`, `IgnoredPatterns` collections | `SettingsViewModel` | `WatchedDirectoryEditor`
Scanning properties (20+) | `SettingsViewModel` | `WatchedDirectoryEditor`
Scanning commands (8) | `SettingsViewModel` | `WatchedDirectoryEditor`
Scanning methods (15) | `SettingsViewModel` | `WatchedDirectoryEditor`
`IProjectScanner` dependency | `SettingsViewModel` constructor | used to construct `WatchedDirectoryEditor`
`IProjectDialogService` dependency | `SettingsViewModel` constructor | `WatchedDirectoryEditor` constructor
`ValidationError` | `SettingsViewModel` | `SettingsViewModel` (scanning editor reports to VM)

## Decisions and Rationale

### 1. Update Card as a Record
- **Decision**: use a sealed record with computed `Glyph` property.
- **Rationale**: the card is pure state with no mutable workflows. A record is concise and testable. The glyph
  derivation (lines 1557-1562) belongs with the data.
- **Alternative rejected**: five independent properties on the VM — more verbose, harder to reason about as a unit.

### 2. AccentSelection Owns the Catalog
- **Decision**: `AccentSelection` exposes `AccentOptions` from the static catalog.
- **Rationale**: the catalog (`CustomAccentChoice`, `AccentChoices`) is tightly coupled to the selection logic.
  Keeping it in the collaborator reduces the VM's static surface.
- **Alternative rejected**: leave the catalog on the VM and pass it to `AccentSelection` — splits the data from
  its use.

### 3. WatchedDirectoryEditor Exposes All Scanning State
- **Decision**: all scanning properties, collections, commands, and counts move to the editor.
- **Rationale**: the scanning section is a cohesive subsystem with immediate-apply logic and status subscriptions.
  Splitting it across two classes would introduce tighter coupling and harder testing.
- **Alternative rejected**: keep counts on the VM, delegate commands only — the counts derive from scanner status,
  which the editor already owns.

### 4. ValidationError Stays on the VM
- **Decision**: `ValidationError` remains a VM property; the scanning editor reports errors via callback or
  exposes a property the VM reads after apply.
- **Rationale**: `ValidationError` is shared across multiple sections (hotkey, log fields, scanning). Moving it
  would require a more invasive refactor.
- **Implementation option A**: callback `Action<string?>` passed to `WatchedDirectoryEditor`.
- **Implementation option B**: expose `string? LastError { get; }` on the editor, VM reads it after `ApplyScanningSettings()`.

### 5. Parked Defect Preservation
- **Decision**: `TryAddWatchedDirectory` returns `true` after a failed apply (lines 1218-1228) and clears
  `ValidationError` (line 1170); keep this behavior.
- **Rationale**: fixing the defect is out of scope. The implementer must not silently correct it; this is a
  behavior-preserving refactor.

### 6. Constructor Parameter Reduction
- **Decision**: drop `IProjectScanner`, `IProjectDialogService`, and `AccentSelectionMemory` from the VM constructor.
- **Rationale**: the VM no longer uses them directly; the collaborators take ownership. The VM constructor drops
  from 13 to 10 parameters.

## Risks

1. **XAML binding breakage**: 30+ binding paths change. The implementer must verify every binding compiles and
   displays correctly.
2. **Notification chains**: property change notifications must propagate through the collaborators to the XAML.
   `AccentSelection` and `WatchedDirectoryEditor` must inherit `ObservableObject` and raise `PropertyChanged`.
3. **Disposal order**: `SettingsViewModel.Dispose()` must call `Scanning.Dispose()` before its own cleanup.
4. **Test coupling**: tests reflecting on `SettingsViewModel` fields/properties must be updated to reflect through
   the collaborators or read their public APIs.

## Alternatives Considered

### 1. Extract Only One Section
- **Rejected**: the goal is to reduce the VM file size and complexity. Extracting only one section leaves the VM
  still large and the other sections still tangled.

### 2. Keep the Catalogs on the VM
- **Rejected**: the static data (`AccentChoices`, `ScanDepthOptions`, `DiscoveryModeOptions`) are tightly coupled
  to their respective sections. Moving them with the collaborators reduces the VM's static surface and makes the
  collaborators independently testable.

### 3. Move ValidationError to a Shared Coordinator
- **Rejected**: `ValidationError` is used by multiple sections (hotkey, logging, scanning). Extracting it into
  a coordinator is a larger refactor that would touch more of the codebase. The scanning editor can report errors
  to the VM without owning the property.

## Open Questions

1. **ValidationError reporting mechanism**: should the implementer use a callback (`Action<string?>`) or expose
   a property (`string? LastError`) on `WatchedDirectoryEditor` that the VM reads after apply?
   - **Recommendation**: callback is simpler and matches the immediate-apply pattern. The implementer decides.

2. **UpdateMessage binding**: is `UpdateMessage` bound in XAML, or only set from code?
   - **Verified**: no XAML binding found for `UpdateMessage` in grep results. It stays on the VM and is set by
     `CheckForUpdatesAsync`/`RunUpdateCheckAsync`.

3. **Test coverage**: which tests touch the moved members, and do they need to reflect through the collaborators
   or access them via `InternalsVisibleTo`?
   - **Identified**: `SettingsViewModelApplicationTests.cs`, `SettingsNavigationTests.cs`,
     `SettingsOperationLifetimeTests.cs`, `SettingsApplicationServiceTests.cs` (exact tests TBD by implementer).

## Deviations from Brief

None. The brief's line numbers and structure match the actual file.
