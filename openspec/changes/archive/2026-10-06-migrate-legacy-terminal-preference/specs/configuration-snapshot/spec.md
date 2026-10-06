## ADDED Requirements

### Requirement: Legacy terminal preferences are migrated to the runtime token

The configuration migration layer SHALL convert schema 0 `terminalExecutable` preferences into the schema 1 `defaultRuntime` token while preserving unrelated configuration values.

#### Scenario: Windows Terminal executable maps to the Windows Terminal token
- **GIVEN** a legacy configuration contains `terminalExecutable` set to `wt.exe`
- **WHEN** the configuration is migrated to the current schema
- **THEN** `defaultRuntime` is set to `wt`

#### Scenario: Command Prompt executable maps to the Command Prompt token
- **GIVEN** a legacy configuration contains `terminalExecutable` set to `cmd.exe`
- **WHEN** the configuration is migrated to the current schema
- **THEN** `defaultRuntime` is set to `cmd`

#### Scenario: PowerShell executable maps to the PowerShell token
- **GIVEN** a legacy configuration contains `terminalExecutable` set to a PowerShell executable
- **WHEN** the configuration is migrated to the current schema
- **THEN** `defaultRuntime` is set to `ps`

#### Scenario: Missing or unknown executable uses the default runtime
- **GIVEN** a legacy configuration contains no recognized terminal executable
- **WHEN** the configuration is migrated to the current schema
- **THEN** `defaultRuntime` is set to `wt` and unrelated preferences and project state are unchanged
