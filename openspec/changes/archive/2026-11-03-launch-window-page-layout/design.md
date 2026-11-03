# Design: Launch Window Settings Page Layout Revision

## Current State (HEAD 41a040a)

### XAML Structure (`src/CLIHub/Views/SettingsLaunchWindowPage.xaml`)

The page has three section headers, each followed by one SettingCard:

```xaml
10:     <StackPanel>
11:         <controls:SectionHeader Text="ACTIVATION"/>
12:         <controls:SettingCard Title="Global hotkey"
13:                            Hint="Press a combination. Esc cancels capture."
14:                            Icon="{x:Static themes:IconGlyphs.Keyboard}">
15:             <Border x:Name="HotkeyField"
...
35:         </controls:SettingCard>
36: 
37:         <controls:SectionHeader Text="POSITION"/>
38:         <controls:SettingCard Title="Launcher window position"
39:                            Hint="Active monitor is the one under the pointer."
40:                            Icon="{x:Static themes:IconGlyphs.Location}">
...
52:         </controls:SettingCard>
53: 
54:         <controls:SectionHeader Text="PROJECT PATHS"/>
55:         <controls:SettingCard Title="Path display"
56:                            Hint="Long paths are shortened in project rows."
57:                            Icon="{x:Static themes:IconGlyphs.FolderOpen}">
...
69:         </controls:SettingCard>
70:     </StackPanel>
```

The `SectionHeader` style (`src/CLIHub/Themes/SettingsStyles.xaml:155-182`) applies `Margin="0,18,0,8"` (18 px top, 8 px bottom). Each standalone `SettingCard` has default `Margin="0,0,0,8"` (`SettingsStyles.xaml:186`).

The grouped-card pattern exists in `src/CLIHub/Views/SettingsUpdatesPage.xaml:10-47`: one `<controls:SettingsGroup Margin="0,18,0,8">` container holding four `Grouped="True"` `SettingCard` elements. The `SettingsGroup` style (`SettingsStyles.xaml:301-338`) provides the border and background, and its `ItemContainerStyle` adds hairline separators between rows (none on the first).

### Page Description (`src/CLIHub/ViewModels/Settings/SettingsNavigation.cs`)

```csharp
49:         new(
50:             "launch-window",
51:             "Launch Window",
52:             "Global hotkey, launcher position, and path display.",
53:             "General",
54:             IconGlyphs.OpenInNewWindow),
```

This description is unchanged per the user's explicit rejection of changing it.

### Approved Mockup Evidence (`launch-window-settings-mockup.html`)

The proposed layout (lines 693-737) shows one `settings-group` with three `setting-row` elements and hairline separators. The renames table (lines 890-907) lists:
- `Global hotkey` → `Launcher shortcut`
- `Launcher window position` → `Launcher placement`
- `Path display` → `Project path shortening`

The hints (lines 699, 714, 728) become:
- `Global hotkey. Press a combination; Esc cancels capture.`
- `Where the window opens. The active monitor follows the pointer.`
- `How long paths are shortened in project rows.`

The mockup's proposed view and its notes both keep the page description `Global hotkey, launcher position, and path display.`, matching the user's explicit decision that it stays unchanged.

## Design Decisions

### 1. Vertical Rhythm After Removing Section Headers

The first `SectionHeader` style applies `Margin="0,18,0,8"`. Removing it requires moving the 18 px top margin to the first group. Set `Margin="0,18,0,8"` on the `SettingsGroup` so it starts 18 px below the page divider.

This mirrors the archived `2026-11-03-theme-accent-copy` pattern (`design.md:88-92`), which also removed section headers and moved the 18 px top margin to the first group.

### 2. Grouped SettingsGroup Structure

The Updates page (`SettingsUpdatesPage.xaml:10-47`) establishes the pattern:

```xaml
<controls:SettingsGroup Margin="0,18,0,8">
    <controls:SettingCard ... Grouped="True">...</controls:SettingCard>
    <controls:SettingCard ... Grouped="True">...</controls:SettingCard>
    ...
</controls:SettingsGroup>
```

**Decision:** Apply the same pattern to the Launch Window page. The `SettingsGroup` provides the border and background; each child `SettingCard` has `Grouped="True"` to disable its own border and corner radius. The `SettingsGroup.ItemContainerStyle` adds hairline separators between rows (none on the first).

**Alternative rejected:** Keeping standalone cards with no grouping would preserve the visual separation but would not match the approved mockup's single-surface composition.

### 3. Setting Title and Hint Revisions

The approved mockup renames the settings and adjusts hints to be self-describing without section context:

| Current Title | New Title | Current Hint | New Hint |
|---|---|---|---|
| Global hotkey | Launcher shortcut | Press a combination. Esc cancels capture. | Global hotkey. Press a combination; Esc cancels capture. |
| Launcher window position | Launcher placement | Active monitor is the one under the pointer. | Where the window opens. The active monitor follows the pointer. |
| Path display | Project path shortening | Long paths are shortened in project rows. | How long paths are shortened in project rows. |

**Decision:** Use the exact approved text. The new hints incorporate context that the removed section headers used to provide.

**Alternative rejected:** Keeping the old hints would lose the context that ACTIVATION, POSITION, and PROJECT PATHS used to supply, making the hints less clear on a flat single-surface page.

### 4. HotkeyField Control Structure Preservation

The HotkeyField (lines 15-34) has `MouseLeftButtonDown` and `PreviewKeyDown` event handlers and an `ItemsControl` binding to `HotkeyParts`. This structure must remain unchanged because it implements the capture behavior.

**Decision:** Move the entire `<Border x:Name="HotkeyField" ... </Border>` block (lines 15-34) into the first grouped SettingCard's content without modification.

### 5. Page Description Unchanged

The user explicitly rejected changing the page description (`Global hotkey, launcher position, and path display.`). The mockup shows a proposed new description, but the brief's NON-GOALS clause states: "Keep the page description in `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs` EXACTLY as-is: `Global hotkey, launcher position, and path display.` The user explicitly rejected changing it."

**Decision:** Do not modify `SettingsNavigation.cs:52`. Document this as an explicit non-change.

### 6. Spec Delta Scope

The `preferences-ui` spec has three requirements affected by this change:

1. **Path display preference** (lines 129-157): Pins the "PROJECT PATHS" section header (line 132) and the old control labels. Must be updated to reflect the new single-group layout and new labels.

2. **Popup screen position** (lines 250-278): Pins the "POSITION" section header (line 252) and the old label `Launcher window position` (line 253). Must be updated to reflect the new single-group layout and new label.

3. **Settings sections and pages** (lines 661-713): The `Existing controls on their pages` scenario (line 696) names the three section headers and old labels. The `Hotkey section first on Launch Window` scenario (lines 709-712) names the three sections in order. These must be updated to remove section-header language and adopt the new labels, while keeping the requirement name and scenario names byte-for-byte.

**Decision:** Write MODIFIED deltas for all three requirements. Preserve every original requirement name and scenario name exactly. Change only the body text needed to remove section-header language and adopt the new labels and single-group layout.

### 7. Test Impact

The test file `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs` does not assert setting titles, hints, or section headers. It tests preference persistence and application behavior, which are unchanged.

**Decision:** No test changes required. The layout and copy changes are presentation concerns without testable business logic beyond the existing persistence tests.

### 8. README Screenshot

The existing screenshot at `docs/images/settings-launch-window.png` (if it exists) will become stale. The user's brief notes that release-preparation owns screenshot refresh, and AGENTS.md specifies that a release sequence starts with `Capture-ReadmeImages.ps1` before tagging.

**Decision:** Do not refresh screenshots in this change. The screenshot will be updated in the release-preparation sequence.

## Open Questions

None. All copy and layout are approved, and the grouped-card pattern is already established.

## Deviations

None. The design follows the approved mockup exactly, including the explicit non-change to the page description per the user's rejection.
