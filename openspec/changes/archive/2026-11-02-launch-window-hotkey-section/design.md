## Context

The Settings window has eleven pages organized under three groups ("General", "Engines & Repos", "System"). The "Global Hotkey" page under "General" contains only the `ACTIVATION` section header and the global hotkey capture field (verified at `src/CLIHub/Views/SettingsHotkeysPage.xaml:10-34`). The "Launch Window" page under "General" has `POSITION` (launcher window position selector) and `PROJECT PATHS` (path display style selector) sections (verified at `src/CLIHub/Views/SettingsLaunchWindowPage.xaml:11-43`).

The global hotkey's purpose is to show the launch window, making the "Launch Window" page its natural location. The dedicated "Global Hotkey" page was appropriate when additional activation settings were anticipated, but with a single control, it adds navigation depth without benefit.

**Verified current state:**
- `src/CLIHub/Views/SettingsHotkeysPage.xaml:10-34` — `ACTIVATION` section header + `SettingCard` with title "Global hotkey", hint text, `Keyboard` icon, and the `HotkeyField` Border (x:Name, handlers, ItemsControl of `HotkeyParts` with chip rendering)
- `src/CLIHub/Views/SettingsHotkeysPage.xaml.cs:9-101` — `SettingsHotkeysPage : UserControl` with handlers `HotkeyField_MouseLeftButtonDown` (focus), `HotkeyField_PreviewKeyDown` (capture logic with validation), and private static helpers `IsModifierKey(Key)` and `ToHotkeyModifiers(ModifierKeys)` — uses `CLIHub.Core.Hotkeys` types and `SettingsViewModel.SetHotkeyError`/`SetCapturedHotkey`
- `src/CLIHub/Views/SettingsLaunchWindowPage.xaml:1-45` — StackPanel with `POSITION` section (line 11), launcher position card (lines 12-26), `PROJECT PATHS` section (line 28), path display card (lines 29-43)
- `src/CLIHub/Views/SettingsLaunchWindowPage.xaml.cs:1-14` — `SettingsLaunchWindowPage : UserControl` with only the constructor (line 13)
- `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:49-60` — `hotkeys` entry (lines 49-54: key "hotkeys", title "Global Hotkey", description "The global hotkey used to show the launcher.", group "General", icon `IconGlyphs.KeyboardShortcut`); `launch-window` entry (lines 55-60: key "launch-window", title "Launch Window", description "Where the launch window appears and how project paths are shortened in it.", group "General", icon `IconGlyphs.OpenInNewWindow`)
- `src/CLIHub/Views/SettingsWindow.xaml:505` — `<views:SettingsHotkeysPage Visibility="{Binding SelectedPage.Key, Converter={StaticResource PageKeyVisibility}, ConverterParameter=hotkeys}"/>`; line 507 renders `SettingsLaunchWindowPage` with `ConverterParameter=launch-window`
- `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs:26` — asserts 11 pages; line 29 lists keys including `"hotkeys"`, `"launch-window"`; line 39 asserts hotkeys title "Global Hotkey"; line 75 asserts slot tuple `("hotkeys", null, null, null)`
- `openspec/specs/preferences-ui/spec.md:667-714` — requirement "Settings sections and pages" with scenarios "Sections shown" (line 671: eleven pages in order including "Global Hotkey"), "Launch Window page in the sidebar" (line 682: appears immediately after "Global Hotkey"), "Existing controls on their pages" (line 700: global-hotkey capture field on "Global Hotkey")

## Goals / Non-Goals

**Goals:**
- Consolidate activation-related settings on one page
- Reduce Settings navigation from eleven to ten pages
- Keep the hotkey capture logic and UI unchanged (same handlers, same validation, same styling)
- Maintain correct sidebar ordering: "General & Startup", "Launch Window", then "Projects"

**Non-Goals:**
- No changes to hotkey capture behavior, validation logic, error messages, or defaults
- No changes to SettingsViewModel's public API, command bindings, or configuration persistence
- No visual restyling or new abstractions
- No README/CHANGELOG updates (consistent with recent UI-only commits)

## Decisions

### Decision 1: Move the entire `ACTIVATION` block to `SettingsLaunchWindowPage.xaml`

**Chosen:** Copy the `ACTIVATION` section header and the hotkey capture `SettingCard` (lines 10-34 from `SettingsHotkeysPage.xaml`) into `SettingsLaunchWindowPage.xaml` as the first block inside the StackPanel, above the existing `POSITION` section.

**Rationale:** The hotkey activates the launch window, so grouping it with other launch-window settings is semantically correct. Keeping the XAML structure identical (same x:Name, same handlers, same styles, same ItemsControl) ensures the control works without any binding or style changes.

**Alternatives considered:**
- Extract the hotkey field into a shared UserControl: Rejected — adds abstraction for one use, and code-behind state (focus, event handlers) would complicate the extraction
- Keep the page as-is: Rejected — adds unnecessary navigation depth for one control

**Evidence:** `SettingsHotkeysPage.xaml:10-34` shows the complete `ACTIVATION` block structure; `SettingsLaunchWindowPage.xaml:10` is a StackPanel that can accept the prepended block.

### Decision 2: Move code-behind handlers verbatim

**Chosen:** Copy the three methods (`HotkeyField_MouseLeftButtonDown`, `HotkeyField_PreviewKeyDown`, and the two private static helpers `IsModifierKey` and `ToHotkeyModifiers`) verbatim from `SettingsHotkeysPage.xaml.cs` (lines 19-100) into `SettingsLaunchWindowPage.xaml.cs`, preserving the exact logic, comments, variable names, and signatures.

**Rationale:** The capture logic is page-local (focuses the field, validates, calls ViewModel methods); it does not depend on the page class name. Reusing it verbatim avoids introducing bugs and keeps the validation behavior identical. The XAML already names the handlers, so they must exist in the new code-behind.

**Alternatives considered:**
- Create a shared static helper class: Rejected — the handlers are instance methods that reference `this.HotkeyField` and `this.DataContext`; extracting them would complicate the abstraction for no multi-page reuse
- Refactor into a behavior or attached property: Rejected — over-engineered for a one-page move

**Evidence:** `SettingsHotkeysPage.xaml.cs:19-100` shows the handlers use `this.HotkeyField`, `this.DataContext`, and `SettingsViewModel` casting.

### Decision 3: Delete the `hotkeys` page and its catalog entry

**Chosen:** Delete `SettingsHotkeysPage.xaml`, `SettingsHotkeysPage.xaml.cs`, remove the `hotkeys` entry from `SettingsPageCatalog.AllPagesData` (lines 49-54 in `SettingsNavigation.cs`), and remove the `<views:SettingsHotkeysPage>` line from `SettingsWindow.xaml` (line 505).

**Rationale:** The page is now empty and serves no purpose. Removing it from the catalog eliminates it from the sidebar; removing it from `SettingsWindow.xaml` stops the view from being instantiated.

**Alternatives considered:**
- Keep the page as a placeholder: Rejected — an empty page is dead code
- Repurpose it for other hotkeys: Deferred — not in scope for this change

**Evidence:** `SettingsNavigation.cs:49-54` is the `hotkeys` entry; `SettingsWindow.xaml:505` renders the page conditionally.

### Decision 4: Update the `launch-window` page description

**Chosen:** Change the `launch-window` description (currently "Where the launch window appears and how project paths are shortened in it.") to "The global hotkey, where the launch window appears, and how project paths are shortened in it."

**Rationale:** The page now owns three concerns (hotkey, position, path display); the description should reflect that. Keeping it one line maintains consistency with other page descriptions. The hotkey is listed first because it will be the first section on the page.

**Alternatives considered:**
- Keep the old description: Rejected — misleading, as the page now has the hotkey
- Use "Activation, window position, and path display": Rejected — "Activation" is less concrete than "The global hotkey"

**Evidence:** `SettingsNavigation.cs:58` holds the current description for `launch-window`.

### Decision 5: Sidebar ordering after removal

**Chosen:** After removing the `hotkeys` entry, the sidebar will show ten pages: "General & Startup", "Launch Window", "Projects", "Project Scanning", "CLI Agents", "Agent Detection", "Terminal Profiles", "Theme & Accent", "Logs", "Updates". "Launch Window" is now the first page under "General" after "General & Startup".

**Rationale:** The `AllPagesData` array defines sidebar order; removing the `hotkeys` entry (lines 49-54) shifts subsequent entries up. No explicit reordering is needed.

**Alternatives considered:** None — array order determines display order.

**Evidence:** `SettingsNavigation.cs:41-100+` defines the page array; `SettingsNavigationTests.cs:29-30` asserts the key order.

### Decision 6: Update XML doc on `SettingsLaunchWindowPage`

**Chosen:** Update the class XML doc from "Settings page for the launch window: launcher positioning and path display." to "Settings page for the launch window: hotkey capture, launcher positioning, and path display."

**Rationale:** The class now owns three concerns; the doc should list them.

**Evidence:** `SettingsLaunchWindowPage.xaml.cs:5-7` holds the current XML doc.

## Risks / Trade-offs

**[Risk] XAML copy-paste error** → Mitigation: the XAML block is small (25 lines), self-contained, and uses existing styles/icons; validation includes a build check.

**[Risk] Namespace or using statement mismatch in code-behind** → Mitigation: the moved code uses `System.Windows.Input`, `CLIHub.Core.Hotkeys`, and `CLIHub.ViewModels`, which are standard; the task will add the necessary `using` directives.

**[Trade-off] Code duplication if a second hotkey page is added later** → Trade-off accepted: current scope is a single hotkey; extraction can happen if a second use appears.

**[Trade-off] Test update churn** → Acceptable: three test changes (page count, key list, slot tuple removal) are straightforward and deterministic.

## Migration Plan

1. **Prepend the `ACTIVATION` block to `SettingsLaunchWindowPage.xaml`**: copy lines 10-34 from `SettingsHotkeysPage.xaml` (section header + card) and insert them as the first children of the StackPanel, before the existing `POSITION` section header (current line 11).
2. **Add code-behind handlers to `SettingsLaunchWindowPage.xaml.cs`**: copy the three handler methods and two private static helpers (lines 19-100 from `SettingsHotkeysPage.xaml.cs`) into the `SettingsLaunchWindowPage` class body; add the necessary `using` statements (`System.Windows.Input`, `CLIHub.Core.Hotkeys`, `CLIHub.ViewModels`); update the class XML doc to list all three concerns.
3. **Delete the `hotkeys` page files**: delete `src/CLIHub/Views/SettingsHotkeysPage.xaml` and `src/CLIHub/Views/SettingsHotkeysPage.xaml.cs`.
4. **Remove the `hotkeys` catalog entry**: delete lines 49-54 (the `hotkeys` entry) from `SettingsPageCatalog.AllPagesData` in `SettingsNavigation.cs`; update the `launch-window` description (line 58) to the new text.
5. **Remove the page from `SettingsWindow.xaml`**: delete line 505 (the `<views:SettingsHotkeysPage>` element).
6. **Update tests**: change the count assertion from 11 to 10 (line 26); remove `"hotkeys"` from the key list (line 29); remove the hotkeys title assertion (line 39); remove the `("hotkeys", null, null, null)` tuple (line 75).
7. **Build and verify**: `dotnet build CLIHub.sln` with 0 errors/warnings; `dotnet test CLIHub.sln` passes.

Rollback: revert all file changes; no persisted state changes.

## Open Questions

- **Should the `ACTIVATION` section be renamed to something else (e.g. `GLOBAL HOTKEY`)?** → No: `ACTIVATION` is semantically accurate and consistent with existing section naming (uppercase, one-word or compound nouns).
- **Should the hotkey be moved to "General & Startup" instead?** → No: the hotkey activates the launch window specifically, not general startup behavior; "Launch Window" is the correct page.
- **Will this affect keyboard focus or tab order?** → No: the WPF default tab order is document order, which is already correct (hotkey field first, then position combo, then path combo).
