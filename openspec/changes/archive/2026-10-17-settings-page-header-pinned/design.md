## Context

The Settings window composes one `ScrollViewer` that hosts every page in an overlapping `Grid`. Each page view starts with its own title, description, and divider, so those elements participate in scrolling. Pages are switched by toggling visibility through `SelectedPage.Key` data triggers; the window already knows the active page through `SelectedPage`.

## Goals / Non-Goals

**Goals:**

- Keep the active page's title, description, and divider visible regardless of content height.
- Keep a single scrolling region for page content so scrollbar behavior stays consistent across pages.
- Preserve the existing page-specific status lines (agent metrics, current version).

**Non-Goals:**

- Changing the sidebar, window chrome, footer, or validation-message placement.
- Changing page content, card layout, or any preference behavior.
- Adding per-page scrolling or sticky section headers.

## Decisions

### D1: Own the page header by the window, not by each page

The window renders the fixed header from `SelectedPage.Title` and `SelectedPage.Description` in a new `Auto` row of the content grid, above the existing `ScrollViewer`. Each page view drops its title/description/divider block and starts directly with its content.

Rationale: a header inside a page cannot stay outside the window's single scroll region without duplicating scroll plumbing in every page. Rendering it once in the window keeps the header definition in one place and guarantees identical behavior on every page.

Alternative considered: give each page its own header row plus an internal `ScrollViewer`. Rejected because it duplicates scroll padding/scrollbar behavior nine times and makes the page views responsible for window-level chrome.

### D2: Page description is navigation metadata

`SettingsPage` gains a `Description` value supplied by the static page catalog. `SelectedPage` already raises `PropertyChanged`, so the header follows page switches without extra code-behind.

Rationale: the description belongs to the page identity alongside its title and icon, so metadata is the natural source. The placeholder page needs no dedicated description and shows an empty line.

### D3: Status lines remain page content

The "CLI Agents" metrics line and the "Updates" current-version line move to the top of the scrollable content, directly under the fixed header. They are live status readouts rather than page identity and are allowed to scroll away with their controls.

### D4: No change to the scroll host

The existing `ScrollViewer` keeps its padding and auto vertical scrollbar; only its row assignment changes to the content row beneath the header. This keeps the scrollbar themed by the existing app-level style and avoids scrollbar remounting when pages switch.

## Risks / Trade-offs

- Header spacing differs slightly from the old in-page header because the header row and the scroll padding meet at the divider. Keep the divider at the bottom of the header row and retain the existing content top padding so the visual rhythm is unchanged.
- A placeholder page now shows the window-rendered header but no description line. Render the description only when it is non-empty.
