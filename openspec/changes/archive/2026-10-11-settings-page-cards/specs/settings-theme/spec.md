## ADDED Requirements

### Requirement: Setting cards

Each setting SHALL be presented as a card: a surface carrying the setting's leading icon, title, an optional inline badge, and a short hint, with the setting's control shown inline. A compact control (a checkbox, a short segmented selector, or a numeric field) SHALL be aligned on the trailing edge of the card; a wide or compound control SHALL be shown stacked below the title and hint, spanning the card width.

#### Scenario: Card content
- **WHEN** a setting card is shown
- **THEN** it shows the setting's leading icon, title, hint, and its control

#### Scenario: Optional badge
- **WHEN** a setting carries a status badge
- **THEN** the badge appears inline next to the title with a tone (neutral, accent, or success)

#### Scenario: Compact setting card
- **WHEN** a setting with a compact control (for example a toggle) is shown
- **THEN** its title and hint appear on the leading side and its control appears on the trailing side of the card

#### Scenario: Wide setting card
- **WHEN** a setting with a wide or compound control (for example the hotkey field, a runtime selector, the accent swatches, or the log-file row) is shown
- **THEN** its title and hint appear on top and its control appears below, spanning the card width

#### Scenario: Card visual language
- **WHEN** a setting card is shown
- **THEN** it uses the shared dark palette with a rounded border and dark surface, consistent with the launch window

### Requirement: Grouped setting surface

A group of related settings SHALL be renderable either as standalone cards or as rows on one shared bordered surface, where the rows are separated by hairlines and the first row has no separator.

#### Scenario: Standalone cards
- **WHEN** settings are shown as standalone cards
- **THEN** each card has its own rounded border and cards are separated by a gap

#### Scenario: Grouped rows
- **WHEN** settings are shown as a group
- **THEN** the rows share one rounded bordered surface, consecutive rows are separated by a hairline, and the first row has no top separator

### Requirement: Page description and sections

A settings page SHALL show a short description under its page title, and SHALL group its settings under section headers where the page has more than one group of settings. A section header SHALL show an uppercase muted label and an optional accent marker.

#### Scenario: Page description
- **WHEN** a settings page is shown
- **THEN** a one-line description appears under the page title in the muted hint style

#### Scenario: Section headers
- **WHEN** a page groups settings into more than one area
- **THEN** each area is introduced by an uppercase muted section header above its cards, with an accent marker, without a trailing caption

#### Scenario: Single-group page
- **WHEN** a page has a single group of settings
- **THEN** its cards appear under the page description without a redundant section header
