## MODIFIED Requirements

### Requirement: Dependency injection container

The application SHALL configure a dependency-injection container at startup with all required services. Each production Core service registered for constructor injection SHALL expose one unambiguous public production constructor.

#### Scenario: Container configuration
- **WHEN** the application initializes
- **THEN** `IConfigService`, `IProjectService`, `IPluginManager`, `IProcessLauncher`, `IAgentCommandService`, `IAgentDetectionService`, and `IAgentVersionService` are registered as singletons and UI components resolve their dependencies from the container

#### Scenario: Service resolution
- **WHEN** a component requests a registered Core service from the container
- **THEN** the container provides the registered implementation with all constructor dependencies injected without an ambiguous-constructor exception

#### Scenario: Application startup after service resolution
- **WHEN** the application initializes its service provider and resolves the tray controller
- **THEN** service resolution completes, the tray controller is created, and startup proceeds to the normal single-instance/window initialization path

#### Scenario: No static service locator
- **WHEN** a class needs a service
- **THEN** it receives it through constructor injection rather than a static accessor
