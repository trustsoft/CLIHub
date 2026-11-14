# Feed Redirect Hop Limit Off-By-One: Implementation Tasks

## Stage 1: Source fix and test update

- [x] 1.1 Fix the loop condition in `src/CLIHub.Core/Plugins/PluginFeedClient.cs:886` — change `hop < MaxRedirectHops` to `hop <= MaxRedirectHops`.
- [x] 1.2 Update the XML doc comment on `SendFollowingRedirectsAsync` (`:863-864`) — rephrase "manually follows HTTP redirects up to the maximum hop count" to "manually follows up to the maximum hop count of HTTP redirects."
- [x] 1.3 Update `GetFeedAsync_TooManyRedirects_IsRejected` in `tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs:310-326` — enqueue 6 redirects (not 5), assert 6 requests (not 5), keep the failure and error-message assertions.
- [x] 1.4 Add new test `GetFeedAsync_FiveRedirects_AreFollowedAndSucceed` — enqueue 5 redirects + 1 success (200 with a valid feed body), assert 6 total requests, assert `result.Success` is true.

### Gate: Build and test

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

- All projects must build without errors or warnings.
- All tests must pass, including the updated `GetFeedAsync_TooManyRedirects_IsRejected` and the new `GetFeedAsync_FiveRedirects_AreFollowedAndSucceed`.

## Stage 2: Spec delta

- [x] 2.1 Write `openspec/changes/feed-redirect-hop-limit/specs/plugin-feed/spec.md` — MODIFIED "Fetch and parse feed index" requirement: add two ADDED scenarios: "Exactly five redirects are followed" (5-redirect chain is followed, terminal response decides) and "Redirect beyond the hop limit is rejected" (a 6th redirect triggers the limit error).

### Gate: Validate

```powershell
openspec validate --all
```

- `openspec validate --all` must report 0 errors.

## Stage 3: Backlog hygiene

- [x] 3.1 Update `docs/backlog.md` — mark the "Plugin feed cancellation is reported as a download failure" entry (around line 447) as `**Status: Resolved by \`feed-redirect-hop-limit\` (2026-11-14)** — both sub-items addressed: cancellation was already resolved; the redirect off-by-one is fixed by the loop correction in \`PluginFeedClient.SendFollowingRedirectsAsync\`.` Keep the existing cancellation evidence and severity lines intact.

### Gate: Validate and diff

```powershell
openspec validate --all
git status --porcelain
```

- `openspec validate --all` must report 0 errors.
- `git status --porcelain` must show only the intended files: source, test, spec delta, and backlog — no unrelated modifications.

## Stage 4: Refresh knowledge graph

- [x] 4.1 Run `graphify update .` to refresh the knowledge graph with the updated source.

### Gate: Final validation

```powershell
openspec validate --all
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

- All gates must pass with zero errors.