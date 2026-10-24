# Tasks

Stages are sized for a fresh session. Each stage ends green: `dotnet build CLIHub.sln` and
`dotnet test CLIHub.sln`. No stage commits; the user decides about commit and archive.

## Stage 1: Pane failure routing (`AgentPaneController`)

**Goal**: unexpected pane population failures reach `ILogger` instead of `Debug.WriteLine`;
nothing else changes on this surface.

- [x] 1.1 `src/CLIHub/ViewModels/Agents/AgentPaneController.cs`: add a constructor parameter
  `ILogger<AgentPaneController> logger` (last position), a `private readonly ILogger<AgentPaneController>
  logger;` field with the standard null-guard (`this.logger = logger ?? throw
  new ArgumentNullException(nameof(logger));`), and a `<param name="logger">` XML doc entry
  ("Logger for version-population failures."). Add `using Microsoft.Extensions.Logging;` in the
  file's configured ordering. Remove `using System.Diagnostics;` (line 4) — `Debug` was its only
  consumer.
- [x] 1.2 Same file, `PopulateVersionsAsync` (lines 184-187): replace
  `catch (Exception ex) { Debug.WriteLine($"Agent version population failed: {ex}"); }` with
  `catch (Exception ex) { this.logger.LogError(ex, "Agent version population failed"); }`.
  Keep the preceding `catch (OperationCanceledException) when
  (cancellationToken.IsCancellationRequested)` (lines 181-183) exactly as is — it is what keeps
  refresh-supersede cancellation out of the runner's generic Error catch
  (`src/CLIHub/Lifetime/ApplicationOperationLifetime.cs:98-105`, design D2).
- [x] 1.3 Update every direct construction site with `NullLogger<AgentPaneController>.Instance`
  (`using Microsoft.Extensions.Logging.Abstractions;`): the five sites in
  `tests/CLIHub.Tests/ViewModels/Agents/AgentPaneControllerTests.cs` (lines 27, 54, 84, 118,
  144), the three in `tests/CLIHub.Tests/ViewModels/Shared/StatusMessageCoordinatorTests.cs`
  (lines 118, 160, 223), and the two in `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs`
  (lines 255, 328). Update the illustrative constructor in `tests/CLIHub.Tests/README.md`
  (lines 55, 209) to include the logger argument.
- [x] 1.4 In `AgentPaneControllerTests`, add `PopulateVersionsAsync_WhenProbeThrows_LogsError`:
  mock `IApplicationOperationLifetime.RunAsync` to *execute* the operation (capture the
  `Func<CancellationToken, Task>`, invoke it with `CancellationToken.None`, await it — unlike the
  existing no-execute mock at lines 115-117), mock `IAgentVersionService.GetVersionAsync` to
  throw `InvalidOperationException`, use a Moq `Mock<ILogger<AgentPaneController>>` (or
  `SpyLogger` if one exists in the test project), call `Refresh(null, false)`, and assert one
  `LogLevel.Error` entry was written and the row keeps its `"…"` version
  (`src/CLIHub/ViewModels/Agents/AgentItem.cs:107`). Name and assertions follow the existing
  `MethodOrScenario_Condition_ExpectedResult` pattern.

**Gate:** `dotnet build CLIHub.sln` — 0 warnings, 0 errors.

**Gate:** `dotnet test CLIHub.sln` — every suite green, including the new test.

## Stage 2: Management loop adopts the canonical shape

**Goal**: `AgentManagementViewModel.PopulateVersions` has the pane's generation + CTS +
parallel shape with surface-exit cancellation, while its user-visible fill-in behavior is
unchanged.

- [x] 2.1 `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`: add fields
  `private CancellationTokenSource? versionPopulationCts;` and
  `private int versionPopulationGeneration;` next to the existing `disposed` field (line 32).
- [x] 2.2 Rewrite `PopulateVersions` (lines 259-283) as the pane's header plus a loop body:
  cancel-and-dispose `versionPopulationCts`, create a new CTS, `var generation =
  ++this.versionPopulationGeneration;`, capture the token (mirroring
  `src/CLIHub/ViewModels/Agents/AgentPaneController.cs:87-91`), snapshot
  `this.Cards.ToList()`, and start `_ = this.RunAsync("Agent version population",
  applicationToken => this.PopulateVersionsAsync(snapshot, generation, refreshToken,
  applicationToken))` (operation name unchanged). Extract the private
  `PopulateVersionsAsync(IReadOnlyList<AgentCardItem> cards, int generation, CancellationToken
  refreshToken, CancellationToken applicationToken)` mirroring
  `AgentPaneController.PopulateVersionsAsync` (`AgentPaneController.cs:159-188`): link the two
  tokens; per card in parallel (`Task.WhenAll` over `cards.Select`), await `GetVersionAsync`,
  write `card.Version = ver ?? "unknown"` under `!cancellationToken.IsCancellationRequested &&
  generation == this.versionPopulationGeneration`, then await `CheckAsync` and write
  `card.UpdateAvailability` under the same guard (preserves the current per-card ordering:
  version applied as soon as it resolves, availability after its check). After `WhenAll`,
  rebuild the default-agent options under the same guard only on the success path
  (`RebuildDefaultAgentOptions(this.SelectedDefaultAgent?.Id)`). Wrap the body in the two local
  catches: `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
  { }` and `catch (Exception ex) { this.logger.LogError(ex, "Agent version population
  failed"); }` (design D2 — the local catches stop surface-local cancellation and failures from
  reaching the runner's generic Error catch; the runner stays as backstop).
- [x] 2.3 Same file, `Dispose` (lines 228-231): before setting `disposed = true`, mirror
  `AgentPaneController.Dispose` (`AgentPaneController.cs:144-157`) —
  `this.versionPopulationGeneration++; this.versionPopulationCts?.Cancel();
  this.versionPopulationCts?.Dispose(); this.versionPopulationCts = null;`. Keep the
  `disposed` flag and the existing `Load`/`DetectAsync` guards (lines 179-182, 306-309)
  unchanged.
- [x] 2.4 `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`: extend
  `CreateViewModel` (lines 36-70) with an optional `ILogger<AgentManagementViewModel>? logger =
  null` parameter defaulting to `NullLogger<AgentManagementViewModel>.Instance`, and add a
  small non-blocking test lifetime beside `InlineOperationLifetime` (lines 464-477) —
  `DeferredOperationLifetime` that invokes the operation with `CancellationToken.None` and
  returns its task *without* blocking (so pending-probe scenarios cannot deadlock the test).
  Leave the existing `Load_PopulatesVersions` (164-171) and
  `Load_PopulatesUpdateAvailabilityPerCard` (174-188) untouched — a `Task.WhenAll` body with
  completed mock tasks completes synchronously under `InlineOperationLifetime`, so they pin the
  unchanged fill-in outcome through the new shape.
- [x] 2.5 Same test file, add `Dispose_CancelsInFlightPopulation`: version mock whose
  `GetVersionAsync` returns a `TaskCompletionSource`-backed task and captures the passed
  `CancellationToken`; `Load(null)` under `DeferredOperationLifetime`; `viewModel.Dispose()`;
  assert the captured token `IsCancellationRequested`, complete the source, and assert the card
  version remains `"…"` (the generation guard rejects the write).
- [x] 2.6 Same test file, add `Load_SecondLoadCancelsFirstPass`: two plugins; first
  `GetVersionAsync` call blocks on a `TaskCompletionSource` and captures its token; second
  `Load(null)`; assert the first pass's captured token is cancelled and only the second pass's
  results reach the (new) card objects — the pane's supersession semantics
  (`AgentPaneController.Refresh`, `AgentPaneController.cs:87-91`).
- [x] 2.7 Same test file, add `PopulateVersions_WhenProbeThrows_LogsErrorAndKeepsOtherCards`:
  plugin "a" throws from `GetVersionAsync`, plugin "b" returns "2.0"; mock logger; assert one
  `LogLevel.Error` entry, card "a" keeps `"…"`, card "b" shows "2.0" (`Task.WhenAll` runs every
  task; the failing card's writes are skipped, the others apply).

**Gate:** `dotnet build CLIHub.sln` — 0 warnings, 0 errors.

**Gate:** `dotnet test CLIHub.sln` — every suite green, including the four population tests
(two existing, behavior-identical) and the three new ones.

## Stage 3: Spec delta alignment, backlog, and final verification

The delta already exists under `openspec/changes/consolidate-version-population/specs/`;
correct it in place, do not recreate it.

- [x] 3.1 Re-read `openspec/specs/agent-version/spec.md` (requirement at lines 97-107) against
  the implemented code from Stages 1-2 and correct any statement that does not match. Keep both
  original scenario names ("Refresh supersedes a population pass", "Current population
  completes"); the two new scenarios ("Surface exit cancels its population pass", "Population
  pass failure is logged") stay accurate — in particular the in-flight-probe clause: a probe
  already started may complete and cache its result (`AgentVersionService.cs:89-90` runs the
  process with `CancellationToken.None`), it just must not update the abandoned surface.
- [x] 3.2 `docs/backlog.md`: replace only the "Version population loop consolidation" entry
  (lines 62-70) with a resolved entry in the format of the accent entry (lines 410+): resolved
  by the `consolidate-version-population` change (2026-10-24); state the resolved model — both
  live loops use the pane's generation + CTS + parallel shape, failures log through each
  surface's injected `ILogger<T>`, page exit cancels the management pass (in-flight probes
  complete and cache by design), and the legacy `MainWindow` remains the documented untouched
  exception. The file carries two unrelated pre-existing uncommitted entries — rewrite only
  this entry and leave every other line untouched.
- [x] 3.3 Run `dotnet build CLIHub.sln` — 0 warnings, 0 errors.
- [x] 3.4 Run `dotnet test CLIHub.sln` — every suite green; report the per-project counts.
- [x] 3.5 Run `openspec validate --all` — every item passes; quote the output.
- [x] 3.6 Run `graphify update .` from the repository root so the graph reflects the new loop
  shape.
- [x] 3.7 Manual smoke (user): open Settings → CLI Agents with at least two agents declaring
  `version` commands; confirm cards appear with `…` and versions fill in; close Settings while
  the chip still shows `…` and confirm no `Agent version population` errors appear in the
  current day's log (`%APPDATA%\CLIHub\logs\`); re-open Settings and confirm versions fill in
  again (cache-served or re-probed).
- [x] 3.8 Report the exact gate outputs, any deviation, and the files changed for review. Do
  not commit; ask the user about archiving and committing.

**Final gate:** every step above passes or is explicitly reported as not run.
