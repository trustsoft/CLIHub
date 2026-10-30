# Proposal: Extract Three Settings Sections from SettingsViewModel

## Why

`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` (1,610 lines, 95+ public members, 13-parameter
constructor after slice 1) remains complex. The view model mixes three distinct responsibilities that can be
independently tested, understood, and evolved:

1. **Update status card** — five update-status properties that track check outcomes.
2. **Accent selection** — accent text normalization, preset detection, custom-accent memory, and computed labels.
3. **Watched directory editor** — the full scanning/ignored/watcher section: rows, patterns, validation,
   immediate-apply logic, counts, status subscriptions, and disposal.

This change is **slice 2 of the `settings-viewmodel-decomposition` portfolio item**. It extracts these three
sections into focused collaborators and delegates the XAML bindings to them. **Behavior is preserved**: this is
a pure refactor with no spec changes, no feature changes, and no unrelated API changes.

## What Changes

Move three sections out of `SettingsViewModel` into separate collaborators, all in
`src/CLIHub/ViewModels/Settings/` under the `CLIHub.ViewModels` namespace:

1. **`SettingsUpdateCard` record** (new file) — a sealed record with `Title`, `Detail`, `Badge`,
   `Tone` parameters, and a `Glyph` computed property. Replaces the five update-status backing fields
   and properties. `SetUpdateStatus` builds a new instance, `ResetUpdateStatus` restores a safe default, and
   `ApplyUpdateResult` builds from an `UpdateCheckResult`. Expose as `public SettingsUpdateCard UpdateCard { get; private set; }`.
   
2. **`AccentSelection` class** (new file) — owns `AccentText`, `SelectedAccent`, `AccentOptions`,
   `ActiveAccentLabel`, and the custom-accent memory logic. Constructor takes `AccentSelectionMemory` and
   initializes the catalog. Expose `Load(string accentColor)` to seed from a draft and a public read-only
   property (e.g., `AccentText`) that the VM's `Save` can read. Expose as `public AccentSelection Accent { get; }`.
   
3. **`WatchedDirectoryEditor` class** (new file) — owns the watched-directory rows, ignored patterns, scan
   depth/discovery mode, watcher status, project/scan counts, and all scanning-section commands. Constructor
   takes `SettingsApplicationService`, `IProjectScanner`, `IProjectDialogService`, and a callback or
   property-reader to report validation errors to the VM. Expose `Load(SettingsDraft)` to seed from a draft,
   `CreateInput()` returning `ScanningSettingsInput`, `ApplyScanningSettings()`, and `Dispose()`. Expose as
   `public WatchedDirectoryEditor Scanning { get; }`.

Update `SettingsViewModel`:

- Constructor drops `IProjectScanner`, `IProjectDialogService`, and `AccentSelectionMemory` parameters (the
  VM no longer uses them directly; verify that `SettingsApplicationService` stays).
- `Load` delegates accent seeding to `Accent.Load(draft.AccentColor)` and scanning seeding to
  `Scanning.Load(draft)`.
- `Save` reads `Accent.AccentText` and calls `Scanning.CreateInput()` to collect scanning values.
- `Dispose` calls `Scanning.Dispose()` and stops subscribing to scanner/row events itself.
- `UpdateCard`, `Accent`, and `Scanning` properties replace the moved members.

Update XAML bindings to reflect the new paths:

- **`SettingsWindow.xaml`** (Update status header, lines 44-126): `UpdateCard.Tone`, `UpdateCard.Detail`,
  `UpdateCard.Glyph`, `UpdateCard.Badge`.
- **`SettingsWindow.xaml`** (Project Scanning header, lines 160-200): `Scanning.AddWatchedDirectoryFromPickerCommand`,
  `Scanning.ScanNowCommand`, `Scanning.ProjectMixSummary`, `Scanning.TotalProjectCount`,
  `Scanning.WatcherDetailText`, `Scanning.WatcherStateLabel`.
- **`SettingsAppearancePage.xaml`** (lines 107-203): `Accent.ActiveAccentLabel`, `Accent.AccentOptions`,
  `Accent.SelectedAccent`, `Accent.SelectedAccent.IsCustom`, `Accent.AccentText`.
- **`SettingsProjectsPage.xaml`** (lines 17-269): `Scanning.WatchedRootsSummary`, `Scanning.WatchedDirectories`,
  `Scanning.NewWatchedDirectoryText`, `Scanning.BrowseWatchedDirectoryCommand`, `Scanning.AddWatchedDirectoryCommand`,
  `Scanning.WatchedDirectoryError`, `Scanning.HasWatchedDirectoryError`, `Scanning.ScanDepthOptions`,
  `Scanning.SelectedScanDepth`, `Scanning.DiscoveryModes`, `Scanning.SelectedDiscoveryMode`,
  `Scanning.IgnoredPatterns`, `Scanning.NewIgnoredPatternText`, `Scanning.AddIgnoredPatternCommand`,
  and relative-source `DataContext.ScanWatchedDirectoryCommand`/`DataContext.RemoveWatchedDirectoryCommand`/`DataContext.RemoveIgnoredPatternCommand`
  to `DataContext.Scanning.*`.

`ValidationError` stays on the VM; the scanning editor reports errors to it (either via callback or by
exposing a property the VM reads after apply). `UpdateMessage` stays on the VM. `CheckForUpdatesAsync` and
`RunUpdateCheckAsync` stay on the VM.

## Impact

- **Testability**: each collaborator is independently testable with narrower dependencies.
- **Clarity**: the view model becomes a thinner coordinator; the sections' logic is localized.
- **Maintainability**: smaller, focused files make both agent and human edits safer. The VM drops from
  ~1,610 lines to ~1,300 lines.

No runtime behavior changes, no spec changes, no feature changes.

## Non-Goals

- Extracting the scanning/save pipeline unification (`SettingsInput`/`SettingsDraft` convergence).
- Fixing the parked Settings defects (e.g., `TryAddWatchedDirectory` swallowing apply failures and clearing
  `ValidationError`).
- Removing dead placeholder pages or changing the Settings DI lifetime.
- Any behavior changes, new specs, or XAML restyling.

## Dependencies

Depends on **slice 1** (`settings-viewmodel-file-split`, archived as
`2026-10-30-settings-viewmodel-file-split`), which split the file and introduced the `AddTransient<SettingsViewModel>()`
registration.
