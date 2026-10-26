## MODIFIED Requirements

### Requirement: Cards on the data pages

The Project Scanning and CLI Agents pages SHALL present their settings as cards in the shared palette, with a page description and section headers, consistent with the other settings pages. List editors SHALL be rendered as stacked cards that carry their list and controls. The per-agent cards SHALL follow the shared card visual language (a rounded bordered surface, a title, and a hint) and SHALL present the agent logo as a plain unframed image, not as the framed icon block the other setting cards use.

#### Scenario: Projects page cards

- **WHEN** the Project Scanning page is shown
- **THEN** watched directories, scan depth, discovery mode, ignored patterns, and the watcher status each appear as cards under the page description and section headers

#### Scenario: List editor card

- **WHEN** a list setting (watched directories or ignored patterns) is shown
- **THEN** its list and its add/remove controls appear inside a card whose title and hint are on top and the content is stacked

#### Scenario: CLI Agents page cards

- **WHEN** the CLI Agents page is shown
- **THEN** the default-agent selector and the agent detection fields appear as cards under the page description and section headers, and the agent list follows under the management section

#### Scenario: Default agent dropdown

- **WHEN** the default agent is shown
- **THEN** it is a dropdown selector (a combo box) offering "None" and the loaded agents, showing the stored choice

#### Scenario: Agent cards share the visual language

- **WHEN** a per-agent card is shown
- **THEN** it uses the same rounded bordered surface and title typography as the other setting cards
- **AND** its logo is presented as a plain unframed image instead of the framed icon block the other setting cards use

#### Scenario: Agent card logo rendered unframed

- **WHEN** a per-agent card is shown
- **THEN** its logo is rendered as a plain image of 32 by 32 DIP, uniformly stretched with high-quality bitmap scaling, aligned to the top of the card's leading column
- **AND** the logo carries no border stroke, no rounded-corner clipping, and no background plate, matching the launch window's plain agent logo presentation
