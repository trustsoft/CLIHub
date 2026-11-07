# Proposal

## Why

The Settings summary cards (`SummaryCard` control) are invisible to screen readers: verified UIA
inspection on 2026-11-08 shows every card's text reports `IsControlElement = False` and
`IsContentElement = False`, and zero elements with `ClassName = "SummaryCard"` appear in the UIA
control view. This means Narrator (browse/scan mode) never reads any summary card — including the
danger chip and count badge introduced by the archived change `2026-11-08-projects-card-alert-badge`,
which added the spec clause "the status is announced by screen readers without requiring keyboard
focus on the card" (`openspec/specs/settings-theme` requirement "Projects page summary cards",
scenario "Missing folders reported"). That clause is currently unmet. Task 7.3 of the archived
change was not run; this change closes it.

## What Changes

- Add `OnCreateAutomationPeer()` to `src/CLIHub/Controls/SummaryCard.cs`, returning a new
  `SummaryCardAutomationPeer : FrameworkElementAutomationPeer` defined in the same file.
- The peer overrides:
  - `GetClassNameCore()` → `"SummaryCard"`.
  - `GetAutomationControlTypeCore()` → `AutomationControlType.Group`.
  - `GetNameCore()` — compose from non-empty parts (`Label` and `Value` joined by a space as one
    leading part, then `Detail` and, when `IsBadgeVisible`, the `BadgeTooltip` — falling back to
    `BadgeText` when the tooltip is empty — joined by commas); if `AutomationProperties.Name` is set
    explicitly on the owner, return it unchanged.
  - `IsControlElementCore()` → `true` (exposes the card in the UIA control view).
  - `IsContentElementCore()` → `true` (exposes the card in the UIA content view).
- Add unit tests in `tests/CLIHub.Tests/Controls/SummaryCardTests.cs` covering: peer name with badge
  visible includes the status text; peer name with badge hidden omits it; control type is Group;
  class name is SummaryCard.
- The card remains `Focusable=False` and out of the tab order. No template changes; no ViewModel
  changes; no XAML changes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. The durable scenario "Missing folders reported" in `openspec/specs/settings-theme` already
carries the screen-reader announcement clause; this change makes the implementation meet it without
altering any spec text. `skip_specs: true` is declared in `.openspec.yaml`.

## Impact

- `src/CLIHub/Controls/SummaryCard.cs` — `OnCreateAutomationPeer()` override added; new nested
  `SummaryCardAutomationPeer` class added in the same file.
- `tests/CLIHub.Tests/Controls/SummaryCardTests.cs` — four new test methods covering the peer's
  observable properties.
- No other source file is modified.

## Non-goals

- No change to `Focusable`, tab order, or keyboard navigation.
- No change to the XAML template or any ViewModel.
- No change to any spec text; the existing scenario wording already covers the requirement.
- No automation peer for any other control in the project.
- Narrator speech ordering beyond what the composed `Name` string delivers is not controlled here.

## Dependencies

None. No open change touches `SummaryCard.cs` or `SummaryCardTests.cs`.
