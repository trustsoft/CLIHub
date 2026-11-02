# Proposal: Shorter labels on the Launch Window page

## Why

The Launch Window page names its two sections "LAUNCHER POSITIONING" and "PATHS". The page itself is
already called "Launch Window", so the first label repeats the page name, and "PATHS" is ambiguous
next to the launch window's other path-shaped concepts (watched folders, project registration). Both
labels can be shorter without losing meaning. The selector options also separate the subject from its
qualifier with an em dash, which reads long in a compact combo box.

## What changes

- `LAUNCHER POSITIONING` becomes `POSITION`.
- `PATHS` becomes `PROJECT PATHS`.
- The option labels in both selectors use an en dash instead of an em dash: "Active monitor –
  centered", "Active monitor – top-center", "Primary monitor – centered", "Left trim – keep the end of
  the path", and "Middle ellipsis – keep both ends".
- The `preferences-ui` requirement for the positioning section is restated with the new header name
  and separator, and the path display requirement now names its section header too.

## Impact

- `src/CLIHub/Views/SettingsLaunchWindowPage.xaml` — two `SectionHeader` texts.
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — the two option arrays' display labels.
- `preferences-ui`: "Popup screen position" and "Path display preference" requirement text.
- No behavior, binding, configuration, or persistence change.

## Explicit non-goals

- Renaming the page, the cards, or the selector options.
- Reordering the two sections.

## Dependencies

None.
