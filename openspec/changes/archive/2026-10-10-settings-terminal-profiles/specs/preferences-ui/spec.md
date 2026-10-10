## ADDED Requirements

### Requirement: Terminal runtime options

The Terminal Profiles page SHALL offer the runtime hosts PowerShell, PowerShell 7, Command Prompt, and Git Bash, and SHALL default to PowerShell when no runtime has been stored.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Terminal Profiles"
- **THEN** the runtime selector offers PowerShell, PowerShell 7, Command Prompt, and Git Bash, with the stored runtime selected

#### Scenario: Default runtime
- **WHEN** no runtime has been stored or the stored value is not in the set
- **THEN** PowerShell is selected

#### Scenario: Persist on save
- **WHEN** the user selects a runtime and chooses "Save"
- **THEN** the value is written to `config.json`

#### Scenario: Applied without restart
- **WHEN** the user saves a different runtime
- **THEN** subsequent agent commands open in the newly selected runtime without restarting the application
