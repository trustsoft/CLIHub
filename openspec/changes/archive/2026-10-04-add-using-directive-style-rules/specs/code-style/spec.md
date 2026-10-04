## ADDED Requirements

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
