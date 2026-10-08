## ADDED Requirements

### Requirement: First-instance session owns application event subscriptions

The application SHALL give first-instance event subscriptions an explicit owner and SHALL remove those subscriptions during shutdown before stopping tracked operations and disposing the service provider.

#### Scenario: Session starts after first-instance initialization
- **WHEN** the first application instance completes required plugin and preference initialization
- **THEN** the application creates its startup UI and connects update-state, update-request, and second-instance activation events through the session owner

#### Scenario: Session disposal removes subscriptions
- **WHEN** application shutdown begins
- **THEN** the session removes its application-level event subscriptions before tracked operations are stopped
- **AND** later update or activation events do not invoke the disposed session handlers

#### Scenario: Second instance does not create a session
- **WHEN** startup detects that another instance already owns the application
- **THEN** the second process signals activation and shuts down without starting the first-instance session
