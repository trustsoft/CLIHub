# code-style Specification

## Purpose
Defines the repository-wide code style contract: one root style configuration covering every project, naming conventions enforced by tooling rather than memory, and XML documentation comments validated by the compiler.

## Requirements

### Requirement: Single root style configuration

The repository SHALL have exactly one `.editorconfig`, located at the repository root and declaring `root = true`, whose rules apply to all source projects, the test project, and non-code files. No additional `.editorconfig` SHALL exist in subdirectories.

#### Scenario: Style rules cover the test project

- **WHEN** a C# file under `tests/` is evaluated against the style configuration
- **THEN** the root `.editorconfig` rules apply to it

#### Scenario: No nested style configuration

- **WHEN** the repository is searched for `.editorconfig` files
- **THEN** only the root file is found

### Requirement: Naming conventions are machine-enforced

The style configuration SHALL flag violations of the project naming conventions: private instance and static fields in `_camelCase`, private constants and private static readonly fields in PascalCase, and interface names with the `I` prefix.

#### Scenario: Interface without the I prefix

- **WHEN** an interface is declared without the `I` prefix (e.g. `interface Foo`)
- **THEN** the style configuration reports a naming warning

#### Scenario: Prefixed private instance field

- **WHEN** a private instance field is declared with the underscore prefix (e.g. `_services`)
- **THEN** no naming warning is reported

#### Scenario: PascalCase private constant

- **WHEN** a private constant is declared in PascalCase (e.g. `ResourceMarker`)
- **THEN** no naming warning is reported

### Requirement: Style configuration contains only applicable rules

The style configuration SHALL NOT contain rules for technologies the project does not use (such as Unity or ASP.NET MVC/Razor), and SHALL NOT duplicate settings already provided by its default file section.

#### Scenario: No foreign technology rules

- **WHEN** the style configuration is searched for Unity serialized-field or ASP.NET MVC/Razor rules
- **THEN** none are found

### Requirement: XML documentation comments are compiler-validated

The application projects SHALL enable documentation file generation so the compiler validates XML documentation comments, reporting malformed XML and `<param>` tags that do not match actual parameters. The missing-documentation warning (CS1591) SHALL NOT be enabled.

#### Scenario: Doc comment drifts from the signature

- **WHEN** a documented member has a `<param>` tag that does not correspond to any actual parameter
- **THEN** the build reports a compiler warning

#### Scenario: Undocumented public member

- **WHEN** a public member has no XML documentation comment
- **THEN** the build succeeds without a missing-documentation warning
