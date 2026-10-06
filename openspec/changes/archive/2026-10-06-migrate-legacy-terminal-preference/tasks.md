## 1. Implement Legacy Preference Migration

- [x] 1.1 Add the schema 0 → 1 migration that maps legacy terminal executable values to runtime tokens and preserves unrelated snapshot data.
- [x] 1.2 Register the concrete migration in Core DI so `ConfigMigrationRunner` applies it for legacy documents.

## 2. Verify Compatibility

- [x] 2.1 Add tests for Windows Terminal, Command Prompt, PowerShell, missing/unknown values, current-schema no-op, and preserved project/preferences state.
- [x] 2.2 Run `dotnet build CLIHub.sln -c Release`.
- [x] 2.3 Run `dotnet test CLIHub.sln -c Release`.
- [x] 2.4 Run `openspec validate migrate-legacy-terminal-preference` and verify the delta spec.
