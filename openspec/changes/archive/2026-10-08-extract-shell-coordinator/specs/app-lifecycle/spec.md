## MODIFIED Requirements

### Requirement: First-instance session owns application event subscriptions

The application SHALL give first-instance event subscriptions an explicit owner and SHALL remove those subscriptions during shutdown before stopping tracked operations and disposing the service provider. The session SHALL delegate shell creation and event wiring to a shell coordinator while retaining ownership of subscription lifecycle.

#### Scenario: Session starts after first-instance initialization

- **WHEN** the first application instance completes required plugin and preference initialization
- **THEN** the application creates a shell coordinator and delegates startup UI creation and event wiring to it
- **AND** the session receives the created startup UI from the coordinator

#### Scenario: Session disposal removes subscriptions

- **WHEN** application shutdown begins
- **THEN** the session disposes the shell coordinator, which removes its application-level event subscriptions before tracked operations are stopped
- **AND** later update or activation events do not invoke the disposed coordinator handlers

#### Scenario: Second instance does not create a session

- **WHEN** startup detects that another instance already owns the application
- **THEN** the second process signals activation and shuts down without starting the first-instance session or creating a shell coordinator
