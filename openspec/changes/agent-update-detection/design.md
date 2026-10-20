# Design: agent update detection

## Capability name

Renamed from `agent-update-availability` to `agent-update-detection` and the change directory with
it. `agent-update-availability` sat one word away from the existing `agent-availability-display`
capability, which is about dimming agents unavailable in the selected project
(`openspec/specs/agent-availability-display/spec.md:1-7`). The new capability is about detecting
whether a newer version exists, so `agent-update-detection` names the behavior and removes the
collision. The user-facing action stays "Update"; only the capability and change id change.

## Sources

| Kind | Request | Latest-version field |
|---|---|---|
| `npm` | `GET https://registry.npmjs.org/<package>/latest` | `version` |
| `githubReleases` | `GET https://api.github.com/repos/<repo>/releases/latest` | `tag_name`, leading `v` stripped |

Unauthenticated requests. GitHub requires a `User-Agent` header; the adapter sets `CLIHub` on the
shared client. Only one built-in agent uses GitHub releases, so the 60 requests/hour limit is
irrelevant once results are cached for the shared probe TTL.

## Verified source mapping (2026-10-20)

| Agent (descriptor id) | Source | Reference value |
|---|---|---|
| `opencode` | npm `@opencode/cli` | `2.0.32` |
| `cline-cli` | npm `cline` | `3.0.73` |
| `pi` | npm `@earendil-works/pi-coding-agent` | `1.3.0` |
| `openclaude` | npm `@twigpine/openclaude` | `0.33.0` |
| `qwen-code` | npm `@qwen-code/qwen-code` | `0.25.3` |
| `github-copilot` | GitHub releases `github/copilot-cli` | `v1.0.100` → `1.0.100` |

Deviation from the original proposal: Qwen Code was mapped to GitHub releases `QwenLM/qwen-code`.
On 2026-10-20 that repository's `releases/latest` returned `sdk-typescript-v0.1.21`, an SDK tag
rather than the CLI, so the GitHub channel would silently report the wrong component. The npm
package `@qwen-code/qwen-code` tracks the CLI and is the correct channel. Only `github-copilot`
uses `githubReleases`.

## Verified current state

### Plugins and descriptors

- `Plugin` has no update-source member. `Copy()` clones scalars, commands, and detection arrays at
  `src/CLIHub.Core/Models/Plugins/Plugin.cs:49-80`; commands are cloned per kind at
  `Plugin.cs:82-89`, and detection arrays are copied at `Plugin.cs:59-63`.
- The detached-read contract is documented at `src/CLIHub.Core/Plugins/IPluginCatalog.cs:8-18` and
  `GetAllPlugins()` returns copies at `IPluginCatalog.cs:36-40`. A new descriptor member must be
  cloned or it disappears for every consumer that reads the catalog.
- Descriptors are deserialized with `CoreJson.Options` at
  `src/CLIHub.Core/Plugins/PluginDescriptorReader.cs:24-33`.
- `CoreJson.Options` sets only `WriteIndented` and camelCase naming at
  `src/CLIHub.Core/Configuration/CoreJson.cs:14-18`. There is no `UnmappedMemberHandling`, so
  System.Text.Json's default skips unknown members: an unknown `source` value is not rejected at
  load time.
- `PluginDescriptorValidator` checks only id, name, and the launch command at
  `src/CLIHub.Core/Plugins/PluginDescriptorValidator.cs:11-33` (id `:17-20`, name `:22-25`, launch
  `:27-30`). It never inspects `updateCheck`.
- Six seed descriptors live under `src/CLIHub.Core/SeedPlugins/{opencode,cline-cli,pi,openclaude,qwen-code,github-copilot}/plugin.json`.
  Their ids are confirmed by `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs:135-148`.
- Descriptor documentation is `docs/architecture/plugins.md:5-25`.

### Version probe and cache

- `AgentVersionService` keeps a per-plugin completed cache and an in-flight map at
  `src/CLIHub.Core/Agents/AgentVersionService.cs:34-35`.
- Concurrent lookups coalesce through `Lazy<Task<string?>>` at `AgentVersionService.cs:52-77`.
- `ProbeAndCacheAsync` writes one entry keyed by plugin id at `AgentVersionService.cs:79-104`.
- `Invalidate()` clears all at `:107`; `InvalidateFor(plugin)` removes only that plugin's completed
  entry at `:110-119` and deliberately leaves in-flight probes alone (documented at `:110-114`).
- TTL comes from `AgentProbeTtlMinutes` with a 15-minute default at `AgentVersionService.cs:121-128`;
  the timeout uses `AgentProbeTimeoutSeconds` with a 10-second default at `:130-137`.
- Version extraction uses the first `\d+(?:\.\d+)+` match at `:28` and `:143-156`.
- `AgentDetectionService` uses the same TTL preference at
  `src/CLIHub.Core/Agents/AgentDetectionService.cs:14` and `:77-84`.

### Management composition and UI

- `AgentManagementService` is the composition point for agent operations
  (`src/CLIHub.Core/Agents/AgentManagementService.cs:7-10`). `ProbeAsync` invalidates detection and
  version for the probed plugin at `:48-49` and returns `AgentProbeResult` at `:55`.
- `AgentProbeResult` is declared at `src/CLIHub.Core/Agents/IAgentManagementService.cs:10`.
- `UpdateAsync` starts the update command and returns `Started` at `AgentManagementService.cs:85-88`.
- `AgentManagementViewModel` takes the version service in its constructor at
  `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:44-51`; `Load()` rebuilds the cards at
  `:168-206`; `PopulateVersions()` runs serially through the operation lifetime at `:239-255`;
  Test and Detect call `ProbeAsync` at `:257-293`; Update calls `UpdateAsync` at `:295-304`.
- The card's state chip text comes from `VersionChipText` at
  `src/CLIHub/ViewModels/Agents/AgentCardItem.cs:104-123`; `CanUpdate` is a separate flag at
  `:128-132`; the known-version sentinels are `"…"` and `"unknown"` at `:159-160`.
- The XAML state chip sits in the name row at
  `src/CLIHub/Views/SettingsAgentsPage.xaml:138-149`, with `AgentStateChip` styles at `:13-28`.
  The Update button binds `IsEnabled="{Binding CanUpdate}"` at `:178-189`, and the message line
  uses `BoolToVisibility` at `:157-160`.
- `AddAgentServices` is the DI feature module at
  `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:104-117`, called from
  `AddClIHubCoreServices` at `:38`.
- Composition coverage: `AddClIHubCoreServices_ResolvesCoreServices` at
  `tests/CLIHub.Core.Tests/Composition/ServiceCollectionExtensionsTests.cs:16-35`, and the WPF
  graph test at `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs:22-66` (it resolves
  `IAgentManagementSection` at `:58`, which constructs the view model).
- No `HttpClient` or HTTP port exists in `src/`. `CLIHub.Core.csproj` references only DI
  Abstractions, Logging, Serilog, Serilog.Sinks.File, and Velopack; `System.Net.Http` is in the
  framework, so no package is added.

## Decisions

### 1. Descriptor value and `Copy()`

Add a nullable immutable value type:

```csharp
public sealed record PluginUpdateCheck(string Source, string? Package = null, string? Repo = null);
```

`Plugin` gains `public PluginUpdateCheck? UpdateCheck { get; set; }`. `Copy()` assigns the record
directly (records are immutable, so sharing the instance is safe) and its XML docs mention the new
state. The domain shape keeps the raw `Source` string so an unknown kind survives loading and can
be ignored at use time with a warning.

Rationale: this mirrors how `commands` and `detection` are descriptor-declared state; the catalog
detached-read contract (`IPluginCatalog.cs:8-18`) then continues to hold. A round-trip test next
to `PluginCatalogTests.cs:143-179` loads a descriptor with `updateCheck`, mutates the returned
plugin's block, and asserts later reads are unchanged.

### 2. Unknown source handling

Unknown `source` values are ignored at use time, not rejected at load: `CoreJson.Options` has no
`UnmappedMemberHandling` (`CoreJson.cs:14-18`) and the validator does not inspect the block
(`PluginDescriptorValidator.cs:11-33`). Rejecting in the validator would make the agent vanish for
a descriptor written for a newer CLIHub, which is worse than ignoring the block. The availability
service returns `Unknown` and logs a warning; the result is cached, so the warning is bounded by
the TTL rather than repeated on every refresh.

### 3. Source lookup

Two narrow pieces in `CLIHub.Core.Agents`:

- `IAgentReleaseClient.GetLatestVersionAsync(PluginUpdateCheck, CancellationToken)` returns the
  published version string or `null`. `AgentReleaseClient` implements it over a shared `HttpClient`
  and parses npm `version` and GitHub `tag_name` (single leading `v` stripped). It applies the
  `AgentProbeTimeoutSeconds` preference through a linked `CancellationTokenSource.CancelAfter`, so
  the HTTP bound matches the probe bound instead of `HttpClient.Timeout`.
- `IAgentUpdateAvailabilityService.CheckAsync(Plugin, CancellationToken)` resolves the installed
  version through `IAgentVersionService`, asks the release client for the latest, compares, and
  caches the result.

Rationale: the release client is a real seam for per-source parsing tests (a stub `HttpMessageHandler`
exercises the production code), and the availability service stays a domain service with no HTTP
knowledge. This follows the existing port style (`IAgentVersionService`,
`IAgentDetectionService`) rather than inventing a generic HTTP abstraction.

Rejected: adding `Microsoft.Extensions.Http` for `AddHttpClient`; one singleton `HttpClient`
registered by factory in `AddAgentServices` is enough and adds no package. Rejected: one fused
class that caches and parses; it makes the parsing tests depend on the version cache.

### 4. Version extraction and comparison

- Extraction takes the first `\d+\.\d+\.\d+` match from the string it is given, so
  `opencode v2.0.32` → `2.0.32`, `0.30.0 (OpenClaude)` → `0.30.0`, and `1.0.100` → `1.0.100`.
  The release client returns a raw field; the parser is applied to both sides.
- Comparison is numeric over `(major, minor, patch)` tuples. Latest greater than installed means
  `UpdateAvailable`; equal or older means `UpToDate`; a missing or unparseable value on either side
  means `Unknown`. Pre-release and build suffixes are ignored.
- If the installed version is unknown, the release request is skipped entirely.

Rationale: the installed side already arrives extracted by
`AgentVersionService.ExtractVersion` (`AgentVersionService.cs:143-156`), but parsing again keeps
the service correct if that extraction changes or a caller passes the first line as-is (the
fallback at `:154-155`). `System.Version` was rejected because it accepts 2–4 components and
throws on the text forms above; an explicit triple keeps the contract narrow.

### 5. Cache shape and invalidation

`AgentUpdateAvailabilityService` owns a separate per-plugin cache and in-flight map, mirroring
`AgentVersionService.cs:34-35`, `:52-77`, `:79-104`, and `:110-119`:

- `ConcurrentDictionary<string, CacheEntry>` keyed by plugin id, plus
  `ConcurrentDictionary<string, Lazy<Task<AgentUpdateAvailability>>>` for coalescing.
- TTL from `AgentProbeTtlMinutes`, default 15 minutes (`AgentVersionService.cs:121-128`).
- Negative results are cached too, so an offline machine is not retried on every refresh within
  the TTL.
- `Invalidate()` clears all; `InvalidateFor(plugin)` removes only that plugin's completed entry and
  leaves in-flight probes alone, with the same "at most one probe old" remark as
  `AgentVersionService.cs:110-114`.

It does not share the version cache: the values have different lifetimes (installed version versus
remote latest) and different invalidation triggers even though both currently read the same TTL
preference.

Invalidation call sites:

- `AgentManagementService.ProbeAsync` adds `availability.InvalidateFor(plugin)` next to the existing
  detection/version invalidations at `AgentManagementService.cs:48-49`, so Test and Detect refresh
  the agent's availability.
- `AgentManagementService.UpdateAsync` invalidates the plugin's version and availability when an
  update starts (`AgentManagementService.cs:85-88`), so the next population recomputes instead of
  showing a stale "update available" cue after the user acted on it. The interactive command may
  still be running; invalidation only forces the next lookup to re-probe.

`AgentProbeResult` (`IAgentManagementService.cs:10`) stays `(IsInstalled, Version)`. Availability is
not part of the probe result; the view model requests it directly, which keeps the probe's
installed/version contract and its tests unchanged.

### 6. UI placement

The card gains an update chip immediately after the state chip in the name row at
`src/CLIHub/Views/SettingsAgentsPage.xaml:144-148` (insert after `:148`):

- New `AgentCardItem` members: `UpdateAvailabilityText` (for example `2.0.32 → 2.0.33`) and
  `HasUpdateAvailable`. `UpdateAvailabilityText` is `null` when the state is `Unknown` or
  `UpToDate`, so no placeholder text is ever rendered.
- New `Border` styled like `AgentStateChip` (`SettingsAgentsPage.xaml:13-28`) with an accent
  treatment and `Visibility="{Binding HasUpdateAvailable, Converter={StaticResource BoolToVisibility}"}`,
  `AutomationProperties.Name="Update available"`, and a tooltip explaining that a newer version is
  published.
- `CanUpdate` (`AgentCardItem.cs:128-132`) is not touched. The chip never enables, disables, or
  blocks the Update button (`SettingsAgentsPage.xaml:178-189`).

Rationale: the state chip already owns "what is installed", so the adjacent chip reads as "what is
new"; a separate line was rejected because the card already carries a commands line and an
action-message line, and the indicator is one short token pair.

### 7. Advisory-only failure behavior

Any failure path returns `Unknown`: no descriptor block, unknown or malformed block, unknown
installed version, HTTP non-success, timeout, network exception, malformed JSON, or unparseable
latest version. `CheckAsync` never throws for these conditions. Caller cancellation still
propagates (`OperationCanceledException`) so the operation lifetime can log a clean cancellation,
matching how the version service surfaces `WaitAsync(cancellationToken)`.

## Backlog dispositions

- **Version population loop consolidation** (`docs/backlog.md:21-29`) — deferred. The trigger says
  "touching agent version population", and this change does add one awaited call inside
  `AgentManagementViewModel.PopulateVersions` (`AgentManagementViewModel.cs:239-255`). However, the
  loop already runs through `IApplicationOperationLifetime`, whose `RunObservedAsync` catches and
  logs failures (`ApplicationOperationLifetime.RunObservedAsync`), `Load()` is the only population
  trigger on a transient view model (`AgentManagementViewModel.cs:168-206`), and the new call takes
  the same operation token and honors it. Adopting the pane's generation/CTS shape
  (`AgentPaneController.cs:159-188`) is an independent refactor of two loops; folding it here would
  enlarge the change without changing the failure or cancellation behavior this change relies on.
- **Magic version sentinels** (`docs/backlog.md:31-37`) — deferred. This change adds no sentinel:
  the new indicator is driven by a typed `AgentUpdateAvailability.Status`, and
  `UpdateAvailabilityText` returns `null` for `Unknown` and `UpToDate`. Consolidating the existing
  `"…"`/`"unknown"` checks in `AgentCardItem.cs:159-160`, `AgentManagementViewModel.cs:110,261,283`,
  `AgentPaneController.cs:177`, and the legacy `MainWindow` is orthogonal and would touch unrelated
  surfaces.

## Alternatives considered

- Running agents' update commands in check-only mode — rejected: none of the six CLIs documents a
  dry-run flag, and the command itself performs the mutation.
- Parsing agents' internal update-check logs — rejected: not a contract, agent-specific, and only
  observable after the agent has run.
- Hard-coding sources in app code — rejected: the descriptor is the per-agent metadata home, next
  to `detection`, and stays user-editable.
- Rejecting unknown sources in `PluginDescriptorValidator` — rejected: forward compatibility;
  ignoring with a warning matches the existing unknown-command pattern.
- A single global availability cache — rejected: it would defeat the scoped `InvalidateFor`
  behavior landed in `91035c2` and would re-fetch every agent when one is probed.
- Showing availability in the launch-window row — deferred: the launch window already carries
  version and status; this change scopes the indicator to the management card.
- GitHub releases for Qwen Code — rejected on 2026-10-20 evidence (wrong component tag).

## Open questions

- Chip visual treatment: which accent token to use so the indicator reads as informational rather
  than as an error next to the danger-styled "Not installed" state. Resolve during implementation
  with the existing theme resources.
- Should the launch-window agent row surface the same indicator later? Out of scope here; revisit
  if users ask for it.
- GitHub conditional requests (`ETag`/`If-None-Match`) or a token: unnecessary with one GitHub
  agent and a 15-minute TTL; revisit if more agents move to GitHub releases.
- Whether invalidating on update *start* is early enough for agents whose interactive updater
  completes later. The current plan re-probes on the next population, which is after the user
  returns; if a stale cue is observed, invalidate again on the next page activation.
