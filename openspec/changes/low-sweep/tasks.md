# Tasks: low-sweep

Stages 1–3 are the three independent items (code, tests), 4 is the backlog rewrite, 5 is final
verification. Each stage ends with a gate; do not start the next stage on a red gate. The three
items are independent — a red gate in one stage never justifies reverting another stage's work.
Per `AGENTS.md`, run GitNexus impact (`node .gitnexus/run.cjs impact "<symbol>" --direction
upstream --repo .`) before the edits in each stage and `detect-changes` before any commit; the
census in `design.md` ("Current state") is the baseline any impact result must agree with — if
impact reports a caller not listed there, stop and reconcile before editing. If open question Q1
(spec delta for item E) was decided "yes" during review, do that decision's added task in stage 3
first; otherwise proceed as written.

## 1. Stage 1 — Item A: `AgentVersionText` helper and the ten sentinel sites

- [x] 1.1 Run impact for `AgentCardItem`, `AgentOption`, `AgentManagementViewModel`,
      `AgentPaneController`, and `AgentItem` and compare with design "Current state" (consumers:
      Settings Agents page bindings, launch-window agent list, tests listed there). Stop and
      reconcile on any surprise.
- [x] 1.2 Create `src/CLIHub/Helpers/AgentVersionText.cs` (`namespace CLIHub.Helpers`,
      `public static class`, XML docs on every member) with exactly the design A2 API:
      `Pending = "…"`, `Unknown = "unknown"`, `IsPending(string?)`, `IsKnown(string?)`. Follow
      the `PreferenceSyncHelper.cs` file shape (namespace, using placement, doc style).
- [x] 1.3 Replace the sentinel literals at the live sites (display behavior byte-identical):
      `src/CLIHub/ViewModels/Agents/AgentItem.cs:107` → `= AgentVersionText.Pending;`;
      `AgentCardItem.cs:19` (`AgentOption.DisplayName`) → `AgentVersionText.IsPending(this.Version)`;
      `AgentCardItem.cs:193-194` (`HasKnownVersion`) → `AgentVersionText.IsKnown(version)`;
      `AgentManagementViewModel.cs:123` → `AgentVersionText.IsPending(version)`;
      `AgentManagementViewModel.cs:211` → `AgentVersionText.Pending,`;
      `AgentManagementViewModel.cs:304,397,420` and `AgentPaneController.cs:182` →
      `?? AgentVersionText.Unknown`. Add the `using CLIHub.Helpers;` directive only where not
      already present. Do **not** touch `Views/MainWindow.xaml.cs:206` or anything under
      `src/CLIHub.Core/Updates/`.
- [x] 1.4 Create `tests/CLIHub.Tests/Helpers/AgentVersionTextTests.cs` per design A3: `IsPending`
      true for `null`, `""`, `"…"`; false for `"unknown"`, `"1.2.3"`. `IsKnown` false for
      `null`, `""`, `"…"`, `"unknown"`; true for `"1.2.3"`. Test naming follows
      `MethodOrScenario_Condition_ExpectedResult`.
- [x] 1.5 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures, and no
      existing test edited or skipped (the sentinel-pinning tests in `AgentCardItemTests.cs:28-41`,
      `AgentManagementViewModelTests.cs:224,297`, `AgentPaneControllerTests.cs:179` must pass
      unchanged; they are the exact-display proof).

## 2. Stage 2 — Item B: the two update-detection tests

- [x] 2.1 In `tests/CLIHub.Core.Tests/Agents/AgentReleaseClientTests.cs`, extend the private
      `StubHandler` with `public TimeSpan? Delay { get; set; }` and, in `SendAsync`, when set,
      `await Task.Delay(this.Delay.Value, cancellationToken);` before returning the response
      (token-respecting, per design B1). Do not change `Response`/`Throw` behavior.
- [x] 2.2 Add `GetLatestVersion_RequestTimesOut_ReturnsNull` per design B1: build a
      `PreferencesStore` over a fresh `FakeConfigurationRepository`, apply
      `preferences.Update(p => p.AgentProbeTimeoutSeconds = 1)`, construct the client directly
      with that store, set `this.handler.Delay = TimeSpan.FromSeconds(30)`, call
      `GetLatestVersionAsync(new PluginUpdateCheck("npm", Package: "cline"))`, and assert the
      result is null. Do not pass a caller token (default token only) — the timeout branch is
      distinguished from caller cancellation by the filter at `AgentReleaseClient.cs:80`.
- [x] 2.3 Add `UpdateAvailability_WhenNotInstalledWithDeclaredUpdateCheck_ShowsNoIndicator` to
      `tests/CLIHub.Tests/ViewModels/Agents/AgentCardItemTests.cs` per design B2: a card built
      with `isInstalled: false`, a placeholder version, and a plugin fixture declaring
      `UpdateCheck = new PluginUpdateCheck("npm", Package: "test-pkg")`; set
      `card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.Unknown)` (the
      value the real service returns for an uninstalled agent); assert
      `Assert.Null(card.UpdateAvailabilityText)` and `Assert.False(card.HasUpdateAvailable)`.
      Include the one-line comment noting the card never reads `UpdateCheck` (design B2).
- [x] 2.4 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures. The
      timeout test legitimately takes ~1 s (1-second configured probe timeout); if it ever
      flakes, diagnose before weakening — the assertion is a null result, not a duration.

## 3. Stage 3 — Item E: verifiable Git registry InstallPath preference

- [x] 3.1 Run impact for `WindowsRuntimeLocator`, `WindowsRuntimeEnvironment`, and
      `IWindowsRuntimeEnvironment` and compare with design "Current state" (consumers: the
      locator, the fake, `WindowsRuntimeLocatorTests`, and the DI registration — nothing else).
      Stop and reconcile on any surprise.
- [x] 3.2 In `src/CLIHub.Core.Windows/WindowsRuntimeLocator.cs`: rename the interface member
      `ReadGitForWindowsInstallPath` (`:22`) to `ReadGitForWindowsInstallPaths()` returning
      `IReadOnlyList<string>` and rewrite its XML doc for plural, per-view-fault semantics
      (design E1); move the collection into `internal static IReadOnlyList<string>
      CollectInstallPaths(IReadOnlyList<(RegistryHive Hive, RegistryView View)> views,
      Func<RegistryHive, RegistryView, string?> readInstallPath)` on `WindowsRuntimeEnvironment`
      with the `GitViews` array `(LocalMachine, Registry64), (LocalMachine, Registry32),
      (CurrentUser, Default), (CurrentUser, Registry32)` (design E2); keep `ReadInstallPath`
      (`:170-191`) and `ReadPowerShellCoreInstallPath` (`:30`, `:93-104`) untouched.
- [x] 3.3 In the same file, convert `ReadGitRegistryInstallPath` (`:370-389`) to
      `ReadGitRegistryInstallPaths()` returning the candidate list (empty on registry fault —
      keep the try/catch), and make `LocateGitBash`'s registry step (`:233-246`) a
      `foreach` over the candidates around the unchanged `TryGitCandidate` call and success log
      (design E3). Do not touch the well-known (`:248-258`), PATH (`:260-295`), warning
      (`:297-301`), or PowerShell (`:304-368`) sections.
- [x] 3.4 In `tests/CLIHub.Core.Windows.Tests/FakeWindowsRuntimeEnvironment.cs`: replace
      `InstallPath` (`:16`) with `public List<string> InstallPaths { get; } = [];`, return it
      from the plural method, keep `ThrowOnRegistryRead` throwing `IOException` from it; update
      the three existing call sites in `WindowsRuntimeLocatorTests.cs` (`:13-17`, `:96`) to
      collection initializers — no assertion changes.
- [x] 3.5 Add the new locator and collector tests per design E5: in
      `WindowsRuntimeLocatorTests.cs`, (a) stale first registry candidate (`InstallPaths[0]`
      without a registered `bin\bash.exe`) plus verifiable second → second candidate's
      `bin\bash.exe` wins and well-known/PATH are not consulted (leave their entries empty);
      (b) all registry candidates stale → the well-known root's `bin\bash.exe` wins. Create
      `tests/CLIHub.Core.Windows.Tests/WindowsRuntimeEnvironmentTests.cs` with the hermetic
      `CollectInstallPaths` facts: view order (fake reader keyed by hive/view), null and
      whitespace values skipped, values trimmed.
- [x] 3.6 Add the guarded real-registry fact per design E4 to
      `WindowsRuntimeEnvironmentTests.cs`: save the prior `HKCU\SOFTWARE\GitForWindows`
      `InstallPath` value and key existence, write a unique marker path, assert
      `Assert.Contains(marker, new WindowsRuntimeEnvironment().ReadGitForWindowsInstallPaths())`
      (containment only — never sequence), and restore/delete in `finally` (delete the key only
      if this test created it). If the user answered Q2 with "drop", skip this task and record
      that in the stage report.
- [x] 3.7 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures,
      including the three updated existing locator tests (registry-wins, registry-throws,
      uncovered-kind) with unchanged assertions.

## 4. Stage 4 — `docs/backlog.md`: three resolved-format rewrites

Rewrite each swept entry to the resolved format used by "Version population loop consolidation
(resolved)" (`docs/backlog.md:90-113`): heading gets an ` (resolved)` suffix, bullets become
`Status: Resolved by the low-sweep change (2026-10-24).` / `Resolved model: …` / `Guarded by: …`
/ `Trigger: …`. One edit per entry — three separate hunks; every other line in the file
untouched.

- [ ] 4.1 Rewrite "Magic version sentinels" (`docs/backlog.md:115-121`): resolved model — the
      `AgentVersionText` helper in `src/CLIHub/Helpers/` owns `Pending`/`Unknown` and the
      `IsPending`/`IsKnown` checks; all live surfaces (including the `AgentItem` placeholder
      initializer the entry had missed) route through it; the legacy `MainWindow` copy stays the
      documented exception. Guarded by `AgentVersionTextTests` plus the unchanged sentinel-
      pinning tests listed in design A3.
- [ ] 4.2 Rewrite "Update-detection test gaps: timeout path and missing-installation indicator"
      (`:159-169`): resolved model — `GetLatestVersion_RequestTimesOut_ReturnsNull`
      (handler-stub delay past the 1-second configured probe timeout) and
      `UpdateAvailability_WhenNotInstalledWithDeclaredUpdateCheck_ShowsNoIndicator`; no
      production change. Guarded by those two tests directly.
- [ ] 4.3 Rewrite "Registry InstallPath precedence can shadow a valid install" (`:137-146`):
      resolved model — the plural seam collects every non-empty `InstallPath` across
      HKLM64/HKLM32/HKCU-default/HKCU-32 (including the fourth view the entry had not listed),
      and `LocateGitBash` prefers the first candidate whose `bin\bash.exe` exists, falling
      through to well-known/PATH when none verifies. Guarded by the collector facts, the two
      new locator facts, and the guarded HKCU real-registry fact.
- [ ] 4.4 Gate: `git diff docs/backlog.md` shows exactly the three entry hunks and nothing else;
      `openspec validate --all` still passes.

## 5. Stage 5 — final verification

- [x] 5.1 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures (full
      re-run, not cached results).
- [x] 5.2 Gate: `openspec validate --all` passes.
- [x] 5.3 `graphify update .` to refresh the knowledge graph after the source edits.
- [x] 5.4 Review `git status --porcelain` and `git diff --stat`: only files belonging to this
      change (design/proposal/tasks under `openspec/changes/low-sweep/`, the code/test files
      listed in proposal Impact, `docs/backlog.md`) are modified; the legacy
      `Views/MainWindow.xaml.cs`, `src/CLIHub.Core/Updates/`, and every other parked backlog
      entry are untouched. Report, then wait for the user's commit/archive approval per the
      repo policy.
