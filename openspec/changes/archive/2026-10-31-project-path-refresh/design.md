# Design: project-path-refresh

## Current state (verified in code)

| Concern | Today | File |
|---|---|---|
| `DisplayPath` property | `public string DisplayPath => this.Path;` (`:134`), a computed property with no setter and no explicit notification when `Path` changes | `src/CLIHub/ViewModels/Projects/ProjectModel.cs` |
| `Path` property setter | `:63-67`: calls `SetValue(ref this.path, value)`, which raises `PropertyChanged` for `Path` only when the value changes (`:170`); does not notify `DisplayPath` | `src/CLIHub/ViewModels/Projects/ProjectModel.cs` |
| Settings row XAML binding | `:166,168`: binds the path TextBlock's `Text` and `ToolTip` to `DisplayPath`, so the UI only updates when `PropertyChanged("DisplayPath")` is raised | `src/CLIHub/Views/SettingsProjectRegistryPage.xaml` |
| Launch window binding | Binds to `Path` directly, not `DisplayPath`, and already updates correctly when `Path` changes | Verified by inspection and launch window behavior |
| Spec requirement | `:198`, `:264-267`: project changes SHALL apply immediately, and every other surface SHALL reflect them without a restart | `openspec/specs/project-management/spec.md` |

## Decision 1: Notification fix — raise `PropertyChanged(nameof(DisplayPath))` in `Path` setter

The fix is a one-line addition in the `Path` property setter (`:63-67` in `ProjectModel.cs`):

```csharp
public string Path
{
    get => this.path;
    private set
    {
        if (this.SetValue(ref this.path, value))
        {
            this.OnPropertyChanged(nameof(DisplayPath));
        }
    }
}
```

`SetValue` returns `true` when the value changed (`:171`), so the `DisplayPath` notification is raised only when `Path` actually changed, avoiding redundant events.

Rationale: `DisplayPath` is a computed property that returns `Path` (`:134`), so any binding to `DisplayPath` must be notified when `Path` changes. The Settings XAML binds to `DisplayPath` (`:166,168`), so the binding never updates unless `PropertyChanged("DisplayPath")` is raised. The launch window binds `Path` directly and does not need a change. This fix is the minimal coherent change that restores the spec's immediate-update contract.

## Decision 2: Alternative considered and rejected — rebind the Settings XAML to `Path`

An alternative would be to change `SettingsProjectRegistryPage.xaml:166,168` to bind `Text` and `ToolTip` to `Path` instead of `DisplayPath`, and remove or deprecate the `DisplayPath` property entirely.

**Rejected.** The `DisplayPath` property's XML doc (`:130-133`) explicitly states its purpose: "The full folder path for surfaces that show it untrimmed (registry rows and tooltips); the launch window keeps its width-aware trimming over `Path`." This documents an intentional semantic distinction: `DisplayPath` is for surfaces that show the full untrimmed path (Settings registry), while `Path` is for the launch window, which applies width-aware trimming. Removing `DisplayPath` would lose that documented distinction. The notification fix preserves the existing model contract and avoids churning the XAML or removing a documented property.

## Decision 3: Test coverage — model and view-model regression tests

Add tests to ensure the fix stays correct:

1. **Model test** (`tests/CLIHub.Core.Tests/Projects/ProjectModelCatalogTests.cs` if it exists, or `tests/CLIHub.Tests/ViewModels/Projects/ProjectModelTests.cs` otherwise):
   - `ProjectModel_PathChanged_RaisesDisplayPathChanged`: create a `ProjectModel` with an enriched snapshot, subscribe to `PropertyChanged`, call `Update(snapshot with new path)`, verify `PropertyChanged` was raised for both `"Path"` and `"DisplayPath"`.
   - `ProjectModel_PathChanged_DisplayPathReturnsNewPath`: create a `ProjectModel`, update it with a new path, verify `DisplayPath` returns the new path.

2. **View-model test** (`tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs`):
   - Extend or add a `ChangePathCommand` test to verify that after `Load()`, the changed project's `DisplayPath` property returns the new folder path. The existing test `ChangePath_ManualProject_CallsServiceAndReloads` (if it exists from the `project-path-correction` change) asserts `Name` and `Path`; extend it to also assert `DisplayPath`.

Rationale: the model test ensures the `DisplayPath` notification is raised and the property returns the updated value. The view-model test ensures the full change-path flow (service call → `Load()` → enrichment → model update) produces a row whose `DisplayPath` reflects the new folder, closing the regression scenario described in the brief.

## Decision 4: No XAML change required

The XAML at `:166,168` already binds `DisplayPath` correctly. Once the model raises `PropertyChanged("DisplayPath")`, the binding updates automatically. No XAML edit is required.

Rationale: the binding target is correct; the missing piece is the notification, which the model fix provides.

## Decision 5: No service or config change

The service API (`IProjectService.ChangeProjectPath`) already updates `project.Path` and persists immediately. The enrichment flow (`ProjectRegistryViewModel.Load()` → `ProjectModelCatalog.Synchronize` → `model.Update(snapshot)`) already applies fresh snapshots to existing models. The fix is entirely in the model's notification logic.

Rationale: the service and enrichment contracts are correct; the bug is in the model's property-change notification only.

## Open questions

None. The fix is localized to the `Path` setter, with clear test coverage and no XAML or service changes required.

## Alternatives considered and rejected

### Alternative 1: Make `DisplayPath` a stored field with a setter

**Rejected.** `DisplayPath` is intentionally a computed property (`:134`). Introducing a stored field and setter would duplicate the `Path` value and require synchronizing both fields in `Apply` (`:153-160`), adding complexity for no benefit. The computed property is correct; the missing piece is the notification.

### Alternative 2: Raise `PropertyChanged` for `DisplayPath` in `Apply` instead of the `Path` setter

**Rejected.** The `Apply` method (`:153-160`) sets `Name`, `Path`, `LogoPath`, `IsFolderAvailable`, and `IsFavorite`, and each setter already raises its own notification via `SetValue`. Adding a manual `PropertyChanged("DisplayPath")` call in `Apply` would raise the notification even when `Path` did not change (because `Apply` always sets all fields). The `Path` setter is the correct place: it raises the notification only when `Path` actually changed.

### Alternative 3: Override `SetValue` to auto-notify dependent properties

**Rejected.** `SetValue` is a generic helper (`:162-172`) used by multiple properties. Adding logic to auto-notify `DisplayPath` when `Path` changes would couple `SetValue` to domain knowledge about property dependencies, breaking its generic contract. The explicit notification in the `Path` setter is clearer and localized.

## Deviations from brief

None. The design follows the brief's fix direction: raise `PropertyChanged(nameof(DisplayPath))` from the `Path` setter, preserve the existing model contract and Settings binding, add regression tests, and justify `skip_specs: true` (done in `.openspec.yaml` and proposal.md).
