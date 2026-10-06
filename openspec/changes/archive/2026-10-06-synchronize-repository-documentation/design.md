## Context

The source of truth for this change is the current solution, project files, DI registration, active WPF composition root, and the latest archived refactors. The main discrepancies are concentrated in architecture and repository navigation documentation; historical `CHANGELOG.md` and `RELEASE-NOTES.md` entries should remain historical records.

## Goals / Non-Goals

**Goals:**

- Make documented project structure and dependencies match `CLIHub.sln` and both test project files.
- Make service and boundary descriptions match current Core DI registrations and the plugin catalog refactor.
- Make release instructions use the current development baseline without rewriting historical release sections.
- Make active and legacy window descriptions unambiguous.

**Non-Goals:**

- Moving or deleting `MainWindow`.
- Changing version properties, project references, CI workflows, or release automation.
- Splitting `docs/architecture.md` into multiple documents.
- Changing historical changelog or release-note content.

## Decisions

- Treat `Directory.Build.props`, `global.json`, `CLIHub.sln`, project files, `ServiceRegistration`, and Core DI registration as authoritative over prose.
- Update the documentation set as one coordinated change so cross-links and repeated project descriptions remain consistent.
- Keep `0.7.0` historical release entries intact, but use `0.9.0` where documents describe the current development default or a new release command example.
- Describe `CLIHub.Core.Tests` as Core-only and `CLIHub.Tests` as WPF/application-specific, including their actual project references.
- Describe `PluginCatalog` as the catalog implementation and `PluginManager` as the compatibility adapter; list both `IPluginCatalog` and `IPluginManager` where the compatibility boundary matters.

## Risks / Trade-offs

- [Risk] Repeated documentation may drift again after future refactors. -> Mitigation: tie each edited statement to a concrete source file and keep the scope limited to currently verifiable facts.
- [Risk] Release examples can be mistaken for historical release claims. -> Mitigation: label current development baseline guidance separately from historical release-note sections.
