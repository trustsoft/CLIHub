## REMOVED Requirements

### Requirement: Sidebar navigation styling

## ADDED Requirements

### Requirement: Sidebar navigation and page styling

The Settings window SHALL render a sidebar navigation region in the application palette: an uppercase muted label per group, one navigable item per page with a leading icon, and the active page highlighted with the accent. The active page's content SHALL be shown in the content area under a page header, and the sidebar SHALL remain readable on the active palette's surface.

#### Scenario: Group labels and items
- **WHEN** the sidebar is rendered
- **THEN** each group ("General", "Engines & Repos", "System") has an uppercase muted label and its pages appear as navigable items

#### Scenario: Each item shows a leading icon
- **WHEN** the sidebar is rendered
- **THEN** every page item shows a leading icon before its title

#### Scenario: Active page highlighted
- **WHEN** a page is active
- **THEN** its sidebar item is highlighted with the accent while the other items stay neutral

#### Scenario: Page header
- **WHEN** a page is shown
- **THEN** its content appears under a page header on the themed content surface
