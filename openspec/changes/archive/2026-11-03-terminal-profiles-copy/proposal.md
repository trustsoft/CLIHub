# Revise the Terminal Profiles page copy

## Why

The Settings → Terminal Profiles page contains three strings that are verbose and use inconsistent
terminology. The page description (`The shell used to open interactive agent commands.`) mixes "shell"
with the page title's "Terminal Profiles" and says "open interactive agent commands" when the page is
about the runtime, not command types. The card hint (`Runtime used to open interactive agent commands.`)
repeats "interactive agent commands" again. The unavailable-runtime help text (`The runtime executable
was not found, so a command in this runtime cannot be launched.`) is a long passive-voice explanation
when a user is more likely scanning for "not installed" signals.

The user reviewed an HTML mockup (`terminal-profiles-copy-mockup.html`) and approved shorter, clearer
replacements: the page description now states what the terminal setting does ("The terminal that CLI
agents run in"), the hint states when the setting applies ("Used for every agent command"), and the
unavailable-runtime help text front-loads the key fact ("Not installed. Agent commands can't run in
this runtime").

**Word count reduction:** 38 words → 23 words (−39%).

## What changes

Replace three UI strings:

1. **Page description** (sidebar + header): `The shell used to open interactive agent commands.` →
   `The terminal that CLI agents run in.` (SettingsNavigation.cs:87)
2. **"Default runtime" card hint**: `Runtime used to open interactive agent commands.` →
   `Used for every agent command.` (SettingsTerminalPage.xaml:13)
3. **Unavailable runtime AutomationProperties.HelpText**: `The runtime executable was not found, so a command in this runtime cannot be launched.` →
   `Not installed. Agent commands can't run in this runtime.` (SettingsTerminalPage.xaml:34)

Do not change:
- Page title "Terminal Profiles" (sidebar + header)
- Card title "Default runtime"
- Runtime option names ("PowerShell", "PowerShell 7", "Command Prompt", "Git Bash")
- The "(not installed)" suffix displayed after unavailable runtime names
- Card order, icons, bindings, ComboBox styles, ItemTemplate layout, or XAML structure
- Window chrome, footer Save/Cancel, SettingsViewModel properties, configuration keys, or behavior

## Impact

- UI: `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs` (line 87),
  `src/CLIHub/Views/SettingsTerminalPage.xaml` (lines 13, 34)
- No tests pin these strings: `tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs` validates only
  page keys and template keys, not display text.
- No spec deltas: these UI strings are not quoted in any durable specification; the underlying facts
  (terminal runtime selection, default runtime behavior, unavailable runtime handling) are documented
  in `openspec/specs/terminal-integration/spec.md` under "Terminal runtime selection" and
  "Unavailable runtime" requirements.
- No documentation changes: `README.md` does not quote these strings.
- No configuration, layout, or behavior change.

## Non-goals

- Changing the page title "Terminal Profiles" or the card title "Default runtime", which remain unchanged
- Changing runtime option names ("PowerShell", "PowerShell 7", "Command Prompt", "Git Bash")
- Revising any other Settings page copy
- Changing layout, icons, bindings, ComboBox behavior, or the "(not installed)" suffix logic
