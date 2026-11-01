# Design: Launch Window Settings Page

## Verified current state

### Settings page catalog

The catalog in `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:41-110` currently defines 10 pages in this order:

1. `general` — "General & Startup", group `General` (line 43)
2. `hotkeys` — "Hotkeys & Quick Launcher", description "The global hotkey and where the launcher appears.", group `General` (line 50)
3. `projects-registry` — "Projects", group `Engines & Repos` (line 56)
4. `projects` — "Project Scanning", group `Engines & Repos` (line 62)
5. `agents` — "CLI Agents", group `Engines & Repos` (line 71)
6. `agent-detection` — "Agent Detection", group `Engines & Repos` (line 78)
7. `terminal` — "Terminal Profiles", group `Engines & Repos` (line 85)
8. `appearance` — "Appearance", description "Theme, accent color, and how project paths are shown.", group `System` (line 91)
9. `telemetry` — "Logs", group `System` (line 97)
10. `updates` — "Updates", group `System` (line 103)

The `SettingsPage` record (lines 26-34) includes `Key`, `Title`, `Description`, `Group`, `Icon`, and optional slot keys.

### View-model properties

The two bound properties remain on `SettingsViewModel` (`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`):

- `PathDisplays` (line 178) and `SelectedPathDisplay` (line 207) — for the path display control
- `PopupPositions` (line 471) and `SelectedPopupPosition` (line 476) — for the launcher window position control

Both are public properties that bind to the respective XAML controls. They remain where they are.

### Page routing in SettingsWindow.xaml

The page stack in `src/CLIHub/Views/SettingsWindow.xaml:502-522` hosts each page view with a `Visibility` binding that uses the `PageKeyVisibility` converter with the page key as `ConverterParameter`. Each page is a `UserControl` that is shown when its key matches `SelectedPage.Key`.

### Hotkeys page XAML

`src/CLIHub/Views/SettingsHotkeysPage.xaml:10-53` contains two sections:

1. `ACTIVATION` section (lines 11-35) with the "Global hotkey" card
2. `LAUNCHER POSITIONING` section (lines 37-52) with the "Launcher window position" card bound to `PopupPositions` and `SelectedPopupPosition`

The launcher positioning block is lines 37-52 verbatim.

### Appearance page XAML

`src/CLIHub/Views/SettingsAppearancePage.xaml:10-225` contains two sections:

1. `THEME` section (lines 11-207) with "Application theme" and "Accent color" cards
2. `PATHS` section (lines 209-224) with the "Path display" card bound to `PathDisplays` and `SelectedPathDisplay`

The path display block is lines 209-224 verbatim.

### Page code-behind convention

`src/CLIHub/Views/SettingsGeneralPage.xaml.cs:1-14` shows the thin code-behind pattern: a `partial class` with a parameterless constructor that calls `this.InitializeComponent()`, and an XML doc comment. All Settings page code-behind files follow this convention.

### Test assertions

`tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs:19-31` asserts 10 pages in the current order (line 26), and the slot-keys tuple (lines 67-86) lists the 10 pages with their slot keys.

### README caption

`README.md:36` currently reads:

```
*Settings → Appearance: theme, accent color, and path display. Theme and accent changes apply immediately after Save, and the System theme follows the Windows light/dark mode live.*
```

The screenshot table in `docs/images/README.md:12` describes `settings-appearance.png` as "Settings → Appearance (theme, accent color, and path display)."

### OpenSpec requirements

The relevant requirements in `openspec/specs/preferences-ui/spec.md` are:

- "Path display preference" (lines 129-156) — currently says the control appears on "Appearance"
- "Popup screen position" (lines 249-269) — currently says the control appears on "Hotkeys & Quick Launcher" under "LAUNCHER POSITIONING"
- "Application theme selector" (lines 271-289) — says the control appears on "Appearance"
- "Accent color selector" (lines 296-326) — says the control appears on "Appearance"
- "Settings sections and pages" (lines 653-693) — describes 10 pages including "Appearance" with theme, accent, and path display

## Decisions

### New page placement

The new `launch-window` page is inserted after `hotkeys` and before `projects-registry` in the catalog order, keeping the General pages together. The page key is `launch-window` (kebab-case, following the existing `agent-detection` and `projects-registry` precedent).

### Icon choice

`IconGlyphs.Equalizer` is used for the launch-window page icon because it visually suggests positioning/levels and is distinct from the other icons in the General group (Settings and Keyboard). The Equalizer glyph is already in the Segoe MDL2 Assets font used throughout the application.

### Page description

The description "Where the launch window appears and how project paths are shortened in it." is a single sentence (following the catalog convention) and explicitly names both concerns on the page.

### Hotkeys page rename

The "Hotkeys & Quick Launcher" title becomes "Global Hotkey" because the page now contains only the global hotkey capture field. The description becomes "The global hotkey used to show the launcher." (single sentence, no mention of positioning).

### Appearance page rename

The "Appearance" title becomes "Theme & Accent" to accurately reflect that it now contains only theme and accent controls. The description becomes "Theme and accent color." (single sentence, parallel structure to the other System pages).

### XAML block moves

The two moved blocks are copied verbatim:
- `SettingsHotkeysPage.xaml:37-52` (the `LAUNCHER POSITIONING` section header and the "Launcher window position" card) moves to the new page.
- `SettingsAppearancePage.xaml:209-224` (the `PATHS` section header and the "Path display" card) moves to the new page.

Both bindings (`SelectedPopupPosition` / `PopupPositions` and `SelectedPathDisplay` / `PathDisplays`) remain unchanged — they still resolve to the same view-model properties.

### No slot keys

The new page has no header title extra, header action, or summary row, so all three slot keys are `null` in the catalog entry.

### Test update scope

The navigation tests update:
- The page count assertion changes from 10 to 11 (line 26).
- The page key order assertion adds `launch-window` between `hotkeys` and `projects-registry` (line 29).
- The hotkeys title assertion changes from "Hotkeys & Quick Launcher" to "Global Hotkey" (line 39).
- The slot-keys tuple adds a row for `launch-window` with all-null slots (between `hotkeys` and `projects-registry` in lines 72-86).

No new behavior tests are needed — the page simply hosts existing controls with unchanged bindings.

### Documentation updates

The README caption (line 36) and the `docs/images/README.md` table row (line 12) are updated to describe the Appearance page as showing only theme and accent color, and mention the new Launch Window page. The actual screenshot refresh is out of scope and follows `docs/releasing.md` step 0.

## Alternatives considered and rejected

### Option B: A "Paths" page with only the path display control

Rejected because a page with only one preference feels sparse, and because launcher positioning and path display both describe the launch window — splitting them would leave one launch-window concern on the hotkey page and the other on a page of its own.

### Option C: Keep path display on the hotkeys page

Rejected because path display is a launch-window concern, not a hotkey concern.

### Moving view-model properties to a dedicated section

Not done. The properties remain on `SettingsViewModel` to avoid introducing a new section object and changing the existing binding surface. The XAML simply rebinds to the same properties.

## Open questions

None. All decisions have been made per the approved brief.

## Deviations from brief

None. The design follows the approved decisions exactly.
