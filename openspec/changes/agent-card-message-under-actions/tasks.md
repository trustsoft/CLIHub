## 1. Action-column stack layout

- [x] 1.1 In `src/CLIHub/Views/SettingsAgentsPage.xaml`, remove the `Grid.RowDefinitions` block from the per-agent card grid (currently lines 178-182) and remove the row-1 message `TextBlock` (currently lines 279-287); the card grid returns to three columns and no rows
- [x] 1.2 Wrap the action `controls:SpacedPanel` (currently lines 225-277) in a vertical `StackPanel` placed in `Grid.Column="2"` with `VerticalAlignment="Top"`; add `HorizontalAlignment="Right"` to the `SpacedPanel`; insert the message `TextBlock` as the stack's second child after the buttons with exactly: `Margin="0,2,0,0"`, `MaxWidth="{StaticResource Sizing.AgentRow.TextMaxWidth}"`, `HorizontalAlignment="Right"`, `Style="{StaticResource StatusLine}"`, `FontSize="{StaticResource Sizing.FontSize.Section}"`, `Text="{Binding Message}"`, `TextAlignment="Right"`, `Visibility="{Binding HasMessage, Converter={StaticResource BoolToVisibility}}"` — the 2 DIP gap is measured directly from the buttons, and the 11 DIP font matches the "Supported commands" hint; nothing else on the card changes
- [x] 1.3 Scope check: `git diff src/CLIHub/Views/SettingsAgentsPage.xaml` shows only the row-definition removal, the action-column stack wrapper, and the moved message `TextBlock`; the card chrome, logo, name/chips block, hint, all four action buttons with their commands/glyphs/tooltips/enabled-state bindings, and every other element in the file are untouched; `git status --short` shows no unexpected source file
- [x] 1.4 Gate: `dotnet build CLIHub.sln` (0 warnings / 0 errors), `dotnet test CLIHub.sln` (expect 1202 passed, 0 failed), and `openspec validate --all` (0 failed); all three must pass before moving on

## 2. Blocked-update copy (completed in the previous iteration)

- [x] 2.1 In `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`, change the blocked-state pass to write exactly `card.Message = "Update blocked while running";`, gate the write on `running.Count > 0` instead of `blockedNames.Length > 0`, and delete the now-unused `blockedNames` join (currently lines 453,461-463); no other message wording changes
- [x] 2.2 In `src/CLIHub.Core/Agents/AgentManagementService.cs`, return exactly `"Update blocked while running"` as the `AgentUpdateOutcome.Blocked` message and delete the now-unused `names` join (currently lines 93-96); no other behavior changes
- [x] 2.3 Update the exact-message expectations rather than weakening them: in `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs` the four `Update blocked while the agent is running: a` expectations become `Update blocked while running`; in `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs` the runtime-hosted assertion at ~:403 becomes `Assert.Equal("Update blocked while running", result.Message)`; no test is deleted, skipped, or loosened
- [x] 2.4 Gate: build, tests (1202 passed, 0 failed), and `openspec validate --all` (0 failed) — passed in the previous iteration

## 3. Repository hygiene

- [x] 3.1 Run `node .opencode/scripts/check-model-policy.mjs` and confirm it passes
- [x] 3.2 Run `graphify update .` so the knowledge graph reflects the final XAML and message-copy changes
- [x] 3.3 Gate: both commands exit clean, and `git status --short` shows no changed files beyond `src/CLIHub/Views/SettingsAgentsPage.xaml`, `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`, `src/CLIHub.Core/Agents/AgentManagementService.cs`, the two updated test files, `docs/images/settings-agents.png`, and this change's directory

## 4. Manual smoke (blocks commit/archive)

- [x] 4.1 Run the app (`dotnet run --project src/CLIHub/CLIHub.csproj`), open Settings → CLI Agents, and exercise the message producers: a blocked-update explanation (choose Update while a matching registered agent process is running), a per-card Detect outcome, a per-card Update outcome, and a per-card Check updates outcome (`Up to date` / `Update available · {installed} → {latest}` / `Update availability unknown`) — the message sits directly under the action buttons with a 2 DIP vertical gap, right-aligned to the panel's right edge, at the same 11 DIP font size as the "Supported commands" line, wrapping inside 240 DIP without clipping or overlapping the buttons/card border; the blocked explanation reads exactly `Update blocked while running`
- [x] 4.2 Confirm short messages hug the right edge directly under the buttons, and that a message appearing or disappearing (run Detect, then observe) shifts nothing above it and does not change the card's width
- [x] 4.3 Confirm cards with no message keep their exact current height and show no leftover gap; cards side by side look consistent
- [x] 4.4 Confirm everything else is unchanged: logo, agent name, state chip, advisory update chip, "Supported commands" hint, the four buttons' order/glyphs/tooltips and enabled states, the AGENTS / DEFAULT / DETECTION summary cards, the header-wide Detect / Check updates actions, and all other Settings pages
- [x] 4.5 Record the smoke result in this tasks file, report back, and only then ask the user for `git commit` and `openspec archive` approval — per repo policy both MUST wait for the confirmed smoke result

Smoke result: user confirmed 2026-10-27 — the message sits directly under the action buttons with the 2 DIP gap at the 11 DIP hint font, right-aligned, the blocked copy reads `Update blocked while running`, and the rest of the card/page is unchanged. A separate follow-up was raised (per-card Detect/Check updates should be independent across cards); it belongs to a new change for the action-concurrency model, not this layout/copy change.

## 5. Docs image refresh (manual, orchestrator/user)

- [x] 5.1 Refresh only `docs/images/settings-agents.png`: with CLIHub closed, run `docs/images/tools/Capture-ReadmeImages.ps1 -SkipBuild -SkipWebImages -OutputDirectory <scratch folder inside the repository>` (the script refuses an output directory outside the repo and backs up/restores `%APPDATA%\CLIHub\config.json` and the logo cache), then copy only `settings-agents.png` from the scratch folder over `docs/images/settings-agents.png` and delete the scratch folder — leave every other capture untouched
- [ ] 5.2 Confirm `git status --short` shows only the expected source/test files, `docs/images/settings-agents.png`, and `openspec/changes/agent-card-message-under-actions/`; the image diff is binary-only; then fold the image into the commit per the user's approval decision
