## Why

The main window splits into a fixed-width Projects pane and a flexible AI Agents pane with no way to adjust their proportions, so long project names or paths are clipped and users cannot give more room to the list they care about. The two panes also have inconsistent internal spacing: their bottom action rows and outer margins do not line up, making the layout look unfinished.

## What Changes

- Add a draggable vertical splitter between the Projects and AI Agents panes so the user can change the width ratio.
- Give the Projects pane a sensible default and minimum/maximum widths so neither pane can be collapsed away.
- Align the bottom action rows of both panes ("Add Project..." and the Launch/Resume/Init/Update/Version/Refresh row) so they share the same bottom edge.
- Normalize the outer margins/borders of both panes so they are visually consistent (equal padding on all sides).

## Capabilities

### New Capabilities
- `main-window-layout`: Describes the main window's two-pane layout and its resizability and alignment guarantees.

### Modified Capabilities
<!-- None: existing specs describe behavior, not window layout. -->

## Impact

- `src/CLIHub/Windows/MainWindow.xaml` — content grid columns, new `GridSplitter`, pane margins/padding.
- No changes to `CLIHub.Core`, services, configuration, or dependencies.
- No user-facing behavior beyond the window layout.
