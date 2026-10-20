# launch-window-theme (partial): blue palette surfaces

## MODIFIED Requirements

### Requirement: Dark launch window theme

The launch window SHALL be rendered with the application's shared theme palette rather than the
operating system's default window theme: the dark palette by default, the light palette when the
effective application theme is Light (or System resolved to light), and the blue palette when the
effective application theme is Blue.

#### Scenario: Dark surfaces
- **WHEN** the launch window is shown with the dark theme
- **THEN** the window background, both panes, the row surfaces, the pane headers, and the footer use the dark palette

#### Scenario: Light surfaces
- **WHEN** the launch window is shown with the light theme
- **THEN** the window background, both panes, the row surfaces, the pane headers, and the footer use the light palette

#### Scenario: Blue surfaces
- **WHEN** the launch window is shown with the blue theme
- **THEN** the window background, both panes, the row surfaces, the pane headers, and the footer use the blue palette

#### Scenario: Readable text
- **WHEN** a pane header, a row, or the footer is rendered
- **THEN** primary text contrasts with its surface, and secondary text such as a project path uses a dimmed tone that still contrasts with the surface

#### Scenario: Separators remain visible
- **WHEN** rows are listed in a pane
- **THEN** the boundary between consecutive rows remains visible on the active palette's background
