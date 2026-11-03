# Design: revise the General & Startup page copy

## Current state (verified)

Three strings on the Settings → General & Startup page:

| Location | Line | Current text |
| --- | --- | --- |
| `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs` | 46 | `"Startup behavior and how the window opens."` (page Description) |
| `src/CLIHub/Views/SettingsGeneralPage.xaml` | 12 | `"Launch CLIHub automatically when you sign in to Windows."` (first SettingCard Hint) |
| `src/CLIHub/Views/SettingsGeneralPage.xaml` | 19 | `"Start CLIHub minimized in the system tray instead of showing the window."` (second SettingCard Hint) |

Page metadata: `SettingsNavigation.cs:43-48` defines the "general" page with key `"general"`, title
`"General & Startup"`, and the Description at line 46.

XAML cards: `SettingsGeneralPage.xaml` has two `SettingCard` controls:
- Line 10: `Title="Start with Windows"`, `Hint` at line 12, `Icon="{x:Static themes:IconGlyphs.Power}"`,
  `CheckBox` bound to `StartWithWindows` (TwoWay)
- Line 18: `Title="Start minimized to tray"`, `Hint` at line 19, `Icon="{x:Static themes:IconGlyphs.ChevronDown}"`,
  `CheckBox` bound to `StartMinimized` (TwoWay)

Verification against specs:
- `openspec/specs/app-lifecycle/spec.md:92-111` documents "Start with Windows" requirement:
  per-user HKCU Run registration, no admin rights required.
- `openspec/specs/app-lifecycle/spec.md:112-123` documents "Startup window visibility" requirement:
  minimized-to-tray mode starts the app in the tray with no window, while remaining reachable from
  the tray, the hotkey, and a subsequent launch.

The new hints' stated facts are true:
1. "Applies to this Windows account only" — confirmed: HKCU registration is per-user, no elevated
   rights.
2. "Starts in the tray without a window" — confirmed: `openspec/specs/app-lifecycle/spec.md:120-122`
   scenario "Start in the system tray" says "the launch window is not shown and the application runs
   in the system tray, still reachable from the tray, the hotkey, and a subsequent launch."

No durable spec quotes any of these three strings: grep of `openspec/specs` for
`Launch CLIHub automatically`, `system tray instead of`, `Startup behavior`, and
`how the window opens` returns nothing.

No test pins these strings: `tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs` validates page
keys and template keys only, not display text.

`README.md` does not quote these strings: grep returns nothing.

## Approved copy changes

The user approved the HTML mockup `settings-general-startup-copy.html` (variant A). The three
replacements from that mockup:

| # | Where | Before | After | Rationale |
| --- | --- | --- | --- | --- |
| 1 | Page description (SettingsNavigation.cs:46) | `Startup behavior and how the window opens.` | `How CLIHub starts and opens.` | 42 → 28 characters (−33%). Instead of repeating "startup" (already in the page title) and restating the same thing twice ("behavior" and "how the window opens"), the revised description states what the page covers in five words. |
| 2 | First card hint (SettingsGeneralPage.xaml:12) | `Launch CLIHub automatically when you sign in to Windows.` | `Applies to this Windows account only.` | 56 → 37 characters (−34%). Instead of restating what "Start with Windows" already means, the hint now states a fact the title does not: this registration is per-user only (no admin rights, no all-users impact). |
| 3 | Second card hint (SettingsGeneralPage.xaml:19) | `Start CLIHub minimized in the system tray instead of showing the window.` | `Starts in the tray without a window.` | 72 → 36 characters (−50%). Instead of restating what "Start minimized to tray" already means, the hint now states what minimized mode does: starts in the tray with no window visible. |

Total: 28 words → 18 words (−36%).

## Code changes

### 1. SettingsNavigation.cs, line 46 (page Description)

Before:
```csharp
"Startup behavior and how the window opens.",
```

After:
```csharp
"How CLIHub starts and opens.",
```

### 2. SettingsGeneralPage.xaml, line 12 (first SettingCard Hint)

Before:
```xml
Hint="Launch CLIHub automatically when you sign in to Windows."
```

After:
```xml
Hint="Applies to this Windows account only."
```

### 3. SettingsGeneralPage.xaml, line 19 (second SettingCard Hint)

Before:
```xml
Hint="Start CLIHub minimized in the system tray instead of showing the window."
```

After:
```xml
Hint="Starts in the tray without a window."
```

## Decisions

### Why hints now carry facts the titles lack

The current hints restate what the titles already mean ("Start with Windows" → "Launch CLIHub
automatically when you sign in to Windows"; "Start minimized to tray" → "Start CLIHub minimized in
the system tray instead of showing the window"). This is redundant and wastes space.

The revised hints state facts the titles do not: the registration is per-user only, and minimized
mode starts in the tray with no window. These facts are drawn from the existing specifications
(`app-lifecycle/spec.md`) and answer questions users might have before toggling the settings.

### Why the titles stay unchanged

The two card titles ("Start with Windows", "Start minimized to tray") are concise, accurate, and
recognizable. The user explicitly approved keeping them as-is. Changing them was not part of the
approved mockup.

### Why the page title stays "General & Startup"

The user reviewed whether to rename the page and explicitly rejected it. The page title remains
"General & Startup" in both the sidebar and the page header.

## Rejected / deliberately unchanged

- **Page title**: "General & Startup" remains unchanged (sidebar + header).
- **Card titles**: "Start with Windows" and "Start minimized to tray" remain unchanged.
- **Card order, icons, bindings**: The two cards, their order, their icons (`IconGlyphs.Power`,
  `IconGlyphs.ChevronDown`), their CheckBox bindings (`StartWithWindows`, `StartMinimized`), and
  their IsChecked modes (TwoWay) are unchanged.
- **XAML layout**: No change to `StackPanel`, `SettingCard` layout, or `CheckBox` styles.
- **Window chrome, footer Save/Cancel**: No change to the Settings window chrome or footer controls.
- **SettingsViewModel properties, configuration keys, behavior**: No change to `StartWithWindows`,
  `StartMinimized` properties, their configuration keys, or the underlying behavior (HKCU registration,
  minimized-to-tray startup).
- **Other Settings pages**: No other Settings page is touched.

## Specification impact

None. No durable specification under `openspec/specs/` quotes these UI strings. Behavior is unchanged
(same HKCU registration, same minimized-to-tray behavior). The change uses `skip_specs: true` in
`.openspec.yaml`.

## Manual smoke task

A manual smoke task is included: after building and running the application, the user must verify
that the three revised strings appear correctly on the Settings → General & Startup page. This
verification confirms that the strings are rendered as expected, with no layout issues or truncation.

## Dependencies

None. This change is self-contained.

## Open questions

None.
