# Revise the three shared add-row validation messages

## Why

The Settings → Projects and Settings → Project Scanning pages share three identical validation
messages in their add-row controls. The current wording uses verbose, formal phrasing
(`Enter a folder path or browse for one.`, `Enter an existing absolute folder path.`,
`Enter a valid absolute folder path.`) with technical terms like "absolute" that are unfamiliar
to many Windows users.

The user reviewed an HTML mockup of the revised messages (`validation-errors-copy-mockup.html`)
and approved shorter, conversational alternatives that replace "absolute" with "full" and reduce
redundancy. These strings appear in two view models: `ProjectRegistryViewModel` (Projects page)
and `WatchedDirectoryEditor` (Project Scanning page). Both use exactly the same three messages
today; this change keeps them identical after the revision.

## What changes

Replace three validation messages in both view models:

1. **Empty input**: `Enter a folder path or browse for one.` → `Choose a folder or type its path.`
   (41 → 35 characters, −15%)
2. **Not fully qualified OR folder does not exist**: `Enter an existing absolute folder path.` →
   `Use a full folder path that exists.` (40 → 35 characters, −13%)
3. **Path normalization fails** (invalid characters / too long): `Enter a valid absolute folder path.`
   → `That path isn't valid.` (35 → 24 characters, −31%)

Update the test assertion in `ProjectRegistryViewModelTests.cs` that checks the second message
(currently `Assert.Contains("existing absolute", section.ErrorText)`); replace with an exact
assertion `Assert.Equal("Use a full folder path that exists.", section.ErrorText)` for the new
wording.

Do not change:
- Validation order (empty → non-fully-qualified / missing → normalization failure)
- Empty check, `Path.IsPathFullyQualified`, `Directory.Exists`, or the normalization catch
- Duplicate-folder messages (`This folder is already watched.` / `This folder is already registered.`)
- Add-row placeholder (`Absolute folder path, e.g. C:\repos`), buttons (`Browse…`, `Add`), or layout
- Any other string in either view model or page

## Impact

- UI: `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs` (lines 182, 188, 199),
  `src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs` (lines 470, 476, 487)
- Tests: `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs` (line 200)
- No spec deltas: these validation messages are not quoted in any durable specification, and
  behavior is unchanged.
- No configuration, layout, or validation-order change.

## Non-goals

- Changing the duplicate-folder messages, which differ between the two pages by design
  (`already watched` vs. `already registered`)
- Revising any other error message, placeholder, or button label
- Adding a manual smoke task: this change only replaces string literals in two view models; the
  error-line binding and layout are unchanged, and the exact strings are covered by the view-model
  test.
