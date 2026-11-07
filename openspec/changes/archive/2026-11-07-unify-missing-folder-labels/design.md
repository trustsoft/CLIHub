# Design: unify the missing-folder labels on the Projects page

## Verified current state

### The two labels describe the same condition

The Settings > Projects page surfaces "a registered project whose folder no longer exists" in two
places, with different wording:

1. **PROJECTS summary card** — danger-tone alert line, bound from the view model:
   - `ProjectRegistryViewModel.cs:78-83` — `FolderStatusSummary` returns `"1 folder missing"` for
     one, `"{count} folders missing"` otherwise, `string.Empty` when none.
   - `SettingsWindow.xaml:190-194` — `SummaryCard Alert="{Binding ProjectRegistry.FolderStatusSummary}"`
     with `IsAlertVisible="{Binding ProjectRegistry.HasMissingFolders}"` and
     `AlertToolTip="Folders that no longer exist"`.

2. **Per-row chip** — danger-tone border + text, hardcoded in XAML (not bound):
   - `SettingsProjectRegistryPage.xaml:141-145` — `<Border Style="{StaticResource ProjectMissingChip}">`
     containing `<TextBlock Style="{StaticResource ProjectMissingChipText}" Text="Folder not found"/>`.
   - `SettingsProjectRegistryPage.xaml:12-25` — style `ProjectMissingChip` (a `Border`,
     `BasedOn HeaderChip`) with `BorderBrush = DangerBrush`, `Visibility` collapsed by default and
     visible when `{Binding IsFolderAvailable}` is `False` (`:17-19`); style
     `ProjectMissingChipText` (a `TextBlock`, `BasedOn HeaderChipLabel`) with
     `Foreground = DangerBrush` (`:23-25`).
   - `SettingsProjectRegistryPage.xaml:10-11` — comment: `The "Folder not found" chip: visible while
     the row's enriched snapshot reports the folder unavailable, so no inverted converter is needed.`

The chip text is a hardcoded XAML literal on the `TextBlock` at `:144`. It is **not** bound from a
view-model property. Confirmed by reading the XAML directly.

### The change-folder error text is a separate string

`ProjectRegistryViewModel.cs:259-262`:

```csharp
catch (DirectoryNotFoundException ex)
{
    this.ErrorText = $"Folder not found: {ex.Message}";
}
```

This is a transient validation error set when the user picks a non-existent folder while changing a
manual project's path. It surfaces via the page-level `ErrorText` binding
(`SettingsProjectRegistryPage.xaml:86-89`), not via the per-row chip. Its test assertion is
`ProjectRegistryViewModelTests.cs:383`:

```csharp
Assert.Equal($"Folder not found: Folder does not exist: {newFolder}", section.ErrorText);
```

### No durable spec or live doc pins the chip string

Searched the repository for `Folder not found` and `Folder missing`:

- `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:10,144` — the chip text and its comment (in
  scope).
- `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs:261` — the change-folder error text
  (out of scope, see above).
- `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs:383` — asserts the
  change-folder error text (out of scope, untouched).
- `openspec/changes/archive/...` — several archived changes reference the chip string in their
  prose; archived changes must not be edited and are historical context only.
- `openspec/specs/project-management/spec.md:204` — `a project whose folder no longer exists is
  marked as missing its folder`. This is conceptual behavior language ("missing"), not the literal
  chip string `Folder not found`. It does not pin the chip text.
- `openspec/specs/settings-theme/spec.md` — the "Projects page summary cards" requirement
  (`:339-369`) and the "Missing folders reported" scenario (`:360-364`) specify the card's
  singular/plural "folder"/"folders" wording and the danger tone, but never quote the row chip text.
- `README.md` — no reference to either chip string.

No live (non-archived) spec or doc pins the chip text `Folder not found`. A spec delta is not
required.

## Decision: standardize both labels on "missing"

The card already says "missing" and the project-management spec already says "missing its folder".
Changing the row chip from `Folder not found` to `Folder missing` makes the two surfaces
consistent with each other and with the spec's existing language, at the cost of a one-word XAML
literal swap. The card copy stays exactly as it is.

## Alternatives considered

- **Unify on "not found" instead** — change the card strings to `1 folder not found` /
  `{n} folders not found`. Rejected: the brief fixes the card copy, and "not found" is the more
  technical phrasing. The spec's existing wording ("missing") favors "missing".

- **Differentiate card-count vs row-state phrasing on purpose** — keep "missing" for the count
  summary and "not found" for the per-row state, arguing they answer different questions ("how
  many" vs "this one"). Rejected: the user's goal is to stop calling the same condition two names;
  both surfaces report the identical per-folder condition, so one term is clearer.

## Scope decision: the change-folder error text stays

The string `Folder not found: {ex.Message}` (`ProjectRegistryViewModel.cs:261`) was considered for
inclusion because it contains the words "Folder not found". It is **out of scope**:

- It is a transient validation error about a path the user just picked in the change-folder dialog,
  shown in the page-level error line (`SettingsProjectRegistryPage.xaml:86-89`), not in the per-row
  chip.
- It is a different message with a different audience and lifecycle than the two missing-folder
  status labels this change unifies.
- Its test (`ProjectRegistryViewModelTests.cs:383`) asserts the exact error string; changing it
  would require a coordinated test edit that is unrelated to the label-unification goal.
- The brief explicitly excludes it.

Recorded here so the implementation does not touch it.

## Open questions

None.
