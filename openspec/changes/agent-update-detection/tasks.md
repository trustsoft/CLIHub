# Tasks: agent-update-detection

Each stage is a fresh-session-sized unit and ends with its own build/test gate. Do not start a
stage until the previous gate is green. Only the final stage runs the full solution and OpenSpec
validation.

## Stage 1 — Descriptor model and detached copy

- [ ] 1.1 Add `PluginUpdateCheck` as a `sealed record` in `src/CLIHub.Core/Models/Plugins/` with
      `Source`, `Package`, and `Repo` (nullable). Document that `Source` stays a raw string so
      unknown kinds survive loading.
- [ ] 1.2 Add `public PluginUpdateCheck? UpdateCheck { get; set; }` to `Plugin`
      (`src/CLIHub.Core/Models/Plugins/Plugin.cs`, after `Detection`).
- [ ] 1.3 Clone it in `Plugin.Copy()` (`Plugin.cs:49-80`) and update the method's XML docs to
      mention the update block.
- [ ] 1.4 Add a round-trip/immutability test next to
      `GetAllPlugins_MutatingReturnedPlugin_DoesNotChangeLaterReadsOrCatalog`
      (`tests/CLIHub.Core.Tests/Plugins/PluginCatalogTests.cs:143-179`): load a descriptor with
      `updateCheck`, assert the value survives `GetAllPlugins()`, replace it on the returned
      plugin, and assert a later read is unchanged.
- [ ] 1.5 Extend `tests/CLIHub.Core.Tests/Plugins/PluginDescriptorReaderTests.cs` to assert an
      unknown `"source": "cargo"` block still deserializes (no load error) and keeps its raw value.

Gate:

```powershell
dotnet build CLIHub.sln
dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj
```

## Stage 2 — Version extraction and comparison

- [ ] 2.1 Add an internal static helper in `CLIHub.Core.Agents` (for example
      `AgentUpdateVersion`) exposing `TryExtract(string?, out ...)` for the first
      `\d+\.\d+\.\d+` match and `Compare(installed, latest)` over the numeric triple.
- [ ] 2.2 Add theory tests covering extraction from `opencode v2.0.32`, `0.30.0 (OpenClaude)`,
      `1.0.100`, and an unparseable first line.
- [ ] 2.3 Add comparison tests: `2.0.32` vs `2.0.33` → available; equal triple → up to date;
      `1.0.100` vs `1.0.99` → up to date; `1.2.3` vs `1.10.0` → available (numeric, not lexical);
      a pre-release suffix is ignored.

Gate:

```powershell
dotnet build CLIHub.sln
dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj
```

## Stage 3 — Release client

- [ ] 3.1 Add `IAgentReleaseClient` in `src/CLIHub.Core/Agents/`:
      `Task<string?> GetLatestVersionAsync(PluginUpdateCheck updateCheck, CancellationToken cancellationToken = default)`.
- [ ] 3.2 Implement `AgentReleaseClient` over an injected `HttpClient` and `IPreferencesStore`.
      Set `User-Agent: CLIHub`; build the npm and GitHub URLs from the block; apply the
      `AgentProbeTimeoutSeconds` preference (default 10 s) with a linked `CancellationTokenSource.CancelAfter`.
- [ ] 3.3 Parse npm `version` and GitHub `tag_name`; strip one leading `v`; return `null` for an
      unsupported/ malformed block, non-success status, malformed JSON, or a missing field. Let
      caller cancellation propagate.
- [ ] 3.4 Tests in `tests/CLIHub.Core.Tests/Agents/AgentReleaseClientTests.cs` with a stub
      `HttpMessageHandler`: npm `{"version":"2.0.32"}` → `2.0.32`; GitHub `{"tag_name":"v1.0.100"}`
      → `1.0.100`; HTTP 500, network exception, malformed JSON, and missing field → `null` with no
      escaping exception; the request carries `User-Agent`.

Gate:

```powershell
dotnet build CLIHub.sln
dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj
```

## Stage 4 — Availability service and cache

- [ ] 4.1 Add `AgentUpdateStatus` (`Unknown`, `UpToDate`, `UpdateAvailable`) and
      `AgentUpdateAvailability(Status, InstalledVersion?, LatestVersion?)` in
      `src/CLIHub.Core/Agents/`.
- [ ] 4.2 Add `IAgentUpdateAvailabilityService` with `CheckAsync(Plugin, CancellationToken)`,
      `Invalidate()`, and `InvalidateFor(Plugin)`.
- [ ] 4.3 Implement `AgentUpdateAvailabilityService` with a per-plugin
      `ConcurrentDictionary<string, CacheEntry>` plus
      `ConcurrentDictionary<string, Lazy<Task<AgentUpdateAvailability>>>`, mirroring
      `AgentVersionService.cs:34-35`, `:52-77`, and `:79-104`. TTL from `AgentProbeTtlMinutes`
      (default 15 minutes, `AgentVersionService.cs:121-128`). Cache `Unknown` outcomes too.
- [ ] 4.4 Return `Unknown` without a request when there is no block, the block is unknown or
      malformed (log a warning), or the installed version is unknown/unparseable. Map latest
      older than installed to `UpToDate`.
- [ ] 4.5 Implement `InvalidateFor` as a scoped removal that leaves in-flight probes alone
      (`AgentVersionService.cs:110-119`); `Invalidate()` clears all.
- [ ] 4.6 Tests in `tests/CLIHub.Core.Tests/Agents/AgentUpdateAvailabilityServiceTests.cs` using
      fakes for `IAgentReleaseClient`, `IAgentVersionService`, `IPreferencesStore`, and
      `TimeProvider` (follow `AgentVersionServiceTests.cs:11-45`).

Gate:

```powershell
dotnet build CLIHub.sln
dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj
```

## Stage 5 — Composition and invalidation wiring

- [ ] 5.1 In `AddAgentServices` (`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:104-117`)
      register `HttpClient` (singleton factory, `CLIHub` user agent), `IAgentReleaseClient`,
      and `IAgentUpdateAvailabilityService`.
- [ ] 5.2 Add the availability service to `AgentManagementService`'s constructor
      (`AgentManagementService.cs:27-38`) and call `InvalidateFor(plugin)` in `ProbeAsync`
      next to `AgentManagementService.cs:48-49`.
- [ ] 5.3 In `UpdateAsync`, when the outcome is `Started` (`AgentManagementService.cs:85-88`),
      invalidate the plugin's version and availability.
- [ ] 5.4 Update `AgentManagementServiceTests` fakes/constructions for the new constructor argument.
- [ ] 5.5 Extend `AddClIHubCoreServices_ResolvesCoreServices`
      (`tests/CLIHub.Core.Tests/Composition/ServiceCollectionExtensionsTests.cs:16-35`) with
      `IAgentReleaseClient` and `IAgentUpdateAvailabilityService` resolution. Confirm
      `AddClIHubServices_ResolvesWpfSafeServiceGraph`
      (`tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs:22-66`, which constructs
      `IAgentManagementSection` at `:58`) still passes.

Gate:

```powershell
dotnet build CLIHub.sln
dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj
```

## Stage 6 — Seed descriptors and docs

- [ ] 6.1 Add `updateCheck` to the six descriptors:
      `opencode` → `{ "source": "npm", "package": "@opencode/cli" }`;
      `cline-cli` → `{ "source": "npm", "package": "cline" }`;
      `pi` → `{ "source": "npm", "package": "@earendil-works/pi-coding-agent" }`;
      `openclaude` → `{ "source": "npm", "package": "@twigpine/openclaude" }`;
      `qwen-code` → `{ "source": "npm", "package": "@qwen-code/qwen-code" }`;
      `github-copilot` → `{ "source": "githubReleases", "repo": "github/copilot-cli" }`.
- [ ] 6.2 Extend `SeededDescriptors_LoadAsValidPlugins`
      (`tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs:104-121`) or add a sibling test that
      asserts each seeded plugin has the expected source and package/repo.
- [ ] 6.3 Document the block and both source kinds in `docs/architecture/plugins.md:5-25`,
      including that unknown kinds are ignored.

Gate:

```powershell
dotnet build CLIHub.sln
dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj
```

## Stage 7 — Card indicator

- [ ] 7.1 Add `UpdateAvailabilityText` (nullable; `"{installed} → {latest}"` only for
      `UpdateAvailable`) and `HasUpdateAvailable` to `AgentCardItem`
      (`src/CLIHub/ViewModels/Agents/AgentCardItem.cs`), with a setter that raises both
      notifications. Do not change `CanUpdate` (`AgentCardItem.cs:128-132`).
- [ ] 7.2 Inject `IAgentUpdateAvailabilityService` into `AgentManagementViewModel`
      (`AgentManagementViewModel.cs:44-51`) and request availability per card in
      `PopulateVersions` (`:239-255`) after the version probe.
- [ ] 7.3 Apply the result after `ProbeAsync` in Test and Detect (`:257-293`) and invalidate it
      after a successful Update start (`:295-304`).
- [ ] 7.4 Insert the update chip after the state chip in the name row
      (`src/CLIHub/Views/SettingsAgentsPage.xaml:144-148`): a `Border` based on `AgentStateChip`
      (`:13-28`), `Visibility` bound through `BoolToVisibility`, `AutomationProperties.Name="Update available"`,
      text bound to `UpdateAvailabilityText`. Add no new dependency from the indicator to the
      Update button.
- [ ] 7.5 Tests: `AgentCardItemTests` for text/flag across `UpdateAvailable`, `UpToDate`, and
      `Unknown`; `AgentManagementViewModelTests` for population and for `CanUpdate` remaining
      unchanged when availability is `Unknown` or `UpdateAvailable`.

Gate:

```powershell
dotnet build CLIHub.sln
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj
```

## Stage 8 — Final gates

- [ ] 8.1 `dotnet build CLIHub.sln`
- [ ] 8.2 `dotnet test CLIHub.sln`
- [ ] 8.3 `openspec validate --all` (must report zero failures)
- [ ] 8.4 `graphify update .`
- [ ] 8.5 Manual live check (user-executed; leave unchecked here):
      launch `dotnet run --project src/CLIHub/CLIHub.csproj`, open Settings → CLI Agents.
      Confirm an outdated installed agent shows `installed → latest`; confirm an up-to-date
      agent shows no chip; disconnect the network, reopen the page, and confirm no chip, no
      crash, and an unchanged Update button. Restore the network, click Test on one agent, and
      confirm the chip refreshes for that agent only.

## Unit test matrix

| Area | Test | Expected |
|---|---|---|
| Descriptor | Unknown `source` kind | Loads; no request; `Unknown`; warning logged |
| Descriptor | Round-trip through `Copy()` | `UpdateCheck` survives `GetAllPlugins()`; later read unchanged after mutation |
| Extraction | `opencode v2.0.32` | `2.0.32` |
| Extraction | `0.30.0 (OpenClaude)` | `0.30.0` |
| Extraction | `1.0.100` | `1.0.100` |
| Extraction | `no version here` | unparseable → `Unknown` |
| Comparison | `2.0.32` → `2.0.33` | `UpdateAvailable(2.0.32, 2.0.33)` |
| Comparison | equal triple | `UpToDate` |
| Comparison | latest older than installed | `UpToDate` |
| Comparison | `1.2.3` vs `1.10.0` | `UpdateAvailable` |
| npm | `{"version":"2.0.32"}` | `2.0.32` |
| GitHub | `{"tag_name":"v1.0.100"}` | `1.0.100` |
| HTTP failure | 500 / network exception / malformed JSON | `Unknown`, no exception |
| Cache | two checks within TTL | one release request |
| Cache | check after TTL | second release request |
| Cache | `InvalidateFor(A)` then A and B | A re-requests; B stays cached |
| Cache | concurrent checks for one plugin | one release request, same result |
| Advisory | `UpdateAvailable` with a declared update command | `CanUpdate` unchanged |
