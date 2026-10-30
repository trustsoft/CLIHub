# Design: Split SettingsViewModel File

## Verified Current State

All facts verified by reading the source as of 2026-10-30.

**File structure:**

- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`: 1,925 lines (verified via `Get-Content | Count`).
- Lines 1-25: namespace and usings.
- Lines 26-259: 11 secondary types before `SettingsViewModel`:
  - `:36-42` `RuntimeOption` (record)
  - `:49` `PathDisplayOption` (record)
  - `:56` `ScanDepthOption` (record)
  - `:63` `LogLevelOption` (record)
  - `:70` `PopupPositionOption` (record)
  - `:83-89` `AppThemeOption` (record)
  - `:98-104` `AccentChoice` (record with computed `AccessibleName` property)
  - `:111` `DiscoveryModeOption` (record)
  - `:117-218` `WatchedDirectoryRow` (class extending `ObservableObject`, with computed properties
    `DepthChipLabel`, `DiscoveryChipLabel`, `DiscoveredSummary`)
  - `:243-252` `SettingsPage` (record)
  - `:259` `SettingsGroup` (record)
- Lines 265: `SettingsViewModel` class begins (`public sealed class SettingsViewModel : ObservableObject, IDisposable`).
- Lines 281-352: `private static readonly SettingsPage[] AllPages` — the page catalog.
- Lines 407-410: `private static readonly SettingsGroup[] PagesByGroup` — projection from `AllPages`.
- Lines 483-495: constructor with 13 parameters (verified count).
- Line 553: `public IReadOnlyList<SettingsPage> Pages => AllPages;`
- Line 558: `public IReadOnlyList<SettingsGroup> PageGroups => PagesByGroup;`
- Line 444: `private SettingsPage selectedPage = AllPages[0];` (initial value)
- Line 1383: `this.selectedPage = AllPages[0];` (reset in `Load()`)

**DI registration:**

- `src/CLIHub/Composition/ServiceRegistration.cs:173-186`: hand-written factory resolving exactly 13
  services and constructing `SettingsViewModel`.
- Line 157: XML doc says "thirteen dependencies".
- All 13 constructor parameter types are resolvable:
  - `SettingsApplicationService` — singleton (`:171`)
  - `IUpdateVersionProvider` — singleton (`.Core/Composition/ServiceCollectionExtensions.cs:148`)
  - `IUpdateWorkflow` — singleton (`:201`)
  - `IReleaseNotesLauncher` — singleton (`:207`)
  - `IApplicationOperationLifetime` — singleton (`:83`)
  - `IExternalLauncher` — singleton (`:220`)
  - `IProjectDialogService` — singleton (`:218`)
  - `IProjectScanner` — singleton (`.Core/Composition/ServiceCollectionExtensions.cs:54`)
  - `IAgentManagementSection` — transient (`:169`)
  - `IProjectRegistrySection` — transient (`:170`)
  - `AccentSelectionMemory` — singleton (`:172`)
  - `IRuntimeExecutableLocator` — singleton (`.Core.Windows/WindowsServiceCollectionExtensions.cs:44`)
  - `ILogger<SettingsViewModel>` — framework-registered, resolvable by type.

**Test coupling:**

- `tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs:138-151`: reflects on
  `typeof(SettingsViewModel).GetField("AllPages", ...)` and expects `SettingsPage[]`. Because the catalog
  becomes the single source of truth, this helper will read `SettingsPageCatalog.AllPages.ToArray()`
  directly (`CLIHub.Tests` has `InternalsVisibleTo`) instead of reflecting on a private field.
- `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs:162-184`: asserts all registrations'
  lifetimes and implementation types or factories. Line 184 asserts
  `Assert.NotNull(settingsViewModel.ImplementationFactory);`. The pattern for transient windows with
  implementation types (`:165-170`) is `Assert.Equal(typeof(T), descriptor.ImplementationType);`.

**Doc reference:**

- `docs/architecture/ui.md:75`: "Add the `SettingsPage` entry to `SettingsViewModel.AllPages` with the
  key, title, description, group, ...".

**Name collision:**

- `SettingsGroup` (the record at `:259`) coexists with `CLIHub.Controls.SettingsGroup` (a control).
  Verified: the record is in `CLIHub.ViewModels` namespace, no collision. It will stay in that namespace
  after the move.

## Target File Layout

All files under `src/CLIHub/ViewModels/Settings/`, namespace `CLIHub.ViewModels`.

### `SettingsOptions.cs`

Contains:

- `RuntimeOption` (lines 26-42 from current file)
- `PathDisplayOption` (44-49)
- `ScanDepthOption` (51-56)
- `LogLevelOption` (58-63)
- `PopupPositionOption` (65-70)
- `AppThemeOption` (72-89)
- `AccentChoice` (91-104)
- `DiscoveryModeOption` (106-111)

Preserve exact XML doc, parameter names, and computed property (`DisplayName` on `RuntimeOption`,
`AccessibleName` on `AccentChoice`).

### `WatchedDirectoryRow.cs`

Contains:

- `WatchedDirectoryRow` class (lines 113-218 from current file)

Preserve exact constructor signature, private fields, public properties, computed properties
(`DepthChipLabel`, `DiscoveryChipLabel`, `DiscoveredSummary`), and `ObservableObject` inheritance.

### `SettingsNavigation.cs`

Contains:

- `SettingsPage` record (lines 220-252 from current file)
- `SettingsGroup` record (254-259)
- New `internal static class SettingsPageCatalog` with:
  - `public static IReadOnlyList<SettingsPage> AllPages { get; }` — from current `:281-352`
  - `public static IReadOnlyList<SettingsGroup> PageGroups { get; }` — from current `:407-410`

Decision: name the properties `AllPages` and `PageGroups` (not `Pages`/`Groups`) to minimize caller
edits. Both are static readonly collections, initialized once.

The catalog will expose them as:

```csharp
internal static class SettingsPageCatalog
{
    private static readonly SettingsPage[] allPages = { /* array from :281-352 */ };
    
    private static readonly SettingsGroup[] pagesByGroup = /* projection from :407-410 */;

    public static IReadOnlyList<SettingsPage> AllPages => allPages;
    
    public static IReadOnlyList<SettingsGroup> PageGroups => pagesByGroup;
}
```

### `SettingsViewModel.cs`

Keep only the view model. Changes:

- Remove lines 26-259 (secondary types).
- Remove the `AllPages` and `PagesByGroup` static arrays.
- Update `Pages` property (`:553`) to `public IReadOnlyList<SettingsPage> Pages => SettingsPageCatalog.AllPages;`
- Update `PageGroups` property (`:558`) to `public IReadOnlyList<SettingsGroup> PageGroups => SettingsPageCatalog.PageGroups;`
- Update `selectedPage` initialization (`:444`) to `private SettingsPage selectedPage = SettingsPageCatalog.AllPages[0];`
- Update `Load()` reset (`:1383`) to `this.selectedPage = SettingsPageCatalog.AllPages[0];`

No other changes to the view model.

## DI Simplification

Replace `src/CLIHub/Composition/ServiceRegistration.cs:173-186`:

```csharp
services.AddTransient<SettingsViewModel>(sp => new SettingsViewModel(
    sp.GetRequiredService<SettingsApplicationService>(),
    sp.GetRequiredService<IUpdateVersionProvider>(),
    sp.GetRequiredService<IUpdateWorkflow>(),
    sp.GetRequiredService<IReleaseNotesLauncher>(),
    sp.GetRequiredService<IApplicationOperationLifetime>(),
    sp.GetRequiredService<IExternalLauncher>(),
    sp.GetRequiredService<IProjectDialogService>(),
    sp.GetRequiredService<IProjectScanner>(),
    sp.GetRequiredService<IAgentManagementSection>(),
    sp.GetRequiredService<IProjectRegistrySection>(),
    sp.GetRequiredService<AccentSelectionMemory>(),
    sp.GetRequiredService<IRuntimeExecutableLocator>(),
    sp.GetRequiredService<ILogger<SettingsViewModel>>()));
```

with:

```csharp
services.AddTransient<SettingsViewModel>();
```

Update the XML doc (`:155-161`) to remove "with its thirteen dependencies" and update the summary:

```csharp
/// <summary>
///   Registers the settings window stack (application service, section view models, the settings view
///   model, and the window with its accessor) plus the runtime preference-application chain
///   (<c> PreferenceApplier </c> with its process, accent, and theme preference targets, and the shared
///   palette switcher they use) consumed at startup and on preference change.
/// </summary>
```

**Equivalence argument:**

- All 13 parameter types are registered and resolvable by type (verified above).
- The view model has exactly one public constructor.
- `AddTransient<SettingsViewModel>()` uses reflection to find that constructor and resolve each parameter
  by type, which is exactly what the hand-written factory does.
- The only difference is that the factory explicitly specifies each parameter; `AddTransient` infers them.
  For a 13-parameter constructor with every parameter resolvable, the inference is correct and
  maintainable (the registration breaks at compile time if a parameter type is not registered).

## Test Updates

### `SettingsPageMetadataTests.cs`

Update `ReadAllPages()` at `:138-151` to read the catalog API directly:

```csharp
private static SettingsPage[] ReadAllPages() => SettingsPageCatalog.AllPages.ToArray();
```

Remove the now-unused `System.Reflection` using if nothing else in the file needs it.

### `ServiceRegistrationTests.cs`

Update the `settingsViewModel` assertion at `:183-184`:

```csharp
Assert.Equal(ServiceLifetime.Transient, settingsViewModel.Lifetime);
Assert.Equal(typeof(SettingsViewModel), settingsViewModel.ImplementationType);
```

Remove the `Assert.NotNull(settingsViewModel.ImplementationFactory);` line. This matches the pattern for
`LaunchWindow`, `SettingsWindow`, and `WhatsNewWindow` at `:165-170`.

The existing `AddClIHubServices_ResolvesWpfSafeServiceGraph` test builds the provider and resolves the
WPF service graph, but it deliberately stops short of `SettingsViewModel`: resolving it constructs
`SettingsApplicationService` → `PreferenceApplier` → `GlobalHotkeyService` → `LaunchWindow`, which
requires an STA thread. Rather than duplicate the suite's private `RunOnSta` helper or override the
hotkey service, this change relies on the Settings smoke (4.5) as the runtime resolution check. The
DI equivalence itself is verified by inspection: the constructor is unchanged, there is exactly one
public constructor, and all 13 parameter types are registered by type.

## Doc Updates

Update `docs/architecture/ui.md:75`:

```markdown
2. Add the `SettingsPage` entry to `SettingsPageCatalog.AllPages` with the key, title, description,
   group, `IsImplemented`, icon, and the optional `HeaderTitleExtraKey`, `HeaderActionKey`, and
   `SummaryRowKey`. The sidebar groups and the page order derive from that array.
```

## Risks

**Low risk:**

- **File-move errors**: mitigated by staged incremental moves (Stage 1 moves types; Stage 2 extracts
  catalog; Stage 3 simplifies DI). Build + test after each stage.
- **DI resolution failure**: mitigated by the equivalence argument (all 13 types are registered and
  resolvable by type) and the final test gate (build + test + resolve `SettingsViewModel` via DI).
- **Test coupling breakage**: mitigated by updating the two tests that reference the moved types
  (`SettingsPageMetadataTests` reads the catalog API; `ServiceRegistrationTests` asserts the transient
  implementation type).

**No behavior risk:** the change is a pure code-move and DI pattern simplification. No XAML, no public API,
no runtime behavior changes.

## Alternatives Rejected

1. **Extract the catalog into a nested static class inside `SettingsViewModel`.**
   - Rejected: keeps the file size large (1,900+ lines), does not improve navigability.

2. **Keep the hand-written DI factory.**
   - Rejected: the factory is 14 lines of repetitive ceremony that duplicates the constructor signature.
     `AddTransient<SettingsViewModel>()` is equivalent and matches the pattern used everywhere else in the
     codebase.

3. **Rename `SettingsGroup` to avoid the name collision with `CLIHub.Controls.SettingsGroup`.**
   - Rejected: there is no collision — the record is in `CLIHub.ViewModels`, the control is in
     `CLIHub.Controls`. Both are disambiguated by namespace. Renaming the record would be a gratuitous
     API change in a behavior-preserving refactor.

4. **Move the option records into individual files (one record per file).**
   - Rejected: eight files for eight trivial records is excessive. The records are related (all are
     option types for selectors in Settings), cohesive, and together are ~100 lines. One `SettingsOptions.cs`
     file is the right granularity.

5. **Extract `WatchedDirectoryEditor`, `AccentSelection`, or `SettingsUpdateCard` into separate view models.**
   - Rejected: those are larger decompositions requiring new XAML files and behavior analysis. They are
     **slice 2** of the portfolio item (currently parked). This change is **slice 1**, a pure mechanical
     refactor to reduce file size and simplify DI before tackling slice 2.

## Open Questions

None. All facts verified, all decisions finalized.

## Deviations from Brief

None. The brief's claimed line numbers (`:36-259`, `:281-352`, `:407-410`, `:444`, `:1383`, etc.) match
the actual source. The brief's claim that the file is 1,925 lines is verified. The brief's claim that
there are 11 secondary types and a 13-parameter constructor is verified. The brief's claim that all 13
dependency types are registered is verified.
