## MODIFIED Requirements

### Requirement: Update check failure handling

The system SHALL handle update-check failures gracefully and SHALL complete the underlying check lifecycle when a timeout or cancellation occurs.

#### Scenario: Network error during check
- **WHEN** the check fails due to a network or service error
- **THEN** the failure is logged and reported as a failed check without blocking the application

#### Scenario: Update check timeout
- **WHEN** the check does not complete within its timeout
- **THEN** the underlying check is cancelled or fully observed, the service reports a failed check, and the application continues normally

#### Scenario: Caller cancels the check
- **WHEN** the caller cancels an in-progress update check
- **THEN** the check lifecycle is completed without an unobserved exception and the service remains ready for a later check
