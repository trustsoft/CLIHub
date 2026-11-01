# agent-detection (partial): consistent settling after an aborted on-demand pass

## MODIFIED Requirements

### Requirement: On-demand detection re-evaluation

The system SHALL re-evaluate an agent's host installation state on demand, bypassing the cached
result, and report the outcome. When a multi-card detection pass is aborted — a probe throws, or
the pass is abandoned because the owning view is disposed mid-pass — the system SHALL settle every
card consistently: a card the pass had already applied a fresh probe result to SHALL keep that
fresh result (installation state, version, and update-availability indicator together), and a card
the pass had not yet resolved SHALL be restored to its complete pre-pass state (installation state,
version, and update-availability indicator together), so no card is left announcing an in-progress
probe that will never complete and no card is left with an installation state that disagrees with
its shown version or update indicator.

#### Scenario: Manual probe bypasses the cache
- **WHEN** a manual probe is requested for an agent
- **THEN** the host installation state is recomputed from the file system rather than read from the cache

#### Scenario: Probe reports installed
- **WHEN** a manual probe finds a declared system path present
- **THEN** the agent is reported as installed on the host

#### Scenario: Probe reports not installed
- **WHEN** a manual probe finds none of the declared system paths present
- **THEN** the agent is reported as not installed on the host

#### Scenario: Probe requires no project
- **WHEN** a manual probe is requested and no project is selected
- **THEN** the host installation state is still evaluated

#### Scenario: An aborted pass keeps already-settled cards' fresh results
- **WHEN** a multi-card detection pass has already applied a fresh probe result to one card and a
  later card's probe then throws
- **THEN** the already-settled card keeps its fresh installation state and version exactly as the
  probe reported them
- **AND** its update-availability indicator is consistent with that state — preserved when the
  agent is installed and cleared when it is not, as a probe result always settles it
- **AND** it is not rolled back to its pre-pass state

#### Scenario: An aborted pass restores every unresolved card completely
- **WHEN** a multi-card detection pass's probe throws before a card's result has been applied
- **THEN** every card the pass had not yet resolved is restored to its complete pre-pass
  installation state, version, and update-availability indicator together
- **AND** no restored card is left showing a version or update indicator inconsistent with its
  restored installation state

#### Scenario: A pass abandoned by disposal settles every unresolved card
- **WHEN** a multi-card detection pass is abandoned because the owning view is disposed before every
  card has been probed
- **THEN** every card the pass had not yet resolved is restored to its complete pre-pass state exactly
  as it would be for a thrown probe
- **AND** no card is left showing the in-progress "Detecting" state
