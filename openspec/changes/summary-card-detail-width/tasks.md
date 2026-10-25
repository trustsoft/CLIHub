## 1. Template restructure

- [x] 1.1 In `src/CLIHub/Themes/SettingsStyles.xaml`, in the `Style TargetType="{x:Type controls:SummaryCard}"` template (Grid at lines 363-410): insert a `Grid.RowDefinitions` block with two `RowDefinition Height="Auto"` entries before `Grid.ColumnDefinitions`; cut `DetailText` (lines 382-386) and `AlertText` (lines 387-393) out of the column-0 `StackPanel`; add a new `StackPanel` after the icon `Border` carrying `Grid.Row="1"` and `Grid.ColumnSpan="2"` holding the two moved `TextBlock`s with every attribute byte-identical (`x:Name`, `Margin="0,2,0,0"`, `Style="{StaticResource HintText}"`, `Foreground` on `AlertText`, the template bindings, `Visibility="Collapsed"` on `AlertText`); leave the label/value `StackPanel` and the icon `Border` in implicit row 0 with no new attributes; follow the file's existing attribute order (attached `Grid.*` properties first, as on the icon `Border` at line 395)
- [x] 1.2 Extend the English comment above the style (lines 346-351) with one sentence recording the layout rule: the label and value reserve the icon block's column, while the detail and alert lines span both columns below the icon row, so their right edge is the card's right content edge
- [x] 1.3 Confirm the diff is scoped: `git diff src/CLIHub/Themes/SettingsStyles.xaml` shows only the moves above; the nine `SummaryCard` instances in `src/CLIHub/Views/SettingsWindow.xaml` (lines 183-249), `src/CLIHub/Controls/SummaryCard.cs`, and every trigger in the template (lines 412-445) are untouched; leave all pre-existing `agent-install-state` working-tree edits alone
- [x] 1.4 Gate: run `dotnet build CLIHub.sln` (expect 0 warnings / 0 errors), `dotnet test CLIHub.sln` (expect 1161 passed: 463 CLIHub.Tests + 60 CLIHub.Core.Windows.Tests + 638 CLIHub.Core.Tests), and `openspec validate --all` (expect 31 passed, 0 failed — the count includes both active changes); all three must pass before moving on

## 2. Repository hygiene

- [x] 2.1 Run `node .opencode/scripts/check-model-policy.mjs` and confirm it passes
- [x] 2.2 Run `graphify update .` so the knowledge graph reflects the XAML change
- [x] 2.3 Gate: both commands exit clean, and `git status --short` shows no changed files beyond `SettingsStyles.xaml`, this change's directory, and the pre-existing `agent-install-state` edits (untouched)

## 3. Manual smoke (blocks commit/archive)

- [x] 3.1 Run the app (`dotnet run --project src/CLIHub/CLIHub.csproj`), open Settings → Projects, and narrow the window until the summary cards approach the reported width (~188 DIP): the DISCOVERED detail "Managed by Project Scanning" must stay on one line whose right edge aligns with the icon block's right edge; label and value must never run under the icon; the three cards must keep equal heights
- [x] 3.2 Check the same alignment on the Project Scanning page (PROJECTS/AGENTS/DAEMON, including the watcher detail line) and the CLI Agents page (AGENTS/DEFAULT/DETECTION), and — when a project folder is missing so the alert shows — on the Projects page's danger-toned alert line; the empty-detail and empty-alert cards must look exactly as before
- [x] 3.3 Record the smoke result in this tasks file, report back, and only then ask the user for `git commit` and `openspec archive` approval — per repo policy both MUST wait for the confirmed smoke result

> Smoke result (2026-10-26): confirmed OK by the user on the running application; the detail line ends at the icon block's right edge, label/value stay clear of the icon, and the cards keep their heights.
