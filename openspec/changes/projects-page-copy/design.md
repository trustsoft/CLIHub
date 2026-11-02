# Design: revise the Projects page copy

## Current state (verified)

| Location | Current text |
| --- | --- |
| `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:58` | `Registered projects, their provenance, and manually added entries.` |
| `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:33` | `Project changes apply immediately. Save and Cancel only affect the other settings.` |
| `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:38` | `Discovered projects are managed by Project Scanning and are read-only here.` |
| `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:68` | `Enter absolute directory path (e.g., C:\repos)…` (placeholder) |
| `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:81` | `Register` (button) |
| `src/CLIHub/Views/SettingsWindow.xaml:198` | `Detail="{Binding ProjectRegistry.ProjectMixSummary}"` (PROJECTS card) |
| `src/CLIHub/Views/SettingsWindow.xaml:199` | `DetailToolTip="Found by scanning or added manually"` (PROJECTS card) |
| `src/CLIHub/Views/SettingsWindow.xaml:197` | `AlertToolTip="Projects with missing folders"` (PROJECTS card) |
| `src/CLIHub/Views/SettingsWindow.xaml:205` | `Detail="Registered manually"` (MANUAL card) |
| `src/CLIHub/Views/SettingsWindow.xaml:210` | `Detail="Managed by Project Scanning"` (DISCOVERED card) |
| `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:206` | `No projects registered yet.` (empty state) |
| `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs:208` | `This folder is already registered as a project.` |
| `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs:264` | `The selected folder does not exist: {ex.Message}` |
| `src/CLIHub/ViewModels/Projects/IProjectRegistrySection.cs:38-40` | `ProjectMixSummary` property with XML doc |
| `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs:78` | `ProjectMixSummary` property implementation |
| `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs:310` | `OnPropertyChanged(nameof(this.ProjectMixSummary))` call |
| `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs:72` | `Assert.Equal("1 discovered · 2 manual", section.ProjectMixSummary);` |
| `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs:364` | `Assert.Equal($"The selected folder does not exist: Folder does not exist: {newFolder}", section.ErrorText);` |
| `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs:175` | `Assert.Contains("already registered", section.ErrorText);` |
| `docs/backlog.md:349-358` | Backlog entry: "Projects page add row still uses `Register` and the old placeholder" |

Layout: both notes currently follow the `REGISTERED PROJECTS` header — immediate-apply note
`Margin="0,0,0,4"`, discovered note `Margin="0,0,0,10"`. They move above the header, both at
`Margin="0,2,0,0"` (see "Note placement").

## Approved copy changes (from `projects-page-copy-mockup.html`)

The table below is the single source of truth for the approved wording. The mockup's "Что меняется"
table (lines 1084-1174) documents every change with character counts and rationale. Wording only:
the two notes also move above the `REGISTERED PROJECTS` header (see "Note placement" below).

| # | Where | Before | After | Rationale |
| --- | --- | --- | --- | --- |
| 1 | Page description | `Registered projects, their provenance, and manually added entries.` | `Where projects come from and how to manage them.` | 66 → 48 characters (−27%). Removes "provenance" and "entries"; names the two tasks: origin and management. |
| 2 | Immediate-apply note | `Project changes apply immediately. Save and Cancel only affect the other settings.` | `Changes apply immediately. Save and Cancel only affect the other pages.` | 82 → 71 characters (−13%). Matches the already-approved Project Scanning wording; "Project" and "settings" removed. |
| 3 | Discovered note | `Discovered projects are managed by Project Scanning and are read-only here.` | `Discovered projects are read-only – manage them in Project Scanning.` | 75 → 68 characters. Active voice: fact first (read-only), then where to go. |
| 4 | Add-row placeholder | `Enter absolute directory path (e.g., C:\repos)…` | `Absolute folder path, e.g. C:\repos` | Removes "Enter" and "directory"; the example shows the format. Matches Project Scanning and closes the backlog item. |
| 5 | Add button | `Register` | `Add` | Shorter; matches Project Scanning and the glossary ("Add Project…"). Action is to add a folder to the list. |
| 6 | PROJECTS card detail | `4 discovered · 2 manual` (binding: `ProjectMixSummary`) | `In the launch window` (static text) | The old binding repeated the two sibling cards' numbers. New text says where the project appears. |
| 7 | MANUAL card detail | `Registered manually` | `Added by you` | 19 → 12 characters. Label already says MANUAL; detail explains who added it. |
| 8 | DISCOVERED card detail | `Managed by Project Scanning` | `From watched folders` | 27 → 20 characters. Explains origin without repeating the note below or naming the page twice. |
| 9 | Empty state | `No projects registered yet.` | `No projects yet – add a folder above.` | Same fact plus the next step. Visible only when the list is empty. |
| 10 | Duplicate error | `This folder is already registered as a project.` | `This folder is already registered.` | 50 → 37 characters. Same pattern as "This folder is already watched." on Project Scanning. |
| 11 | Change-folder error | `The selected folder does not exist: {ex.Message}` | `Folder not found: {ex.Message}` | 37 → 17 characters before the colon. Matches the "Folder not found" chip in the project row. |
| 12 | PROJECTS card detail tooltip | `Found by scanning or added manually` | (removed) | Detail is now "In the launch window"; old tooltip repeated the two sibling cards. |
| 13 | PROJECTS card alert tooltip | `Projects with missing folders` | `Folders that no longer exist` | More precise: the alert refers to folders, not projects. Visible only on hover. |

## Copy principles (from the mockup)

1. **Deduplicate**: The discovered/manual split is already shown by two separate cards, so the
   PROJECTS card no longer repeats it.
2. **Replace jargon**: "provenance" → "where they came from", "directory" → "folder". Vocabulary
   stays: manual, discovered, watched folder.
3. **One fact, once**: Immediate-apply and read-only for discovered are stated once, without
   repeating in the header and cards.
4. **Match Project Scanning**: "Add", "Absolute folder path, e.g. C:\repos", and "Changes apply
   immediately…" come from the already-approved sibling page.

## Code changes

### 1. Page description (SettingsNavigation.cs:58)

Before:
```csharp
"Registered projects, their provenance, and manually added entries.",
```

After:
```csharp
"Where projects come from and how to manage them.",
```

### 2. Immediate-apply note (SettingsProjectRegistryPage.xaml:29, was :33)

Before:
```xaml
Text="Project changes apply immediately. Save and Cancel only affect the other settings."
```

After:
```xaml
Text="Changes apply immediately. Save and Cancel only affect the other pages."
```

The note also moves above the `REGISTERED PROJECTS` header with `Margin="0,2,0,0"`; see
"Note placement".

### 3. Discovered note (SettingsProjectRegistryPage.xaml:34, was :38)

Before:
```xaml
Text="Discovered projects are managed by Project Scanning and are read-only here."
```

After:
```xaml
Text="Discovered projects are read-only – manage them in Project Scanning."
```

The note also moves above the `REGISTERED PROJECTS` header with `Margin="0,2,0,0"`; see
"Note placement".

### 3a. Note placement

Before (lines 28-41):

```text
StackPanel
  SectionHeader  "REGISTERED PROJECTS"
  TextBlock      immediate-apply note (Margin="0,0,0,4")
  TextBlock      discovered note      (Margin="0,0,0,10")
  Grid           add row
```

After:

```text
StackPanel
  TextBlock      immediate-apply note (Margin="0,2,0,0")
  TextBlock      discovered note      (Margin="0,2,0,0")
  SectionHeader  "REGISTERED PROJECTS"
  Grid           add row
```

This mirrors Project Scanning: there the immediate-apply note is the first child of the content
`StackPanel`, before the first section header, with `Margin="0,2,0,0"`. Moving both notes above the
header reuses the section header's built-in `0,18,0,8` margin for the note-to-section gap, so no
extra spacer is needed. Text, styles (`HintText`), `TextWrapping="Wrap"`, child order below the add
row, and every other element stay unchanged.

### 4. Add-row placeholder (SettingsProjectRegistryPage.xaml:68)

Before:
```xaml
Tag="Enter absolute directory path (e.g., C:\repos)…"
```

After:
```xaml
Tag="Absolute folder path, e.g. C:\repos"
```

### 5. Add button (SettingsProjectRegistryPage.xaml:81)

Before:
```xaml
Content="Register"
```

After:
```xaml
Content="Add"
```

### 6. PROJECTS card detail (SettingsWindow.xaml:198)

Before:
```xaml
Detail="{Binding ProjectRegistry.ProjectMixSummary}"
```

After:
```xaml
Detail="In the launch window"
```

### 7. PROJECTS card detail tooltip (SettingsWindow.xaml:199)

Before:
```xaml
DetailToolTip="Found by scanning or added manually"
```

After: (remove this line)

### 8. PROJECTS card alert tooltip (SettingsWindow.xaml:197)

Before:
```xaml
AlertToolTip="Projects with missing folders"
```

After:
```xaml
AlertToolTip="Folders that no longer exist"
```

### 9. MANUAL card detail (SettingsWindow.xaml:205)

Before:
```xaml
Detail="Registered manually"
```

After:
```xaml
Detail="Added by you"
```

### 10. DISCOVERED card detail (SettingsWindow.xaml:210)

Before:
```xaml
Detail="Managed by Project Scanning"
```

After:
```xaml
Detail="From watched folders"
```

### 11. Empty state (SettingsProjectRegistryPage.xaml:206)

Before:
```xaml
Text="No projects registered yet."
```

After:
```xaml
Text="No projects yet – add a folder above."
```

### 12. Duplicate error (ProjectRegistryViewModel.cs:208)

Before:
```csharp
this.ErrorText = "This folder is already registered as a project.";
```

After:
```csharp
this.ErrorText = "This folder is already registered.";
```

### 13. Change-folder error (ProjectRegistryViewModel.cs:264)

Before:
```csharp
this.ErrorText = $"The selected folder does not exist: {ex.Message}";
```

After:
```csharp
this.ErrorText = $"Folder not found: {ex.Message}";
```

### 14. Remove unused property (IProjectRegistrySection.cs:38-40)

Remove:
```csharp
/// <summary>
///   The discovered/manual split summary, e.g. "31 discovered · 4 manual".
/// </summary>
string ProjectMixSummary { get; }
```

### 15. Remove unused property (ProjectRegistryViewModel.cs:78)

Remove:
```csharp
public string ProjectMixSummary => $"{this.DiscoveredCount} discovered · {this.ManualCount} manual";
```

### 16. Remove unused property change notification (ProjectRegistryViewModel.cs:310)

Remove the line:
```csharp
this.OnPropertyChanged(nameof(this.ProjectMixSummary));
```

### 17. Update test (ProjectRegistryViewModelTests.cs:72)

Remove:
```csharp
Assert.Equal("1 discovered · 2 manual", section.ProjectMixSummary);
```

### 18. Update test (ProjectRegistryViewModelTests.cs:364)

Before:
```csharp
Assert.Equal($"The selected folder does not exist: Folder does not exist: {newFolder}", section.ErrorText);
```

After:
```csharp
Assert.Equal($"Folder not found: Folder does not exist: {newFolder}", section.ErrorText);
```

(The test at line 175 asserts `Contains("already registered")`, so it still passes with the new
wording "This folder is already registered.")

### 19. Remove backlog entry (docs/backlog.md:349-358)

Remove the section "Projects page add row still uses `Register` and the old placeholder" (lines
349-358), including the heading, severity, evidence, proposed remedy, and trigger.

## Rejected / deliberately unchanged

- **Page title `Projects`**, section header `REGISTERED PROJECTS`, card labels
  `PROJECTS` / `MANUAL` / `DISCOVERED` — short, accurate, and parallel.
- **`Browse…`** — standard folder-picker action.
- **Row tooltips and accessibility names** (`Change project folder`, `Open project folder`, `Remove
  project`) — concrete, 2-3 words each.
- **`Folder not found` chip** — explicit, shown only on broken rows.
- **Provenance tooltips** (`Manual` / `Discovered`) — domain terms, already short.
- **Shared validation messages** (`Enter a folder path or browse for one.`, `Enter an existing
  absolute folder path.`, `Enter a valid absolute folder path.`) — brief, shared with Project
  Scanning; change only together with the sibling page.
- **Error prefixes** (`Could not add project: …`, `Could not open folder: …`, `Could not change
  project path: …`, `Could not remove project: …`) — short prefix + exception detail.
- **`Save` / `Cancel`** — global footer, not page-specific copy.
- **Folder-picker dialog title** (`Change Project Folder`) and removal-confirmation dialog text —
  not part of the page surface.

## Follow-ups

The three shared add-row validation messages remain unchanged in this change by explicit decision:
- `Enter a folder path or browse for one.`
- `Enter an existing absolute folder path.`
- `Enter a valid absolute folder path.`

After this change, a separate mockup/approval cycle will revise these messages on both Settings
pages (Projects and Project Scanning) together. This change must not touch them.

## Specification impact

None. These strings are not quoted in any durable OpenSpec specification (`openspec/specs/`).
Behavior, configuration keys, layout, and persistence are unchanged. The change uses
`skip_specs: true` with the rationale that no spec quotes the UI copy and the change is copy-only.

## Backlog closure

`docs/backlog.md:349-358` — "Projects page add row still uses `Register` and the old placeholder" —
is resolved by this change. The trigger ("the next Projects-page copy or layout change") has
arrived. The proposed remedy (apply the same placeholder and `Add` label) is implemented here.

## Approval

The user reviewed and approved the HTML mockup `projects-page-copy-mockup.html`. That mockup's
interactive "Что меняется" table (lines 1056-1194) documents every before/after pair with character
counts, rationale, and an "unchanged" section listing the non-goals above.
