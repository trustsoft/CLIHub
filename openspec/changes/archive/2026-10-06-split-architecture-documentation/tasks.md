## 1. Create Topical Documents

- [x] 1.1 Create `docs/architecture/` with `startup.md` (Process Bootstrap, First-Instance Startup Sequence, Startup Failure Policy, Shutdown Sequence) moved from `docs/architecture.md`, and verify content moved verbatim with only link-path fixes.
- [x] 1.2 Create `docs/architecture/configuration.md` (Configuration Persistence schema/location/atomic writes/forward compatibility) moved from `docs/architecture.md`, and verify content moved verbatim.
- [x] 1.3 Create `docs/architecture/plugins.md` (Plugin Descriptor Format including JSON example, commands, detection, seeding) moved from `docs/architecture.md`, and verify content moved verbatim.
- [x] 1.4 Create `docs/architecture/processes.md` (process execution: runtimes, launching vs capture, error handling for processes) moved from `docs/architecture.md`, and verify content moved verbatim.
- [x] 1.5 Create `docs/architecture/ui.md` (windows, MVVM view models, XAML themes and styles recipe, resource organization) moved from `docs/architecture.md`, and verify content moved verbatim.

## 2. Reduce Entry Point and Fix References

- [x] 2.1 Reduce `docs/architecture.md` to overview content (solution structure, project responsibilities, dependency flow, tech stack, capabilities, testing strategy, conventions, build/run, security, logging, release notes, packaging) and add one-line links to each new topical document where its section used to be.
- [x] 2.2 Search the repository for `architecture.md#` deep anchors and update all inbound references (AGENTS.md, docs/repo-structure.md, docs/releasing.md, specs, source comments) to the new topical files, and verify grep finds no stale deep anchors into moved sections.

## 3. Verification

- [x] 3.1 Run `openspec validate split-architecture-documentation` and verify it passes.
- [x] 3.2 Run `dotnet build CLIHub.sln -c Release` and verify 0 warnings/errors (docs-only change must not affect build).
- [x] 3.3 Run `dotnet test CLIHub.sln -c Release` and verify all tests pass.
