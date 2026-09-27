## 1. Model

- [x] 1.1 Add `bool IsAvailable` and a read-only `double RowOpacity` (1.0 when available, 0.4 otherwise) to `src/CLIHub/Windows/AgentItem.cs`, and verify it compiles

## 2. UI

- [x] 2.1 In `MainWindow.RefreshAgents`, set `IsAvailable` from `IAgentDetectionService.IsAvailableInProject` when a project is selected, and `true` when no project is selected, and verify the window compiles
- [x] 2.2 Bind the agent row template's `Opacity` to `RowOpacity` in `MainWindow.xaml` and verify the list renders

## 3. Verification

- [x] 3.1 Build the full solution with 0 warnings/errors and verify `dotnet test` still passes
- [x] 3.2 Manually verify: with a project selected, agents not used in it appear dimmed and the rest normal; switching project updates the dimming; with no project selected, none are dimmed; a dimmed agent can still be selected and its commands run

> Verified: build 0 warnings/errors; 91/91 tests pass; user-confirmed dimming (unavailable agents dimmed, available normal, updates on project switch, none dimmed without a project, dimmed agents remain usable).
