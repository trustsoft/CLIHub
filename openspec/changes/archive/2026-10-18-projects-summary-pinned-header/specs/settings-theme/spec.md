## MODIFIED Requirements

### Requirement: Persistent page header

The Settings window SHALL keep the active page's header visible while page content scrolls. The header SHALL show the active page's title and its one-line description above the content, separated from the content by a hairline. On the "Projects & Paths" page the header SHALL also show the three summary cards for projects, agents, and watcher beneath the title and its actions, and the hairline SHALL sit below those cards. The vertical scrollbar SHALL belong to the content region below the header, so the drawn title bar, sidebar, page header, summary cards, validation message, and footer remain in place while the content scrolls.

#### Scenario: Content taller than the window

- **WHEN** the active page's content is taller than the content region
- **THEN** a vertical scrollbar appears on the content region below the page header
- **AND** scrolling the content leaves the page title, description, and divider visible
- **AND** the drawn title bar, sidebar, and footer do not scroll

#### Scenario: Content fits the window

- **WHEN** the active page's content fits in the content region
- **THEN** no vertical scrollbar is shown and the page header remains in place

#### Scenario: Header follows page switches

- **WHEN** the user selects another page in the sidebar
- **THEN** the fixed header shows that page's title and description
- **AND** the page's content is shown below the same header

#### Scenario: Page without a description

- **WHEN** the active page has no description
- **THEN** the header shows the page title without an empty description line

#### Scenario: Projects summary stays pinned

- **WHEN** the "Projects & Paths" page is active and its content is taller than the content region
- **THEN** the three summary cards for projects, agents, and watcher remain visible in the page header while the content scrolls
- **AND** the header hairline sits below the summary cards
- **AND** another active page shows no summary cards in the header

#### Scenario: Projects content starts below the header

- **WHEN** the "Projects & Paths" page is shown
- **THEN** the scrollable content begins at the "WATCHED REPOSITORY DIRECTORIES" section below the header divider
