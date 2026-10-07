## Context

`RuntimeKind` and `RuntimeKinds` already define the internal runtime vocabulary and legacy aliases. `AppPreferences.DefaultRuntime` was the remaining string-valued application field; `SettingsApplicationService` and `StartupPreferencesApplier` converted it at use sites.

## Decision

Use `RuntimeKind` for `AppPreferences.DefaultRuntime`, `SettingsDraft`, preference applier ports, and process runner contracts. `AppConfigDocument.PreferencesDocument.DefaultRuntime` remains a string because it is the JSON persistence boundary. Mapping uses `RuntimeKinds.ToToken` and `RuntimeKinds.Parse`, so unknown stored values safely resolve to Windows Terminal. Legacy migration resolves old executable values to the enum before the repository writes the current token.

## Verification

- Existing flat JSON shape and migration tests remain valid.
- Unknown token and canonical token tests cover safe defaults and storage mapping.
- Release build and full test suite pass.
