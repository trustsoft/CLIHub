## Why

The Projects & Paths page opens with three summary cards (projects, agents, and watcher) and a long settings list below them. The cards currently sit inside the scrolling content region, so scrolling down to the watched directories or discovery rules hides the status the user was watching. The page header, by contrast, is already pinned above that region, but its hairline divider stops the header before the summary cards, which visually separates the cards from the page identity they belong to.

## What Changes

- Move the three Projects & Paths summary cards out of the scrolling content and into the fixed page header, shown only while that page is active.
- Place the page-header hairline below those cards, so the header reads as title, description, actions, summary cards, then the divider.
- Keep every other page unchanged: without summary cards the header keeps its current title, description, actions, and divider spacing.
- Keep the cards' data bindings, layout, styling, and live values unchanged; only their host changes.
- Keep the summary content out of the page's scroll region, so the content now starts at the "WATCHED REPOSITORY DIRECTORIES" section.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `settings-theme`: The "Persistent page header" requirement gains the Projects & Paths summary cards as part of the fixed header and states that the header divider sits below them.

## Impact

- `src/CLIHub/Views/SettingsWindow.xaml` — a summary-card template in the window resources, an extra fixed-header row for the Projects & Paths page, and the divider moved below it.
- `src/CLIHub/Views/SettingsProjectsPage.xaml` — the three-card block is removed from the scrolling content.
- No changes to the view model, configuration, persistence, or tests.
