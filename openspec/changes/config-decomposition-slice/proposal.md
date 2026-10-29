# Proposal: Configuration Domain Value Objects - Scanning Slice

## Why

AppPreferences couples 6 unrelated concerns (startup, hotkey, runtime, logging, agents, updates, appearance, scanning) in a single 28-property class. Adding any field forces synchronized edits at 3 mapping sites (PreferencesDocument.From/ToPreferences, ConfigurationSnapshot.ClonePreferences) plus 2 test sites, regardless of which domain it belongs to. Recent commit 88e516c evidenced this: adding scanning fields (WatchedDirectorySettings, WatchedDirectories, ScanDepth, IgnoredPatterns, DiscoveryMode) touched all 5 edit points even though the change was orthogonal to startup, logging, and update preferences.

## What Changes

Introduce domain-aligned preference value objects at the snapshot/repository boundary, eliminating cross-domain edit ripple:

1. Extract scanning-related fields from AppPreferences into ScanningPreferences (6 properties: WatchedDirectories, WatchedDirectorySettings, ScanDepth, IgnoredPatterns, DiscoveryMode, and one computed property for ProjectScanningOptions)
2. AppPreferences retains a ScanningPreferences property; document mapper splits into specialized mappers per section
3. ConfigurationSnapshot.ClonePreferences delegates cloning to each value object
4. ProjectScanningOptions.From accepts ScanningPreferences instead of AppPreferences
5. Wire format unchanged; AppConfigDocument.PreferencesDocument flattens all sections into the existing camelCase fields

Adding a scanning field now touches 1 value object + its mapper + its test; other concerns remain untouched.

## Impact

- Zero wire-format change (existing config.json files round-trip byte-compatible)
- Zero behavior change (preferences store, project scanning coordinator unchanged)
- Reduced ripple: future scanning fields touch ScanningPreferences only; future agent fields touch AgentPreferences only (follow-up)
- Test surface: one new type (ScanningPreferences), PersistenceCloneRoundTripTests extended

## Non-Goals

- Splitting the config.json file (ADR-0002 single-document boundary preserved)
- Extracting other preference groups (startup, updates, appearance remain in AppPreferences for this slice)
- Changing the store interfaces (PreferencesStore still exposes AppPreferences; narrower views deferred)
- Schema version bump (internal refactor, wire unchanged)

## Dependencies

None
