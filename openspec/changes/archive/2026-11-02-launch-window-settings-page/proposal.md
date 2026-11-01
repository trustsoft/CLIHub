# Proposal: Launch Window Settings Page

## Why

The Settings Appearance page currently mixes three distinct concerns: theme/accent color (visual appearance), path display format (launch window text presentation), and launcher window positioning (where the launch window opens). The launcher positioning control was previously on the Hotkeys page, creating further conceptual split. Users looking for launch-window behavior now must check two pages, and the "Appearance" label under-represents its actual scope.

This change splits the launch-window-specific preferences (path display and launcher positioning) onto their own page under the General group, leaving theme and accent color on a renamed "Theme & Accent" page. This groups related settings, makes the page titles accurately describe their contents, and places launch-window behavior next to the other General preferences (startup toggles and global hotkey).

## What changes

### New Settings page

A new "Launch Window" page under the General group, inserted after "Global Hotkey" and before "Projects":

- **Key**: `launch-window`
- **Title**: `Launch Window`
- **Group**: `General`
- **Description**: `Where the launch window appears and how project paths are shortened in it.`
- **Icon**: `IconGlyphs.Equalizer`
- No header slots

The page body contains two sections:
1. `LAUNCHER POSITIONING` section header + the "Launcher window position" card (moved from SettingsHotkeysPage.xaml:37-52)
2. `PATHS` section header + the "Path display" card (moved from SettingsAppearancePage.xaml:209-224)

### Renamed pages

- **Hotkeys & Quick Launcher** → **Global Hotkey**
  - Key stays `hotkeys` (no routing change)
  - Description updated to reflect that only the global hotkey is on this page
  - Loses the `LAUNCHER POSITIONING` section

- **Appearance** → **Theme & Accent**
  - Key stays `appearance` (no routing change)
  - Description updated to `Theme and accent color.`
  - Loses the `PATHS` section

### New files

- `src/CLIHub/Views/SettingsLaunchWindowPage.xaml` and `.xaml.cs`
- `openspec/changes/launch-window-settings-page/specs/preferences-ui/spec.md` (delta)

### Modified files

- `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs` — catalog entry for the new page, and title/description updates for `hotkeys` and `appearance`
- `src/CLIHub/Views/SettingsWindow.xaml` — page routing for `launch-window`
- `src/CLIHub/Views/SettingsHotkeysPage.xaml` — remove `LAUNCHER POSITIONING` section
- `src/CLIHub/Views/SettingsAppearancePage.xaml` — remove `PATHS` section
- `src/CLIHub/Views/SettingsAppearancePage.xaml.cs` — refresh the stale XML doc comment
- `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs` — 11 pages, new order, slot assertions
- `tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs` — page-count guard 10 → 11
- `README.md` — Appearance screenshot caption and alt text reflect theme + accent only
- `docs/images/README.md` — screenshot table row and capture description updated to the new page names
- `docs/releasing.md` — capture description uses the new page name
- `docs/architecture/configuration.md` — `popupScreenPosition` attributed to the Launch Window page

## Impact

### User-facing

- Settings sidebar now shows 11 pages instead of 10
- Users looking for path display or launcher position settings will find them grouped under "Launch Window" in the General section, rather than split between "Appearance" and "Hotkeys & Quick Launcher"
- "Appearance" is now "Theme & Accent" (same key, same group)
- "Hotkeys & Quick Launcher" is now "Global Hotkey" (same key, same group)

### Code

- No configuration schema changes — `SelectedPathDisplay` and `SelectedPopupPosition` stay bound to the existing view-model properties
- No persistence changes — `config.json` keys remain unchanged
- No behavior changes — `PreferenceApplier`, theme/accent application, popup positioning, and path display all stay as-is
- View-model properties remain on `SettingsViewModel` (not moved)
- Navigation catalog grows by one entry; test assertions update to 11 pages

### Documentation

- README screenshot caption for Appearance will be outdated until the next release-prep screenshot refresh (step 0 in `docs/releasing.md`)
- The screenshot itself is out of scope for this change; it will be regenerated as part of the release process

## Explicit non-goals

- Re-capturing `docs/images/settings-appearance.png` — out of scope; release prep refreshes screenshots (see `docs/releasing.md`, step 0)
- Changing configuration keys or persistence format — no data migration
- Moving view-model properties — `SelectedPathDisplay` and `SelectedPopupPosition` stay on `SettingsViewModel`
- Adding new preferences or changing existing behavior

## Dependencies

None. This change is a pure UI reorganization with no dependencies on other changes.
