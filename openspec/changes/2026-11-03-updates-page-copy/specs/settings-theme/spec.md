# settings-theme Specification

## MODIFIED Requirements

### Requirement: Persistent page header

The Settings window SHALL keep the active page's header visible while page content scrolls. The header SHALL show the active page's title and its one-line description above the content, separated from the content by a hairline. Before the title and description, the header SHALL show a static emblem of the active page's own icon, rendered as a plain glyph with no background, border, or accent color, vertically centered against the title-and-description block; the emblem SHALL NOT take keyboard focus, SHALL NOT expose an action, tooltip, or explicit accessible label, and is announced as a text element the same way the window's other icon glyphs already are. A page MAY add a summary row of summary cards between its description and the hairline: the "Project Scanning" page shows the projects, agents, and watcher cards, the "Projects" page shows the total, manual, and discovered cards, the "CLI Agents" page shows the agents, default-agent, and updates cards, and the "Agent Detection" page shows the cache TTL, timeout, and defaults cards. On the "Project Scanning" page the projects card SHALL show the total number of registered projects with the number of discovered and manually added projects beneath it, and both numbers SHALL refresh whenever the scanner status changes. The watcher card SHALL report the number of folders examined by the last scan after its last scan time, using the singular "folder" for one and the plural "folders" otherwise. The vertical scrollbar SHALL belong to the content region below the header, so the drawn title bar, sidebar, page header, summary row, validation message, and footer remain in place while the content scrolls.

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

- **WHEN** the "Project Scanning" page is active and its content is taller than the content region
- **THEN** the three summary cards for projects, agents, and watcher remain visible in the page header while the content scrolls
- **AND** the header hairline sits below the summary cards
- **AND** a page without summary cards, such as "General & Startup", shows no summary row in the header

#### Scenario: Projects card shows the provenance split

- **WHEN** the "Project Scanning" page is shown
- **THEN** the projects card shows the total number of registered projects
- **AND** beneath it shows the number of discovered and manually added projects, e.g. "31 discovered · 4 manual"
- **AND** both numbers refresh whenever the scanner status changes

#### Scenario: Watcher card shows scan coverage

- **WHEN** the "Project Scanning" page is shown after a scan completed
- **THEN** the watcher card shows the last scan time followed by the number of folders the last scan examined
- **AND** the folder count refreshes whenever the scanner status changes
- **AND** the folder count reads "1 folder" for one folder and "N folders" otherwise

#### Scenario: Projects content starts below the header

- **WHEN** the "Project Scanning" page is shown
- **THEN** the scrollable content begins with the immediate-apply note, followed by the "WATCHED FOLDERS" section, below the header divider

#### Scenario: Summary detail lines span the card width

- **WHEN** a summary card shows its detail or alert line
- **THEN** the line's text box extends to the card's right content edge, level with the icon block's right edge, instead of stopping at the icon block's left edge
- **AND** the line's text stays left-aligned
- **AND** the label and value lines keep reserving the icon block's column so they never run under the icon

#### Scenario: Every page shows its emblem

- **WHEN** any Settings page is active
- **THEN** the header shows that page's own icon as a plain glyph before the title and description, with no background tile, border, or accent color
- **AND** the emblem is vertically centered against the title-and-description block
- **AND** no page omits the emblem and no page shows a different page's icon
- **AND** the emblem's vertical position relative to the title-and-description block is the same on every page, including a page whose header shows actions taller than the title and description

#### Scenario: Emblem does not grow the header on its own

- **WHEN** any Settings page is active and its title and description each already fit on one line at the window's current width
- **THEN** the page header's height is the same with the emblem shown as it would be without it

#### Scenario: Long description wraps instead of clipping

- **WHEN** the active page's description does not fit on one line at the header's current width, including the width the emblem and its gap take from the title column
- **THEN** the description wraps onto as many lines as it needs instead of being clipped or truncated
- **AND** the page header grows to fit the wrapped description
- **AND** the emblem stays vertically centered against the whole title-and-description block

#### Scenario: Emblem stays with the title on page switches

- **WHEN** the user selects another page in the sidebar
- **THEN** the emblem shown is that page's own icon, matching the new title and description

#### Scenario: Title chip still wraps with the title

- **WHEN** the "Updates" page is active, which shows two metadata chips and a "Release notes" link beside its title
- **THEN** the chips and link wrap together with the title text as they do without the emblem
- **AND** the emblem never moves to a second line with the title, chips, or link

#### Scenario: Emblem is not an interactive element

- **WHEN** the user tabs through the page header's controls, or a screen reader inspects it
- **THEN** the emblem is never a keyboard focus stop and Tab traversal skips it
- **AND** the emblem exposes no action and no tooltip
- **AND** the title is still announced as text immediately beside the emblem
