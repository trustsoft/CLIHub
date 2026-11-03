# Design: revise the three shared add-row validation messages

## Current state (verified)

Both `ProjectRegistryViewModel` (Projects page) and `WatchedDirectoryEditor` (Project Scanning page)
use exactly the same three validation messages in their add-row controls:

| Location | Line | Current text |
| --- | --- | --- |
| `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs` | 182 | `Enter a folder path or browse for one.` |
| `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs` | 188 | `Enter an existing absolute folder path.` |
| `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs` | 199 | `Enter a valid absolute folder path.` |
| `src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs` | 470 | `Enter a folder path or browse for one.` |
| `src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs` | 476 | `Enter an existing absolute folder path.` |
| `src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs` | 487 | `Enter a valid absolute folder path.` |
| `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs` | 200 | `Assert.Contains("existing absolute", section.ErrorText);` |

Validation order in both view models:
1. Empty check (`text.Length == 0`)
2. Not fully qualified OR folder does not exist (`!Path.IsPathFullyQualified(text) || !Directory.Exists(text)`)
3. Path normalization fails (catch `ArgumentException or NotSupportedException or PathTooLongException`)

The duplicate-folder checks come after normalization and use different messages by design:
- Projects: `This folder is already registered.` (line 205)
- Project Scanning: `This folder is already watched.` (asserted in
  `SettingsViewModelApplicationTests.cs:356, 751`)

## Approved copy changes (from `validation-errors-copy-mockup.html`)

The user approved the HTML mockup `validation-errors-copy-mockup.html` (at the repo root). The
three table rows in its "Что меняется" section are the approved copy. Character counts and rationale
from that table:

| # | When shown | Before | After | Rationale |
| --- | --- | --- | --- | --- |
| 1 | Empty input | `Enter a folder path or browse for one.` | `Choose a folder or type its path.` | 41 → 35 characters (−15%). Instead of "Enter", names two explicit actions: choose a folder or type its path. Reads as an action, not a formal instruction. |
| 2 | Path not fully qualified OR folder does not exist | `Enter an existing absolute folder path.` | `Use a full folder path that exists.` | 40 → 35 characters (−13%). "Absolute" replaced with everyday "full"; one sentence covers both validation conditions: full path and folder existence. |
| 3 | Path normalization fails (invalid characters / too long) | `Enter a valid absolute folder path.` | `That path isn't valid.` | 35 → 24 characters (−31%). Directly names the cause without repeating "absolute folder path"; the field is already labeled. |

All three messages become shorter and more conversational. "Absolute" (a technical term unfamiliar
to many Windows users) is replaced with "full" in message 2 and dropped in message 3. The "Enter…"
imperative is replaced with action-oriented phrasing.

## Code changes

### 1. ProjectRegistryViewModel.cs, line 182 (empty input)

Before:
```csharp
this.ErrorText = "Enter a folder path or browse for one.";
```

After:
```csharp
this.ErrorText = "Choose a folder or type its path.";
```

### 2. ProjectRegistryViewModel.cs, line 188 (not fully qualified OR missing folder)

Before:
```csharp
this.ErrorText = "Enter an existing absolute folder path.";
```

After:
```csharp
this.ErrorText = "Use a full folder path that exists.";
```

### 3. ProjectRegistryViewModel.cs, line 199 (normalization failure)

Before:
```csharp
this.ErrorText = "Enter a valid absolute folder path.";
```

After:
```csharp
this.ErrorText = "That path isn't valid.";
```

### 4. WatchedDirectoryEditor.cs, line 470 (empty input)

Before:
```csharp
this.WatchedDirectoryError = "Enter a folder path or browse for one.";
```

After:
```csharp
this.WatchedDirectoryError = "Choose a folder or type its path.";
```

### 5. WatchedDirectoryEditor.cs, line 476 (not fully qualified OR missing folder)

Before:
```csharp
this.WatchedDirectoryError = "Enter an existing absolute folder path.";
```

After:
```csharp
this.WatchedDirectoryError = "Use a full folder path that exists.";
```

### 6. WatchedDirectoryEditor.cs, line 487 (normalization failure)

Before:
```csharp
this.WatchedDirectoryError = "Enter a valid absolute folder path.";
```

After:
```csharp
this.WatchedDirectoryError = "That path isn't valid.";
```

### 7. ProjectRegistryViewModelTests.cs, line 200 (test assertion)

The test `RegisterCommand_WithMissingFolder_ShowsErrorAndAddsNothing` currently uses
`Assert.Contains("existing absolute", section.ErrorText)` to verify the second validation message.
Replace with an exact assertion for the new wording:

Before:
```csharp
Assert.Contains("existing absolute", section.ErrorText);
```

After:
```csharp
Assert.Equal("Use a full folder path that exists.", section.ErrorText);
```

Using `Assert.Equal` with the full message ensures the exact approved wording and is more precise
than the current `Contains` check.

## Rejected / deliberately unchanged

- **Validation order and behavior**: The empty check, `Path.IsPathFullyQualified`, `Directory.Exists`,
  and normalization catch remain unchanged. Validation order stays: empty → non-fully-qualified /
  missing → normalization failure.
- **Duplicate-folder messages**: `This folder is already watched.` (Project Scanning) and
  `This folder is already registered.` (Projects) differ by design and are not touched.
- **Add-row placeholder**: `Absolute folder path, e.g. C:\repos` is unchanged; this change revises
  only error messages.
- **Buttons**: `Browse…` and `Add` are unchanged.
- **Layout**: No XAML change; only string literals in view-model C# code are replaced.
- **Other error messages**: The change-folder error in `ProjectRegistryViewModel` (`Folder not found: {ex.Message}`)
  and all other strings in both view models are unchanged.

## Specification impact

None. No durable specification under `openspec/specs/` quotes these validation messages. Behavior
is unchanged (same validation order, same checks, same error-line binding). The change uses
`skip_specs: true` in `.openspec.yaml`.

## Manual smoke rationale

No manual smoke task is required. This change only replaces string literals in two view models; the
error-line binding and layout are unchanged. The exact strings are covered by the view-model test
(`ProjectRegistryViewModelTests.cs`), which is updated as part of this change. The watched-directory
tests (`SettingsViewModelApplicationTests.cs`) assert only the duplicate message
(`This folder is already watched.`), which is not changed.

Rendered-layout verification: no XAML or layout change. The error text is bound to `ErrorText` /
`WatchedDirectoryError` properties; the binding itself is not touched. Message length decreases
(−15%, −13%, −31%), so no overflow or truncation risk.

## Dependencies

None. This change is self-contained.

## Open questions

None.
