# Design: Update Chip Replaces the Version Chip

## Current State (Verified)

### Chip styles — `src/CLIHub/Views/SettingsAgentsPage.xaml:13-47`

```xml
<!-- line 13 -->
<Style x:Key="AgentStateChip" TargetType="Border" BasedOn="{StaticResource OutlineChipBorder}">
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

<!-- line 41 -->
<Style x:Key="AgentUpdateChip" TargetType="Border" BasedOn="{StaticResource VersionChipBorder}">
    <Setter Property="BorderBrush" Value="{DynamicResource AccentBrush}"/>
</Style>
```

`AgentStateChip` is `BasedOn="{StaticResource OutlineChipBorder}"` (changed by the prior
`2026-11-06-agent-version-chip-outline` change) and has exactly one `DataTrigger`, keyed on
`InstallState=NotInstalled`, that paints the danger border. It has no `Visibility` setter today —
the `Border` is always visible via the default `Visibility="Visible"` inherited from `Border`'s
own default.

### Installed-card row — `src/CLIHub/Views/SettingsAgentsPage.xaml:245-271`

```xml
<!-- 245 -->
<StackPanel Orientation="Horizontal">
    <TextBlock VerticalAlignment="Center"
               FontSize="{StaticResource Sizing.FontSize.Body}"
               FontWeight="Medium"
               Foreground="{DynamicResource TextPrimaryBrush}"
               Text="{Binding Name}"/>
    <Border Margin="8,0,0,0"
            Style="{StaticResource AgentStateChip}">
        <TextBlock Style="{StaticResource AgentStateChipText}"
                   Text="{Binding VersionChipText}"/>
    </Border>
    <Border Margin="8,0,0,0"
            AutomationProperties.Name="Plugin origin"
            Style="{StaticResource AgentOriginChip}"
            ToolTip="{Binding OriginBadgeTooltip}">
        <TextBlock Style="{StaticResource AgentOriginChipText}"
                   Text="{Binding OriginBadgeText}"/>
    </Border>
    <Border Margin="8,0,0,0"
            AutomationProperties.Name="Update available"
            Style="{StaticResource AgentUpdateChip}"
            ToolTip="Newer version available"
            Visibility="{Binding HasUpdateAvailable, Converter={StaticResource BoolToVisibility}}">
        <TextBlock Style="{StaticResource AgentUpdateChipText}"
                   Text="{Binding UpdateAvailabilityText}"/>
    </Border>
</StackPanel>
<!-- 271 -->
```

Render order today: name, state chip (always visible), origin chip (always visible), update chip
(visible only when `HasUpdateAvailable`). Inside a horizontal `StackPanel`, each child's
`Margin="8,0,0,0"` creates the gap from whichever element precedes it, so reordering the `Border`
elements is sufficient to reposition the gap — no margin values need to change.

### Disabled-card and invalid-card templates (confirmed unaffected)

- Disabled-card row, `SettingsAgentsPage.xaml:386-405`: renders name, `InvalidAgentStateChip`
  (bound to `VersionChipText`), then `AgentOriginChip`. No `AgentUpdateChip` `Border` exists in this
  template at all.
- Invalid-card row, `SettingsAgentsPage.xaml:447-454`: renders name then `InvalidAgentStateChip`
  bound to the literal text `"Invalid"`. No origin chip, no update chip.

Neither template needs a change: the brief's "update chip replaces version chip" rule only applies
where both chips exist, which is the installed-card row alone.

### `AgentCardItem` (`src/CLIHub/ViewModels/Agents/AgentCardItem.cs`)

- `InstallState` (line 129): `AgentInstallState` enum — `Detecting`, `Installed`, `NotInstalled`.
- `HasUpdateAvailable` (line 219): `this.UpdateAvailability?.Status ==
  AgentUpdateStatus.UpdateAvailable`. True only when availability was checked and is
  `UpdateAvailable`; false while `UpdateAvailability` is null (not yet checked), `Unknown`, or
  `UpToDate`.
- `VersionChipText` (line 155): switches on `InstallState`, independent of `HasUpdateAvailable`.
- No existing relationship between `InstallState` and `HasUpdateAvailable` is enforced by the
  ViewModel beyond what the "Agent card actions" requirement already states (a probe reporting not
  installed clears any previously-shown update indicator, i.e. `UpdateAvailability` is reset so
  `HasUpdateAvailable` becomes false). This change does not add or alter that relationship; it only
  changes how the two already-independent flags are rendered together.

### Existing test coverage (confirmed by search — no visibility/order assertions)

`grep` across `tests/CLIHub.Tests/` for `HasUpdateAvailable`, `UpdateAvailabilityText`, and
`VersionChipText` returns 52 matches, all of them `Assert.Equal`/`Assert.True`/`Assert.False`/
`Assert.Null`/`Assert.Contains` against the `AgentCardItem` properties themselves (in
`AgentCardItemTests.cs` and the `AgentManagementViewModelTests.*.cs` files). None reference XAML,
`Visibility`, `Border`, chip order, or render output. Since this change touches only XAML (chip
order and a `Visibility` `DataTrigger`) and not any ViewModel property, no existing test needs to
change, and no new automated test is added — there is no WPF visual-tree test harness in this
project to assert `Border.Visibility` against, and adding one is out of scope for a chip-placement
change.

## Decisions

### 1. Reorder the `Border` elements in the row; do not introduce a `Grid` or named slot

The update chip's `Border` moves from its current position (after the origin chip) to before the
origin chip, directly swapping places with the state chip's `Border` conceptually — though
literally it is a cut-and-paste of the existing `AgentUpdateChip` block to just after the name's
`TextBlock` and before the `AgentStateChip` `Border`. The `AgentStateChip` `Border` itself does not
move; it is simply collapsed when not needed, and the `StackPanel`'s horizontal flow closes the gap.

**Rationale:** The row is a plain horizontal `StackPanel`; a `Visibility=Collapsed` child takes no
space and contributes no margin gap in a `StackPanel`, so reordering two `Border` elements already
achieves "update chip occupies the version chip's position, before the origin badge" without a
`Grid`, named columns, or a swappable `ContentControl`/`DataTemplateSelector`. This matches the
existing pattern in this file (plain declarative XAML, `DataTrigger`s for conditional styling) and
needs no new control.

### 2. Hide the state chip via a `DataTrigger` on `AgentStateChip`, not a converter or VM property

Add to `AgentStateChip` (`SettingsAgentsPage.xaml:13`):

```xml
<Style x:Key="AgentStateChip" TargetType="Border" BasedOn="{StaticResource OutlineChipBorder}">
    <Style.Triggers>
        <DataTrigger Binding="{Binding InstallState}" Value="NotInstalled">
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="BorderBrush" Value="{DynamicResource DangerBrush}"/>
        </DataTrigger>
        <DataTrigger Binding="{Binding HasUpdateAvailable}" Value="True">
            <Setter Property="Visibility" Value="Collapsed"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

**Rationale:** `App.xaml:47` defines only `BooleanToVisibilityConverter` as `BoolToVisibility`
(`true` -> `Visible`); there is no inverse converter in the resource dictionary today. The brief
asks to prefer a `DataTrigger` over adding a converter, and a `DataTrigger` on the existing style is
strictly smaller: one new `<DataTrigger>` block, zero new resources, zero `ConverterParameter`
plumbing, and it reads declaratively next to the existing `NotInstalled` trigger as "the state chip
collapses exactly when an update is available." Binding `HasUpdateAvailable` directly (rather than
inverting it with a converter) also keeps the trigger's condition legible as the same flag the
`AgentUpdateChip`'s own `Visibility` binding already uses, so the two chips' visibility rules read
as complements of each other at the same binding.

### 3. Trigger order and precedence — reasoned through for all four states

`Style.Triggers` in WPF are not short-circuited by order: every `DataTrigger` whose condition is
true applies its setters, and for a given property, the trigger that appears later in the
`<Style.Triggers>` list wins when more than one trigger targets the same property and both are
active at once (last-one-wins for same-property conflicts; triggers that set different properties
never conflict). The two triggers here never target the same property — the `NotInstalled` trigger
sets `Background`/`BorderBrush`; the new `HasUpdateAvailable` trigger sets only `Visibility` — so
there is no precedence conflict to resolve and the order the two triggers appear in has no visible
effect. The relevant state-by-state behavior comes instead from whether `HasUpdateAvailable` can
ever be true while `InstallState` is not `Installed`:

| State | `InstallState` | `HasUpdateAvailable` | `AgentStateChip` renders? | `AgentUpdateChip` renders? |
|---|---|---|---|---|
| Installed, no update known | `Installed` | `false` | Yes — shows `VersionChipText` (e.g. `v0.26.4`), normal outline styling (`NotInstalled` trigger inactive) | No (`BoolToVisibility` collapses it) |
| Installed, update available | `Installed` | `true` | **No** — new trigger fires, `Visibility=Collapsed`; the `NotInstalled` trigger's `Background`/`BorderBrush` setters are irrelevant here since that trigger's own condition (`InstallState=NotInstalled`) is false | Yes — shows `UpdateAvailabilityText` (e.g. `0.26.4 → 0.26.8`), accent styling, in the version chip's former slot |
| Not installed | `NotInstalled` | `false` (per "Agent card actions" requirement, a not-installed probe result clears any update indicator) | Yes — `NotInstalled` trigger fires (danger `Background`/`BorderBrush`); new trigger's condition (`HasUpdateAvailable=True`) is false, so `Visibility` stays at its default `Visible` | No |
| Detecting | `Detecting` | unchanged from its pre-probe value (a detect pass does not touch `UpdateAvailability` per `AgentManagementViewModelTests.Detection.cs`) | Yes — shows `Detecting…`; `NotInstalled` trigger inactive; the `HasUpdateAvailable` trigger could in principle still be true here only if a stale `true` value survives from before the probe started, in which case the state chip would stay collapsed and the update chip would keep showing its last value until the probe settles — consistent with "Header Detect leaves update indicators untouched" (`agent-ui` spec, "Agent management header actions") | Only if `HasUpdateAvailable` was already true before detecting started |

No case requires both triggers to be active with conflicting setters on the same property, so the
order of the two `DataTrigger` elements in the style is not load-bearing; they are listed with the
pre-existing `NotInstalled` trigger first and the new one second simply to keep the diff additive
(append, don't reorder existing XML).

### 4. The update chip's own `Visibility` binding and styling are unchanged

`AgentUpdateChip`'s `Visibility="{Binding HasUpdateAvailable, Converter={StaticResource
BoolToVisibility}}"`, its `ToolTip="Newer version available"`, and its
`AutomationProperties.Name="Update available"` are untouched — only its position in the `StackPanel`
moves. `AgentUpdateChipText`'s styling (`AccentBrush` foreground) is untouched.

## Alternatives Considered and Rejected

### A. Add an inverse `InverseBooleanToVisibilityConverter` and bind it on `AgentStateChip.Visibility`

**Rejected:** Requires a new converter class, a new `ConverterParameter`-free resource registration
in `App.xaml`, and a binding on the `Border` itself — three moving parts versus one `DataTrigger`
appended to an existing style. The brief explicitly asks to confirm the `DataTrigger` is the
smallest correct change before using it; this alternative is strictly larger for the same visible
result and is rejected on that basis.

### B. Swap a shared `ContentControl`/`DataTemplateSelector` between "version" and "update" content in one slot

**Rejected:** This would introduce a new data-template-selection abstraction for what is otherwise a
two-trigger, two-`Border` change. It also risks losing the update chip's distinct automation name
and tooltip if the two chips' templates were merged into one control, which the brief says must stay
unchanged. Rejected as an unnecessary abstraction for this codebase's established plain-`Style`/
`DataTrigger` pattern (see the prior `2026-11-06-agent-version-chip-outline` change's own rejection
of a custom `Chip` control class for the same reason).

### C. Hide the state chip from the ViewModel with a new `AgentCardItem` property (e.g. `ShowVersionChip`)

**Rejected:** The brief's decided implementation shape is XAML-only ("No ViewModel, converter,
binding... changes"). A new VM property would also need `OnPropertyChanged` wiring on both
`InstallState` and `UpdateAvailability` setters to stay correct, which is strictly more surface area
than one `DataTrigger` that already has direct access to `HasUpdateAvailable` via the existing
binding path. Rejected as it contradicts the approved implementation shape without any
corresponding benefit.

## Deviations

None from the brief's decided implementation shape.

## Open Questions

None blocking implementation. Two non-blocking notes for the user, not for the implementation
session to guess at:

- The "Detecting" row in the precedence table above describes a corner case (a stale
  `HasUpdateAvailable=true` surviving into a `Detecting` state) that is only reachable if a future
  code path sets `InstallState=Detecting` without first clearing `UpdateAvailability` — today's
  `AgentManagementViewModelTests.Detection.cs` coverage does not exercise this exact combination.
  This design does not change that behavior; it is noted so the implementation session does not
  mistake it for something this change introduces.
- Whether the disabled-card and invalid-card templates should eventually gain their own update-chip
  support (so the replacement rule would apply there too) is out of scope here, per Non-Goals; raised
  only as a possible follow-up if the product later adds update checks for disabled/invalid plugins.
