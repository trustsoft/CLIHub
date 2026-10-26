# Design: agent-logo-unframed

## Context (verified current state)

All file:line references were verified against the clean working tree at HEAD `4550b99` on
2026-10-26 (`git status --short` is empty; no active change exists under `openspec/changes/`).

### The framed logo in the settings agent card

- The per-agent list is an `ItemsControl` over `AgentsState.Cards` whose `DataTemplate`
  (src/CLIHub/Views/SettingsAgentsPage.xaml:168-276) renders each card as a `Border` chrome with
  `Padding="12"`, `Background="{DynamicResource PopoverBgBrush}"`,
  `BorderBrush="{DynamicResource FieldStrokeBrush}"`, `BorderThickness="1"`,
  `CornerRadius="{StaticResource Sizing.CornerRadius.Card}"` (lines 171-176).
- The card grid defines three columns `Auto` / `*` / `Auto` (lines 177-182).
- Column 0 holds the framed logo (src/CLIHub/Views/SettingsAgentsPage.xaml:184-195): a `Border`
  with `Width="32"` / `Height="32"` (185-186), `VerticalAlignment="Top"` (187),
  `BorderBrush="{DynamicResource ControlStrokeBrush}"` (188), `BorderThickness="1"` (189),
  `CornerRadius="6"` (190), and `Border.Background` set to an `ImageBrush` with
  `ImageSource="{Binding LogoPath, Converter={StaticResource PathToImage}}"` and
  `Stretch="Uniform"` (191-194). The `ImageBrush` fills the border's rectangle, so the logo is
  letterboxed into 32x32 and clipped by the 6 px rounded corners, with a 1 px stroke ring on top.
- Column 1 is the text `StackPanel` with `Margin="12,0,12,0"` (line 197) — the 12 px gutter between
  the logo and the text comes from this margin, not from the logo block, so removing the frame does
  not shift anything. Column 1 stacks the name (199-203), the state chip (204-208), the advisory
  update chip (209-216), the "Supported commands" hint (218-223), and the message line (225-228).
- Column 2 is the action row (Detect / Update / Edit Config, lines 231-271).

### The plain-image precedent

- The launch window's agent list renders the logo as a plain `Image`
  (src/CLIHub/Views/LaunchWindow.xaml:299-304): `Width`/`Height` from
  `Sizing.AgentLogo.Width`/`Height` (28, src/CLIHub/Themes/Sizing.xaml:71-72),
  `VerticalAlignment="Center"` (301), `RenderOptions.BitmapScalingMode="HighQuality"` (302),
  `Source="{Binding LogoPath, Converter={StaticResource PathToImage}}"` (303),
  `Stretch="Uniform"` (304).
- The legacy `MainWindow` also uses plain `Image` + `PathToImage` for logos
  (src/CLIHub/Views/MainWindow.xaml:57,114).
- The `PathToImage` converter is application-scoped (commented at src/CLIHub/App.xaml:33, registered
  into application resources at src/CLIHub/Startup/ApplicationHost.cs:138) and already feeds plain
  `Image.Source` bindings, so the identical binding works unchanged on an `Image`. The default-logo
  fallback is resolved upstream in the logo pipeline (`LogoImageSourceCache` /
  `PathToImageConverter`, src/CLIHub/Converters/PathToImageConverter.cs:13-26), so fallback behavior
  is identical for `Image.Source` and `ImageBrush.ImageSource`.

### What the "icon block treatment" is

- The shared `SettingCard` template's icon block is a 28x28 `Border` named `IconBox`
  (src/CLIHub/Themes/SettingsStyles.xaml:220-236) with `Background="{DynamicResource ThumbBgBrush}"`
  (226), `BorderBrush="{DynamicResource ControlStrokeBrush}"` (227), `BorderThickness="1"` (228),
  `CornerRadius="6"` (229), hosting a monochrome glyph `TextBlock` (230-235).
- The per-agent card's `Border` at SettingsAgentsPage.xaml:184-195 mirrors this treatment at 32x32
  with an `ImageBrush` instead of a glyph. After this change the per-agent card no longer follows
  that part of the shared language — which is exactly what the spec delta records.

### Spec state

- `openspec/specs/settings-theme/spec.md:181-208` — requirement "Cards on the data pages". Its body
  (line 183) ends: "The per-agent cards SHALL follow the shared card visual language (a rounded
  bordered surface, an icon block, a title, and a hint)." Its scenario "Agent cards share the visual
  language" (lines 205-208) claims: "it uses the same rounded bordered surface, title typography,
  and icon block treatment as the other setting cards". Both claims become false once the frame is
  removed.
- `openspec/specs/agent-ui/spec.md:221` — requirement "Agent card displayed content" requires the
  card to present "display name, logo, a state chip …, and the declared command set"; its scenario
  "Identity shown" (lines 223-226) requires the card to show "the agent's display name and logo
  (falling back to the default logo)". A plain `Image` still presents the logo with the same
  fallback, so `agent-ui` stays satisfied without a delta.

### Test state

- No test under `tests/` references `SettingsAgentsPage` or walks a WPF visual tree; the logo-related
  tests are ViewModel/pipeline-level (`AgentItemTests`, `AgentListSynchronizerTests`,
  `LogoImageSourceCacheTests`, resolver tests). The change touches XAML markup only — no binding,
  converter, or code-behind signature changes.

### Baseline gates (captured 2026-10-26 at `4550b99`)

- `dotnet build CLIHub.sln` → Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test CLIHub.sln` → 1176 passed, 0 failed (478 CLIHub.Tests + 60 CLIHub.Core.Windows.Tests
  + 638 CLIHub.Core.Tests).
- `openspec validate --all` → "Totals: 29 passed, 0 failed (29 items)".

## Decisions

### D1: Replacement markup (per the brief; recorded with its exact attributes)

The framed `Border` (SettingsAgentsPage.xaml:184-195) becomes:

```xml
<Image Grid.Column="0"
       Width="32"
       Height="32"
       VerticalAlignment="Top"
       RenderOptions.BitmapScalingMode="HighQuality"
       Source="{Binding LogoPath, Converter={StaticResource PathToImage}}"
       Stretch="Uniform"/>
```

- 32x32 keeps the card's existing footprint and column widths (`Auto` column 0 sizes to 32 today);
  switching to the launch window's 28 (Sizing.xaml:71-72) would shrink the leading column and shift
  the text — excluded by the "keep layout unchanged" decision.
- `VerticalAlignment="Top"` preserves the current top alignment (the framed `Border` sets it at
  SettingsAgentsPage.xaml:187) so the logo stays level with the name; the card can grow taller than
  the logo when the message line wraps.
- `Stretch="Uniform"` + `RenderOptions.BitmapScalingMode="HighQuality"` match the launch window's
  presentation (LaunchWindow.xaml:302,304) and replace the `ImageBrush`'s `Stretch="Uniform"`
  letterboxing with identical behavior.
- Attribute order follows the file's existing convention (attached `Grid.*` first, then layout
  attributes), matching both the current `Border` (184-190) and the launch window's `Image`
  (299-304).
- Deliberate deviation from the launch window's exact markup: size 32 vs 28 and `Top` vs `Center`
  alignment — both recorded above; "matches the launch window" means the plain unframed
  presentation (no stroke, no radius, no plate, uniform high-quality scaling), not a byte-copy of
  its element.

### D2: Spec delta in `settings-theme` only; not `skip_specs`

- The change is user-visible presentation, and the current `settings-theme` text (body at spec line
  183, scenario at lines 205-208) explicitly claims the framed icon block for per-agent cards.
  `skip_specs: true` would leave the durable contract contradicting the code, so the change carries
  a MODIFIED "Cards on the data pages" requirement (precedent: `2026-10-24-consolidate-version-
  population` decision D5 — "spec delta, not skip_specs"; `skip_specs` is reserved for pure internal
  refactors per `2026-10-24-consolidate-clone-siblings` D1).
- The MODIFIED requirement keeps all five original scenario names verbatim; four scenario bodies are
  unchanged, "Agent cards share the visual language" is rewritten to keep the shared surface and
  title typography claims and drop the icon block claim, and one new scenario ("Agent card logo
  rendered unframed") states the presentation precisely (32 by 32 DIP, uniform stretch,
  high-quality scaling, top of the leading column, no stroke, no rounded-corner clipping, no plate).
- No `agent-ui` delta: its requirement (spec line 221) and "Identity shown" scenario claim the logo
  is presented with a default-logo fallback — still true for a plain `Image`.

### D3: No automated test

Pure XAML presentation swap with no behavioral code path; the repo has no template/visual-tree test
harness (same reasoning as archived `2026-10-26-summary-card-detail-width`, "Testing decision").
The durable guarantee is the new spec scenario; the visual check is a manual smoke task in tasks.md
stage 3.

### D4: Literal `32`, no new sizing resource

The replacement keeps literal `Width="32"` / `Height="32"` exactly as the current `Border` does
(SettingsAgentsPage.xaml:185-186). Introducing a shared sizing key (or reusing
`Sizing.AgentLogo.*`, which is 28) would either change the footprint or add theme surface for one
call site — both excluded by the minimal-diff intent.

## Alternatives considered

1. **Keep the frame, soften it (thinner stroke, larger radius, no plate).** Rejected: the user
   asked for the frame's removal, and any remaining chrome re-introduces the inconsistency with the
   launch window.
2. **`skip_specs: true` and leave `settings-theme` claiming the icon block.** Rejected: the spec
   would directly contradict the implementation (see D2).
3. **Reuse `Sizing.AgentLogo.Width/Height` (28x28) for exact parity with the launch window.**
   Rejected: shrinks the card's leading column and shifts the text block — conflicts with the
   decided "keep layout unchanged".
4. **Keep an element wrapper (borderless `Border` or `Grid`) around the `Image`.** Rejected: a
   single-child wrapper with no styling adds markup without effect; the launch window precedent is
   a bare `Image`.
5. **Also delta `agent-ui`.** Rejected: `agent-ui` requires the logo to be presented, which remains
   true; duplicating presentation detail in two capabilities would create drift (see D2).

## Deviations

- From the launch window's literal markup: size (32 vs 28) and vertical alignment (`Top` vs
  `Center`), both deliberate under D1.
- The spec delta edits the requirement body (line 183) in addition to the scenario. The
  `2026-10-26-summary-card-detail-width` precedent avoided body edits because that body was already
  over the validator's length warning threshold; this body is short enough, and leaving "an icon
  block" in the body while removing the frame in the scenario would contradict the delta itself.

## Open questions

1. **Follow-up (non-blocking): should the 32 DIP settings-card logo size eventually become a named
   sizing resource** (e.g., `Sizing.AgentCardLogo.Width/Height` in Themes/Sizing.xaml) alongside the
   launch window's 28 DIP `Sizing.AgentLogo.*`? This change hardcodes `32` per D4; if the value is
   ever revisited, a sizing key would keep both surfaces tunable without markup edits. Parked for a
   future tidy change; not needed here.
