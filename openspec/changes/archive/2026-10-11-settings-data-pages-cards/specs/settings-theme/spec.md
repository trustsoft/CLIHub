## ADDED Requirements

### Requirement: Cards on the data pages

The Projects & Paths and CLI Agents pages SHALL present their settings as cards in the shared dark palette, with a page description and section headers, consistent with the other settings pages. List editors SHALL be rendered as stacked cards that carry their list and controls. The per-agent cards SHALL follow the shared card visual language (a rounded bordered surface, an icon block, a title, and a hint).

#### Scenario: Projects page cards
- **WHEN** the Projects & Paths page is shown
- **THEN** watched directories, scan depth, discovery mode, ignored patterns, and the watcher status each appear as cards under the page description and section headers

#### Scenario: List editor card
- **WHEN** a list setting (watched directories or ignored patterns) is shown
- **THEN** its list and its add/remove controls appear inside a card whose title and hint are on top and the content is stacked

#### Scenario: CLI Agents page cards
- **WHEN** the CLI Agents page is shown
- **THEN** the default-agent selector and the agents-probe fields appear as cards under the page description and section headers, and the agent list follows under the management section

#### Scenario: Default agent dropdown
- **WHEN** the default agent is shown
- **THEN** it is a dropdown selector (a combo box) offering "None" and the loaded agents, showing the stored choice

#### Scenario: Agent cards share the visual language
- **WHEN** a per-agent card is shown
- **THEN** it uses the same rounded bordered surface, title typography, and icon block treatment as the other setting cards

### Requirement: Card content alignment

A setting card SHALL let its content be aligned within the card both horizontally and vertically. Compact cards SHALL align their content to the trailing edge by default, stacked cards SHALL let their content fill the card width by default, and a page SHALL be able to override the alignment (for example, left-aligning a segmented selector so the control does not stretch to the full card width).

#### Scenario: Compact card default alignment
- **WHEN** a compact setting card is shown without an explicit content alignment
- **THEN** its control is aligned to the trailing edge of the card

#### Scenario: Stacked card default alignment
- **WHEN** a stacked setting card is shown without an explicit content alignment
- **THEN** its content fills the card width

#### Scenario: Overridden alignment
- **WHEN** a page sets a card's content alignment (for example to the leading edge)
- **THEN** the card's content is aligned accordingly instead of the default
