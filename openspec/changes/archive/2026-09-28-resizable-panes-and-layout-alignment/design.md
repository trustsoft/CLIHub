## Context

See `proposal.md` — Why. The main window (`src/CLIHub/Windows/MainWindow.xaml`) hosts a header row, a content grid, and a status bar. The content grid currently uses two fixed columns (`250` and `*`) and places the "Projects" and "AI Agents" `GroupBox` panes in them, each with slightly different margins and internal spacing. There is no view model for the window; layout is entirely XAML with code-behind.

## Goals / Non-Goals

**Goals:**
- Make the vertical boundary between the two panes draggable.
- Make the two panes visually consistent (bottom action rows and outer margins aligned).

**Non-Goals:**
- Persisting the chosen width ratio across restarts (not requested; would need config changes).
- Restructuring the window into MVVM (existing window is code-behind and is migrated separately).
- Changing the header, status bar, or pane content.

## Decisions

- **Use a third `GridSplitter` column in the existing content grid** instead of switching to a `DockPanel`. Rationale: minimal diff, keeps the existing two `GroupBox` children, and `GridSplitter` natively enforces `MinWidth` via column definitions. Alternative considered: `DockPanel` — rejected because enforcing both panes' minimum widths is more awkward.
  - Column setup: `Width="250" MinWidth="180"` (Projects), `Width="Auto"` (splitter), `Width="*" MinWidth="300"` (Agents). The splitter uses `Width="5"`, `HorizontalAlignment="Stretch"`, `VerticalAlignment="Stretch"`, and `ResizeBehavior="PreviousAndNext"` so dragging resizes the two panes and preserves total width.
- **Align bottom action rows via matching bottom spacing.** The Projects pane's "Add Project..." button and the Agents pane's action `WrapPanel` currently have different effective bottom padding (the `WrapPanel` children carry an extra bottom margin). Normalize by giving both panes the same content margin and removing the redundant per-button bottom margin so both action rows share a bottom edge.
- **Normalize outer margins/padding by using one shared margin value** for both `GroupBox` panes (instead of Projects `Margin="0,0,10,0"`), and letting the splitter column provide the inter-pane gap.

## Risks / Trade-offs

- [Splitter makes the `GroupBox` gap look too wide] → Use a thin (≈5px) transparent splitter with a subtle hover cursor instead of a thick visible bar.
- [Removing per-button margins changes the Agents action row wrap spacing] → Keep horizontal spacing between buttons, adjust only the bottom margin to match the Projects pane.
- [Hard-coded `MinWidth` may be too large on small windows/resolutions] → Choose conservative minimums and verify at the default 800×600 window size.
