# Design: Outline the Agent Version Chip

## Current State (Verified)

### Shared chip styles — `src/CLIHub/Themes/Common/Controls.xaml:44-55`

```xml
<Style x:Key="VersionChipBorder" TargetType="Border">
    <Setter Property="CornerRadius" Value="{StaticResource Sizing.CornerRadius.Chip}"/>
    <Setter Property="Padding" Value="6,2"/>
    <Setter Property="Background" Value="{DynamicResource ChipBgBrush}"/>
    <Setter Property="BorderBrush" Value="{DynamicResource ChipStrokeBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
</Style>

<Style x:Key="VersionChipText" TargetType="TextBlock">
    <Setter Property="FontSize" Value="{StaticResource Sizing.FontSize.Chip}"/>
    <Setter Property="Foreground" Value="{DynamicResource TextMutedBrush}"/>
</Style>
```

`VersionChipBorder` is the filled chip look: translucent `ChipBgBrush` fill plus a faint
`ChipStrokeBrush` border. It is the base style for every chip in the agents list and for two
consumers outside it.

### Card-local chip styles — `src/CLIHub/Views/SettingsAgentsPage.xaml:10-69`

```xml
<!-- line 13 -->
<Style x:Key="AgentStateChip" TargetType="Border" BasedOn="{StaticResource VersionChipBorder}">
    <Style.Triggers>
        <DataTrigger Binding="{Binding InstallState}" Value="NotInstalled">
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="BorderBrush" Value="{DynamicResource DangerBrush}"/>
        </DataTrigger>
    </Style.Triggers>
</Style>

<!-- line 22 -->
<Style x:Key="AgentStateChipText" TargetType="TextBlock" BasedOn="{StaticResource VersionChipText}">
    <Style.Triggers>
        <DataTrigger Binding="{Binding InstallState}" Value="NotInstalled">
            <Setter Property="Foreground" Value="{DynamicResource DangerBrush}"/>
        </DataTrigger>
    </Style.Triggers>
</Style>

<!-- line 30 -->
<Style x:Key="InvalidAgentStateChip" TargetType="Border" BasedOn="{StaticResource VersionChipBorder}">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderBrush" Value="{DynamicResource DangerBrush}"/>
</Style>

<!-- line 35 -->
<Style x:Key="InvalidAgentStateChipText" TargetType="TextBlock" BasedOn="{StaticResource VersionChipText}">
    <Setter Property="Foreground" Value="{DynamicResource DangerBrush}"/>
</Style>

<!-- line 41 -->
<Style x:Key="AgentUpdateChip" TargetType="Border" BasedOn="{StaticResource VersionChipBorder}">
    <Setter Property="BorderBrush" Value="{DynamicResource AccentBrush}"/>
</Style>

<!-- line 45 -->
<Style x:Key="AgentUpdateChipText" TargetType="TextBlock" BasedOn="{StaticResource VersionChipText}">
    <Setter Property="Foreground" Value="{DynamicResource AccentBrush}"/>
</Style>

<!-- line 52 -->
<Style x:Key="AgentOriginChip" TargetType="Border" BasedOn="{StaticResource VersionChipBorder}">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderBrush" Value="{DynamicResource TextMutedBrush}"/>
    <Style.Triggers>
        <DataTrigger Binding="{Binding Origin}" Value="installed">
            <Setter Property="BorderBrush" Value="{DynamicResource AccentBrush}"/>
        </DataTrigger>
    </Style.Triggers>
</Style>

<!-- line 62 -->
<Style x:Key="AgentOriginChipText" TargetType="TextBlock" BasedOn="{StaticResource VersionChipText}">
    <Setter Property="Foreground" Value="{DynamicResource TextMutedBrush}"/>
    <Style.Triggers>
        <DataTrigger Binding="{Binding Origin}" Value="installed">
            <Setter Property="Foreground" Value="{DynamicResource AccentBrush}"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

Current per-chip visual state, by style:

| Style | Background | Border | Outlined today? |
|---|---|---|---|
| `AgentStateChip` (normal/installed case — no trigger fires) | `ChipBgBrush` (filled) | `ChipStrokeBrush` | **No — this is the chip being changed** |
| `AgentStateChip` (`NotInstalled` trigger) | `Transparent` | `DangerBrush` | Yes |
| `InvalidAgentStateChip` | `Transparent` | `DangerBrush` | Yes (out of scope — already correct, untouched) |
| `AgentUpdateChip` | `ChipBgBrush` (filled — not overridden) | `AccentBrush` | No, but out of scope per brief: only `AgentStateChip` and `AgentOriginChip` are named for this change |
| `AgentOriginChip` (default) | `Transparent` | `TextMutedBrush` | Yes |
| `AgentOriginChip` (`Origin=installed` trigger) | `Transparent` | `AccentBrush` | Yes |

Note: `AgentUpdateChip` is also still filled today (it only overrides `BorderBrush`, not
`Background`), but the brief scopes this change to `AgentStateChip` and `AgentOriginChip` only. This
is called out as a discrepancy, not silently fixed — see "Deviations" below.

### Usage sites in the card template

- Installed-card row: `src/CLIHub/Views/SettingsAgentsPage.xaml:253-272` — `AgentStateChip` (253),
  `AgentOriginChip` (260), `AgentUpdateChip` (267), in that order, each with `Margin="8,0,0,0"`.
- Disabled-card row: `src/CLIHub/Views/SettingsAgentsPage.xaml:395-406` — `InvalidAgentStateChip`
  (396), `AgentOriginChip` (402).
- Standalone invalid chip: `src/CLIHub/Views/SettingsAgentsPage.xaml:453-454` —
  `InvalidAgentStateChip`, unaffected by this change.

### Other `VersionChipBorder` consumers (must keep the filled look — confirmed by grep)

- `src/CLIHub/Themes/Common/SettingsStyles.xaml:245` — `SettingCard` badge.
- `src/CLIHub/Views/SettingsWindow.xaml:556` — footer version chip ("1.1.1").

Neither is touched by this change; both keep using `VersionChipBorder` directly, which is untouched.

### Palette values behind the brushes involved (confirmed by reading theme files)

| Brush | Dark (`Themes/Dark.xaml:25,26,39`) | Light (`Themes/Light.xaml:25,26,39`) | Blue (`Themes/Blue.xaml:25,26,39`) |
|---|---|---|---|
| `ChipBgBrush` | `#0DFFFFFF` | `#0D000000` | `#0DFFFFFF` |
| `ChipStrokeBrush` | `#14FFFFFF` | `#16000000` | `#14FFFFFF` |
| `TextMutedBrush` | `#FF9A9A9A` | `#FF5F5F66` | `#FF9BA4B5` |

All three palettes define `TextMutedBrush`, so `OutlineChipBorder`'s `DynamicResource
TextMutedBrush` resolves under every shipped theme, including Blue (not explicitly named in the
brief's "Dark/Light" callout but verified here for completeness).

### Launch window (confirmed out of scope)

`src/CLIHub/Views/LaunchWindow.xaml:347-351` renders the agent version as a plain `TextBlock` with
`Style="{StaticResource HintText}"` (muted text, no `Border`, no chip). It does not use
`VersionChipBorder` or any chip style, so this change does not touch it.

### Visual reference

`ui-mockup-agent-version-chip.html` (repo root, untracked) renders Current vs. Proposed side by side
with a Dark/Light toggle. It is the agreed visual target; this change does not modify it.

## Decisions

### 1. New shared style `OutlineChipBorder`, not a change to `VersionChipBorder`

```xml
<Style x:Key="OutlineChipBorder" TargetType="Border" BasedOn="{StaticResource VersionChipBorder}">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderBrush" Value="{DynamicResource TextMutedBrush}"/>
</Style>
```

Placed in `src/CLIHub/Themes/Common/Controls.xaml` immediately after `VersionChipText` (after line
55), so the three related chip-border styles stay adjacent. It inherits `CornerRadius`, `Padding`,
and `BorderThickness` from `VersionChipBorder`, overriding only `Background` and `BorderBrush` —
exactly the outline treatment already used ad hoc by `AgentOriginChip` and `InvalidAgentStateChip`.

**Rationale:** `VersionChipBorder` is consumed by two call sites outside the agents page (footer
chip, `SettingCard` badge) that must keep the filled look. Editing it in place would repaint those
unrelated chips. A new keyed style, based on the existing one, keeps the inheritance chain (padding,
radius, thickness) in one place while letting each consumer opt into filled or outlined.

### 2. `AgentStateChip` and `AgentOriginChip` both rebase onto `OutlineChipBorder`

- `AgentStateChip` (`SettingsAgentsPage.xaml:13`): `BasedOn` changes from `VersionChipBorder` to
  `OutlineChipBorder`. The existing `NotInstalled` trigger already sets `Background="Transparent"`
  and `BorderBrush="{DynamicResource DangerBrush}"`, which still applies on top of (and overrides)
  the new base — no trigger changes needed.
- `AgentOriginChip` (`SettingsAgentsPage.xaml:52`): `BasedOn` changes from `VersionChipBorder` to
  `OutlineChipBorder`; the style's own `Background="Transparent"` and
  `BorderBrush="{DynamicResource TextMutedBrush}"` setters are deleted because `OutlineChipBorder`
  now supplies the same values. The `Origin=installed` trigger (`BorderBrush={DynamicResource
  AccentBrush}`) is untouched and keeps overriding the base at runtime.

**Rationale:** Rebasing `AgentOriginChip` too (rather than leaving it pointed at `VersionChipBorder`
with its own transparent/muted setters) means both chips now visibly share one definition of
"outlined chip," which is the point of the change — and removes duplicate setters that would
otherwise need to stay in sync by convention alone.

### 3. `InvalidAgentStateChip` and `AgentUpdateChip` left untouched

`InvalidAgentStateChip` already fully overrides `Background` and `BorderBrush` for the danger look;
rebasing it onto `OutlineChipBorder` would be a no-op for its rendered appearance (its own setters
would just re-override `OutlineChipBorder`'s `TextMutedBrush` border with `DangerBrush` the same way
they currently override `VersionChipBorder`'s `ChipStrokeBrush`). The brief does not ask for this
rebase, and because the visual result is identical either way, leaving it on `VersionChipBorder`
avoids an unrequested edit with zero visual benefit.

`AgentUpdateChip` is still filled (`ChipBgBrush` background, only `BorderBrush` overridden to
`AccentBrush`). This is a real inconsistency with the row's otherwise-outlined chips, but it is not
in the brief's enumerated scope (`AgentStateChip`, `AgentOriginChip` only). Left as-is; see "Open
Questions" / backlog note below.

## Alternatives Considered and Rejected

### A. Edit `VersionChipBorder` in place to be transparent/outlined

**Rejected:** `VersionChipBorder` is also the base style for the footer "1.1.1" chip
(`SettingsWindow.xaml:556`) and the `SettingCard` badge (`SettingsStyles.xaml:245`). Changing its
`Background`/`BorderBrush` setters directly would repaint both of those to the outlined look as an
unintended side effect, which is out of scope and not requested.

### B. Set `Background="Transparent"` / `BorderBrush="{DynamicResource TextMutedBrush}"` as inline
setters directly on `AgentStateChip`, without a new shared style

**Rejected:** This duplicates the exact two setters already present (today) on `AgentOriginChip`.
Keeping two independent copies of "the outline chip look" invites drift: a future change to the
outline treatment (e.g. a different muted border opacity) would require remembering to update both
styles. A single shared `OutlineChipBorder` style is the one-definition-of-truth fix and matches how
`VersionChipBorder`/`VersionChipText` already work as the shared base for the filled look.

### C. Introduce a new `Chip` custom control class with a `Variant` dependency property

**Rejected:** Overkill for a two-property visual delta, and it deviates from this codebase's
established pattern of plain keyed `Style` resources with `BasedOn` chains and `DataTrigger`s for
chip variants (see `AgentUpdateChip`, `InvalidAgentStateChip`, `AgentOriginChip` — all plain
`Style`/`BasedOn`/trigger, no custom control classes). `AGENTS.md` directs using existing MVVM,
coordinator, controller, service, and interface patterns before adding abstractions; a new control
class is an abstraction this change does not need.

## Deviations

None from the brief's decided implementation shape. One discrepancy is recorded for transparency,
not acted on:

- **`AgentUpdateChip` remains filled.** The brief's "verified current state" section does not list
  it among the chips to change, and its explicit task list only touches `AgentStateChip` and
  `AgentOriginChip`. Fixing it would be a scope expansion beyond what was approved. Recorded here and
  left as a candidate for a follow-up change if the user wants full consistency across all four chip
  styles.

## Open Questions

None blocking implementation. One non-blocking note for the user to decide, not for the
implementation session to guess at: whether `AgentUpdateChip`'s remaining filled background should
be a follow-up change (see Deviations above).
