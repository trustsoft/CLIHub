# Quick Wins Sweep — Design

## Verified Current State

### 1. Unused glyph constants

**File:** `src/CLIHub/Theming/IconGlyphs.cs:24-25`

```csharp
public const string Diagnostic = "\uE9D9";
public const string Process = "\uE9F3";
```

**Reference count:** Zero references in `src/` and `tests/` (verified with `grep -r "IconGlyphs\.Diagnostic" src/` and `grep -r "IconGlyphs\.Process" src/`, both XAML and C# checked). The constants were unused after the `agent-detection-page-icons` change swapped them out.

**Decision:** Remove both. `IconGlyphs` is the single compile-time-checked glyph catalog. An unreferenced member is dead code with no catalog-preservation value; deliberate reintroduction on future need is straightforward (copy the glyph constant back from git history).

### 2. Static maximize tooltip

**File:** `src/CLIHub/Views/SettingsWindow.xaml:297-309`

```xml
<Button Click="OnMaximizeRestoreClick"
        Style="{StaticResource WindowCloseButton}"
        ToolTip="Maximize">
    <TextBlock FontFamily="{StaticResource Sizing.FontFamily.Icon}" FontSize="10">
        <TextBlock.Style>
            <Style TargetType="TextBlock">
                <Setter Property="Text" Value="{x:Static themes:IconGlyphs.Maximize}"/>
                <Style.Triggers>
                    <DataTrigger Binding="{Binding WindowState, RelativeSource={RelativeSource AncestorType=Window}}" Value="Maximized">
                        <Setter Property="Text" Value="{x:Static themes:IconGlyphs.Restore}"/>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
```

The `ToolTip="Maximize"` attribute on the `Button` (line 299) is static. The glyph already flips via the `DataTrigger` on `WindowState` (lines 305-307). After maximizing, the tooltip remains "Maximize" while the glyph is Restore.

**Decision:** Move the `ToolTip` attribute into a new style for the `Button` with a baseline setter (`ToolTip="Maximize"`) and override it with a `DataTrigger` on `WindowState == Maximized` that sets `ToolTip="Restore"`. This keeps the glyph's existing structure and extends it to the tooltip. The button already has `Style="{StaticResource WindowCloseButton}"`, so the new tooltip logic will be an inline style overlay. Alternatively, extend the button's existing style resource with the tooltip logic if the style is local to this file (it is not — `WindowCloseButton` is a shared resource from `Theming/Common/WindowChromeStyles.xaml`), so the inline style approach is appropriate.

**Implementation approach:** Replace the static `ToolTip="Maximize"` attribute with an inline `Button.Style` that includes both the `StaticResource WindowCloseButton` as the `BasedOn` and adds the tooltip trigger. Example structure:

```xml
<Button Click="OnMaximizeRestoreClick">
    <Button.Style>
        <Style TargetType="Button" BasedOn="{StaticResource WindowCloseButton}">
            <Setter Property="ToolTip" Value="Maximize"/>
            <Style.Triggers>
                <DataTrigger Binding="{Binding WindowState, RelativeSource={RelativeSource AncestorType=Window}}" Value="Maximized">
                    <Setter Property="ToolTip" Value="Restore"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Button.Style>
    <TextBlock FontFamily="{StaticResource Sizing.FontFamily.Icon}" FontSize="10">
        <!-- existing glyph structure unchanged -->
    </TextBlock>
</Button>
```

### 3. Plugin feed cancellation and redirect limit

**File:** `src/CLIHub.Core/Plugins/PluginFeedClient.cs`

#### Cancellation handling

**GetFeedAsync (lines 80-128):**

```csharp
try
{
    var (finalUrl, redirectError) = await this.FollowRedirectsAsync(feedUrl, timeoutCts.Token).ConfigureAwait(false);
    // ... (lines 83-110)
}
catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
{
    this.logger.LogWarning("Feed request timed out");
    return new PluginFeedResult(false, null, ["Feed request timed out"]);
}
catch (HttpRequestException ex)
{
    this.logger.LogWarning(ex, "Feed request failed");
    return new PluginFeedResult(false, null, ["Could not load plugin feed. Check your network connection."]);
}
catch (Exception ex)
{
    // A malformed redirect Location (UriFormatException) or any other unexpected failure
    // must degrade to a structured error rather than escape to the caller.
    this.logger.LogWarning(ex, "Feed request failed unexpectedly");
    return new PluginFeedResult(false, null, ["Could not load plugin feed. Check your network connection."]);
}
```

Line 112 catches timeout cancellation (the linked CTS) when `!cancellationToken.IsCancellationRequested`, but line 122 catches general `Exception`, which intercepts external cancellation and reports "Could not load plugin feed" instead of propagating `OperationCanceledException`.

**DownloadPackageAsync (lines 145-209):** Same pattern at lines 192-208.

**Caller analysis:**

- `BrowsePluginsDialogViewModel.LoadFeedAsync` (line 151): calls `GetFeedAsync` inside a try-catch that catches general `Exception` at line 228, so external cancellation would be swallowed and reported as "Could not load plugin feed. Check your network connection." (line 230). However, `LoadFeedAsync` itself runs inside `IApplicationOperationLifetime.RunAsync` (line 322), which provides a cancellation token tied to application shutdown or user cancel. The ViewModel's `InstallPluginAsync` (line 245) calls `DownloadPackageAsync` and explicitly catches `OperationCanceledException` at line 294 separately from general exceptions at line 298, so propagation is safe and expected.

**Decision:** Add `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }` before the general `catch (Exception)` in both methods. This lets external cancellation propagate to the caller, which already handles it correctly.

**Rejected alternative:** Rethrow inside the general `catch` if `ex is OperationCanceledException && cancellationToken.IsCancellationRequested`. The dedicated catch is clearer and matches the existing timeout-cancellation filter.

#### Redirect hop limit

**FollowRedirectsAsync (lines 221-256):**

```csharp
private async Task<(string? FinalUrl, string? Error)> FollowRedirectsAsync(string url, CancellationToken cancellationToken)
{
    var currentUrl = url;
    for (var hop = 0; hop < MaxRedirectHops; hop++)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, currentUrl);
        using var response = await this.httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if ((int)response.StatusCode < 300 || (int)response.StatusCode >= 400)
        {
            // Not a redirect
            return (currentUrl, null);
        }

        if (response.Headers.Location is null)
        {
            this.logger.LogWarning("Redirect response without Location header");
            return (currentUrl, null);
        }

        var nextUrl = response.Headers.Location.IsAbsoluteUri
            ? response.Headers.Location.AbsoluteUri
            : new Uri(new Uri(currentUrl), response.Headers.Location).AbsoluteUri;

        if (!IsHttpsUrl(nextUrl))
        {
            this.logger.LogError("Redirect to non-HTTPS URL rejected: {NextUrl}", nextUrl);
            return (null, "Redirect to non-HTTPS URL rejected");
        }

        currentUrl = nextUrl;
    }

    this.logger.LogWarning("Maximum redirect hops ({MaxHops}) exceeded", MaxRedirectHops);
    return (null, $"Too many redirects (more than {MaxRedirectHops} hops)");
}
```

Line 224: `hop < MaxRedirectHops` where `MaxRedirectHops = 5` (line 20) means hops 0–4 are allowed (5 HEAD requests), and hop 5 is rejected. The loop exits at line 254 after 5 iterations without entering the body for hop 5, so the message "more than 5 hops" (line 255) is accurate but confusing — the limit is actually "5 or more hops" when hop is 0-based. The docs (XML comment at line 214 and `openspec/specs/plugin-feed/spec.md` line 63-67) say "up to 5 hops" and "more than 5 hops", which both imply a 5-hop allowance.

**Semantics clarification:** The loop allows 5 HEAD requests (hops 0–4). A chain of 5 redirects (6 requests total: initial + 5 redirects) is rejected. The phrase "up to 5 hops" in the docs means "at most 5 redirect responses", which the code enforces by rejecting the 6th request (hop 5). The message "more than 5 hops" is accurate for the rejection threshold but misleading because the loop bound is `< 5`.

**Decision:** Keep the loop bound `hop < MaxRedirectHops` unchanged (5 HEAD requests, rejecting the 6th). Reword the message from `"Too many redirects (more than {MaxRedirectHops} hops)"` to `"Too many redirects ({MaxRedirectHops} or more hops)"` or `"Too many redirects (limit: {MaxRedirectHops} hops)"` to clarify that the limit itself is 5 and the rejection is at 5, not after 5. Alternatively, change the message to `"Redirect limit exceeded (maximum {MaxRedirectHops} hops)"` which avoids the off-by-one confusion entirely.

**Chosen wording:** `"Redirect limit exceeded (maximum {MaxRedirectHops} hops)"` for clarity without changing behavior.

**Rejected alternative:** Change the loop to `hop <= MaxRedirectHops` and keep the message — this would allow 6 HEAD requests (hops 0–5), which exceeds the documented 5-hop limit. The current behavior is correct; only the message is misleading.

### 4. Stale comment

**File:** `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Detection.cs:347`

```csharp
// Torn down mid-pass: the abandoned pass must settle like a thrown probe instead of
// leaving the cards it announced stuck on "Detecting…" forever.
```

The comment says `"Detecting…"` (with ellipsis), but the constant is `AgentVersionText.Detecting` which is `"Detecting"` (no ellipsis) as of the `busy-marker-control` change. The comment documents the state, not the literal, so rewording to describe the state is more accurate than quoting an outdated literal.

**Decision:** Reword to `"leaving the cards it announced stuck in the Detecting state forever."` to describe the state rather than quoting the literal.

**Rejected alternative:** Change to `"Detecting"` (no ellipsis) to match the current constant. This is less clear because the comment is about the state, not the display string.

## Decisions Beyond the Brief

- **Item 2 (tooltip):** Chose inline `Button.Style` with `BasedOn` rather than modifying the shared `WindowCloseButton` resource, keeping the change local to `SettingsWindow.xaml`.
- **Item 3 (hop limit):** Chose to reword the message rather than change the loop bound, preserving the existing 5-request limit without behavioral change.
- **Item 4 (comment):** Chose to describe the state rather than quote the literal, making the comment more resilient to future string changes.

## Alternatives Rejected

- **Item 1:** Keep the constants as deliberate catalog entries. Rejected: an unreferenced constant serves no catalog-preservation purpose and is pure dead weight.
- **Item 2:** Move the tooltip logic into the shared `WindowCloseButton` style. Rejected: the shared style is used by other windows (e.g., `BrowsePluginsWindow`), and not all chrome buttons need dynamic tooltips.
- **Item 3 (cancellation):** Rethrow inside the general `catch` instead of adding a dedicated catch. Rejected: a dedicated catch is clearer and matches the existing timeout-cancellation filter.
- **Item 3 (hop limit):** Change the loop bound to `hop <= MaxRedirectHops` and keep the message. Rejected: this would allow 6 HEAD requests, exceeding the documented 5-hop limit.

## Deviations

None. Each item follows the backlog's proposed remedy.

## Open Questions

None. All decisions are resolved and justified above.

## Spec Decision

**Skip specs** (`skip_specs: true`). Justification:

- **Items 1, 2, 4:** Non-behavior changes (dead code removal, cosmetic tooltip, comment wording). No spec delta needed.
- **Item 3 (cancellation):** Observable behavior change (exception propagation), but `openspec/specs/plugin-feed/spec.md` does not pin the error strings or explicitly specify cancellation handling. The spec says "no unhandled exceptions" (line 302-306) and lists expected error messages for network failures (lines 78-79, 92), but does not require swallowing external cancellation. The fix makes the client's cancellation handling consistent with standard .NET async patterns (propagate `OperationCanceledException` when `cancellationToken.IsCancellationRequested`). The caller (`BrowsePluginsDialogViewModel`) already handles propagated cancellation correctly, so no UI behavior change.
- **Item 3 (hop limit):** Message wording change only; no behavioral change. The spec does not quote the rejection message verbatim.

The plugin-feed spec is about observable UI and network behavior, not internal error-handling mechanics. The cancellation fix does not change what the UI displays (the dialog still handles cancellation correctly); it only changes which exception type reaches the caller. A MODIFIED delta would add no value and would risk pinning an implementation detail that the spec deliberately left unspecified.
