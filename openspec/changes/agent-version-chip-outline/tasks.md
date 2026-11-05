# Tasks: Outline the Agent Version Chip

Two small XAML edits, each stage gated by a build/test run. Sized for a fresh session with no prior
context beyond this change's `proposal.md` and `design.md`.

## Stage 1: Add the shared outline style

- [x] 1.1 In `src/CLIHub/Themes/Common/Controls.xaml`, add a new style immediately after
  `VersionChipText` (after line 55, before the `ProjectProvenanceMark` comment on line 57):

  ```xml
  <Style x:Key="OutlineChipBorder" TargetType="Border" BasedOn="{StaticResource VersionChipBorder}">
      <Setter Property="Background" Value="Transparent"/>
      <Setter Property="BorderBrush" Value="{DynamicResource TextMutedBrush}"/>
  </Style>
  ```

- [x] 1.2 Do not modify `VersionChipBorder` or `VersionChipText` themselves.

## Stage 2: Rebase the two agent-list chip styles

- [x] 2.1 In `src/CLIHub/Views/SettingsAgentsPage.xaml`, change `AgentStateChip` (line 13) so its
  `BasedOn` reads `{StaticResource OutlineChipBorder}` instead of `{StaticResource
  VersionChipBorder}`. Keep its `InstallState=NotInstalled` `DataTrigger` exactly as-is.
- [x] 2.2 In the same file, change `AgentOriginChip` (line 52) so its `BasedOn` reads
  `{StaticResource OutlineChipBorder}` instead of `{StaticResource VersionChipBorder}`, and delete
  its now-redundant `Background="Transparent"` and `BorderBrush="{DynamicResource
  TextMutedBrush}"` setters (the base style now supplies both). Keep its `Origin=installed`
  `DataTrigger` exactly as-is.
- [x] 2.3 Leave `InvalidAgentStateChip`, `AgentUpdateChip`, `AgentStateChipText`, and
  `AgentOriginChipText` unchanged (see `design.md` Decisions #3).

**Gate: Build succeeds**

- [x] 2.4 Run `dotnet build CLIHub.sln` — must report 0 errors, 0 warnings.

## Stage 3: Verify and validate

- [x] 3.1 Run `dotnet test CLIHub.sln` — must report 0 failed tests. (No test currently asserts
  chip background/border colors; this gate confirms the edit introduced no regressions elsewhere.)
- [x] 3.2 Manual smoke (requires a human): launch CLIHub, open Settings -> CLI Agents, and compare
  the version/state chip and origin chip against `ui-mockup-agent-version-chip.html`'s "Proposed"
  view in both Dark and Light themes (use the mockup's own toggle to switch). Confirm:
  - The version/state chip (e.g. "v1.2.3") is now transparent-background with a 1px muted border,
    matching the origin chip's outline.
  - The "Not installed" danger state and the "Built-in"/plugin origin badge still render exactly as
    before (no regression).
  - The update-available chip is unchanged (still filled) — this is expected per `design.md`
    Deviations.
- [x] 3.3 Run `openspec validate --all` — must report success (0 errors).
- [x] 3.4 Run `graphify update .` to refresh the knowledge graph after the source edit.

## Notes

- This change is presentation-only: no ViewModel, binding, or behavior change. `skip_specs: true`
  is set in `.openspec.yaml`; no `specs/` delta is produced.
- If Stage 2's build gate fails, check for a typo in the `BasedOn` resource key or a missing closing
  tag; revert the stage's edits and retry rather than patching around a build error blindly.
