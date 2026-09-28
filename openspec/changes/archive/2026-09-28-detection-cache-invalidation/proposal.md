## Why

`agent-detection` caches results for a configurable TTL, but nothing can clear that cache: the window's **Refresh** button only calls `IAgentVersionService.Invalidate()`. After a project gains or loses an indicator, availability can stay stale for up to the TTL (15 minutes by default).

## What Changes

- Add `Invalidate()` to `IAgentDetectionService`; the implementation clears all cached detection results.
- Make the window's **Refresh** clear the detection cache as well as the version cache, so the next agent refresh re-evaluates availability.

## Capabilities

### New Capabilities
<!-- None. -->

### Modified Capabilities
- `agent-detection`: adds a detection-cache invalidation capability (clear results on demand).

## Impact

- `src/CLIHub.Core/Interfaces/IAgentDetectionService.cs` — add `Invalidate()`.
- `src/CLIHub.Core/Services/AgentDetectionService.cs` — clear the cache.
- `src/CLIHub/Windows/MainWindow.xaml.cs` — `Refresh_Click` also invalidates detection.
- `tests/CLIHub.Tests/Services/AgentDetectionServiceTests.cs` — add an invalidation test.
