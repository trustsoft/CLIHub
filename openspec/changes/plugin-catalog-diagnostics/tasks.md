# Plugin Catalog Diagnostics - Tasks

## Phase 1: Schema Version Support

- [x] 1.1 Add `SchemaVersion` property (int?) to Plugin model with JSON name "schemaVersion"
  - Gate: `dotnet build CLIHub.sln` succeeds
- [x] 1.2 Extend PluginDescriptorValidator to validate schemaVersion before existing checks
  - Absent or 1 -> valid
  - > 1 -> error "Unsupported schema version {value}; maximum supported is 1"
  - <= 0 -> error "Schema version must be positive"
  - Non-integer (null when required, string, float) -> error "Schema version must be an integer"
  - Gate: validator tests pass + new tests for each schema version case
- [x] 1.3 Add validation tests covering schemaVersion matrix
  - Absent (null property)
  - Value 1
  - Value 2 (unsupported)
  - Value 0 (invalid)
  - Value -1 (invalid)
  - String "1" (type mismatch)
  - Null (type mismatch)
  - Gate: all PluginDescriptorValidatorTests pass
- [x] 1.4 Build and test
  - Gate: `dotnet build CLIHub.sln && dotnet test CLIHub.sln` both succeed

## Phase 2: Invalid Entry Retention

- [x] 2.1 Create InvalidPluginEntry record in Core/Plugins namespace
  - Fields: PluginDirectory (string), PartialPlugin (Plugin?), ReadResult (PluginDescriptorReadResult?), ValidationResult (PluginValidationResult?)
  - Gate: compiles
- [x] 2.2 Add GetInvalidPlugins() to IPluginCatalog interface
  - Returns IReadOnlyList<InvalidPluginEntry>
  - Gate: interface compiles
- [x] 2.3 Add invalidPlugins field to PluginCatalog and implement GetInvalidPlugins()
  - private IReadOnlyList<InvalidPluginEntry> invalidPlugins = Array.Empty<InvalidPluginEntry>()
  - public IReadOnlyList<InvalidPluginEntry> GetInvalidPlugins() => this.invalidPlugins
  - Gate: compiles
- [x] 2.4 Modify LoadPluginsCore to populate invalidPlugins list
  - Collect InvalidPluginEntry for read errors (lines 80-84 pattern)
  - Collect InvalidPluginEntry for validation errors (lines 122-134 pattern)
  - Set this.invalidPlugins = collected list before returning valid plugins
  - Gate: compiles
- [x] 2.5 Update PluginCatalog tests to verify invalid-entry retention
  - Test: malformed JSON -> GetInvalidPlugins() returns entry with ReadResult.Error
  - Test: missing name -> GetInvalidPlugins() returns entry with ValidationResult.Errors
  - Test: schemaVersion=2 -> GetInvalidPlugins() returns entry with schema version error
  - Test: GetAllPlugins() unchanged (still empty/valid-only)
  - Test: reload clears previous invalid entries
  - Gate: all PluginCatalogTests pass
- [x] 2.6 Build and test
  - Gate: `dotnet build CLIHub.sln && dotnet test CLIHub.sln` both succeed

## Phase 3: Descriptor Source Port

- [x] 3.1 Create IPluginDescriptorSource interface in Core/Plugins namespace
  - Method: IEnumerable<PluginDescriptorSourceEntry> EnumerateDescriptors()
  - Gate: compiles
- [x] 3.2 Create PluginDescriptorSourceEntry record
  - Fields: PluginDirectory (string), Plugin (Plugin?), ReadError (Exception?)
  - Gate: compiles
- [x] 3.3 Create LocalFolderPluginSource implementing IPluginDescriptorSource
  - Constructor: pluginsPath (string), descriptorReader (IPluginDescriptorReader)
  - EnumerateDescriptors: extract lines 63-86 logic from PluginCatalog.LoadPluginsCore
  - Gate: compiles
- [x] 3.4 Refactor PluginCatalog to accept and use IPluginDescriptorSource
  - Add constructor parameter IPluginDescriptorSource source
  - LoadPluginsCore calls source.EnumerateDescriptors() instead of inline directory scan
  - Existing pluginsPath constructor parameter kept for logo resolver and backward compat
  - Default source = new LocalFolderPluginSource(pluginsPath, descriptorReader) when null
  - Gate: compiles
- [x] 3.5 Update ServiceCollectionExtensions DI registration
  - Register IPluginDescriptorSource as LocalFolderPluginSource
  - Wire to PluginCatalog constructor
  - Gate: compiles + ServiceCollectionExtensionsTests pass
- [x] 3.6 Verify all PluginCatalog tests still pass with refactored structure
  - Gate: all PluginCatalogTests pass (no behavior change)
- [x] 3.7 Build and test
  - Gate: `dotnet build CLIHub.sln && dotnet test CLIHub.sln` both succeed

## Phase 4: Logging Context Integration

- [x] 4.1 Add ILoggingContextProvider parameter to PluginCatalog constructor
  - Store as private readonly field
  - Gate: compiles
- [x] 4.2 Add logging scope to LoadPluginsCore
  - using var scope = this.loggingContext.BeginOperation("PluginCatalogLoad")
  - Wrap the entire method body (after var plugins = new List<Plugin>())
  - Gate: compiles
- [x] 4.3 Add logging scope to ReloadPlugins
  - using var scope = this.loggingContext.BeginOperation("PluginCatalogReload")
  - Wrap LoadPluginsCore call and notification
  - Gate: compiles
- [x] 4.4 Update PluginCatalog test constructor to provide mock/null context
  - Use NullLoggerFactory or mock ILoggingContextProvider
  - Gate: all PluginCatalogTests pass
- [x] 4.5 Update DI registration to wire ILoggingContextProvider
  - ServiceCollectionExtensions: sp.GetRequiredService<ILoggingContextProvider>()
  - Gate: ServiceCollectionExtensionsTests pass
- [x] 4.6 Build and test
  - Gate: `dotnet build CLIHub.sln && dotnet test CLIHub.sln` both succeed

## Phase 5: Documentation and Spec Delta

- [x] 5.1 Create spec delta at openspec/specs/plugin-seeding/deltas/plugin-catalog-diagnostics.md
  - ADDED requirement: Handle unsupported schema versions
  - Three scenarios: absent defaults to 1, unsupported rejected, invalid type rejected
  - Gate: file created with required structure
- [x] 5.2 Update docs/architecture/plugins.md with schemaVersion field
  - Add schemaVersion to example descriptor
  - Document absent=1, >1=invalid, supported max=1
  - Gate: doc updated
- [x] 5.3 Update docs/project-context.md if plugin section references validation
  - Check if update needed
  - Gate: consistency verified
- [x] 5.4 Run openspec validate --all
  - Gate: validation passes with no errors

## Phase 6: Final Verification

- [x] 6.1 Validation matrix test (manual or automated)
  - Create test descriptors with schemaVersion: absent, 1, 2, 0, -1, "1", null
  - Verify absent and 1 load as valid
  - Verify 2, 0, -1, "1", null appear in GetInvalidPlugins() with correct errors
  - Gate: all cases verified
- [x] 6.2 Catalog retention pin test
  - Load folder with 1 valid + 1 invalid descriptor
  - Verify GetAllPlugins() returns 1, GetInvalidPlugins() returns 1
  - Verify invalid entry has PluginDirectory and error details
  - Gate: pin verified
- [x] 6.3 Port wiring test
  - Verify DI resolves IPluginDescriptorSource
  - Verify PluginCatalog receives and uses source
  - Gate: integration test or manual verification passes
- [x] 6.4 Backward compatibility verification
  - Run against existing plugin folders without schemaVersion
  - Verify no behavior change (GetAllPlugins() identical)
  - Gate: regression-free
- [x] 6.5 Full build and test suite
  - Gate: `dotnet build CLIHub.sln && dotnet test CLIHub.sln` both succeed
- [x] 6.6 Run openspec validate --all
  - Gate: validation passes
- [x] 6.7 Run graphify update .
  - Gate: graph updated successfully
