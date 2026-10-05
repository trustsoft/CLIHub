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

### Requirement: Using directives are grouped and sorted

The style configuration SHALL organize C# using directives into ordered groups: BCL namespaces (`System.*`, with `Windows.*` alongside them) first, then `Microsoft.*` namespaces, then the project's own `CLIHub.*` namespaces, then all remaining third-party namespaces. Each group SHALL be sorted alphabetically by namespace name.

#### Scenario: Group order follows the fixed bucket sequence

- **WHEN** a C# file's using block contains namespaces from several groups (for example `System.Reflection`, `Microsoft.Extensions.Logging`, `CLIHub.Core.Models`, and `Velopack`)
- **THEN** the groups appear in the order BCL, `Microsoft.*`, `CLIHub.*`, remaining third-party, with each group sorted alphabetically by namespace name

#### Scenario: Alphabetical order within each group

- **WHEN** a C# file's using directives within one group are not in alphabetical order
- **THEN** the style configuration reports a style violation

#### Scenario: Compliant layout produces no violation

- **WHEN** a C# file's using block places `System.*` first (with `Windows.*` alongside), then `Microsoft.*`, then `CLIHub.*`, then the remaining third-party namespaces, and every group is alphabetically sorted
- **THEN** no sorting violation is reported

### Requirement: Using groups are separated by a blank line

The style configuration SHALL require a blank line between consecutive using-directive groups in C# files.

#### Scenario: Adjacent groups without separation

- **WHEN** a C# file's using block places the last directive of one group directly above the first directive of the next group with no blank line between them
- **THEN** the style configuration reports a style violation

#### Scenario: Groups separated by a blank line

- **WHEN** a C# file's using block separates consecutive groups with a blank line
- **THEN** no violation is reported

### Requirement: Explicit C# and SDK policy

The repository SHALL declare C# 14.0 as its explicit language version and SHALL declare a stable .NET 10 SDK baseline with controlled feature-band roll-forward and prerelease SDKs disabled. All production and test projects SHALL use this shared policy.

#### Scenario: Project compilation uses the declared language version
- **WHEN** a production or test project is compiled through the repository solution
- **THEN** the compiler uses C# 14.0 rather than an implicit or future SDK-selected language version

#### Scenario: SDK baseline is reproducible
- **WHEN** a developer or CI runner builds the solution with a compatible stable .NET SDK installed
- **THEN** SDK selection starts from the declared .NET 10 baseline and may roll forward only according to the repository's configured feature-band policy

#### Scenario: Prerelease SDK is unavailable by policy
- **WHEN** a prerelease .NET SDK is installed alongside stable SDKs
- **THEN** repository SDK resolution does not select the prerelease SDK for a normal build
