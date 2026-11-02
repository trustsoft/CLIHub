# Design: Shorter section labels on the Launch Window page

## Verified current state

- `src/CLIHub/Views/SettingsLaunchWindowPage.xaml:11` — `<controls:SectionHeader Text="LAUNCHER POSITIONING"/>`.
- `src/CLIHub/Views/SettingsLaunchWindowPage.xaml:28` — `<controls:SectionHeader Text="PATHS"/>`.
- `openspec/specs/preferences-ui/spec.md:251-255` — "Popup screen position" names the
  "LAUNCHER POSITIONING" header in its requirement text and in the "Options offered" scenario.
- `openspec/specs/preferences-ui/spec.md:129-155` — "Path display preference" does not name its section
  header today.

## Decisions

### Section text

"POSITION" is enough on a page whose title already says "Launch Window", and "Where the launch window
appears when it is shown" on the card keeps the meaning explicit. "PROJECT PATHS" disambiguates the
path display setting from the project-scanning and registration pages, where paths also appear.

### Spec wording

The positioning requirement keeps its requirement and scenario names and only swaps the header token
and the separator pattern. The path display requirement gains the header name so the label is part of
the contract rather than an undeclared detail; its scenarios are copied unchanged.

### Option separators

The option labels use an en dash (`–`) between the subject and its qualifier instead of an em dash
(`—`). The en dash is visibly shorter in the compact combo boxes while keeping the separation clearer
than a hyphen. The change applies to all five labelled options on the page (three positions, two path
styles); `Cursor-relative` has no separator and stays as it is.

## Alternatives considered and rejected

- **Keep "LAUNCHER POSITIONING".** Rejected: it repeats the page title and the card hint already states
  what is positioned.
- **Call the second section just "DISPLAY".** Rejected: it does not say what is displayed and could be
  confused with the theme/display settings.

## Open questions

None.
