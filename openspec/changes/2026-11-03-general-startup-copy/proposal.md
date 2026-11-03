# Revise the General & Startup page copy

## Why

The Settings → General & Startup page contains three strings that are redundant and unnecessarily
verbose. The page description (`Startup behavior and how the window opens.`) repeats "startup" from
the page title and says the same thing twice ("behavior" and "how the window opens"). The first
hint (`Launch CLIHub automatically when you sign in to Windows.`) restates what "Start with Windows"
already means. The second hint (`Start CLIHub minimized in the system tray instead of showing the window.`)
restates what "Start minimized to tray" already means.

The user reviewed an HTML mockup (`settings-general-startup-copy.html`, variant A) and approved
shorter, fact-focused replacements: the page description now states what the page covers ("How CLIHub
starts and opens"), and the hints now state facts the titles lack (the registration is per-user only,
and minimized mode starts in the tray with no window).

**Word count reduction:** 28 words → 18 words (−36%).

## What changes

Replace three UI strings:

1. **Page description** (sidebar + header): `Startup behavior and how the window opens.` →
   `How CLIHub starts and opens.` (SettingsNavigation.cs:46)
2. **"Start with Windows" hint**: `Launch CLIHub automatically when you sign in to Windows.` →
   `Applies to this Windows account only.` (SettingsGeneralPage.xaml:12)
3. **"Start minimized to tray" hint**: `Start CLIHub minimized in the system tray instead of showing the window.` →
   `Starts in the tray without a window.` (SettingsGeneralPage.xaml:19)

Do not change:
- Page title "General & Startup" (sidebar + header)
- Card titles "Start with Windows" and "Start minimized to tray"
- Card order, icons, bindings, IsChecked modes, or XAML layout
- Window chrome, footer Save/Cancel, SettingsViewModel properties, configuration keys, or behavior
- The page is not renamed (a rename was considered and rejected by the user)

## Impact

- UI: `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs` (line 46),
  `src/CLIHub/Views/SettingsGeneralPage.xaml` (lines 12, 19)
- No tests pin these strings: `tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs` validates only
  page keys and template keys, not display text.
- No spec deltas: these UI strings are not quoted in any durable specification; the underlying facts
  (per-user HKCU Run registration, minimized-to-tray behavior) are documented in
  `openspec/specs/app-lifecycle/spec.md` under "Start with Windows" and "Startup window visibility"
  requirements.
- No documentation changes: `README.md` does not quote these strings.
- No configuration, layout, or behavior change.

## Non-goals

- Changing the two card titles ("Start with Windows", "Start minimized to tray"), which remain unchanged
- Revising any other Settings page copy
- Renaming the page (kept as "General & Startup")
- Changing layout, icons, bindings, or behavior
