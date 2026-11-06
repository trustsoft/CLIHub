# Tasks: Update Chip Replaces the Version Chip

Two small XAML edits in one file, each stage gated by a build/test run. Sized for a fresh session
with no prior context beyond this change's `proposal.md` and `design.md`.

## Stage 1: Hide the state chip when an update is available

- [x] 1.1 In `src/CLIHub/Views/SettingsAgentsPage.xaml`, add a second `DataTrigger` to the
  `AgentStateChip` style (~line 13), after the existing `InstallState=NotInstalled` trigger:

  ```xml
  <DataTrigger Binding="{Binding HasUpdateAvailable}" Value="True">
      <Setter Property="Visibility" Value="Collapsed"/>
  </DataTrigger>
  ```

  Do not remove or reorder the existing `NotInstalled` trigger. See `design.md` Decision #3 for why
  trigger order does not matter here (the two triggers never set the same property).
- [x] 1.2 Do not modify `AgentStateChipText`, `InvalidAgentStateChip`, `InvalidAgentStateChipText`,
  `AgentOriginChip`, `AgentOriginChipText`, `AgentUpdateChip`, or `AgentUpdateChipText`.

## Stage 2: Move the update chip into the version chip's slot

- [x] 2.1 In the installed-card row's `StackPanel` (~lines 245-271, inside
  `ItemsControl.ItemTemplate` for `AgentsState.Cards`), cut the entire `AgentUpdateChip` `Border`
  block (the one with `AutomationProperties.Name="Update available"`, currently the third `Border`
  after the name `TextBlock`) and paste it so it becomes the first `Border` after the name
  `TextBlock` — i.e. immediately before the `AgentStateChip` `Border`.
- [x] 2.2 After the move, the row's `Border` order (inside the `StackPanel`, after the name) must
  read: `AgentUpdateChip`, `AgentStateChip`, `AgentOriginChip`. Do not change any `Border`'s
  `Margin`, `ToolTip`, `AutomationProperties.Name`, or child `TextBlock` binding/style while moving
  it — only its position in the `StackPanel` changes.
- [x] 2.3 Do not touch the disabled-card template (~lines 386-405) or the invalid-card template
  (~lines 447-454). Neither renders an `AgentUpdateChip` today and neither gains one in this change.

**Gate: Build succeeds**

- [x] 2.4 Run `dotnet build CLIHub.sln` — must report 0 errors, 0 warnings.

## Stage 3: Verify and validate

- [x] 3.1 Run `dotnet test CLIHub.sln` — must report 0 failed tests. No existing test asserts XAML
  visibility, chip order, or render output (confirmed in `design.md` "Current State"); this gate
  confirms the XAML edit introduced no regression in the ViewModel-level test suite.
- [x] 3.2 Run `openspec validate --all` — must report success (0 errors) for this change and for
  every other change/spec in the repository.
- [x] 3.3 Manual smoke (requires a human; do not skip or self-certify): launch CLIHub, open Settings
  -> CLI Agents, and check an agent row that has an update available (e.g. Owen Code, or any agent
  for which "Check updates" reports one). Confirm:
  - The row reads `{Name} [{installed} → {latest}] [{origin badge}]` — the update chip sits
    immediately after the name, before the origin badge, and no separate version/state chip is
    shown next to it.
  - The update chip's accent styling, tooltip ("Newer version available" on hover), and automation
    name ("Update available") are unchanged from before this change.
  - For every other row (no update available, not installed, mid-detect), the version/state chip
    still renders normally in its usual position and styling: a plain version or "Installed" chip
    when up to date or unchecked, the danger-styled "Not installed" chip when missing, and
    "Detecting…" during a probe. None of these rows show an update chip.
  - Run "Check updates" (header or per-card) on a row that currently shows the update chip, if a
    path exists to make it settle back to up-to-date or unknown in this test environment; confirm
    the version/state chip reappears in its usual position when the update chip disappears. If no
    such path is available in this environment, note that this sub-check could not be exercised and
    rely on the automated coverage plus the state table in `design.md` Decision #3 instead.
  - Compare both Dark and Light themes; no color or spacing regression on either chip.
- [x] 3.4 Run `graphify update .` to refresh the knowledge graph after the source edit.

## Notes

- This change is presentation-only: no ViewModel, converter, or binding change. A `specs/` delta is
  included because it changes a stated requirement ("Agent card displayed content" in
  `openspec/specs/agent-ui/spec.md`) — this is not a `skip_specs: true` change.
- If Stage 2's build gate fails, check for an unbalanced `<Border>`/`</Border>` tag introduced while
  moving the block, or a `Border` pasted outside the `StackPanel`; revert the stage's edit and retry
  the cut/paste rather than patching around a build error blindly.
- If Stage 1's `DataTrigger` appears to have no effect after Stage 2, confirm the edited style is
  still named `AgentStateChip` and that the installed-card row's state-chip `Border` still has
  `Style="{StaticResource AgentStateChip}"` (it must not have been accidentally changed while moving
  the neighboring update chip).
