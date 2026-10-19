# logo-display Specification

## Purpose
Renders resolved logo paths in the UI with a single frozen decoded image per distinct logo, so memory scales with the number of distinct logos rather than with the number of image controls, and keeps the default logo visible whenever a path is missing or undecodable.

## Requirements

### Requirement: Shared decoded logo images

The application SHALL decode each distinct resolved logo path once into a frozen, shareable image instance and SHALL reuse that instance in every view that displays the logo, instead of decoding a separate pixel buffer per image control.

#### Scenario: Repeated path shares one image

- **WHEN** the same resolved logo path is displayed by multiple image controls (launch-window rows, settings cards, tray-adjacent views)
- **THEN** all of them use the same frozen image instance

#### Scenario: One decode per distinct path

- **WHEN** a logo path is displayed again after having been displayed before
- **THEN** the file is not decoded again

### Requirement: Default image on missing or undecodable path

The application SHALL display the default logo image when a bound logo path is null or empty, the file no longer exists at that path, or the file cannot be decoded as an image. It SHALL NOT display a blank placeholder in those cases.

#### Scenario: Missing path falls back

- **WHEN** a bound logo path is null or empty
- **THEN** the default logo image is displayed

#### Scenario: Deleted file falls back

- **WHEN** a resolved logo path no longer exists on disk
- **THEN** the default logo image is displayed instead of a blank image

#### Scenario: Undecodable file falls back

- **WHEN** the file at a bound logo path exists but cannot be decoded as an image
- **THEN** the default logo image is displayed
