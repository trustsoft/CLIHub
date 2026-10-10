## MODIFIED Requirements

### Requirement: Segmented selectors

The default-runtime and path-display selectors SHALL render as segmented controls: one bordered group of segments, one per option, with the selected segment visually highlighted using the accent. Each runtime segment SHALL show an icon together with its label; each path-display segment SHALL show its label.

#### Scenario: Selected segment is visible
- **WHEN** a runtime or path-display option is selected
- **THEN** its segment is highlighted with the accent while the others stay neutral

#### Scenario: Segments carry short labels
- **WHEN** the runtime selector is rendered
- **THEN** each segment shows an icon (PowerShell, PowerShell 7, Command Prompt, Git Bash) with its label rather than a long combo-box label
