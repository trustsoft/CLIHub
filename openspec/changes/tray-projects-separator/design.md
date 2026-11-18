## Context

See `proposal.md` — Why and What Changes. This change has two parts: two tray-menu separators (already
landed in the working tree) and the version header that replaces the current-project header.

### Verified current state (2026-11-18)

**`TrayMenuBuilder.Populate` (`src/CLIHub/Tray/TrayMenuBuilder.cs:36-85`).** The header item (`:44-48`)
renders `Current: <name>` / `No project selected` and is `IsEnabled = false`; the header separator
(`:50`) and project-block separator (`:68`) are already present, and the pre-`Exit` separator (`:80`) is
unchanged — three separators.

**`TrayMenuState` (`src/CLIHub/Tray/TrayMenuState.cs:23-27`).** The record's first field is
`Project? CurrentProject`. `TrayMenuBuilder.cs:46` is the only reader of `state.CurrentProject`
(verified by grep); the only producer is `TrayStateProjection.GetState()`.

**`TrayStateProjection` (`src/CLIHub/Tray/TrayStateProjection.cs:60-74`).** `GetState()` passes
`this.projects.GetCurrentProject()` as the first argument (`:69`); the projection already holds
`this.updates` (`IUpdateWorkflow`, `:17`). `IUpdateWorkflow : IUpdateVersionProvider`
(`src/CLIHub/Updates/IUpdateWorkflow.cs:8`); `GetCurrentVersion()` returns a normalized version or
`"unknown"`, never empty (`src/CLIHub.Core/Updates/UpdateService.cs:103-114`,
`src/CLIHub.Core/Updates/IUpdateVersionProvider.cs:9-11`).

**Tests.** `TrayMenuBuilderTests.CreateState` (`:545-576`) sets up `projects.GetCurrentProject()` and
uses a fallback `Mock<IUpdateWorkflow>()` with no `GetCurrentVersion()` stub; two assertions read the
header as `"No project selected"` (`:30`, `:471`). `TrayActionsTests.GetState_…` (`:18-50`) asserts
`state.CurrentProject` (`:43`) and stubs `projects.GetCurrentProject()` (`:26`); its `updates` mock
(`:30-32`) does not stub `GetCurrentVersion()`.

**Spec Purpose.** `openspec/specs/tray-menu/spec.md:4-7` still says "the current-project header". A
delta cannot modify the Purpose, so this is fixed post-archive (see `tasks.md`).

## Decisions

### Decision 1: Insert separators after the header and after the project block (DECIDED, already landed)

In `Populate`, a `Separator` follows the disabled header and another follows the project block; the
pre-`Exit` separator is untouched. Rationale: each `Separator` is an icon-less, label-less rule, so the
header and the project block read as their own groups.

### Decision 2: The header shows the version from the update workflow, not assembly metadata (DECIDED)

`TrayMenuState` replaces `Project? CurrentProject` with `string Version` (first field);
`TrayStateProjection.GetState()` passes `this.updates.GetCurrentVersion()`; `TrayMenuBuilder` renders
`$"Version {state.Version}"`. The version is sourced from `IUpdateWorkflow` (an `IUpdateVersionProvider`),
not read from assembly metadata in the builder/projection.

Rationale: `UpdateService.GetCurrentVersion()` already centralizes the version — it strips build
metadata via `UpdateVersionText.StripBuildMetadata` and returns the `"unknown"` fallback when
undeterminable, so the tray header always shows a non-empty, normalized value and agrees with the
Settings page "Version" chip (which also uses `IUpdateVersionProvider`). The projection already holds
`IUpdateWorkflow`, so no new dependency is added.

Consequence: the tray no longer surfaces the current project. The concept itself remains in the app —
`IProjectService.GetCurrentProject()` is still used by the launch flow (`TrayCommandHandlers.cs:100`) —
so only the tray *display* changes. `TrayMenuBuilder.cs:46` is the sole reader of `state.CurrentProject`,
so removing the field breaks no other production code.

### Alternatives rejected

- **Read assembly metadata directly in the builder/projection.** Rejected: duplicates the normalization
  + `unknown` fallback that `UpdateService.GetCurrentVersion()` already owns, and risks the tray header
  disagreeing with the Settings version chip.
- **Keep `CurrentProject` in the record alongside `Version`.** Rejected: the current project is no
  longer displayed anywhere in the tray, so a dead field would linger for no consumer.

## Test Plan

Stage 4 of `tasks.md` updates both test files: `TrayMenuBuilderTests` (drop `currentProject` plumbing,
stub `GetCurrentVersion()` in `CreateState` and the four update-state mocks, change the two
`"No project selected"` assertions to `"Version 1.2.1"`) and `TrayActionsTests` (replace the
`state.CurrentProject` assertion with `state.Version` and stub `GetCurrentVersion()`). No assertion is
weakened; the two header scenarios keep their names but are rewritten to the version header, and the
new scenario "Header shows the current version" states the removed-behavior fact explicitly (no item
names the current project).

Gates: `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, `openspec validate --all`,
`node .opencode/scripts/pipeline-state.mjs check`, `graphify update .` (see `tasks.md`).

## Deviations

One deviation from the brief: it asked to "retire" the two header scenario names. OpenSpec's validator
rejects a MODIFIED requirement that omits a scenario still present in the main spec (verified:
`openspec validate` fails with "omits scenario(s) the current spec still has: … Header reflects the
current project … Header when no project is selected"). So the two names are kept and their WHEN/THEN
rewritten to the version header, and the canonical "Header shows the current version" scenario is added
alongside them. No other behavior moves.

## Open Questions

None open. The behavior the two header scenarios described (showing the current project in the tray
header) is genuinely deleted — their content is rewritten to the `Version <version>` header and the new
"Header shows the current version" scenario states that no item names the current project. The
capability Purpose sentence is fixed post-archive because a delta cannot modify it.
