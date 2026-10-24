# agent-process-matching delta

## ADDED Requirements

### Requirement: Executable-name matching

The system SHALL match a running process to a plugin when the process executable's file name
equals the plugin's declared launch executable's file name, ignoring case. A plugin without a
launch command SHALL match no process.

#### Scenario: Direct executable match

- **WHEN** a process runs from an executable whose file name equals the plugin's declared launch
  executable's file name, ignoring case
- **THEN** the process matches the plugin

#### Scenario: Different executable does not match

- **WHEN** a process runs from an executable whose file name differs from the plugin's launch
  executable's file name
- **THEN** the process does not match the plugin by executable name

#### Scenario: Plugin without a launch command

- **WHEN** a plugin declares no launch command
- **THEN** no running process matches that plugin

### Requirement: Runtime-host command-line matching

The system SHALL additionally match a running process to a plugin when all of the following hold:
the process runs inside a known shared runtime host (`node`, `python`, or `pythonw`), the plugin
declares an npm distribution source with a package name, and the process's command line contains
the plugin's declared package as a `node_modules` path segment — `node_modules\` followed by the
package name (scope separator normalized) followed by a path separator — compared ignoring case
after forward slashes are normalized to backslashes. The trailing path separator SHALL be part of
the marker, so a package name that is a prefix of another package's name SHALL NOT match.

#### Scenario: npm shim command line matches

- **WHEN** a `node.exe` process is running with the command line
  `node.exe C:\Users\Maxim\AppData\Roaming\npm\node_modules\@opencode\cli\bin\opencode.js run`
- **THEN** the process matches the plugin whose declared npm package is `@opencode/cli`

#### Scenario: Unscoped package matches

- **WHEN** a runtime-host process's command line contains `node_modules\cline\` and the plugin's
  declared npm package is `cline`
- **THEN** the process matches that plugin

#### Scenario: Forward slashes in the command line

- **WHEN** a runtime-host process's command line contains the package path with forward slashes
  (for example `node_modules/@qwen-code/qwen-code/bin/qwen.js`)
- **THEN** the command line is normalized and the process matches the plugin whose declared npm
  package is `@qwen-code/qwen-code`

#### Scenario: Sibling package with a common prefix does not match

- **WHEN** a runtime-host process's command line contains `node_modules\@opencode\cli-extra\` and
  the plugin's declared npm package is `@opencode/cli`
- **THEN** the process does not match that plugin

#### Scenario: Unrelated runtime-host script does not match

- **WHEN** a runtime-host process is running with a command line that contains no
  `node_modules\` path segment for any declared npm package (for example `node.exe server.js` or
  `python.exe app.py`)
- **THEN** the process matches no plugin

#### Scenario: Plugin without an npm source never command-line-matches

- **WHEN** a plugin declares no `updateCheck` block or a non-npm source kind
- **THEN** that plugin matches processes by executable name only

#### Scenario: Host process without command-line data does not match

- **WHEN** a runtime-host process's command line is unavailable (null or empty)
- **THEN** the process does not command-line-match any plugin

### Requirement: Match detail tiers

Running-agent matching SHALL offer two detail tiers. The executable-name tier SHALL compare
executable file names only and SHALL NOT retrieve command lines. The command-line tier SHALL
additionally apply runtime-host command-line matching and SHALL be the authoritative basis for
the update execution-time running-agent checks on every surface. Availability checks that run
synchronously on UI requery paths MAY use the executable-name tier as an advisory early-out; in
that case choosing "Update" while a runtime-hosted agent is running SHALL still be blocked by the
command-line-tier execution-time check.

#### Scenario: Requery availability uses the executable-name tier

- **WHEN** an update availability check runs on a synchronous UI requery path
- **THEN** it consults only executable-name matching and performs no command-line retrieval

#### Scenario: Execution-time checks use the command-line tier

- **WHEN** an update is about to start and the running-agent check runs
- **THEN** the check applies command-line matching, so a runtime-hosted agent process blocks the
  update even though its executable is a shared runtime host

#### Scenario: Advisory availability does not bypass the gate

- **WHEN** a UI surface shows "Update" as available because its advisory check used the
  executable-name tier, and a runtime-hosted process of that agent is running
- **THEN** starting the update is blocked by the execution-time check and the surface reports the
  block instead of starting the update

### Requirement: Command-line retrieval degradation

Command-line retrieval SHALL be best effort with a bounded wait. When command lines cannot be
retrieved — the query fails, exceeds its time budget, or a specific process's command line is
unreadable — the system SHALL log a warning, supply no command line for the affected processes,
and evaluate matching for that pass using executable names only. Update availability SHALL NOT be
withheld because command-line retrieval degraded.

#### Scenario: Retrieval failure degrades to executable-name matching

- **WHEN** command-line retrieval fails or times out during a running-agent enumeration
- **THEN** the failure is logged and the enumeration completes using executable-name matching
  only

#### Scenario: Degraded retrieval does not block updates

- **WHEN** command-line retrieval is unavailable and only a runtime-hosted process of the agent
  is running
- **THEN** the update action remains available and the update is not blocked by that process in
  the degraded pass

### Requirement: Candidate narrowing for command-line retrieval

Command-line retrieval SHALL be limited to processes whose names are in the enumeration's
candidate set. Runtime host names SHALL enter the candidate set only when command-line detail is
requested and at least one plugin declares both a launch command and an npm distribution package.
When no candidate-named process is running, no command-line retrieval SHALL occur.

#### Scenario: Host names are candidates only for command-line detail

- **WHEN** running-agent enumeration runs with executable-name detail
- **THEN** runtime host names are not candidates and no command-line retrieval occurs even though
  host processes are running

#### Scenario: No host candidates without an npm-declared agent

- **WHEN** no plugin declares an npm distribution package
- **THEN** runtime host names are not candidates, so a machine running many host processes
  performs no command-line retrieval

#### Scenario: One bulk retrieval per enumeration

- **WHEN** a command-line-detail enumeration runs and candidate-named processes are running
- **THEN** command lines are retrieved with a single bulk query over the candidate names, joined
  to the enumerated processes by process identifier
