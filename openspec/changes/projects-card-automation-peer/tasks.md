# Tasks

## 1. Automation peer implementation

- [x] 1.1 In `src/CLIHub/Controls/SummaryCard.cs`, add the following using directives (after the
  existing `System.Windows.Controls` import):
  `System.Windows.Automation` — needed for `AutomationProperties`;
  `System.Windows.Automation.Peers` — needed for `FrameworkElementAutomationPeer`,
  `AutomationControlType`, and `AutomationPeer`.
  Note: the directives sort **before** `System.Windows.Controls` because `.editorconfig` pins
  `dotnet_sort_system_directives_first` (System usings sorted alphabetically first).
- [x] 1.2 Add `protected override AutomationPeer OnCreateAutomationPeer()` to the `SummaryCard`
  class body, returning `new SummaryCardAutomationPeer(this)`.
  Verify: `SummaryCard.cs` compiles with the new method present.
- [x] 1.3 Add the `SummaryCardAutomationPeer` class as a `private sealed` nested class inside
  `SummaryCard`, inheriting from `FrameworkElementAutomationPeer`. Implement:
  - Constructor: `SummaryCardAutomationPeer(SummaryCard owner) : base(owner)`.
  - `GetClassNameCore()` → `return "SummaryCard";`
  - `GetAutomationControlTypeCore()` → `return AutomationControlType.Group;`
  - `IsControlElementCore()` → `return true;`
  - `IsContentElementCore()` → `return true;`
  - `GetNameCore()` — implementation per design D3:
    1. Read `AutomationProperties.GetName(owner)` (the CLR accessor for `AutomationProperties.Name`
       attached property); if non-null and non-empty, return it unchanged.
    2. Collect non-empty parts: `owner.Label`, `owner.Value`, `owner.Detail`.
       When `owner.IsBadgeVisible`, append the first non-empty value among
       `owner.BadgeTooltip` and `owner.BadgeText`.
    3. Return `string.Join(", ", parts)`.
  Verify: `dotnet build CLIHub.sln` exits with 0 errors and 0 warnings.
- [x] 1.4 Gate: `dotnet build CLIHub.sln` must exit 0 errors, 0 warnings.
  Evidence: `Build succeeded. 0 Warning(s) 0 Error(s)`.

## 2. Unit tests

- [x] 2.1 In `tests/CLIHub.Tests/Controls/SummaryCardTests.cs`, add the following four tests using
  the existing `RunOnSta` helper and the public
  `UIElementAutomationPeer.CreatePeerForElement(card)` factory (returns `AutomationPeer`).
  All tests run on STA via `RunOnSta`. No template load is required (see design D7).

  Test naming follows the `MethodOrScenario_Condition_ExpectedResult` convention:

  - `GetName_WithBadgeVisible_IncludesStatusText`
    — create a card with `Label="PROJECTS"`, `Value="14"`, `Detail="In the launch window"`,
    `IsBadgeVisible=true`,
    `BadgeTooltip="1 folder missing · folders that no longer exist"`,
    `BadgeText="1 missing"`;
    call `peer.GetName()`; assert the result equals
    `"PROJECTS 14, In the launch window, 1 folder missing · folders that no longer exist"`.

  - `GetName_WithBadgeHidden_OmitsStatusText`
    — same card properties but `IsBadgeVisible=false`;
    assert the result equals `"PROJECTS 14, In the launch window"`.

  - `GetAutomationControlType_Always_ReturnsGroup`
    — create a default `SummaryCard`; assert
    `peer.GetAutomationControlType() == AutomationControlType.Group`.

  - `GetClassName_Always_ReturnsSummaryCard`
    — create a default `SummaryCard`; assert
    `peer.GetClassName() == "SummaryCard"`.

  Note: the file also gained `using System.Windows.Automation;` for `AutomationControlType`; no
  template load was needed and `CreatePeerForElement` returned a non-null peer in all four tests.

- [x] 2.2 Gate: `dotnet test CLIHub.sln` must pass all tests (expect the prior count + 4 new).
  Evidence: `CLIHub.Tests.dll`: Passed 623 (prior 619 + 4), Failed 0; `CLIHub.Core.Windows.Tests.dll`:
  67; `CLIHub.Core.Tests.dll`: 833; all Passed.

## 3. Validation and graph refresh

- [x] 3.1 Run `openspec validate --all` and confirm it exits with 0 errors; record the output as the
  gate evidence.
  Evidence: `Totals: 32 passed, 0 failed (32 items)`; `change/projects-card-automation-peer` passed
  (skip_specs INFO note only; warnings are pre-existing long-requirement advisories on other specs).
- [x] 3.2 Run `node .opencode/scripts/check-model-policy.mjs` and confirm it exits clean.
  Evidence: `model policy check OK (6 roles, 5 presets, 23 models, 6 display providers)`, exit 0.
- [x] 3.3 Run `graphify update .` to refresh the knowledge graph with the new peer class.
  Evidence: AST extraction 39/39 files, graph rebuilt (20869 nodes, 37081 edges), exit 0.
- [x] 3.4 Confirm `git status --short` shows only: `src/CLIHub/Controls/SummaryCard.cs` and
  `tests/CLIHub.Tests/Controls/SummaryCardTests.cs`; no other source or spec file is modified.
  Evidence: besides these two modified files, status shows only the pre-existing `D opencode.json`,
  the untracked mockup, and the change folder itself.

## 4. Manual smoke (blocks commit/archive)

- [x] 4.1 Run the app (`dotnet run --project src/CLIHub/CLIHub.csproj`), open Settings → Projects
  while at least one project folder is missing. Open Inspect.exe (from the Windows SDK) or
  Accessibility Insights and navigate the UIA **control view**. Verify:
  - At least one element with `ClassName = "SummaryCard"` and `ControlType = Group` is present in
    the control view (previously 0 elements; expected 3 — PROJECTS, MANUAL, DISCOVERED).
  - The PROJECTS card's `Name` property reads the composed string, e.g.
    `"PROJECTS 14, In the launch window, 1 folder missing · folders that no longer exist"`.
  - No card appears as a keyboard focus stop (Tab traversal skips all three cards).
  - Narrator in browse mode (Win+Ctrl+Space to start, arrow keys) announces the PROJECTS card's
    composed name and type ("group") without the user focusing it with Tab.
  - **Report the actual result honestly.** If the UIA name or Narrator announcement differs from the
    expected text, record the discrepancy before marking this task passed.

  Evidence (2026-11-08, Windows PowerShell UIA client, real desktop session):
  - control-view `SummaryCard` elements: **3**, all `ControlType.Group`, `Control=True`,
    `Content=True`, `IsKeyboardFocusable=False`.
  - PROJECTS: `Name = "PROJECTS 18, In the launch window, 1 folder missing · folders that no longer exist"`.
  - MANUAL: `Name = "MANUAL 8, Added by you"`.
  - DISCOVERED: `Name = "DISCOVERED 10, From watched folders"`.
  - Narrator (scan mode, arrow keys) announced the PROJECTS card without Tab focus; user confirmed.
  This closes archived change `2026-11-08-projects-card-alert-badge` task 7.3 ("Verify
  accessibility with Narrator/Inspect"), which was explicitly marked "Not independently verified".

- [x] 4.2 Record the smoke result in this tasks file; report to the user and wait for explicit
  `git commit` and `openspec archive` approval before proceeding.
  Recorded above; commit approval requested after the Narrator confirmation.
