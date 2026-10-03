## MODIFIED Requirements

### Requirement: Actions menu presentation

The system SHALL present each pane's actions menu as a themed popover anchored to that pane's Actions control, sized to its entries rather than a fixed width, with every entry showing an icon glyph alongside its label; the open Actions control and the menu SHALL read as a single panel sharing one background and outline; and the menu SHALL close when the user invokes an entry, clicks outside it, or presses Escape.

#### Scenario: Entry icons
- **WHEN** a pane's actions menu is open
- **THEN** every entry shows an icon glyph before its label, and every glyph uses the same neutral color

#### Scenario: Aligned with the Actions control
- **WHEN** a pane's actions menu opens
- **THEN** the menu's right edge aligns with the right edge of that pane's Actions control

#### Scenario: Menu sizes to its entries
- **WHEN** a pane's actions menu is open
- **THEN** the menu is only as wide as its widest entry requires, so a menu with shorter labels is narrower than one with longer labels

#### Scenario: Control and menu form one panel
- **WHEN** a pane's actions menu is open
- **THEN** that pane's Actions control shares the menu's background and outline, and its label and chevron stay in place, so the control and the menu read as a single panel

#### Scenario: Entries reflect their state
- **WHEN** a pane's actions menu is open and an entry is unavailable or is a selected toggle
- **THEN** the unavailable entry is shown disabled and the selected toggle entry is shown as checked

#### Scenario: Pointer feedback
- **WHEN** the pointer is over a menu entry
- **THEN** that entry is highlighted

#### Scenario: Menu closes after an action
- **WHEN** the user invokes an available entry
- **THEN** the menu closes

#### Scenario: Menu closes on outside click
- **WHEN** the user clicks outside an open menu
- **THEN** the menu closes

#### Scenario: Escape closes the menu, not the window
- **WHEN** a pane's actions menu is open and the user presses Escape
- **THEN** the menu closes and the window stays open, and only a subsequent Escape hides the window
