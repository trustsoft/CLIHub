## MODIFIED Requirements

### Requirement: Cards on the data pages

The Project Discovery and CLI Agents pages SHALL present their settings as cards in the shared palette, with a page description and section headers, consistent with the other settings pages. List editors SHALL be rendered as stacked cards that carry their list and controls. The per-agent cards SHALL follow the shared card visual language (a rounded bordered surface, an icon block, a title, and a hint).

#### Scenario: Projects page cards

- **WHEN** the Project Discovery page is shown
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
