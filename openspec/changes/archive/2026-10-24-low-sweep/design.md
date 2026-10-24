# Design: low-sweep

All line numbers below were verified against the current working tree on 2026-10-24 (clean tree,
no active changes). Backlog citations that drifted are called out inline.

## Current state (verified)

### Item A — magic version sentinels

Backlog claim ("`"…"` and `"unknown"` are checked in `AgentCardItem`, `AgentManagementViewModel`,
`AgentPaneController`, and legacy `MainWindow`"): **confirmed**, with two precision notes:

1. The `AgentCardItem.cs:19` check belongs to `AgentOption.DisplayName` (the default-agent
   dropdown record lives in the same file), and `AgentCardItem` itself checks sentinels at
   `:193-194` (`HasKnownVersion`).
2. The backlog misses one placeholder producer: `AgentItem.Version`'s property initializer
   `} = "…";` (`src/CLIHub/ViewModels/Agents/AgentItem.cs:107`) — the launch-window row's
   pending placeholder. It is in scope (a live surface), not a new finding that needs rescoping.

Complete census of live agent-version sentinel sites (grep `"…"|"unknown"` over `src/`, then
read each site):

| Site | Code | Role |
|---|---|---|
| `src/CLIHub/ViewModels/Agents/AgentItem.cs:107` | `} = "…";` | pending placeholder producer (launch-window row) |
| `src/CLIHub/ViewModels/Agents/AgentCardItem.cs:19` | `string.IsNullOrEmpty(this.Version) \|\| this.Version == "…"` | `AgentOption.DisplayName` pending check |
| `src/CLIHub/ViewModels/Agents/AgentCardItem.cs:193-194` | `version is not ("…" or "unknown")` | `AgentCardItem.HasKnownVersion` (feeds `VersionChipText`, `:109-110`) |
| `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:123` | `string.IsNullOrEmpty(version) \|\| version == "…"` | `DefaultAgentDetail` pending check |
| `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:211` | `"…",` | card constructor placeholder in `Load` |
| `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:304` | `card.Version = ver ?? "unknown";` | version-population fallback |
| `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:397` | `card.Version = result.Version ?? "unknown";` | `TestAsync` fallback |
| `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:420` | `card.Version = result.Version ?? "unknown";` | `DetectAsync` fallback |
| `src/CLIHub/ViewModels/Agents/AgentPaneController.cs:182` | `item.Version = version ?? "unknown";` | pane version-population fallback |
| `src/CLIHub/Views/MainWindow.xaml.cs:206` | `item.Version = version ?? "unknown";` | legacy copy — **untouched exception** |

Out-of-domain sentinels deliberately excluded: `src/CLIHub.Core/Updates/UpdateService.cs:132`
and `src/CLIHub.Core/Updates/IUpdateVersionProvider.cs:9` — that `"unknown"` is CLIHub's own
application version, not agent version display (different capability, `update-checking`).

Existing tests that pin the strings (and must keep passing unedited):
`tests/CLIHub.Tests/ViewModels/Agents/AgentCardItemTests.cs:28-29` (`[InlineData("…")]`,
`[InlineData("unknown")]`), `:41` (`version: "unknown"`),
`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs:224,297`
(`Assert.Equal("…", ...)`), `tests/CLIHub.Tests/ViewModels/Agents/AgentPaneControllerTests.cs:179`
(`Assert.Equal("…", ...)`).

Spec touchpoint: `openspec/specs/agent-version/spec.md:79-83` — "the list shows a placeholder
(for example, `unknown`)". Loose ("for example") wording; preserving the exact strings keeps it
true.

### Item B — update-detection test gaps

Backlog claim: **confirmed exactly.**

- Timeout path: `src/CLIHub.Core/Agents/AgentReleaseClient.cs:59-62` bounds the request with a
  linked CTS `CancelAfter(this.ProbeTimeout)`; the catch at `:80-84`
  (`OperationCanceledException` when the *caller's* token is not cancelled) logs the timeout and
  returns `null`. `ProbeTimeout` (`:163-169`) reads `AgentProbeTimeoutSeconds` (`int?`, honored
  when `> 0`, else `AgentVersionService.DefaultProbeTimeout` = 10 s,
  `src/CLIHub.Core/Agents/AgentVersionService.cs:26`).
- Existing `AgentReleaseClientTests` (`tests/CLIHub.Core.Tests/Agents/AgentReleaseClientTests.cs`)
  cover non-success (`:60-68`), `HttpRequestException` (`:71-79`), malformed JSON (`:82-90`),
  missing field (`:93-101`), User-Agent (`:104-113`), and block validation (`:115-131`) — no
  timeout test. The `StubHandler` (`:133-154`) has `Response`/`Throw` but no delay mode, which
  is exactly why the linked-token-timeout branch stayed uncovered: the caller token is
  `CancellationToken.None` in production (`AgentUpdateAvailabilityService.cs:126`), so the only
  way to reach the branch is the probe timeout itself firing.
- Card indicator: the spec scenario "No indicator for a missing installation"
  (`openspec/specs/agent-ui/spec.md:330-333`) holds by construction: uninstalled → installed
  version `null` → `AgentUpdateAvailabilityService.ComputeAvailabilityAsync` returns `Unknown`
  before any release request (`src/CLIHub.Core/Agents/AgentUpdateAvailabilityService.cs:114-120`)
  → `AgentCardItem.UpdateAvailabilityText` is `null` (`AgentCardItem.cs:157-160`, the pattern
  requires `Status: UpdateAvailable`). Coverage today is compositional on both halves:
  service-side `Check_UnknownInstalledVersion_SkipsReleaseRequest`
  (`tests/CLIHub.Core.Tests/Agents/AgentUpdateAvailabilityServiceTests.cs:132-143`) and
  card-side `UpdateAvailability_WhenUnknown_ShowsNoIndicator`
  (`tests/CLIHub.Tests/ViewModels/Agents/AgentCardItemTests.cs:88-96`, installed card only).
  No test combines an uninstalled card with a declared `updateCheck`.

### Item E — registry InstallPath precedence

Backlog claim: **confirmed with one precision note.**
`src/CLIHub.Core.Windows/WindowsRuntimeLocator.cs:77-90`
(`WindowsRuntimeEnvironment.ReadGitForWindowsInstallPath`) chains `??` across four reads —
HKLM/Registry64 (`:86`), HKLM/Registry32 (`:87`), HKCU/Default (`:88`), **HKCU/Registry32
(`:89`)** — so the backlog's "HKLM64 → HKLM32 → HKCU views" understates by one view; the
backlog's `:77-92` span is right. Each read (`ReadInstallPath`, `:170-191`) returns the first
non-empty trimmed `InstallPath` or `null` (registry faults → `null`). The locator consumes a
single value (`ReadGitRegistryInstallPath`, `:370-389`) and tries exactly one registry candidate
(`LocateGitBash`, `:233-246`; `TryGitCandidate` checks `<root>\bin\bash.exe`, `:412-437`); when
that candidate is stale the registry step yields nothing and the lookup falls to well-known
roots (`:248-258`) and PATH (`:260-295`). So a stale HKLM value shadows a valid HKCU/32-bit
registry value today; the common per-user install location
(`%LOCALAPPDATA%\Programs\Git`) is rescued by the well-known step (`:113-117`), but a custom
per-user or 32-bit install dir recorded only in a later view is not.

Test state, verified: `tests/CLIHub.Core.Windows.Tests/` contains no
`WindowsRuntimeEnvironmentTests` — only `WindowsRuntimeLocatorTests` exercises the locator over
`FakeWindowsRuntimeEnvironment` (`FakeWindowsRuntimeEnvironment.cs:9`, single `InstallPath`
property at `:16`, `ThrowOnRegistryRead` at `:27`), so the real registry reader has no direct
test. `InternalsVisibleTo` for both test projects exists
(`src/CLIHub.Core.Windows/CLIHub.Core.Windows.csproj:19-20`), so internal types are testable.

Spec check (the decision gate for skip_specs): the owning requirement is "Terminal vs captured
execution" in `openspec/specs/agent-commands/spec.md:64-119`. Its Git Bash scenario pins only
the source order — "resolved from the `GitForWindows` registry `InstallPath`, a well-known
install location, or a PATH entry outside the Windows directory, in that order"
(`spec.md:99-103`) — plus launch-time re-resolution (`:103`), the WSL-launcher exclusion
(`:105-109`), and safe failure when nothing resolves (`:111-114`). **The spec names no registry
hive or view and no first-non-empty rule; it does not pin the registry precedence being fixed.**
The locator's failure log already summarizes the step as "HKLM/HKCU SOFTWARE\GitForWindows
InstallPath" (`WindowsRuntimeLocator.cs:297-300`), which stays accurate.

## Decisions

### Item A

**A1 — Helper shape and placement: app-layer `Helpers/AgentVersionText.cs`, public static,
`PreferenceSyncHelper` pattern.** All five consumers are in the `CLIHub` app project
(`ViewModels/Agents/*`), and placeholder text is UI display state, which the repo keeps out of
`CLIHub.Core` (Core's `AgentVersionService` returns raw `string?` versions; the `?? "unknown"`
fallback is applied by the surfaces). `src/CLIHub/Helpers/` already hosts exactly this kind of
small static helper (`PreferenceSyncHelper.cs:9`, `public static class`, XML-documented), with a
direct test beside it (`tests/CLIHub.Tests/Helpers/PreferenceSyncHelperTests.cs`). Rejected:
Core placement (`src/CLIHub.Core/Agents/`, beside `AgentConfigurationPath`) — no Core consumer,
and it would put UI text into the UI-independent layer; see Alternatives.

**A2 — API: two constants + two predicates, preserving the asymmetric checks.**

```csharp
public static class AgentVersionText
{
    public const string Pending = "…";     // probe not finished
    public const string Unknown = "unknown"; // probe finished without a version

    public static bool IsPending(string? version) =>
        string.IsNullOrEmpty(version) || version == Pending;

    public static bool IsKnown(string? version) =>
        !IsPending(version) && version != Unknown;
}
```

The current checks are deliberately asymmetric and the helper must preserve each site exactly:
`AgentOption.DisplayName` (`AgentCardItem.cs:19`) and `DefaultAgentDetail`
(`AgentManagementViewModel.cs:123`) treat only null/empty/`"…"` as pending (a later `"unknown"`
renders as `Name (unknown)` / `Version unknown`), while `AgentCardItem.HasKnownVersion`
(`:193-194`) excludes both sentinels. Mapping:

- `AgentItem.cs:107` → `= AgentVersionText.Pending;`
- `AgentCardItem.cs:19` → `AgentVersionText.IsPending(this.Version) ? this.Name : ...`
- `AgentCardItem.cs:193-194` → `AgentVersionText.IsKnown(version)`
- `AgentManagementViewModel.cs:123` → `AgentVersionText.IsPending(version) ? ... : ...`
- `AgentManagementViewModel.cs:211` → `AgentVersionText.Pending,`
- `AgentManagementViewModel.cs:304,397,420`, `AgentPaneController.cs:182` →
  `?? AgentVersionText.Unknown`
- `Views/MainWindow.xaml.cs:206` — untouched (documented exception; the window is never
  constructed, `docs/project-context.md:51`).

Naming: `Pending`/`Unknown` over the backlog's single `Placeholder` because the two strings
carry different meanings (in-flight vs failed/absent) and the checks differ; collapsing them to
one `Placeholder` would either change behavior or lie about one of the two. The `IsKnown` /
`IsPending` names follow the existing `HasKnownVersion` vocabulary.

**A3 — Tests: one small direct test class, no existing test edits.**
`tests/CLIHub.Tests/Helpers/AgentVersionTextTests.cs` pins: `IsPending` true for `null`, `""`,
`"…"` and false for `"unknown"`/`"1.2.3"`; `IsKnown` false for all four sentinel inputs
(including `null`/empty) and true for `"1.2.3"`. The consumer tests listed in Current state
already pin the rendered strings end-to-end and stay green unedited — that is the
"preserve exact display behavior" gate.

### Item B

**B1 — Timeout test mechanics: token-respecting delay in the existing `StubHandler`, 1-second
configured probe timeout.** Add `public TimeSpan? Delay { get; set; }` to the private
`StubHandler` and, in `SendAsync`, `await Task.Delay(delay, cancellationToken)` before the
response. The test builds its own client over `PreferencesStore(new
FakeConfigurationRepository())` with `preferences.Update(p => p.AgentProbeTimeoutSeconds = 1)`
(`PreferencesStore.Update` → repository `Update`, `PreferencesStore.cs:25-30`;
`AgentProbeTimeoutSeconds` is a settable `int?`, `src/CLIHub.Core/Models/Configuration/AppConfig.cs:81`;
the effective floor is 1 s because `ProbeTimeout` honors `seconds is > 0`,
`AgentReleaseClient.cs:163-169`). With `Delay = 30 s`, the linked CTS fires at ~1 s,
`Task.Delay` throws `TaskCanceledException` (an `OperationCanceledException`), the filter at
`:80` passes (the caller token is `default`), and the method returns `null`. Assert `null`;
optionally assert the returned task completes well under the 30 s delay (a generous bound like
`< 10 s` cannot flake). The test costs ~1 s wall time; no timing assertion on the exact
timeout duration. Do not cancel the *caller* token instead — that path deliberately does not
take the timeout branch (the exception propagates), which is precisely the distinction the
filter encodes.

**B2 — Card test mechanics: uninstalled card + declared `updateCheck` + `Unknown` availability.**
New fact in `AgentCardItemTests` (own plugin fixture declaring
`UpdateCheck = new PluginUpdateCheck("npm", Package: "test-pkg")`, mirroring
`PluginWithUpdateCheck` in the service tests), `isInstalled: false`, version placeholder, then
`card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.Unknown)` — the value
the real service returns for an uninstalled agent (pinned service-side by
`Check_UnknownInstalledVersion_SkipsReleaseRequest`, which also proves no source request is
made). Assert `UpdateAvailabilityText` is `null` and `HasUpdateAvailable` is `false`. The
declared block in the fixture documents the scenario's precondition and guards against a future
card change that keys the indicator on installation/source combinations; the card itself never
reads `UpdateCheck` (verified — `AgentCardItem` touches only `Plugin.Commands`, `:46,:205-222`),
which is honestly noted in the test comment rather than papered over.

**B3 — No production changes.** Both gaps are coverage gaps; the code paths are correct as
verified. Anything more (e.g., surfacing "timed out" distinctly) would change the pinned
"degrade to Unknown" contract (`agent-update-detection` spec.md:84-108) — rejected.

### Item E

**E1 — Seam change: plural read.** `IWindowsRuntimeEnvironment.ReadGitForWindowsInstallPath()`
(`:22`) becomes `ReadGitForWindowsInstallPaths()` returning `IReadOnlyList<string>`: every
non-empty, trimmed `InstallPath` across the four views in the existing order. The interface XML
doc (`:18-21`) is rewritten for the plural, per-view-fault semantics (a view that faults
contributes nothing; `ReadInstallPath` already swallows per-view faults, `:179-190`).
`ReadPowerShellCoreInstallPath` (`:30`, `:93-104`) is deliberately unchanged — the PowerShell 7
MSI is machine-wide HKLM-only by design, and its comment says so (`:100-101`).

**E2 — Collector seam for hermetic testing.** The collection logic moves to an internal static
helper on `WindowsRuntimeEnvironment`:

```csharp
internal static IReadOnlyList<string> CollectInstallPaths(
    IReadOnlyList<(RegistryHive Hive, RegistryView View)> views,
    Func<RegistryHive, RegistryView, string?> readInstallPath)
```

iterating `views` in order and keeping non-whitespace trimmed values. The instance method
becomes a guard (`OperatingSystem.IsWindows()`, else empty) plus one call passing
`GitViews = [(LocalMachine, Registry64), (LocalMachine, Registry32), (CurrentUser, Default),
(CurrentUser, Registry32)]` and `(hive, view) => this.ReadInstallPath(GitForWindowsKeyPath,
hive, view)`. This makes the view order, non-empty filtering, and trimming testable without any
registry access. No de-duplication: if two HKCU views alias the same physical key the duplicate
value is probed twice — harmless (a stale duplicate fails twice; a valid one succeeds on the
first hit) and not worth the extra surface.

**E3 — Locator change: verify-then-prefer loop.** `ReadGitRegistryInstallPath`
(`:370-389`) becomes `ReadGitRegistryInstallPaths()` returning the candidate list (empty on a
registry fault — keep the existing try/catch so a throwing seam still degrades to a miss, which
`Locate_GitBash_RegistryThrows_FallsThroughToPath` pins). `LocateGitBash`'s registry step
(`:233-246`) becomes `foreach (var installPath in this.ReadGitRegistryInstallPaths())` around
the unchanged `TryGitCandidate` + success log. Behavior: first candidate whose `bin\bash.exe`
exists wins; when every candidate is stale the step falls through to well-known and then PATH
exactly as the single-stale-candidate case does today. The warning text at `:297-300` stays
accurate. The three-source order pinned by `agent-commands` is untouched.

**E4 — Real-registry test strategy: HKCU-only, save/restore-guarded, containment assertion.**
Writing HKLM from tests needs admin (unavailable, rejected); writing HKCU is permitted for the
current user but pollutes the machine, so the new
`tests/CLIHub.Core.Windows.Tests/WindowsRuntimeEnvironmentTests.cs` real-registry fact:

1. opens `HKCU\SOFTWARE\GitForWindows` (default view) and records whether the key and the
   `InstallPath` value existed, and the prior value;
2. writes a unique marker path (e.g. `$"C:\low-sweep-marker-{Guid.NewGuid():N}"`);
3. calls `new WindowsRuntimeEnvironment().ReadGitForWindowsInstallPaths()`;
4. asserts `Assert.Contains(marker, result)` — **containment, not sequence**: HKLM views are
   machine state (a dev box with machine-wide Git contributes its real InstallPath), and the
   HKCU default/32-bit views may alias the same physical key and yield the marker twice
   (shared-key HKCU redirection; tolerated by design E2);
5. `finally` restores the prior value or deletes the value, and deletes the key only if this
   test created it.

Residual risk, accepted and documented in the test: during the millisecond write window a
concurrently launched CLIHub could observe the marker — with E3 it probes
`marker\bin\bash.exe`, finds nothing, and moves on to the remaining candidates, so the window
is benign. This is the same machine-state sensitivity class as the parked "ApplicationHost
null-argument tests" entry (`docs/backlog.md:303-317`), but containment-only assertions keep it
green regardless of machine state. What this test deliberately does **not** pin: cross-hive
ordering against real HKLM values (uncontrollable without admin) — that is E2's hermetic job.

**E5 — Fake and locator tests.** `FakeWindowsRuntimeEnvironment.InstallPath` (`string?`,
`:16`) becomes `List<string> InstallPaths { get; } = [];` returned by the plural method
(`ThrowOnRegistryRead` still throws `IOException` from it). Three existing call sites update
mechanically to collection initializers (`WindowsRuntimeLocatorTests.cs:13-17`, `:96`). New
locator facts: (a) stale first registry candidate + verifiable second → second wins, well-known
and PATH never consulted; (b) all registry candidates stale → well-known root wins (pins the
preserved fall-through). New hermetic collector facts (E2): view order; null/whitespace values
skipped; values trimmed.

**E6 — skip_specs for item E.** Verified: the `agent-commands` scenario pins the registry →
well-known → PATH order, not the registry-view precedence (see Current state). The fix stays
inside the registry step, keeps the pinned order, and leaves the unresolvable-failure scenario
untouched, so no scenario's observable contract changes wording or truth. This follows the
`hygiene-sweep` precedent of skip_specs with a per-item justification when every affected
scenario remains satisfied. Rejected alternative: adding an `agent-commands` scenario (e.g.
"A stale registry InstallPath is skipped in favor of a later view's valid install") under
"Terminal vs captured execution" — rejected as the default because it would pin registry-view
mechanics the spec has deliberately stayed above (it does not even name the hives), but it
remains available if review prefers an explicit contract; see open question Q1.

## Alternatives considered

- **`AgentVersionText` in `CLIHub.Core`** (beside `AgentConfigurationPath`): rejected — no Core
  consumer; placeholder strings are UI display text and Core is UI-independent by rule
  (`AGENTS.md`, `docs/project-context.md:32-34`).
- **A shared sentinel enum / typed version struct** flowing out of `AgentVersionService`:
  rejected — changes the `IAgentVersionService` contract and the public record/property shapes
  for zero behavior gain; the backlog asked for a text helper.
- **Normalizing `"unknown"` out of `AgentOption.DisplayName`** (rendering `Name` instead of
  `Name (unknown)` after a failed probe): rejected — changes rendered text; the mandate is
  exact display behavior.
- **Timeout test via a canceled caller token**: rejected — takes the exception-propagation
  path, not the timeout branch (the `when (!cancellationToken.IsCancellationRequested)` filter
  exists precisely to separate them; `AgentReleaseClient.cs:80`).
- **Injecting a fake `RegistryKey` factory into `WindowsRuntimeEnvironment`** for full
  registry-shape testing: rejected — `Microsoft.Win32.RegistryKey` surface is not
  subclass-friendly for `OpenSubKey`/`GetValue`, and the E2 delegate seam achieves the same
  coverage with less machinery.
- **Elevated tests writing HKLM layouts**: rejected — admin requirement, CI portability,
  machine pollution.
- **Validating Git Bash PATH hits against the distribution** (the other parked Git item):
  explicitly out of scope; stays parked under its own trigger.

## Deviations

None. All three items were verified as open and matching the backlog (with the two precision
notes in Current state: the extra `AgentItem.cs:107` producer in scope for A, and the fourth
registry view — HKCU/Registry32 — for E). No item needed dropping or rescoping.

## Open questions

1. **Item E spec delta (default: no).** Design E6 opts for `skip_specs`. If the user prefers an
   explicit `agent-commands` contract for the stale-value skip, add an `ADDED Scenario` under
   "Terminal vs captured execution" (e.g. "A registry `InstallPath` whose `bin\bash.exe` does
   not exist is skipped in favor of a later view's valid value") before implementation stage 3,
   and drop `skip_specs: true` — the rest of the change is unaffected.
2. **Real-registry HKCU test (default: include).** Design E4 includes one guarded HKCU fact. If
   the user judges any real-registry write unacceptable (e.g. CI images that snapshot HKCU),
   stage 3's task 3.6 can be dropped without touching the rest of item E; the hermetic E2/E5
   coverage is independent.
3. **Watch item — likely identity of the "Single unexplained test failure"**
   (`docs/backlog.md:290-301`). The unexplained 1-of-554 failure occurred in `CLIHub.Core.Tests`
   (backlog:294-296). Planning greps of the timing-sensitive candidates in that project surface:
   `Stop_DisposesWatcherAndClearsStatus`
   (`tests/CLIHub.Core.Tests/Scanning/ProjectScanningServiceTests.cs:119-133`) waits only for
   watcher creation (`:127`) via the 5 s stopwatch poll loop (`:72-86`), then asserts
   `NotConfigured` (`:132`) — a background scan can still hold `Idle`, which is the exact
   mechanism the *separately parked* flake entry documents
   (`docs/backlog.md:9-18`, "failed 1 of 3 full-solution runs … 2026-10-20", same day, same
   test project). It is the strongest candidate. The backlog's alternative candidates check out
   as designed: 100 ms CTS windows in `UpdateCheckerTests.cs:64,82` and
   `UpdateDownloaderTests.cs:87,182`, plus 150–180 ms delays in `UpdateServiceTests.cs:130-155`.
   This change touches none of them; the note is recorded for the parked item's "on recurrence,
   capture the test name" remedy, not acted on here.
4. **Helper naming (default: `Pending`/`Unknown` + `IsPending`/`IsKnown`).** If the user prefers
   the backlog's literal `IsKnown`/`Placeholder` vocabulary, only the constant/predicate names
   in A2 change; the site mapping and tests are unaffected.
