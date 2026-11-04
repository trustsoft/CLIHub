## Why

`RuntimeKinds` has the PowerShell fallback (`RuntimeKind.PowerShell`) hard-coded in two locations: `TryParse` (`out`-init at line 61) and `Parse` (fallback at line 105). Sibling model types (`ProjectDiscoveryModes` and `PathDisplayStyles`) centralize the fallback as a public `Default` constant that is referenced from parse methods and XML documentation. Mirroring that pattern makes the fallback single-source-of-truth and self-documenting.

## What Changes

- Add `public const RuntimeKind Default = RuntimeKind.PowerShell;` to `RuntimeKinds` with English XML documentation describing it as the fallback value used for missing or unrecognized runtime tokens.
- Reference `Default` from the `TryParse` `out`-init (line 61) and the `Parse` fallback (line 105).
- Update the `Parse` XML doc to reference `Default` instead of `RuntimeKind.PowerShell`.
- No behavior change: `Parse` and `TryParse` continue to return `RuntimeKind.PowerShell` for missing/unrecognized values. Existing tests remain unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a pure internal refactor with no spec-level behavior change, so the change opts out of specs (`skip_specs: true` in `.openspec.yaml`).

## Impact

- Code: `src/CLIHub.Core/Models/Processes/RuntimeKind.cs` (3 lines changed: add `Default` constant, two reference-site updates).
- Tests: `tests/CLIHub.Core.Tests/Models/Processes/RuntimeKindsTests.cs` (unchanged; existing tests already verify the fallback behavior).
- Callers: `AppConfigDocument.ToPreferences()` (line 198) calls `RuntimeKinds.Parse`, and `LegacyTerminalPreferenceMigration.Migrate` (line 25) calls `RuntimeKinds.TryParse`; no behavior change for them.
- Documentation: the constant is self-documenting via XML doc; no separate architecture doc changes required.
- Build: no package or project dependencies affected. The change is internal to `CLIHub.Core`.
