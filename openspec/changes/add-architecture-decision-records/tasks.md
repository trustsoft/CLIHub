## 1. ADR Conventions

- [x] 1.1 Create `docs/adr/README.md` defining the ADR format (Title, Status, Date, Context, Decision, Consequences), naming (`NNNN-short-title.md`), status lifecycle (Accepted / Superseded by NNNN), the take-next-number rule, and the index table listing all five ADRs.

## 2. Decision Records

- [x] 2.1 Create `docs/adr/0001-core-ui-boundaries.md` recording that `CLIHub.Core` stays UI-independent (no WPF reference) while remaining Windows-aware through explicit `Infrastructure/` seams, with the dependency-flow rules and consequences.
- [x] 2.2 Create `docs/adr/0002-single-configuration-document.md` recording one `config.json` document with a single atomic write path, narrow `PreferencesStore`/`ProjectStateStore` adapters over it, and schema version with a migration runner.
- [x] 2.3 Create `docs/adr/0003-startup-orchestration.md` recording ordered startup coordinators (plugins, preferences, runtime, tray/window, hotkey, release notes, update check) and the documented failure-policy baseline.
- [x] 2.4 Create `docs/adr/0004-plugin-catalog-and-precedence.md` recording the `PluginCatalog` boundary, deterministic directory ordering, first-wins duplicate policy, and the `PluginManager` compatibility adapter.
- [x] 2.5 Create `docs/adr/0005-ui-dialog-boundaries.md` recording `IProjectDialogService`/`IUserNotificationService` as the boundary replacing direct WPF dialogs in view models, preserving modal behavior.

## 3. Wiring and Verification

- [x] 3.1 Add a "Decision Records" section to `docs/architecture.md` linking `docs/adr/README.md`, list the folder in `docs/repo-structure.md` Documentation inventory, and verify links resolve.
- [x] 3.2 Run `openspec validate add-architecture-decision-records` and verify it passes.
- [x] 3.3 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`, verifying 0 warnings/errors and all tests pass (docs-only change).
