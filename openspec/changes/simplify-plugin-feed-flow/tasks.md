# Tasks: Simplify the plugin feed fetch/install procedure

Conventions for every edit: file-scoped namespace first, `using` directives after it, nullable
reference types, `camelCase` private fields without underscore prefix, `this.` qualification,
braces required, XML docs on new public members, English only, test names in the
`MethodOrScenario_Condition_ExpectedResult` pattern. All `file:line` references are pre-change
positions at HEAD `73c9c69`; when earlier tasks in the same file shift lines, anchor by the named
symbol. Per `AGENTS.md`, run GitNexus impact analysis (`node .gitnexus/run.cjs impact "<symbol>"
--direction upstream --repo .`) before each first edit of `PluginFeedClient`,
`PluginFeedVersionComparator`, `BrowsePluginsDialogViewModel`, and `FeedPluginItem`, and heed
HIGH/CRITICAL/UNKNOWN warnings before proceeding. Do not touch `.opencode/agents/pipe/verifier.md`
(foreign out-of-scope edit — design.md D7). No git write commands, no `git commit`, no
`openspec archive` in these tasks.

## Stage 1: R2 — extract the structured-error wrapper (behavior-identical refactor)

- [ ] 1.1 Edit `src/CLIHub.Core/Plugins/PluginFeedClient.cs`: add a private sealed record
       `StructuredErrorProfile(string TimeoutError, string RequestError, string TimeoutLogMessage,
       string RequestLogMessage, string UnexpectedLogMessage, LogLevel UnexpectedLogLevel)` (needs
       `using Microsoft.Extensions.Logging;`, already imported `:10`) and two private static
       readonly instances with the exact strings from the current catch blocks — feed: "Feed
       request timed out" / "Could not load plugin feed. Check your network connection." / "Feed
       request timed out" / "Feed request failed" / "Feed request failed unexpectedly" with
       `LogLevel.Warning`; package: "Package download timed out" / "Package download failed" /
       "Package download timed out" / "Package download failed" / "Unexpected error during package
       download" with `LogLevel.Error` (source: `GetFeedAsync` catches `:112-132`,
       `DownloadPackageAsync` catches `:196-217`)
- [ ] 1.2 Edit `src/CLIHub.Core/Plugins/PluginFeedClient.cs`: add
       `private async Task<T> ExecuteWithStructuredErrorsAsync<T>(
       Func<CancellationToken, Task<T>> operation, Func<string, T> failure,
       StructuredErrorProfile profile, Action? onFailure, CancellationToken cancellationToken)`
       that creates the linked CTS with `CancelAfter(this.ProbeTimeout)` **inside** its try,
       awaits `operation(timeoutCts.Token)`, and catches in this order (design.md D3):
       `OperationCanceledException` when `!cancellationToken.IsCancellationRequested` →
       `this.logger.LogWarning(profile.TimeoutLogMessage);` then `onFailure?.Invoke();` then
       `return failure(profile.TimeoutError);`; `HttpRequestException ex` →
       `this.logger.LogWarning(ex, profile.RequestLogMessage);` + onFailure + `return
       failure(profile.RequestError);`; `OperationCanceledException` when
       `cancellationToken.IsCancellationRequested` → `throw;`; `Exception ex` → log at
       `profile.UnexpectedLogLevel` with `profile.UnexpectedLogMessage` + onFailure + `return
       failure(profile.RequestError);` (timeout logs carry no exception object, the other two do —
       match the current shapes exactly)
- [ ] 1.3 Edit `src/CLIHub.Core/Plugins/PluginFeedClient.cs` `GetFeedAsync` (`:63-133`): keep the
       feed-URL resolution/disabled check (`:65-69`) and the HTTPS pre-check (`:71-75`) outside;
       replace the CTS creation (`:77-78`) and the try/catch (`:80-132`) with one call to the
       wrapper whose `operation` lambda contains the probe + GET + status/size checks + parse
       exactly as today (`:82-110`, unchanged for now — stage 2 reworks the transport), whose
       `failure` is `errors => new PluginFeedResult(false, null, [errors])`, with the feed
       profile and `onFailure: null`
- [ ] 1.4 Edit `src/CLIHub.Core/Plugins/PluginFeedClient.cs` `DownloadPackageAsync`
       (`:136-218`): keep argument checks, HTTPS pre-check (`:138-145`), and temp-path allocation
       (`:147`) outside; replace the inner CTS + try/catch (`:151-217`) with one wrapper call
       whose `operation` lambda contains the current probe + GET + size/checksum logic
       (`:154-194`, unchanged for now), whose `failure` is `errors => new PluginDownloadResult(false,
       null, [errors])`, with the package profile and `onFailure: () => this.DeleteTempFile(tempFilePath)`
       (note: the two intentional `File.Delete`/`DeleteTempFile` calls inside the lambda at
       `:181` and `:190` stay where they are)
- [ ] 1.5 Gate (R2 refactor): `dotnet build CLIHub.sln` (0 errors, 0 warnings) and
       `dotnet test CLIHub.sln` (0 failed) — **no test file may be edited in this stage**; every
       existing test must pass unchanged, proving the refactor is behavior-identical

## Stage 2: R1 — single-pass GET redirect following

- [ ] 2.1 Edit `src/CLIHub.Core/Plugins/PluginFeedClient.cs`: add
       `private async Task<(HttpResponseMessage? Response, string? Error)>
       SendFollowingRedirectsAsync(string url, HttpCompletionOption completionOption,
       CancellationToken cancellationToken)` implementing design.md D1: loop
       `for (var hop = 0; hop < MaxRedirectHops; hop++)`; per hop send
       `new HttpRequestMessage(HttpMethod.Get, currentUrl)` via
       `this.httpClient.SendAsync(request, completionOption, cancellationToken)`; terminal when
       `(int)response.StatusCode < 300 || (int)response.StatusCode >= 400` → return
       `(response, null)` with the **caller** owning disposal (do **not** use `using var` on the
       returned terminal response; document the ownership in the XML doc); when the status is a
       3xx without `Headers.Location` → `this.logger.LogWarning("Redirect response without
       Location header");` then dispose and return that 3xx response as terminal `(response,
       null)` per D2; otherwise resolve `nextUrl` = absolute Location as-is, else
       `new Uri(new Uri(currentUrl), location).AbsoluteUri` (preserving `:249-251`); if
       `!IsHttpsUrl(nextUrl)` → log + dispose + return `(null, "Redirect to non-HTTPS URL
       rejected")` (`:253-257`); else dispose the intermediate response, set
       `currentUrl = nextUrl`, continue; after the loop → `this.logger.LogWarning("Maximum
       redirect hops ({MaxHops}) exceeded", MaxRedirectHops);` and return `(null,
       $"Redirect limit exceeded (maximum {MaxRedirectHops} hops)")` (`:262-263`)
- [ ] 2.2 Edit `src/CLIHub.Core/Plugins/PluginFeedClient.cs` `GetFeedAsync` operation lambda:
       replace the `FollowRedirectsAsync` call + GET (`:82-88`) with
       `var (response, redirectError) = await this.SendFollowingRedirectsAsync(feedUrl,
       HttpCompletionOption.ResponseHeadersRead, token)` (D1: the feed moves to
       ResponseHeadersRead so the 2 MB bound applies during streaming); if `response is null` →
       return `new PluginFeedResult(false, null, [redirectError ?? "Redirect rejected"])`
       (preserving `:83-86`); otherwise `using var finalResponse = response;` and keep the
       `!IsSuccessStatusCode` (`:89-93`), fast-path (`:97-101`), `ReadBoundedAsync`
       (`:103-108`), and `ParseFeedIndex` (`:110`) logic unchanged against `finalResponse`
- [ ] 2.3 Edit `src/CLIHub.Core/Plugins/PluginFeedClient.cs` `DownloadPackageAsync` operation
       lambda: same replacement (`:154-160`) with
       `HttpCompletionOption.ResponseHeadersRead` (as today `:160`), the same
       `redirectError ?? "Redirect rejected"` fallback (preserving `:155-158`), `using` the
       returned response, and the unchanged status/size/copy/checksum logic (`:161-194`)
- [ ] 2.4 Edit `src/CLIHub.Core/Plugins/PluginFeedClient.cs`: delete
       `FollowRedirectsAsync` (`:220-264`, including its doc comment); verify no references
       remain (its only callers were `:82` and `:154`)
- [ ] 2.5 Edit `tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs`: (a) remove the HEAD
       200 prelude from `EnqueueFeed` and `EnqueuePackage` (`:345-355` — each now enqueues only
       the GET response); (b) rework `GetFeedAsync_RedirectToHttps_IsFollowed` (`:200-215`) to
       enqueue `RedirectResponse("https://example.com/redirected")` + `JsonResponse(ValidIndexJson)`,
       assert success, assert `Equal(2, this.handler.Requests.Count)`, assert
       `Requests[0].RequestUri` is the feed URL and `Requests[1].RequestUri` is
       `https://example.com/redirected`, and assert every request used `HttpMethod.Get`; (c) add
       `GetFeedAsync_NoRedirects_SendsSingleGetRequest` — one `JsonResponse`, assert success and
       exactly 1 request whose `Method` is `HttpMethod.Get` (pins the removed extra round-trip);
       (d) add `GetFeedAsync_RedirectWithoutLocation_ReturnsStructuredError` — enqueue a
       `HttpStatusCode.Redirect` (302) response with no `Location` header, assert
       `Assert.False(result.Success)` and `Assert.Contains("Could not load plugin feed. Check
       your network connection.", result.Errors)` (pins design.md D2); (e) add
       `GetFeedAsync_RelativeRedirectLocation_IsResolvedAgainstCurrentUrl` — enqueue a 302 whose
       `Location` header is the relative URI `new Uri("redirected", UriKind.Relative)` (build the
       response directly or add a relative-variant helper; `RedirectResponse` at `:366-367` takes
       an absolute string), served from `https://example.com/feed.json` so it resolves to
       `https://example.com/redirected`, then the JSON response; assert success and that
       `this.handler.Requests[1].RequestUri!.AbsoluteUri` is `https://example.com/redirected`
       (pins the preserved relative resolution); (f) in `GetFeedAsync_RedirectToHttp_IsRejected`
       (`:217-228`) and `GetFeedAsync_TooManyRedirects_IsRejected` (`:230-245`) keep the
       existing assertions (single request / 5 requests and the two redirect error strings) and
       add `HttpMethod.Get` assertions; (g) in `DownloadPackageAsync_ChecksumMatch_ReturnsTempFile`
       (`:285-310`) add `Assert.Single(this.handler.Requests)` and a GET-method assert; leave
       `GetFeedAsync_MalformedRedirectLocation_ReturnsStructuredError` (`:259-271`), the timeout
       test (`:273-283`), and the three oversize tests (`:159-198`) untouched — they must pass
       as-is
- [ ] 2.6 Gate (R1): `dotnet build CLIHub.sln` (0 errors, 0 warnings) and
       `dotnet test CLIHub.sln` (0 failed; the plugin-feed client suite grows by exactly the
       three new tests from 2.5c-e — any other count delta is a stop-and-report signal)

## Stage 3: R3 — public `IsDowngrade` and the one-call downgrade check

- [ ] 3.1 Edit `src/CLIHub.Core/Plugins/PluginFeedVersionComparator.cs`: add public static
       `bool IsDowngrade(string? installedVersion, string feedVersion)` with XML docs (design.md
       D4): `installedVersion is not null && TryParseVersion(installedVersion) is { } a &&
       TryParseVersion(feedVersion) is { } b && CompareVersions(a, b) > 0`; keep
       `TryParseVersion` (`:75`) and `CompareVersions` (`:93`) `internal`
- [ ] 3.2 Edit `src/CLIHub/ViewModels/Agents/BrowsePluginsDialogViewModel.cs`: replace the
       isDowngrade computation (`:199-205`) with
       `var isDowngrade = PluginFeedVersionComparator.IsDowngrade(installedVersion, entry.Version);`
       — nothing else in `LoadFeedAsync` changes
- [ ] 3.3 Edit `tests/CLIHub.Core.Tests/Plugins/PluginFeedVersionComparatorTests.cs`: add
       `IsDowngrade` cases — installed numerically greater → true; equal → false; feed newer →
       false; `installedVersion` null → false; either version non-parseable (e.g. "abc" or
       "1.0") → false (design.md D4 equivalence proof defines the truth table)
- [ ] 3.4 Gate (R3): `dotnet build CLIHub.sln` (0 errors, 0 warnings) and
       `dotnet test CLIHub.sln` (0 failed; `BrowsePluginsDialogViewModelTests` downgrade-related
       load assertions at `:52-55` must pass unchanged, proving the collapsed computation keeps
       semantics)

## Stage 4: R4 — in-place card update after install

- [ ] 4.1 Edit `src/CLIHub/ViewModels/Agents/FeedPluginItem.cs`: convert `Status` (`:112`),
       `InstalledVersion` (`:117`), and `IsDowngrade` (`:127`) from get-only to
       `ObservableObject.SetProperty`-backed properties with private setters (backing fields
       `status`, `installedVersion`, `isDowngrade`), each raising `OnPropertyChanged` for its
       computed dependents (design.md D5.1): `Status` → `StatusText`, `VersionText`, `ActionText`,
       `ShowActionButton`; `InstalledVersion` → `VersionText`; `IsDowngrade` → `WarningMessage`,
       `HasWarning`; `CanInstall`, `BlockedReason`, and all identity/metadata properties stay
       get-only; update the class doc comment to note the item is mutable only for post-install
       status refresh
- [ ] 4.2 Edit `src/CLIHub/ViewModels/Agents/FeedPluginItem.cs`: add
       `public void MarkInstalled(string? installedVersion)` with XML docs (design.md D5.2-D5.3):
       sets `InstalledVersion = installedVersion`, then `Status = installedVersion is null ?
       PluginUpdateStatus.UpdateAvailable : PluginFeedVersionComparator.Compare(installedVersion,
       this.Version)`, then `IsDowngrade = PluginFeedVersionComparator.IsDowngrade(installedVersion,
       this.Version)` (order matters so `VersionText` recomputes against the new installed version)
- [ ] 4.3 Edit `src/CLIHub/ViewModels/Agents/BrowsePluginsDialogViewModel.cs`
       `InstallPluginAsync`: in the success branch replace `await this.LoadFeedAsync(cancellationToken);`
       (`:286`) with `item.MarkInstalled(preview.Version);` placed after the outcome dialog and
       success log (`:282-283`); remove nothing else — the catch/cleanup shape (`:294-317`) stays
- [ ] 4.4 Edit `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs`: (a)
       extend `InstallCommand_Success_DownloadsInspectsConfirmsInstalls` (`:214-253`) — after
       `InstallCommand.Execute(item)` add `feedClient.Verify(x => x.GetFeedAsync(It.IsAny<CancellationToken>()),
       Times.Once)` (initial load only — pins no re-fetch) and assert `item.Status ==
       PluginUpdateStatus.Current`, `item.InstalledVersion == "1.0.0"`, and
       `Assert.False(item.ShowActionButton)`; (b) add
       `InstallCommand_UpdateAvailable_Success_MarksCardCurrentWithoutRefetchingFeed` — the
       update-path arrangement of `:255-294` (feed 2.0.0, state 1.0.0) with confirm + successful
       install, asserting after Execute: `GetFeedAsync` `Times.Once`, item `Status ==
       PluginUpdateStatus.Current`, `InstalledVersion == "2.0.0"`, `IsDowngrade` false
- [ ] 4.5 Gate (R4): `dotnet build CLIHub.sln` (0 errors, 0 warnings) and
       `dotnet test CLIHub.sln` (0 failed; the suite grows by exactly the one new ViewModel test
       — any other count delta is a stop-and-report signal)

## Stage 5: Final gates and handoff

- [ ] 5.1 Final build gate: `dotnet build CLIHub.sln` — confirm 0 errors, 0 warnings
- [ ] 5.2 Final test gate: `dotnet test CLIHub.sln` — confirm 0 failed, 0 skipped; record the
       passed count and confirm the only delta versus the pre-change baseline is the four new
       tests (2.5c-e, 4.4b)
- [ ] 5.3 Run `openspec validate --all` — confirm 0 errors with
       `✓ change/simplify-plugin-feed-flow` in the output; fix any structural finding only
       inside `openspec/changes/simplify-plugin-feed-flow/`, never in `openspec/specs/`
- [ ] 5.4 Run `node .opencode/scripts/check-model-policy.mjs` if any subagent was dispatched
       during implementation, and `node .opencode/scripts/pipeline-state.mjs add
       simplify-plugin-feed-flow` to record the pipeline entry (all stage transitions through
       the script, never direct `.opencode/pipeline.json` edits)
- [ ] 5.5 Run `graphify update .` so the knowledge graph reflects the reworked feed client and
       item model
- [ ] 5.6 Scope check: `git status --porcelain` lists, for this change, only
       `src/CLIHub.Core/Plugins/PluginFeedClient.cs`,
       `src/CLIHub.Core/Plugins/PluginFeedVersionComparator.cs`,
       `src/CLIHub/ViewModels/Agents/BrowsePluginsDialogViewModel.cs`,
       `src/CLIHub/ViewModels/Agents/FeedPluginItem.cs`,
       `tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs`,
       `tests/CLIHub.Core.Tests/Plugins/PluginFeedVersionComparatorTests.cs`,
       `tests/CLIHub.Tests/ViewModels/Agents/BrowsePluginsDialogViewModelTests.cs`, and
       `openspec/changes/simplify-plugin-feed-flow/` — plus the pre-existing foreign
       modification `.opencode/agents/pipe/verifier.md`, which MUST remain exactly as the other
       session left it (unstaged, unmodified, un-reverted; design.md D7)
- [ ] 5.7 Report the gate results to the user together with a recommended (non-blocking) manual
       smoke of the Browse Plugins dialog — open the dialog, install a plugin from a test feed,
       confirm the card flips to "Installed" with the installed version and the action button
       hidden, and confirm Refresh still re-fetches — then ask for explicit `git commit` and
       `openspec archive` approval (approval never carries over; nothing is staged before it)
