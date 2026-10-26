## 1. XAML swap

- [x] 1.1 In `src/CLIHub/Views/SettingsAgentsPage.xaml`, replace the framed logo `Border` at lines 184-195 (the `Border` with `Width="32"`, `Height="32"`, `VerticalAlignment="Top"`, `BorderBrush="{DynamicResource ControlStrokeBrush}"`, `BorderThickness="1"`, `CornerRadius="6"`, and the `ImageBrush` background bound to `LogoPath` via `PathToImage`) with a plain `Image` carrying exactly: `Grid.Column="0"`, `Width="32"`, `Height="32"`, `VerticalAlignment="Top"`, `RenderOptions.BitmapScalingMode="HighQuality"`, `Source="{Binding LogoPath, Converter={StaticResource PathToImage}}"`, `Stretch="Uniform"` — no stroke, no corner radius, no plate background, no margin (the 12 px gutter stays on the text column at line 197)
- [x] 1.2 Confirm the diff is scoped: `git diff src/CLIHub/Views/SettingsAgentsPage.xaml` shows only that element swap; the card chrome (lines 171-176), grid columns, name/state-chip/update-chip block, "Supported commands" hint, message line, action buttons, and all other XAML in the file are untouched, and no other source file changed
- [x] 1.3 Gate: run `dotnet build CLIHub.sln` (expect 0 warnings / 0 errors), `dotnet test CLIHub.sln` (expect 1176 passed, 0 failed: 478 CLIHub.Tests + 60 CLIHub.Core.Windows.Tests + 638 CLIHub.Core.Tests — the recorded baseline at `4550b99`), and `openspec validate --all` (expect 30 passed, 0 failed — the 29 baseline specs plus this change); all three must pass before moving on

## 2. Repository hygiene

- [x] 2.1 Run `node .opencode/scripts/check-model-policy.mjs` and confirm it passes
- [x] 2.2 Run `graphify update .` so the knowledge graph reflects the XAML change
- [x] 2.3 Gate: both commands exit clean, and `git status --short` shows no changed files beyond `src/CLIHub/Views/SettingsAgentsPage.xaml` and this change's directory `openspec/changes/agent-logo-unframed/`

## 3. Manual smoke (blocks commit/archive)

- [x] 3.1 Run the app (`dotnet run --project src/CLIHub/CLIHub.csproj`), open Settings → CLI Agents, and check every agent card: the logo renders as a plain unframed image — no stroke ring, no rounded-corner clipping, no plate — at 32x32, top-aligned with the agent name; an agent without its own `logo.png` shows the shared default logo unframed
- [x] 3.2 Confirm the unchanged surroundings: the card's rounded surface and title typography, the state chip and advisory update chip next to the name, the "Supported commands" hint, the message line, and the Detect / Update / Edit Config buttons keep their current layout and spacing; the framed icon blocks on the "Default agent" / "Cache TTL" / "Version check timeout" setting cards and the AGENTS / DEFAULT / DETECTION summary cards are still framed; the launch window's agent list still shows its 28x28 plain logos
- [x] 3.3 Record the smoke result in this tasks file, report back, and only then ask the user for `git commit` and `openspec archive` approval — per repo policy both MUST wait for the confirmed smoke result

Smoke result: user confirmed 2026-10-26 — logos render unframed on the CLI Agents page and the surrounding card layout is unchanged.
