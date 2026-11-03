# Design: revise the Terminal Profiles page copy

## Current state (verified)

Three strings on the Settings → Terminal Profiles page:

| Location | Line | Current text |
| --- | --- | --- |
| `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs` | 87 | `"The shell used to open interactive agent commands."` (page Description) |
| `src/CLIHub/Views/SettingsTerminalPage.xaml` | 13 | `"Runtime used to open interactive agent commands."` (SettingCard Hint) |
| `src/CLIHub/Views/SettingsTerminalPage.xaml` | 34 | `"The runtime executable was not found, so a command in this runtime cannot be launched."` (AutomationProperties.HelpText for unavailable runtime) |

Page metadata: `SettingsNavigation.cs:85-89` defines the "terminal" page with key `"terminal"`, title
`"Terminal Profiles"`, and the Description at line 87.

XAML card: `SettingsTerminalPage.xaml` has one `SettingCard` control (line 11):
- `Title="Default runtime"`, `Hint` at line 13, `Icon="{x:Static themes:IconGlyphs.Terminal}"`
- `ComboBox` bound to `SelectedRuntime` (TwoWay), `ItemsSource="{Binding Runtimes}"`
- `ItemTemplate` with runtime icon, display name, and conditional styling for unavailable runtimes
- Line 33-34: `AutomationProperties.HelpText` set when `IsInstalled` is False

Verification against specs:
- `openspec/specs/terminal-integration/spec.md:15-29` documents "Terminal runtime selection" requirement:
  the user selects one runtime, which is used for all agent commands.
- `openspec/specs/terminal-integration/spec.md:30-45` documents "Unavailable runtime" requirement:
  runtimes whose executables are not found are marked "(not installed)" and remain selectable; the
  launch attempt fails with a clear error if the user tries to launch in an unavailable runtime.

The new strings' stated facts are true:
1. "The terminal that CLI agents run in" — confirmed: the runtime selection determines the terminal
   used for agent commands.
2. "Used for every agent command" — confirmed: `openspec/specs/terminal-integration/spec.md:18-19`
   says "The selected runtime is used for all agent commands launched from the project list."
3. "Not installed. Agent commands can't run in this runtime." — confirmed:
   `openspec/specs/terminal-integration/spec.md:37-40` scenario "Launch in unavailable runtime" says
   "the launch fails with an error message indicating the runtime executable was not found."

No durable spec quotes any of these three strings: grep of `openspec/specs` for
`shell used to open`, `Runtime used to open`, `runtime executable was not found`, and
`cannot be launched` returns nothing.

No test pins these strings: `tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs` validates page
keys and template keys only, not display text.

`README.md` does not quote these strings: grep returns nothing.

## Approved copy changes

The user approved the HTML mockup `terminal-profiles-copy-mockup.html`. The three replacements from
that mockup:

| # | Where | Before | After | Character count | Rationale |
| --- | --- | --- | --- | --- | --- |
| 1 | Page description (SettingsNavigation.cs:87) | `The shell used to open interactive agent commands.` | `The terminal that CLI agents run in.` | 51 → 38 (−25%) | Instead of "shell" (inconsistent with "Terminal Profiles") and "open interactive agent commands" (the page is about the runtime, not command types), the revised description states what the terminal setting does in seven words. |
| 2 | Card hint (SettingsTerminalPage.xaml:13) | `Runtime used to open interactive agent commands.` | `Used for every agent command.` | 49 → 29 (−41%) | Instead of repeating "open interactive agent commands", the hint now states when the runtime applies: every agent command. |
| 3 | Unavailable runtime help text (SettingsTerminalPage.xaml:34) | `The runtime executable was not found, so a command in this runtime cannot be launched.` | `Not installed. Agent commands can't run in this runtime.` | 89 → 56 (−37%) | Instead of a long passive-voice explanation, the revised text front-loads the key fact ("Not installed") and states the consequence in active voice. |

Total: 38 words → 23 words (−39%).

## Code changes

### 1. SettingsNavigation.cs, line 87 (page Description)

Before:
```csharp
"The shell used to open interactive agent commands.",
```

After:
```csharp
"The terminal that CLI agents run in.",
```

### 2. SettingsTerminalPage.xaml, line 13 (SettingCard Hint)

Before:
```xml
Hint="Runtime used to open interactive agent commands."
```

After:
```xml
Hint="Used for every agent command."
```

### 3. SettingsTerminalPage.xaml, line 34 (AutomationProperties.HelpText)

Before:
```xml
Value="The runtime executable was not found, so a command in this runtime cannot be launched."
```

After:
```xml
Value="Not installed. Agent commands can't run in this runtime."
```

## Decisions

### Why "terminal" instead of "shell"

The page title is "Terminal Profiles" and the card title is "Default runtime". Using "shell" in the
page description is inconsistent with the page's own terminology. "Terminal" aligns with the page
title and better describes what the user is selecting: a runtime that launches a terminal window.

### Why "CLI agents run in" instead of "open interactive agent commands"

The page is about the runtime selection, not the types of commands. "CLI agents run in" states what
the terminal does (runs agents) without over-specifying command types. The word "interactive" is
redundant: all agent commands launched from CLIHub are interactive (they open a terminal window).

### Why "Used for every agent command"

The current hint repeats "open interactive agent commands" from the page description. The revised
hint states a fact the card title does not: the runtime applies to every agent command, not just
some subset.

### Why front-load "Not installed"

The current help text is long and passive ("The runtime executable was not found, so..."). Users
scanning the ComboBox for unavailable runtimes are more likely looking for "not installed" signals.
The revised text front-loads the key fact ("Not installed") and states the consequence in active
voice ("can't run"), cutting 33 characters.

### Why the card title stays "Default runtime"

The card title "Default runtime" is concise and accurate. The user explicitly approved keeping it
as-is. Changing it was not part of the approved mockup.

### Why the page title stays "Terminal Profiles"

The user reviewed whether to rename the page and explicitly rejected it. The page title remains
"Terminal Profiles" in both the sidebar and the page header.

## Rejected / deliberately unchanged

- **Page title**: "Terminal Profiles" remains unchanged (sidebar + header).
- **Card title**: "Default runtime" remains unchanged.
- **Runtime option names**: "PowerShell", "PowerShell 7", "Command Prompt", "Git Bash" remain unchanged.
- **The "(not installed)" suffix**: The `DisplayName` property appends "(not installed)" to unavailable
  runtimes; this logic is unchanged.
- **Card order, icons, bindings**: The card's icon (`IconGlyphs.Terminal`), the ComboBox binding
  (`SelectedRuntime` TwoWay), the ItemsSource (`Runtimes`), and the ItemTemplate layout (icon, display
  name, conditional foreground color) are unchanged.
- **XAML layout**: No change to `StackPanel`, `SettingCard` layout, `ComboBox` styles, `ItemTemplate`,
  or `DataTrigger` conditions.
- **Window chrome, footer Save/Cancel**: No change to the Settings window chrome or footer controls.
- **SettingsViewModel properties, configuration keys, behavior**: No change to `SelectedRuntime`,
  `Runtimes` properties, their configuration keys, or the underlying behavior (runtime detection,
  unavailable runtime handling, terminal launch logic).
- **Other Settings pages**: No other Settings page is touched.

## Specification impact

None. No durable specification under `openspec/specs/` quotes these UI strings. Behavior is unchanged
(same runtime selection, same unavailable runtime handling, same terminal launch logic). The change
uses `skip_specs: true` in `.openspec.yaml`.

## Manual smoke task

A manual smoke task is included: after building and running the application, the user must verify
that the three revised strings appear correctly on the Settings → Terminal Profiles page. This
verification confirms that the strings are rendered as expected, with no layout issues or truncation.

## Dependencies

None. This change is self-contained.

## Open questions

None.
