## ADDED Requirements

### Requirement: AGENTS card list order

The AGENTS block SHALL list agent cards with installed agents before not-installed agents, and SHALL
preserve the plugin catalog order within each group without any other sort. The order SHALL be
reapplied when a card's installation state settles to `Installed` or `NotInstalled`; a `Detecting`
state SHALL NOT move any card.

#### Scenario: Mixed installation states

- **WHEN** the catalog order is `A` (not installed), `B` (installed), `C` (not installed), `D` (installed)
- **THEN** the AGENTS cards are listed as `B`, `D`, `A`, `C`
- **AND** no alphabetical or other secondary sort is applied

#### Scenario: All agents installed

- **WHEN** the catalog order is `A`, `B`, `C` and every agent is installed
- **THEN** the AGENTS cards are listed as `A`, `B`, `C`

#### Scenario: No agent installed

- **WHEN** the catalog order is `A`, `B`, `C` and no agent is installed
- **THEN** the AGENTS cards are listed as `A`, `B`, `C`

#### Scenario: Detect flips an installed card to not installed

- **WHEN** the AGENTS cards are listed as `A` (installed), `B` (installed), `C` (not installed) and a Detect settles `A` as not installed
- **THEN** the cards are listed as `B`, `A`, `C`
- **AND** `A` joins the not-installed group ahead of `C` because catalog order is preserved within each group

#### Scenario: Detect flips a not-installed card to installed

- **WHEN** the AGENTS cards are listed as `B` (installed), `A` (not installed), `C` (not installed) and a Detect settles `C` as installed
- **THEN** the cards are listed as `B`, `C`, `A`

#### Scenario: Detecting does not reorder the list

- **WHEN** a card's installation state changes to `Detecting`, including at the start of a header-wide Detect pass
- **THEN** that transition does not change any card's position
- **AND** a card that was installed stays in the installed group and a card that was not installed stays in the not-installed group until it settles to `Installed` or `NotInstalled`

#### Scenario: Reload reapplies the order

- **WHEN** the AGENTS cards are rebuilt from the catalog, including after a plugin install, enable, disable, or manual Reload
- **THEN** the rebuilt cards are ordered installed-first with catalog order preserved within each group

#### Scenario: Disabled and invalid sections are unaffected

- **WHEN** the catalog contains disabled or invalid plugin entries alongside the AGENTS cards
- **THEN** those entries remain in their existing Disabled and Invalid sections with unchanged contents and order
- **AND** they do not participate in the AGENTS card ordering
