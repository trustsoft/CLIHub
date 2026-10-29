# Tasks: Configuration Domain Value Objects - Scanning Slice

## 1. Create ScanningPreferences Value Object

- [ ] 1.1 Create src/CLIHub.Core/Models/Configuration/ScanningPreferences.cs with 5 public properties (WatchedDirectories, WatchedDirectorySettings, ScanDepth, IgnoredPatterns, DiscoveryMode), defaults matching AppPreferences, and Copy() method
- [ ] 1.2 Add XML documentation to all public members
- [ ] 1.3 Build: `dotnet build CLIHub.sln`

## 2. Update AppPreferences

- [ ] 2.1 Remove 5 scanning properties from AppConfig.cs:AppPreferences (lines 150-176)
- [ ] 2.2 Add `public ScanningPreferences Scanning { get; set; } = new();` property
- [ ] 2.3 Update XML doc comment listing preference groups
- [ ] 2.4 Build: `dotnet build CLIHub.sln`

## 3. Update Document Mapping

- [ ] 3.1 In AppConfigDocument.PreferencesDocument.From, replace direct scanning-field access with `preferences.Scanning.WatchedDirectories`, etc.
- [ ] 3.2 In AppConfigDocument.PreferencesDocument.ToPreferences, construct `Scanning = new ScanningPreferences { ... }` from flat document fields
- [ ] 3.3 Preserve comment "Both From and ToPreferences must map every public property"
- [ ] 3.4 Build: `dotnet build CLIHub.sln`

## 4. Update Snapshot Cloning

- [ ] 4.1 In ConfigurationSnapshot.ClonePreferences, replace manual scanning-field copy with `Scanning = preferences.Scanning.Copy()`
- [ ] 4.2 Preserve comment "Must copy every public property of AppPreferences"
- [ ] 4.3 Build: `dotnet build CLIHub.sln`

## 5. Update ProjectScanningOptions

- [ ] 5.1 Change ProjectScanningOptions.From signature from `(AppPreferences preferences)` to `(ScanningPreferences scanning)`
- [ ] 5.2 Replace `preferences.WatchedDirectories` with `scanning.WatchedDirectories`, etc.
- [ ] 5.3 Update XML doc comment
- [ ] 5.4 Find all callers of ProjectScanningOptions.From and update to pass `preferences.Scanning`
- [ ] 5.5 Build: `dotnet build CLIHub.sln`

## 6. Update Round-Trip Tests

- [ ] 6.1 In PersistenceCloneRoundTripTests.AssertPreferencesPropertiesMatch, replace 5 scanning assertions with one `Assert.Equal(expected.Scanning.WatchedDirectories, actual.Scanning.WatchedDirectories)` per ScanningPreferences property
- [ ] 6.2 In PersistenceCloneRoundTripTests.CoveredPreferenceProperties, remove 5 scanning property names, add `nameof(AppPreferences.Scanning)`
- [ ] 6.3 Test: `dotnet test CLIHub.sln --filter "FullyQualifiedName~PersistenceCloneRoundTripTests"`

## 7. Add ScanningPreferences-Specific Round-Trip Test

- [ ] 7.1 Add test `ScanningPreferences_Copy_DetachesNestedCollections` verifying WatchedDirectories and WatchedDirectorySettings mutations don't change source
- [ ] 7.2 Test: `dotnet test CLIHub.sln --filter "FullyQualifiedName~PersistenceCloneRoundTripTests"`

## 8. Full Verification

- [ ] 8.1 Build: `dotnet build CLIHub.sln`
- [ ] 8.2 Test: `dotnet test CLIHub.sln`
- [ ] 8.3 Manual smoke: launch application, open Settings -> Projects, verify scanning preferences load/save correctly
- [ ] 8.4 Verify config.json wire format unchanged (no schemaVersion bump, same camelCase fields)

## 9. Update Graph

- [ ] 9.1 Run `graphify update .`
