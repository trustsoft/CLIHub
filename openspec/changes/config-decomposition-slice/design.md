# Design: Configuration Domain Value Objects - Scanning Slice

## Verified Current State

### Configuration Stack

- **AppConfig** (src/CLIHub.Core/Models/Configuration/AppConfig.cs:6-22): in-memory aggregation root; Projects (line 11), Preferences (line 16), CurrentProjectId (line 21)
- **AppPreferences** (AppConfig.cs:27-182): 28 public properties spanning 6 concerns (startup, hotkey, runtime, logging, agents, updates, appearance, scanning); lines 150-176 hold scanning fields
- **AppConfigDocument** (src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs:8-260): wire-format DTO; CurrentSchemaVersion = 2 (line 18), SchemaVersion property (line 13)
- **AppConfigDocument.PreferencesDocument** (AppConfigDocument.cs:117-252): nested document class; From (line 150) maps model->wire, ToPreferences (line 193) maps wire->model; both must map every AppPreferences property (comment lines 148-149)
- **ConfigurationSnapshot** (src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs:8-96): detached mutable boundary state; ClonePreferences (line 61) copies every AppPreferences property (comment lines 59-60)
- **ConfigurationRepository** (src/CLIHub.Core/Configuration/ConfigurationRepository.cs:14-296): owns snapshots, migrations, persistence; Read (line 59), Update (line 69), atomic write (lines 261-293)
- **PreferencesStore** (src/CLIHub.Core/Configuration/PreferencesStore.cs:8-31): adapter exposing AppPreferences; Load (line 22), Update (line 25)
- **ProjectScanningOptions** (src/CLIHub.Core/Scanning/ProjectScanningOptions.cs:16-64): scanning coordinator's options; From(AppPreferences) (line 27) extracts scanning fields

### Evidence of Coupling

Commit 88e516c (feat: add per-directory scan settings) added WatchedDirectorySettings; it touched:

1. AppConfig.cs:159-160 (new property in AppPreferences)
2. AppConfigDocument.cs:142, 176-186 (PreferencesDocument.From serialization)
3. AppConfigDocument.cs:218 (ToPreferences deserialization)
4. ConfigurationSnapshot.cs:86-90 (ClonePreferences copy)
5. PersistenceCloneRoundTripTests.cs:50 (census)
6. PersistenceCloneRoundTripTests.cs:246-251 (assertion)

The change was orthogonal to startup, logging, and updates but forced edits in all shared mapping sites.

### Pinned Contracts

- **PersistenceCloneRoundTripTests** (tests/CLIHub.Core.Tests/Configuration/PersistenceCloneRoundTripTests.cs:9-258): CoveredPreferenceProperties (lines 25-55) census, AssertPreferencesPropertiesMatch (lines 218-257) validates all properties round-trip; census test (lines 128-143) ensures mappers stay synchronized
- **openspec/specs/configuration-snapshot/spec.md**: wire format (Requirement: Snapshot persistence preserves the existing document, lines 37-70), atomic writes, schema version, migration contract

## Decisions

### Chosen Slice: Introduce ScanningPreferences Value Object

**Rationale:** Scanning is the largest cohesive preference group (6 properties), already has a consumer (ProjectScanningOptions.From), and recent evolution (88e516c) evidenced the cross-domain ripple. Extracting it isolates future scanning additions from unrelated preferences.

**Type Design:**

```csharp
// src/CLIHub.Core/Models/Configuration/ScanningPreferences.cs
public sealed class ScanningPreferences
{
    public List<string>? WatchedDirectories { get; set; }
    public IReadOnlyDictionary<string, WatchedDirectoryScanSettings> WatchedDirectorySettings { get; set; } = ...;
    public int ScanDepth { get; set; } = ProjectScanningDefaults.DefaultScanDepth;
    public List<string>? IgnoredPatterns { get; set; }
    public ProjectDiscoveryMode DiscoveryMode { get; set; } = ProjectDiscoveryModes.Default;

    public ScanningPreferences Copy() => new() { /* all fields */ };
}
```

**AppPreferences Change:**

Remove 5 scanning properties (WatchedDirectories, WatchedDirectorySettings, ScanDepth, IgnoredPatterns, DiscoveryMode) from AppConfig.cs:AppPreferences; add:

```csharp
public ScanningPreferences Scanning { get; set; } = new();
```

**Document Mapping:**

AppConfigDocument.PreferencesDocument unchanged (still has flat camelCase fields); From/ToPreferences delegate scanning section to ScanningPreferences:

```csharp
// In From:
WatchedDirectories = preferences.Scanning.WatchedDirectories, // etc.

// In ToPreferences:
Scanning = new ScanningPreferences {
    WatchedDirectories = this.WatchedDirectories, // etc.
}
```

**Snapshot Cloning:**

ConfigurationSnapshot.ClonePreferences calls Scanning.Copy() instead of manual field copy.

**Consumption:**

ProjectScanningOptions.From(AppPreferences) becomes From(ScanningPreferences):

```csharp
public static ProjectScanningOptions From(ScanningPreferences scanning)
```

### Rejected Alternatives

- **Slice B (narrow a store interface):** PreferencesStore exposes whole AppPreferences; no current consumer requests a subset. Would add abstraction without removing mapping sites.
- **Slice C (reject):** The current document layer DOES NOT split sections internally; AppConfigDocument.PreferencesDocument is flat. The coupling exists at the mapping layer.

### Follow-Ups with Triggers

1. **Extract AgentPreferences** (4 properties: ShowOnlyProjectAgents, AgentProbeTtlMinutes, AgentProbeTimeoutSeconds, DefaultAgentId) when agent management expands (e.g. per-agent probe overrides)
2. **Extract UpdatePreferences** (4 properties: CheckForUpdatesOnStartup, AutoDownloadUpdates, NotifyStableUpdates, NotifyPrereleaseUpdates) when update workflows add notification channels
3. **Extract AppearancePreferences** (3 properties: AppTheme, AccentColor, PathDisplayStyle) when theming adds palette presets or per-window overrides
4. **Narrow PreferencesStore interface** after 3+ value objects stabilize; introduce ScanningStore, AgentStore adapters

### Deviations

None. The slice follows the established patterns (value objects with Copy(), document mapper delegates, round-trip tests).

## Open Questions

1. Should ScanningPreferences expose a ToProjectScanningOptions() method instead of ProjectScanningOptions.From(scanning)? (Leans toward From for symmetry with existing From(AppPreferences) signature.)
2. Does ProjectScanningCoordinator need scanning-change events isolated from other preference changes, or is the current Update callback granularity sufficient? (Defer event granularity until evidence shows noise.)
