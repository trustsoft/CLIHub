# Design: Remove Agent Origin Badge

## Current State (Verified)

### ViewModel — `AgentCardItem` (`src/CLIHub/ViewModels/Agents/AgentCardItem.cs:305-349`)

```csharp
// line 305
public string? Origin
{
    get;
    set => this.SetProperty(ref field, value);
}

// line 324
public string OriginBadgeText => this.Origin switch
{
    PluginOrigin.Builtin => "Built-in",
    PluginOrigin.Installed => "Installed",
    _ => "Unknown"
};

// line 335
public string OriginBadgeTooltip
{
    get
    {
        var lines = new List<string?>
        {
            this.Plugin.Version is { } version ? $"Version: {version}" : null,
            this.Plugin.Publisher is { } publisher ? $"Publisher: {publisher}" : null,
            this.Plugin.Homepage is { } homepage ? $"Homepage: {homepage}" : null
        };

        var tooltip = string.Join("\n", lines.Where(line => line is not null));
        return tooltip.Length == 0 ? this.OriginBadgeText : tooltip;
    }
}
```

`Origin` is a string matching one of the `PluginOrigin` constants (`"builtin"`, `"installed"`, or absent/null for unknown). `OriginBadgeText` maps it to display text. `OriginBadgeTooltip` builds a multiline string from the descriptor's declared metadata (version, publisher, homepage) and falls back to `OriginBadgeText` when none are declared.

### ViewModel — `DisabledAgentCardItem` (`src/CLIHub/ViewModels/Agents/DisabledAgentCardItem.cs:42-59`)

```csharp
// line 42
public string? Origin { get; }

// line 45
public string? Version { get; }

// line 51
public string OriginBadgeText => this.Origin switch
{
    PluginOrigin.Builtin => "Built-in",
    PluginOrigin.Installed => "Installed",
    _ => "Unknown"
};

// line 59
public string VersionChipText => this.Version is { } version ? $"v{version}" : "Unknown version";
```

Disabled cards have `Origin` and `Version` but no `Publisher` or `Homepage` (the plugin descriptor is not loaded for disabled entries).

### XAML — active card template (`src/CLIHub/Views/SettingsAgentsPage.xaml:248-273`)

```xml
<!-- line 248 -->
<StackPanel Orientation="Horizontal">
    <TextBlock VerticalAlignment="Center"
               FontSize="{StaticResource Sizing.FontSize.Body}"
               FontWeight="Medium"
               Foreground="{DynamicResource TextPrimaryBrush}"
               Text="{Binding Name}"/>
    <Border Margin="8,0,0,0"
            AutomationProperties.Name="Update available"
            Style="{StaticResource AgentUpdateChip}"
            ToolTip="Newer version available"
            Visibility="{Binding HasUpdateAvailable, Converter={StaticResource BoolToVisibility}}">
        <TextBlock Style="{StaticResource AgentUpdateChipText}"
                   Text="{Binding UpdateAvailabilityText}"/>
    </Border>
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
</StackPanel>
```

The origin badge is the third `Border` after the name (lines 267-273): renders `OriginBadgeText` and uses `OriginBadgeTooltip` as the tooltip.

### XAML — disabled card template (`src/CLIHub/Views/SettingsAgentsPage.xaml:390-407`)

```xml
<!-- line 390 -->
<StackPanel Orientation="Horizontal">
    <TextBlock VerticalAlignment="Center"
               FontSize="{StaticResource Sizing.FontSize.Body}"
               FontWeight="Medium"
               Foreground="{DynamicResource TextPrimaryBrush}"
               Text="{Binding Name}"/>
    <Border Margin="8,0,0,0"
            Style="{StaticResource InvalidAgentStateChip}">
        <TextBlock Style="{StaticResource InvalidAgentStateChipText}"
                   Text="{Binding VersionChipText}"/>
    </Border>
    <Border Margin="8,0,0,0"
            AutomationProperties.Name="Plugin origin"
            Style="{StaticResource AgentOriginChip}"
            ToolTip="{Binding OriginBadgeText}">
        <TextBlock Style="{StaticResource AgentOriginChipText}"
                   Text="{Binding OriginBadgeText}"/>
    </Border>
</StackPanel>
```

The disabled-card origin badge (lines 401-407) uses `OriginBadgeText` for both display and tooltip (no descriptor metadata available).

### XAML — origin styles (`src/CLIHub/Views/SettingsAgentsPage.xaml:55-70`)

```xml
<!-- line 55 -->
<Style x:Key="AgentOriginChip" TargetType="Border" BasedOn="{StaticResource OutlineChipBorder}">
    <Style.Triggers>
        <DataTrigger Binding="{Binding Origin}" Value="installed">
            <Setter Property="BorderBrush" Value="{DynamicResource AccentBrush}"/>
        </DataTrigger>
    </Style.Triggers>
</Style>

<!-- line 63 -->
<Style x:Key="AgentOriginChipText" TargetType="TextBlock" BasedOn="{StaticResource VersionChipText}">
    <Setter Property="Foreground" Value="{DynamicResource TextMutedBrush}"/>
    <Style.Triggers>
        <DataTrigger Binding="{Binding Origin}" Value="installed">
            <Setter Property="Foreground" Value="{DynamicResource AccentBrush}"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

Both styles are referenced only by the two origin `Border` blocks above (grep confirmed no other file references `AgentOriginChip`).

### XAML — invalid card template (`src/CLIHub/Views/SettingsAgentsPage.xaml:447-456`)

The invalid-card template renders name and an `InvalidAgentStateChip` only; it has no origin badge, no version chip, and no update chip. This change does not touch it.

### Tests (`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Plugins.cs`)

Four methods assert on `OriginBadgeText` or `OriginBadgeTooltip`:

1. **`Load_WithDisabledPlugins_PopulatesDisabledCardsAndSectionFlag` (line 188)**: asserts `installed.OriginBadgeText == "Installed"` and `unknown.OriginBadgeText == "Unknown"` for disabled cards.
2. **`Load_WithOriginsFromState_PopulatesOriginBadgesAndCatalogSummary` (line 224)**: asserts `OriginBadgeText` values for three active cards and confirms the catalog summary counts origin-based groups.
3. **`Load_OriginBadgeTooltip_ListsDescriptorMetadataWhenDeclared` (line 248)**: asserts the tooltip contains `"Version: 1.2.0"`, `"Publisher: Community"`, and `"Homepage: https://example.com/a"`.
4. **`Load_OriginBadgeTooltip_FallsBackToBadgeTextWithoutMetadata` (line 272)**: asserts `card.OriginBadgeTooltip == card.OriginBadgeText` when no metadata is declared.

## Decisions

### 1. Tooltip line format: "Origin: {text}" first, then labelled metadata lines

New tooltip structure for `AgentCardItem`:

```csharp
public string IdentityTooltip
{
    get
    {
        var lines = new List<string>
        {
            $"Origin: {this.Origin switch
            {
                PluginOrigin.Builtin => "Built-in",
                PluginOrigin.Installed => "Installed",
                _ => "Unknown"
            }}"
        };

        if (this.Plugin.Version is { } version)
        {
            lines.Add($"Version: {version}");
        }

        if (this.Plugin.Publisher is { } publisher)
        {
            lines.Add($"Publisher: {publisher}");
        }

        if (this.Plugin.Homepage is { } homepage)
        {
            lines.Add($"Homepage: {homepage}");
        }

        return string.Join("\n", lines);
    }
}
```

For `DisabledAgentCardItem`:

```csharp
public string IdentityTooltip
{
    get
    {
        var lines = new List<string>
        {
            $"Origin: {this.Origin switch
            {
                PluginOrigin.Builtin => "Built-in",
                PluginOrigin.Installed => "Installed",
                _ => "Unknown"
            }}"
        };

        if (this.Version is { } version)
        {
            lines.Add($"Version: {version}");
        }

        return string.Join("\n", lines);
    }
}
```

**Rationale:** The origin label is always present as the first line, so the tooltip never collapses to empty. The descriptor metadata follows in the same "Label: value" format used today, preserving the established pattern. This matches the brief's requirement that "the agent NAME's tooltip takes over the information: recorded origin plus the descriptor's version, publisher, and homepage when declared."

**Alternatives considered:**
- Omit "Origin:" and show only "Built-in"/"Installed"/"Unknown" as the first line: rejected, as it would be ambiguous whether the first line is a label or a category when viewed out of context (e.g., screen-reader announcement of tooltip text).
- Place origin last after metadata: rejected, as origin is the primary classification (user-controlled: installed from a package vs. shipped) and should appear first.

### 2. Delete both origin styles after removing the `Border` blocks

Both `AgentOriginChip` and `AgentOriginChipText` are referenced only in the two templates that render the origin badge. After the badge `Border` blocks are deleted, both styles become unused. The build will not warn on unused XAML styles, but leaving them would violate the project's established pattern of deleting dead code (confirmed by the absence of commented-out styles in `SettingsAgentsPage.xaml` today).

**Rationale:** grep confirmed no other file references these styles; the spec delta mentions their removal; the brief explicitly lists deleting the styles as part of the XAML change.

### 3. Bind tooltip on the name `TextBlock`, not a wrapping `Border`

The name `TextBlock` already exists at the start of each card's horizontal `StackPanel`. Adding `ToolTip="{Binding IdentityTooltip}"` directly to that element is the minimal change. Wrapping the name in a new `Border` only to carry the tooltip would add an unnecessary element.

**Rationale:** WPF `ToolTip` can be set on any `FrameworkElement`; the `TextBlock` is sufficient. This matches the existing pattern where other chips' tooltips are bound on their own `Border` elements rather than requiring wrappers.

### 4. Test method renames

Current names:
- `Load_OriginBadgeTooltip_ListsDescriptorMetadataWhenDeclared`
- `Load_OriginBadgeTooltip_FallsBackToBadgeTextWithoutMetadata`

Proposed new names:
- `Load_IdentityTooltip_ListsOriginAndDescriptorMetadataWhenDeclared`
- `Load_IdentityTooltip_ShowsOriginLabelWhenNoMetadataDeclared`

The first two methods (`Load_WithDisabledPlugins_PopulatesDisabledCardsAndSectionFlag`, `Load_WithOriginsFromState_PopulatesOriginBadgesAndCatalogSummary`) mention "badge" in a way that is no longer load-bearing after the change (they assert the badge text, which becomes tooltip text). The second method's name references "BadgesAndCatalogSummary" — the catalog summary still uses `Origin`, so the "badges" part of the name is stale but the test's purpose (populating origin data + summary counts) remains accurate. Proposed rename:
- `Load_WithOriginsFromState_PopulatesOriginDataAndCatalogSummary`

The first method name (`Load_WithDisabledPlugins_PopulatesDisabledCardsAndSectionFlag`) does not specifically mention the badge, so it can stay as-is or be left to the implementation session's judgment.

**Rationale:** Test method names should match what they assert. The new property name is `IdentityTooltip`, not `OriginBadgeTooltip`, so the name should reflect that. The "fallback" case no longer falls back to badge text (it always includes the origin label); instead it shows only the origin label when no descriptor metadata is declared.

## Alternatives Considered and Rejected

### A. Keep the origin badge and add the tooltip to the badge itself

**Rejected:** The brief explicitly states the user chose "remove the badge entirely" after being presented with options. This change implements that decision; keeping the badge would contradict the approved product direction.

### B. Use a custom `ToolTipService.ToolTip` with a styled `ContentPresenter` instead of a plain string

**Rejected:** The current tooltip is a plain multiline string (newline-delimited), and WPF renders it as a simple tooltip. Introducing a custom `DataTemplate` or `ContentPresenter` would add complexity for no visual benefit, and the brief's decided implementation shape is "property and XAML edits only" — no new control classes.

### C. Combine origin, version, publisher, and homepage into a single human-readable sentence

**Rejected:** The existing `OriginBadgeTooltip` uses labelled lines ("Version: ...", "Publisher: ...", "Homepage: ..."), which is the established pattern in this codebase. The brief asks to preserve "the existing label format used today."

## Cross-Spec Note

The `plugin-installation` spec contains a `Plugin origin display` requirement (~line 192) with four badge-specific scenarios (`Built-in badge displayed`, `Installed badge displayed`, `Unknown badge displayed`, `Origin tooltip shows metadata`). Removing the badge makes that requirement false. The replacement behavior (origin in the agent name's tooltip, together with descriptor metadata) is now stated in `agent-ui`'s `Agent card displayed content` requirement.

**Decision:** Remove the `Plugin origin display` requirement rather than modify it. Keeping the requirement name while rewriting its scenarios to describe a tooltip instead of a badge would leave misleading scenario names (`Built-in badge displayed` where no badge exists) and would duplicate the contract now owned by `agent-ui`. The `plugin-installation` delta uses `## REMOVED Requirements` with a migration note pointing to `agent-ui`.

## Deviations

None from the brief's decided implementation shape.

## Open Questions

None blocking implementation. One non-blocking note for the user:

- Whether the origin badge's accent styling for installed plugins (the `Value="installed"` trigger that sets `AccentBrush` foreground) should be reflected in the new tooltip (e.g., by bolding or prefixing the origin line when installed): this design does not introduce such styling, as the tooltip is plain text and adding `<Run>`/`<Bold>` markup would require a `ContentPresenter` (see Alternative B, rejected). Raised only in case the user later wants to explore richer tooltip formatting as a follow-up.
