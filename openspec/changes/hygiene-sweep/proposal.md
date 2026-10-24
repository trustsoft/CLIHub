# Code-hygiene sweep: five parked backlog items

## Why

`docs/backlog.md` carries five small, individually verified hygiene findings whose revisit triggers
have all fired ("the next dead-member sweep", "path handling changes", "any project-domain change",
"any `.gitignore` edit"). Each is mechanical, none changes a durable behavior, and together they
touch five different corners of the tree — bundling them avoids paying a full verify-and-archive
cycle per one-line edit. The user explicitly selected this bundle.

Every backlog citation was re-verified against the current tree (HEAD
`3e9d873d76cc304977eaaf1aecaad5d108b7a993`) before inclusion. One citation had drifted: the
`RuntimeSelector` registration is at `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs:38`
today, not `:37` as the backlog (and the archived `git-bash-resolution` design) records.

## What Changes

1. **Remove the registered-but-unused `RuntimeSelector`** — delete
   `src/CLIHub.Core.Windows/RuntimeSelector.cs`, its registration
   (`src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs:38`), and
   `tests/CLIHub.Core.Windows.Tests/RuntimeSelectorTests.cs` (its only consumer).
2. **Drop `RuntimeType.WindowsTerminal`** — the deleted selector is the member's only producer. The
   persisted settings model never uses `RuntimeType` (config.json stores the `defaultRuntime` token
   parsed by `RuntimeKinds`, which falls back to PowerShell for unknown values), so the member goes
   with the selector. Full analysis in design D1.
3. **Remove `AgentProcessMatcher`'s unused logger** — field, constructor, and the
   `Microsoft.Extensions.Logging` using; update the six test construction sites that pass a
   `NullLogger`. The DI registration (`ServiceCollectionExtensions.cs:124`) is unchanged.
4. **Add `.gitnexus/` to `.gitignore`** next to `.codegraph/` and `graphify-out/`. Confirmed:
   nothing under `.gitnexus/` is tracked (`git ls-files .gitnexus` is empty, including `run.cjs`),
   and the directory is currently hidden only by the local, uncommitted `.git/info/exclude:7`.
5. **Make `ProjectPathPolicy` public and reuse it** — `ProjectRegistryViewModel.TryRegisterProject`
   replaces its hand-rolled normalize/compare (`ProjectRegistryViewModel.cs:185-196`) with
   `ProjectPathPolicy.Normalize`/`AreEqual`, keeping its validation and try/catch shape. One
   documented corner-case nuance for drive/UNC roots (design D4).
6. **Return `Copy()` from `ProjectService.AddProject`/`AddDiscoveredProject`** — all four return
   points (`ProjectService.cs:84,91,112,119`), aligning them with the read methods and the
   detached-copies invariant; update the `IProjectService` XML docs and add the matching
   mutation-invariant test.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Item-by-item justification:

- Items 1–3 are dead code, constructor wiring, and repository hygiene; no runtime behavior is
  observable through any spec scenario. (Verified: `RuntimeSelector` has no production consumer;
  the matcher never logs; `.gitnexus/` is untracked.)
- Item 4 keeps the Settings Projects page behavior identical for every non-root input. For an
  already-registered drive/UNC root the page now shows its duplicate error instead of silently
  accepting the no-op through `AddProject`'s existing-project branch — both outcomes satisfy the
  spec scenarios "Reject duplicate folder" and "Add an already registered folder" ("no duplicate
  project is created and the existing project remains/selected",
  `openspec/specs/project-management/spec.md:17-19,223-226`), which pin no error message. See
  design D4.
- Item 5 moves the Add methods **toward** the existing "Detached project reads" requirement
  (`openspec/specs/project-management/spec.md:283` — "mutating a returned project record SHALL NOT
  change registered project state"); no scenario asserts anything about what the Add methods
  return, and no existing test asserts reference identity.

Therefore the change opts out of specs (`skip_specs: true` in `.openspec.yaml`), following the
convention of `openspec/changes/archive/2026-09-28-extract-user32-interop/` and
`2026-10-20-fix-settings-open-delay/`. If review rejects the D4 root-path alignment, item 4 must be
dropped rather than reworked (there is no way to reuse the policy while preserving the old root-path
outcome — see D4); it must not be implemented as a spec delta either, since no scenario changes.

## Impact

- Code:
  - Deleted: `src/CLIHub.Core.Windows/RuntimeSelector.cs`,
    `tests/CLIHub.Core.Windows.Tests/RuntimeSelectorTests.cs`.
  - Modified: `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs` (one registration
    line), `src/CLIHub.Core/Infrastructure/Processes/RuntimeType.cs` (one enum member),
    `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs` (logger),
    `src/CLIHub.Core/Projects/ProjectPathPolicy.cs` (internal → public, class + three members),
    `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs` (reuse the policy),
    `src/CLIHub.Core/Projects/ProjectService.cs` (four returns),
    `src/CLIHub.Core/Projects/IProjectService.cs` (XML docs), `.gitignore`.
- Tests: construction-site updates in
  `tests/CLIHub.Core.Tests/Infrastructure/Processes/AgentProcessMatcherTests.cs:14`,
  `tests/CLIHub.Core.Tests/Agents/AgentProcessMonitorTests.cs:22-23`,
  `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs:38`,
  `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs:59`,
  `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs:279,353`; one new mutation-invariant
  test in `tests/CLIHub.Core.Tests/Projects/ProjectServiceTests.cs`. No existing test is weakened;
  none asserts the old behavior (verified — see design "Current state").
- Docs: `docs/backlog.md` — the five swept entries rewritten to the resolved format, one hunk per
  entry; the two pre-existing uncommitted parked entries (foreign-repo shas; resolver role
  override) and every other line untouched.
- Specs: none (see Capabilities).
- Risk: low. The riskiest edit is the `RuntimeType.WindowsTerminal` removal; it is guarded by the
  exhaustive reference census in design "Current state" (no remaining reference after the
  deletions) and the build gate, and by the persistence analysis in D1 (`RuntimeType` never
  round-trips config.json). The only observable nuance is the D4 root-path duplicate error.

## Non-goals

- No change to process matching, launch paths, runtime resolution, or any runtime behavior — only
  dead members and wiring go.
- No removal of `RuntimeInfo.Type` (write-only in production today) or of
  `IProjectService.AddDiscoveredProject` (no production caller) — both are wider cleanups, out of
  scope; only the unused `WindowsTerminal` member goes.
- No rewriting of ADR 0006's historical mention of `RuntimeSelector`
  (`docs/adr/0006-windows-platform-library.md:14`) — ADRs are point-in-time records.
- No CHANGELOG entry now; entries land under the next release heading (the 2026-10-24 changes
  `follow-windows-theme-changes` and `consolidate-version-population` have none yet either).
- No `InternalsVisibleTo` grant for the `CLIHub` app project (see design D4 alternatives).
- No edits to the two pre-existing uncommitted backlog entries, any other parked item, or the legacy
  `Views/MainWindow.xaml.cs` (documented exception; it keeps working with a copied return value
  because it only reads `project.Id`/`project.Name`, `MainWindow.xaml.cs:256-258`).
- No commit, push, or archive in this change's own tasks; those follow the repo approval policy.

## Dependencies

- None on other active changes (`openspec/changes/` holds no other active change).
- Implements the remedies recorded in five `docs/backlog.md` entries: "AddProject returns the live
  instance" (`:145-151`), "ProjectPathPolicy duplication" (`:169-175`), "Matcher's unused logger"
  (`:204-208`), "Registered-but-unused `RuntimeSelector`" (`:210-220`), and "`.gitnexus/` is
  excluded only by the local git exclude file" (`:327-334`).
- Consistent with, and does not contradict, the `project-management` spec's detached-copies
  requirement (`spec.md:281-288`); the Git Bash locator work that parked item 1
  (`archive/2026-10-20-git-bash-resolution`, which explicitly excluded this removal) is already
  archived.
