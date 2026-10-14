# 0006. Windows platform library

## Status

Accepted — 2026-10-14. Amends the "Windows-aware Core" clause of [0001](0001-core-ui-boundaries.md).

## Context

ADR-0001 kept `CLIHub.Core` on the portable `net10.0` target while deliberately housing Windows integration (registry access, named mutexes/pipes, process execution, process inspection) inside `Core/Infrastructure/`. That placement made the seams visible, but the compiler could not see them: `CLIHub.Core` compiles against platform-neutral surface-area rules, so every registry call or Windows-annotated type inside it triggered CA1416 platform-compatibility warnings. The warnings were false positives — CLIHub is Windows-only by design — but silencing them with attributes or suppressions would have spread platform noise through code that is otherwise platform-neutral.

## Decision

- Add `src/CLIHub.Core.Windows`, targeting `net10.0-windows`, referencing `CLIHub.Core`. The TFM is the platform contract: code inside it may use Windows APIs without annotations or suppressions.
- Move the platform adapters there: `SystemTheme`, `StartupService`/`IStartupService` and the registry seam, `SingleInstanceGuard`/`ISingleInstanceGuard`, `WindowsInteractiveProcessRunner`, `WindowsProcessOutputRunner`, `WindowsAgentProcessInspector`, and `RuntimeSelector`.
- `CLIHub.Core` keeps the ports (`IInteractiveProcessRunner`, `IProcessOutputRunner`, `IAgentProcessInspector`), their models, and the Windows-shell *convention* logic that uses no Windows APIs (`ProcessLauncher`, `WindowsCommandLineBuilder`, `AgentProcessMatcher`). Core domain services keep depending on `ProcessLauncher`; the dependency direction stays `CLIHub.Core.Windows -> CLIHub.Core`.
- DI registration splits along the seam: `AddClIHubCoreServices` registers platform-neutral services; the new `AddWindowsInfrastructure` in `CLIHub.Core.Windows` registers the adapters. The application composition root calls both. The architecture test that restricts registration to composition roots whitelists the new module.
- Tests mirror the split: `CLIHub.Core.Tests` stays `net10.0` and now genuinely runs on any .NET 10 SDK; `CLIHub.Core.Windows.Tests` (`net10.0-windows`) owns the adapter tests, including the real-process capture-output integration cases.

Rejected alternatives:

- *Retarget `CLIHub.Core` to `net10.0-windows`* — one line, but it erases the portable test target ADR-0001 committed to and makes the platform boundary less visible, not more.
- *Annotate call sites with `[SupportedOSPlatform]`* — correct but cascading attribute noise with no structural gain.

## Consequences

- CA1416 platform warnings disappear correctly: the platform declaration is the project reference graph, not attributes.
- The Windows integration surface is enumerable from the solution layout; a hypothetical non-Windows port would provide a sibling platform library instead of editing Core.
- Building the full service graph now requires both `AddClIHubCoreServices` and `AddWindowsInfrastructure`; resolving `ProcessLauncher`-dependent services from Core alone fails by design, and the composition tests encode that.
- Two more projects exist in the solution and CI; the cost is a wider, but shallower, project graph.
