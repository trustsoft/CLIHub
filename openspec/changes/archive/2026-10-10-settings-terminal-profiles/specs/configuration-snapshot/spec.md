## REMOVED Requirements

### Requirement: Legacy terminal preferences are migrated to the runtime token

**Reason**: The runtime set no longer includes Windows Terminal, so the schema 0 mapping must target the available shells.

**Migration**: A legacy `terminalExecutable` of `wt.exe` or any unrecognized value now maps to `ps` (PowerShell); `cmd.exe` maps to `cmd`; a PowerShell executable maps to `ps`. See the replacement requirement below.

## ADDED Requirements

### Requirement: Legacy terminal executable maps to the available shells

The configuration migration layer SHALL convert a schema 0 `terminalExecutable` preference into a `defaultRuntime` token from the available set (`ps`, `ps7`, `cmd`, `bash`) while preserving unrelated configuration values.

#### Scenario: Windows Terminal executable maps to PowerShell
- **GIVEN** a legacy configuration contains `terminalExecutable` set to `wt.exe`
- **WHEN** the configuration is migrated to the current schema
- **THEN** `defaultRuntime` is set to `ps`

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
- **THEN** `defaultRuntime` is set to `ps` and unrelated preferences and project state are unchanged

### Requirement: Runtime tokens resolve to the available shells

The configuration boundary SHALL parse only the available runtime tokens (`ps`, `ps7`, `cmd`, `bash`) and SHALL resolve any missing, unknown, or no-longer-supported token (including the removed `wt`) to the default runtime PowerShell, without preventing the application from starting.

#### Scenario: Known tokens map to their runtime
- **GIVEN** a configuration stores `defaultRuntime` as `ps`, `ps7`, `cmd`, or `bash`
- **WHEN** the configuration is loaded
- **THEN** the corresponding runtime is used

#### Scenario: Removed or unknown token resolves to the default
- **GIVEN** a configuration stores `defaultRuntime` as the removed token `wt` or any unrecognized value
- **WHEN** the configuration is loaded
- **THEN** PowerShell is used and the application starts normally
