# preferences-ui (partial): Git Bash and PowerShell 7 availability on Terminal Profiles

## MODIFIED Requirements

### Requirement: Terminal runtime options

The Terminal Profiles page SHALL offer the runtime hosts PowerShell, PowerShell 7, Command Prompt, and Git Bash, and SHALL default to PowerShell when no runtime has been stored. When a runtime's executable cannot be resolved — Git for Windows `bash.exe` or PowerShell 7 `pwsh.exe` — the page SHALL label that option as not installed, SHALL keep that option selectable, and SHALL keep the stored selection unchanged.

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

#### Scenario: Git Bash is marked when it cannot be launched
- **WHEN** the Settings window opens on "Terminal Profiles" and no Git for Windows `bash.exe` can be resolved
- **THEN** the Git Bash option is labelled as not installed
- **AND** the other runtime options and the stored selection are shown unchanged

#### Scenario: Availability is re-evaluated when the page opens
- **WHEN** Git for Windows is installed or removed while the application is running and the Settings window is reopened
- **THEN** the Git Bash option's installed state reflects the current installation

#### Scenario: PowerShell 7 is marked when it cannot be launched
- **WHEN** the Settings window opens on "Terminal Profiles" and no PowerShell 7 `pwsh.exe` can be resolved
- **THEN** the PowerShell 7 option is labelled as not installed
- **AND** the other runtime options and the stored selection are shown unchanged
