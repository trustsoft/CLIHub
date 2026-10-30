# Proposal: Split SettingsViewModel File

## Why

`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` (1,925 lines, 101 public members, 13-parameter
constructor) is a large, monolithic file mixing secondary types, static navigation data, the view model
itself, and a hand-written DI factory. The file size and the repeated "scrolling to the class definition"
tax make both human and agent navigation slower than necessary.

This change is **slice 1 of the `settings-viewmodel-decomposition` portfolio item**. It reduces the file
size and complexity through a pure mechanical refactor: moving secondary types into their own files,
extracting the static page catalog, and replacing the hand-written DI factory with a conventional
transient registration. **No behavior changes, no XAML changes, no public API changes.**

## What Changes

Split the file into four focused files, all in `src/CLIHub/ViewModels/Settings/` under the
`CLIHub.ViewModels` namespace:

1. **`SettingsOptions.cs`** — the eight option records (`RuntimeOption`, `PathDisplayOption`,
   `ScanDepthOption`, `LogLevelOption`, `PopupPositionOption`, `AppThemeOption`, `AccentChoice`,
   `DiscoveryModeOption`) in their current order.
2. **`WatchedDirectoryRow.cs`** — the `WatchedDirectoryRow` class.
3. **`SettingsNavigation.cs`** — `SettingsPage` and `SettingsGroup` records, plus a new
   `SettingsPageCatalog` static class exposing `AllPages` and `PageGroups` (names chosen for clarity;
   implementation confirms them).
4. **`SettingsViewModel.cs`** (stays) — only the view model; the `Pages`, `PageGroups`, and
   `SelectedPage` properties, and the `Load()` method, delegate to the catalog.

Replace the hand-written DI factory at `src/CLIHub/Composition/ServiceRegistration.cs:173-186` with:

```csharp
services.AddTransient<SettingsViewModel>();
```

Update the XML doc above it to stop counting dependencies.

Update two coupled tests:

- `tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs:138-151` reflects on
  `typeof(SettingsViewModel).GetField("AllPages")`. After the split, the catalog is the source of truth and
  `CLIHub.Tests` can see it via `InternalsVisibleTo`; replace the reflection helper with
  `SettingsPageCatalog.AllPages.ToArray()`.
- `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs:184` asserts
  `settingsViewModel.ImplementationFactory` is not null. After `AddTransient<SettingsViewModel>()`, the
  factory will be null and `ImplementationType` will be `SettingsViewModel`. Update the assertion to verify
  the transient lifetime and the implementation type (the registration's real contract), consistent with
  the patterns for `LaunchWindow`, `SettingsWindow`, and `WhatsNewWindow` at `:165-170` in the same test.

Update one doc reference:

- `docs/architecture/ui.md:75` says "Add the `SettingsPage` entry to `SettingsViewModel.AllPages` ...".
  Update it to name `SettingsPageCatalog.AllPages`.

## Impact

- **Developer velocity**: smaller, focused files make both agent and human navigation easier. The view model
  file drops from 1,925 lines to ~1,700.
- **Testability**: option records and the catalog are independently testable (though no tests are added
  here; that is a separate concern).
- **Consistency**: `AddTransient<SettingsViewModel>()` matches the DI pattern used for every other
  transient window or view model in the codebase.

No runtime behavior changes, no XAML changes, no public API surface changes.

## Non-Goals

- Extracting `WatchedDirectoryEditor`, `AccentSelection`, or `SettingsUpdateCard` into separate view
  models. Those are larger decompositions requiring new XAML files and behavior analysis; they are
  **slice 2** (currently parked) of the portfolio item.
- Rewriting `Load()`, changing property setters, or simplifying notification logic. Those are refactorings
  not required for the file split.
- Fixing the parked Settings defects (swallowed watched-directory error, stale agent counts,
  persist-then-rollback).
- Removing the placeholder page or the `PagesByGroup` projection (they may be dead, but that is a separate
  cleanup).
- Any XAML edit.

## Dependencies

None. This is a pure internal refactor.

**Slice 2** (`settings-viewmodel-behavior-refactor`, currently parked) depends on this change. This split
provides the file-size reduction needed to make slice 2 safer and more reviewable.
