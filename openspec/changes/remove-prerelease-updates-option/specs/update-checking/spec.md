# update-checking (delta)

## REMOVED Requirements

### Requirement: Update notification preferences

**Reason:** Only stable releases are published; no pre-release builds exist. The pre-release notification preference and pre-release version inclusion in update checks are removed by this change.

**Migration:** Replaced by the "Stable update notification" requirement below, which describes the stable-only checking behavior.

## ADDED Requirements

### Requirement: Stable update notification

The system SHALL always check for stable releases only. The system SHALL notify about stable updates when the stable-notification preference is enabled.

#### Scenario: Stable notifications enabled

- **WHEN** a stable update is found and stable notifications are enabled
- **THEN** the update availability notification is shown

#### Scenario: Stable notifications disabled

- **WHEN** a stable update is found and stable notifications are disabled
- **THEN** no update availability notification is shown
