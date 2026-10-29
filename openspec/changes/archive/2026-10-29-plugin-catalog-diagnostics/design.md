# Plugin Catalog Diagnostics - Design

## Current State (Verified)

### Descriptor Loading and Validation

**PluginDescriptorReader** (src/CLIHub.Core/Plugins/PluginDescriptorReader.cs:14-34)
- Reads plugin.json from pluginDirectory
- Returns PluginDescriptorReadResult(plugin, error) - either succeeds with a plugin or fails with exception
- Uses CoreJson.Options (camelCase, WriteIndented=true)
- Default JsonSerializer ignores unknown fields -> adding schemaVersion is backward-compatible

**PluginDescriptorValidator** (src/CLIHub.Core/Plugins/PluginDescriptorValidator.cs:11-33)
- Validates required fields: id, name, launch command
- Returns PluginValidationResult with errors collection
- Rule order: ID -> Name -> Launch command (line 17-30)

**PluginValidationResult** (src/CLIHub.Core/Plugins/PluginValidationResult.cs:1-26)
- Immutable errors array
- IsValid property checks empty errors

**PluginCatalog.LoadPluginsCore** (src/CLIHub.Core/Plugins/PluginCatalog.cs:57-116)
- Line 79: Reads descriptor
- Line 80-84: Read errors are logged and the directory is skipped (continue)
- Line 88: ValidatePlugin called
- Line 122-134: ValidatePlugin logs invalid plugins and returns false -> directory is skipped
- Line 104: Only valid plugins are added to the list
- **Invalid entries are dropped; validation errors are only logged, never retained**

**IPluginCatalog.GetAllPlugins** (src/CLIHub.Core/Plugins/IPluginCatalog.cs:40, PluginCatalog.cs:119)
- Returns detached copies of the plugins list
- **Only contains valid plugins; no invalid-entry accessor exists**

**Plugin Model** (src/CLIHub.Core/Models/Plugins/Plugin.cs:6-102)
- No schemaVersion field currently

### Consumers

Grep shows 51 matches for IPluginCatalog; key consumers:
- AgentManagementViewModel, AgentPaneController, MainWindow, TrayStateProjection -> display agent lists
- ProjectScanningService (line 59) -> uses GetAllPlugins() for detection markers
- AgentProcessFinder, AgentProcessMonitor -> match processes to plugins
- PluginInitializationService -> loads and seeds

**All consumers call GetAllPlugins() and expect only valid plugins.** Keeping that contract unchanged preserves all current behavior.

### Logging Context Pattern

ILoggingContextProvider (src/CLIHub.Core/Infrastructure/Logging/ILoggingContextProvider.cs:6-17)
- BeginOperation(operationName) -> IDisposable scope
- Used by ProjectScanningService (line 381, 458): `using var scope = this.loggingContext.BeginOperation("ProjectScan");`

## Decisions

### 1. schemaVersion Field

**Shape:**
```json
{
  "schemaVersion": 1,
  "id": "opencode",
  "name": "OpenCode",
  ...
}
```

**Rules:**
- Optional; absent = 1 (default)
- Integer type required; non-integer (string, null, float) -> invalid
- Value > 1 -> invalid with error "Unsupported schema version {value}; maximum supported is 1"
- Value <= 0 -> invalid with error "Schema version must be positive"

**Validation location:** PluginDescriptorValidator.Validate, before existing checks

**Backward compatibility:** JSON deserializer ignores unknown fields (CoreJson.Options has default UnmappedMemberHandling); existing v1 descriptors without schemaVersion load unchanged.

**Rejected:** Making schemaVersion required would break all existing descriptors.

### 2. Invalid Entry Retention

**Chosen shape:** Separate invalid-plugin collection with a paired accessor.

```csharp
// In PluginCatalog
private IReadOnlyList<Plugin> plugins = Array.Empty<Plugin>();
private IReadOnlyList<InvalidPluginEntry> invalidPlugins = Array.Empty<InvalidPluginEntry>();

public record InvalidPluginEntry(string PluginDirectory, Plugin? PartialPlugin, PluginDescriptorReadResult? ReadResult, PluginValidationResult? ValidationResult);
```

**Accessor:**
```csharp
// In IPluginCatalog
IReadOnlyList<InvalidPluginEntry> GetInvalidPlugins();
```

**Rationale:**
- GetAllPlugins() contract unchanged -> zero consumer impact
- InvalidPluginEntry is immutable (record) -> consistent with Core style (PluginDescriptorReadResult, PluginValidationResult)
- PluginDirectory always present -> UI can show where the broken descriptor lives
- PartialPlugin nullable -> handles JSON deserialize failures where no Plugin object exists
- ReadResult XOR ValidationResult -> exactly one is non-null depending on failure stage
- Separate list -> catalog internally builds two lists during LoadPluginsCore; clear separation of valid/invalid

**Rejected alternatives:**
- Union type / discriminated union -> C# lacks clean native support; would need inheritance or marker interfaces
- Single list with IsValid flag -> would require GetAllPlugins() to filter, breaking immutability and forcing consumers to re-filter
- Exception storage only -> loses plugin metadata when validation fails after successful read

### 3. Descriptor Source Port

**Port interface:**
```csharp
public interface IPluginDescriptorSource
{
    IEnumerable<PluginDescriptorSourceEntry> EnumerateDescriptors();
}

public sealed record PluginDescriptorSourceEntry(string PluginDirectory, Plugin? Plugin, Exception? ReadError);
```

**Implementation:** LocalFolderPluginSource wraps existing directory enumeration logic from PluginCatalog.LoadPluginsCore (lines 63-86).

**Wiring:** PluginCatalog constructor accepts IPluginDescriptorSource; DI registration in ServiceCollectionExtensions.

**Rationale:**
- Reserves extension point for marketplace/remote sources (change B or future work)
- Single responsibility: source discovers, catalog validates and stores
- No dead code: catalog immediately consumes the port
- Record matches read-result pattern (PluginDescriptorReadResult)

**Rejected:**
- Keeping enumeration in catalog -> mixes discovery with validation/storage; future remote source would require forking LoadPluginsCore
- Interface with async enumeration -> local folder is synchronous; async belongs on the remote implementation when it arrives

### 4. Logging Scope

**Pattern:** `using var scope = this.loggingContext.BeginOperation("PluginCatalogLoad");` in LoadPluginsCore and ReloadPlugins.

**Rationale:**
- Matches ProjectScanningService pattern (commit 7d92bf3)
- Correlates multi-plugin load diagnostics
- Minimal: just the operation boundary; existing per-plugin warnings stay unchanged

### 5. Spec Delta Decision

**Decision:** MODIFY plugin-seeding/spec.md - add new requirement "Handle unsupported schema versions".

**Evidence:**
- Existing requirement "Each seeded descriptor is loadable" (plugin-seeding/spec.md:34-37) says descriptors must be accepted if they define id, name, launch
- A descriptor with schemaVersion=2 would now be rejected -> observable behavior change
- plugin-seeding is the only capability that governs descriptor acceptance rules
- Original scenario names must be preserved (per OpenSpec rules)

**New requirement (ADDED):**
```
### Requirement: Handle unsupported schema versions

The system SHALL reject descriptors with unsupported schema versions and expose the validation error.

#### Scenario: Schema version absent defaults to 1
- WHEN a descriptor omits schemaVersion
- THEN it is treated as version 1 and loaded if other validation passes

#### Scenario: Unsupported schema version is rejected
- WHEN a descriptor declares schemaVersion > 1
- THEN it is invalid with error "Unsupported schema version X; maximum supported is 1"

#### Scenario: Invalid schema version type is rejected
- WHEN schemaVersion is not an integer (string, null, float)
- THEN the descriptor is invalid with error "Schema version must be an integer"
```

**Rejected:** Creating a new plugin-catalog capability. The schema version is a descriptor validation rule, which already lives under plugin-seeding's validation contract.

## Open Questions

1. Should InvalidPluginEntry.PartialPlugin include PluginDirectory when both are present, or omit it since the record already has PluginDirectory at top level? (Proposal: omit from Plugin, use record field.)

2. Should GetInvalidPlugins() return copies like GetAllPlugins(), or raw records (already immutable)? (Proposal: raw records - no Copy() needed for immutable data.)

3. Does the seeded descriptor set need schemaVersion: 1 explicitly, or rely on absent=1 default? (Proposal: omit for minimal diff; add it when v2 ships.)
