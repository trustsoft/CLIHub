# Design: remove-prerelease-updates-option

## Context

See proposal.md — Why. Only stable releases are published; no beta channel exists. The UI shows a disabled "Notify about pre-releases" toggle and a hardcoded "Channel: Stable" chip with a hint "Not available yet. Stable releases only." A stored `notifyPrereleaseUpdates: true` preference would enable pre-release checks with no remaining UI to disable it.

## Current State

### File: src/CLIHub/Views/SettingsUpdatesPage.xaml, Lines: 35-46

```xaml
<!-- Pre-release builds are not published yet (stable-only releases), so the option
     cannot change the update check or its notification; re-enable with a beta channel. -->
<controls:SettingCard Title="Notify about pre-releases"
                   Hint="Not available yet. Stable releases only."
                   Icon="{x:Static themes:IconGlyphs.Info}"
                   Grouped="True"
                   Opacity="0.6">
    <CheckBox HorizontalAlignment="Right"
              IsChecked="{Binding NotifyPrereleaseUpdates, Mode=TwoWay}"
              IsEnabled="False"
              Style="{StaticResource ToggleCheckBox}"/>
</controls:SettingCard>
```

The card is displayed but disabled.

### File: src/CLIHub/Views/SettingsWindow.xaml, Lines: 91-114

```xaml
<!-- Page-specific metadata shown next to the Updates title. -->
<DataTemplate x:Key="UpdatesHeaderTitleExtra">
    <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
        <Border Style="{StaticResource HeaderChip}">
            <StackPanel Orientation="Horizontal">
                <TextBlock Style="{StaticResource HeaderChipLabel}" Text="Version: "/>
                <TextBlock Style="{StaticResource HeaderChipValue}"
                           Text="{Binding Version, StringFormat=v{0}}"/>
            </StackPanel>
        </Border>
        <Border Margin="8,0,0,0" Style="{StaticResource HeaderChip}">
            <StackPanel Orientation="Horizontal">
                <TextBlock Style="{StaticResource HeaderChipLabel}" Text="Channel: "/>
                <TextBlock Style="{StaticResource HeaderChipValue}" Text="Stable"/>
            </StackPanel>
        </Border>
        <Button VerticalAlignment="Center"
                Margin="8,0,0,0"
                Command="{Binding ShowReleaseNotesCommand}"
                Content="Release notes →"
                Style="{StaticResource LinkButton}"
                ToolTip="Open What's New"/>
    </StackPanel>
</DataTemplate>
```

"Channel: Stable" is hardcoded; no channel model exists.

### File: src/CLIHub.Core/Models/Configuration/AppConfig.cs, Lines: 93-97

```csharp
/// <summary>
///   When true (default is false), update checks include pre-release
///   versions and the user is notified when a pre-release update is available.
/// </summary>
public bool NotifyPrereleaseUpdates { get; set; }
```

### File: src/CLIHub.Core/Updates/UpdateService.cs, Lines: 26, 30, 42, 79-107

```csharp
private readonly IPreferencesStore? preferences;
// ...
private bool cachedManagerPrerelease;
// ...
public UpdateService(ILogger<UpdateService> logger, IPreferencesStore preferencesStore)
{
    this.logger = logger;
    this.preferences = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
    // ...
}
// ...
private UpdateManager? GetDefaultManager()
{
    var prerelease = this.preferences?.Load().NotifyPrereleaseUpdates ?? false;

    lock (this.managerGate)
    {
        if (this.managerCached && this.cachedManagerPrerelease == prerelease)
        {
            return this.cachedManager;
        }

        this.cachedManager = this.CreateDefaultManager(prerelease);
        this.cachedManagerPrerelease = prerelease;
        this.managerCached = true;
        return this.cachedManager;
    }
}

private UpdateManager? CreateDefaultManager(bool prerelease)
{
    try
    {
        if (RepositoryUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase))
        {
            return new UpdateManager(new GithubSource(RepositoryUrl, null, prerelease));
        }

        return new UpdateManager(RepositoryUrl);
    }
    // ...
}
```

The service depends on `IPreferencesStore` only to read `NotifyPrereleaseUpdates` for the `GithubSource` flag. No other UpdateService logic uses preferences.

### File: src/CLIHub/Startup/UpdateStartupCoordinator.cs, Lines: 53-56

```csharp
var appPrefs = this.preferences.Load();
var shouldNotify = VersionClassification.IsPrerelease(version)
    ? appPrefs.NotifyPrereleaseUpdates
    : appPrefs.NotifyStableUpdates;
```

### File: src/CLIHub.Core/Updates/VersionClassification.cs, Lines: 1-24

```csharp
namespace CLIHub.Core.Updates;

/// <summary>
///   Classifies an available update version as stable or pre-release.
/// </summary>
public static class VersionClassification
{
    /// <summary>
    ///   Returns true when the semantic version carries a pre-release identifier (for example <c> 1.2.0-beta.1 </c>).
    /// </summary>
    /// <param name="version"> The version text, possibly with build metadata. </param>
    /// <returns> True when the version is a pre-release. </returns>
    public static bool IsPrerelease(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        var plus = version.IndexOf('+', StringComparison.Ordinal);
        var core = plus >= 0 ? version[..plus] : version;
        return core.Contains('-');
    }
}
```

Grep confirmed `VersionClassification.IsPrerelease` is called only by `UpdateStartupCoordinator` (line 54). No other consumers exist.

### File: docs/releasing.md, Lines: 144-146

```markdown
- **Prerelease channels** — a tag like `v0.9.0-beta.1` would pass the version check but publish as a
  stable release; do not use prerelease tags until a beta channel change exists (stable-only
  decision, 2026-10-03).
```

This note documents the current stable-only policy and warns not to use prerelease tags. It does not require rewording for this change.

### File: docs/architecture/configuration.md, Line: 6

AppPreferences field list includes `notifyPrereleaseUpdates`.

## Goals / Non-Goals

**Goals:**
- Remove the disabled UI controls (SettingCard and channel chip)
- Remove `NotifyPrereleaseUpdates` from every configuration layer
- Simplify UpdateService to stable-only GitHub source construction
- Remove the unused `VersionClassification` class
- Keep stable-update notification working per `NotifyStableUpdates`
- Preserve backward compatibility: old config files with `notifyPrereleaseUpdates: true` load without error and lose the key on next save

**Non-Goals:**
- Changing the release-notes guidance wording in `docs/releasing.md`
- Introducing a beta channel (this is a removal, not a deferral)
- Touching unrelated update surfaces

## Decisions

### Decision 1: Remove the preference from every layer, not just the UI

**Chosen:** Remove `AppPreferences.NotifyPrereleaseUpdates`, `PreferencesDocument` field and mappings, `ConfigurationSnapshot` clone mapping, `SettingsInput`/`SettingsDraft` records, `SettingsViewModel` property and field, and `UpdateService` prerelease logic.

**Rationale:** 
- A stored `notifyPrereleaseUpdates: true` would enable pre-release checks with no UI left to turn it off
- The setting is inert: no pre-release builds are published, so the flag cannot change observed behavior
- Keeping dead paths in the update logic makes the stable-only intent implicit and fragile; removing them makes it explicit

**Alternatives considered:**
- UI-only removal (keep the config field): Rejected — leaving a stored `true` value with no toggle creates a trap.
- Adding a UI notice ("beta channel not available"): Rejected — the disabled card already shows the notice; the field becomes unused.

**Evidence:** 
- Brief states user chose FULL removal.
- System.Text.Json ignores unknown members on load (verified in CoreJson.Options), so dropping the property is backward compatible.

### Decision 2: Remove UpdateService's IPreferencesStore dependency

**Chosen:** Remove the `IPreferencesStore` constructor parameter and the cached-manager prerelease flag. Construct `GithubSource(RepositoryUrl, null, prerelease: false)` (stable-only) unconditionally in `CreateDefaultManager`.

**Rationale:** 
- The `IPreferencesStore` dependency exists only to read `NotifyPrereleaseUpdates` for the `GithubSource` prerelease flag
- With no prerelease preference, the dependency becomes unused
- Stable-only construction simplifies the service and eliminates the cache-invalidation complexity (no need to track `cachedManagerPrerelease`)

**Alternatives considered:**
- Keep the dependency and hardcode `false`: Rejected — why hold a reference to preferences when it's never read?
- Pass the flag as a constructor parameter: Rejected — no need to parameterize a compile-time constant (stable-only).

**Evidence:** 
- File: src/CLIHub.Core/Updates/UpdateService.cs:26,42,79 — the preferences field is read only in `GetDefaultManager`.
- Grep of `UpdateService(` and `new UpdateService` shows DI registration and test seams; both can drop the `IPreferencesStore` parameter.

**DI registration:** Must be updated to remove `IPreferencesStore` from the `UpdateService` registration. The orchestrator will inspect the DI registration file and include it in tasks.

### Decision 3: Simplify UpdateStartupCoordinator to stable-notification-only

**Chosen:** Remove the `VersionClassification.IsPrerelease` call and the ternary. Notify when `NotifyStableUpdates` is true, unconditionally (since only stable updates can be found).

**Rationale:** 
- The service now returns only stable updates (no prerelease flag in `GithubSource`)
- The `IsPrerelease` branch can never fire
- `NotifyStableUpdates` alone controls whether to notify

**Alternatives considered:**
- Keep the classification and treat all versions as stable: Rejected — why classify when the answer is known at compile time?

**Evidence:** 
- File: src/CLIHub/Startup/UpdateStartupCoordinator.cs:54-56 — the ternary selects `NotifyPrereleaseUpdates` or `NotifyStableUpdates` based on `IsPrerelease`.

### Decision 4: Remove VersionClassification entirely

**Chosen:** Delete `src/CLIHub.Core/Updates/VersionClassification.cs` and `tests/CLIHub.Core.Tests/Updates/VersionClassificationTests.cs` (if it exists).

**Rationale:** 
- Grep confirmed `VersionClassification.IsPrerelease` is called only by `UpdateStartupCoordinator` (src/CLIHub/Startup/UpdateStartupCoordinator.cs:54)
- After the coordinator simplification, the class becomes unused
- Removing it prevents "why is this here if pre-release support doesn't exist?" questions

**Alternatives considered:**
- Keep it for future beta-channel work: Rejected — YAGNI; re-adding it with a beta channel is a small, honest change.

**Evidence:** 
- `grep "VersionClassification"` in `src/` returned only the class definition and the coordinator usage.
- `grep "IsPrerelease"` in `src/` returned only the method definition and the same coordinator call.

**Verification note:** The implementer must check for `tests/CLIHub.Core.Tests/Updates/VersionClassificationTests.cs` and remove it if present. If absent, record "none found" in the commit message.

### Decision 5: Retire the old update-notification requirement and add the stable-only one

**Chosen:** Use REMOVED + ADDED in the `update-checking/spec.md` delta, following the precedent in `openspec/changes/archive/2026-11-06-remove-agent-origin-badge/specs/plugin-installation/spec.md` and `openspec/changes/archive/2026-10-11-remove-init-agent-command/specs/agent-ui/spec.md`.

- REMOVED "Update notification preferences" with reason (stable-only releases; pre-release preference removed) and migration note
- ADDED "Stable update notification" with two scenarios: "Stable notifications enabled" and "Stable notifications disabled"

**Rationale:**
- The pre-release-era requirement ("The system SHALL notify about stable and pre-release updates according to two independent preferences") no longer describes the system's behavior
- Preserving the old scenario names ("Pre-release versions included", "Pre-release notification shown", "Both notification preferences disabled") would leave duplicate or nonsensical scenarios after rewriting them to stable-only behavior
- REMOVED + ADDED cleanly retires the pre-release contract and replaces it with the stable-only contract

Spec changes:
- `preferences-ui/spec.md`: MODIFIED "Updates page options" (scenario names preserved; the requirement describes the UI, which still exists in a simpler form)
- `update-checking/spec.md`: REMOVED "Update notification preferences", ADDED "Stable update notification" (clean break, no scenario-name preservation)
- `configuration-snapshot/spec.md`: MODIFIED "Update preferences default safely" (scenario names preserved; the requirement describes config round-trip, which still exists with fewer fields)

**Alternatives considered:**
- MODIFIED with scenario-name preservation: Rejected — the old scenario names ("Both notification preferences disabled") refer to concepts that no longer exist and would produce duplicates or confusing names.

**Evidence:**
- Precedent: `openspec/changes/archive/2026-11-06-remove-agent-origin-badge/specs/plugin-installation/spec.md` (REMOVED "Plugin origin display", migration note to `agent-ui`)
- Precedent: `openspec/changes/archive/2026-10-11-remove-init-agent-command/specs/agent-ui/spec.md` (REMOVED two requirements, ADDED "Menu command enablement based on capabilities")

### Decision 6: Keep docs/releasing.md unchanged

**Chosen:** Leave the prerelease-tag guidance (lines 144-146) verbatim.

**Rationale:** 
- The note documents the current stable-only policy and warns not to use prerelease tags until a beta channel exists
- It does not describe the removed UI or preference; it describes the release workflow
- No rewording is needed — the guidance remains accurate after this change

**Evidence:** 
- File: docs/releasing.md:144-146 reads "do not use prerelease tags until a beta channel change exists" — this is still true.

## Risks / Trade-offs

- [Pre-release channels return] → The `NotifyPrereleaseUpdates` preference and `GithubSource(prerelease: true)` path are re-added with a beta-channel design. `VersionClassification` can be reintroduced if needed (small, pure utility).
- [User had `notifyPrereleaseUpdates: true` stored] → System.Text.Json ignores the unknown key on load; it disappears on the next save. No error, no data loss.

## Migration Plan

No migration needed. System.Text.Json's default deserialization behavior (CoreJson.Options) ignores unknown members, so an old `notifyPrereleaseUpdates: true` key is skipped on load and never written back.

## Deviations from Brief

None. The verified code matches the brief's description. The brief's VersionClassification decision ("remove it and its tests if it becomes unused") is confirmed: grep showed only one caller (UpdateStartupCoordinator), so the class is removed.

## Open Questions

None.
