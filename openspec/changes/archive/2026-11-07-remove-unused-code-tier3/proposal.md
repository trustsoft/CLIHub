## Why

This is the third phase of dead code removal following whole-solution Roslyn analysis (IDE0051/IDE0052/IDE0060/IDE0005, Roslynator RCS1213/RCS1163, plus compiler warnings). Tiers 1 and 2 removed dead types, interface members, and pass-through chains. The remaining verified set is smaller and more mechanical: dead fields, a dead test helper, a dead test record, one unused constructor parameter, one unused method parameter propagated through an interface chain, dead null-conditionals, and six unused `using` directives. Removing them completes the cleanup and reduces maintenance surface while preserving all documented behavior.

## What Changes

Remove 8 verified items grouped into four categories:

**A. Dead fields (assigned but never read):**
1. `PluginFeedClient.versionProvider` — `IUpdateVersionProvider` field assigned in both constructors but never read (IDE0052). Remove the field, the parameter from both constructors, the `?? throw` guard, the `<param>` XML doc lines, and the `using CLIHub.Core.Updates;` directive. DI registration is type-based (`AddSingleton<IPluginFeedClient, PluginFeedClient>()`) and adapts automatically. Update the single test construction site and drop its now-unused `Mock` and `using`.
2. `BrowsePluginsDialog.viewModel` — private readonly field assigned in the constructor but never read (the `DataContext` holds the VM). Remove the field and the assignment; keep the constructor parameter and `this.DataContext = viewModel;`.

**B. Dead null-conditionals (unreachable branch):**
3. `ApplicationHost.Shutdown()` — `this.services` is proven non-null by the guard at lines 89-95, yet lines 101, 110-111, and 121 use `?.` on `services` (CA1508 at line 101). Replace with direct member access. Keep the `?.` on `GetService<IApplicationSession>()` and `GetService<IApplicationOperationLifetime>()` (those can return null).

**C. Unused parameter propagated through an interface chain:**
4. `WindowsCommandLineBuilder.BuildInteractive` — `workingDirectory` parameter is unused inside the method (IDE0060/RCS1163). Remove it from the static builder, the `ICommandLineBuilder.BuildInteractive` interface member, the `CommandLineBuilderAdapter` implementation, and the `ProcessOrchestrator.LaunchInteractiveAsync` call site (keep `request.WorkingDirectory` for the runner at line 94). Update test call sites in three test files.

**D. Dead test-only code and unused usings:**
5. `SpacedPanelTests.CreatePanel(params UIElement[])` — private static overload with zero callers (IDE0051/RCS1213). Delete only that overload; the other three overloads stay.
6. `DomainEventBusTests.AnotherEvent` — private record never instantiated or referenced (CA1812). Delete it.
7. Six unused `using` directives (IDE0005): `AgentCommandService.cs:7`, `AgentVersionService.cs:9`, `PluginFeedClient.cs:13` (covered by item 1), `ServiceRegistration.cs:10`, `WatchedDirectoryRow.cs:3`, `AccentSelection.cs:4`. Remove exactly those directives.
8. `Program.Main(string[] args)` — `args` is never used (IDE0060). **Decision: keep the parameter.** `Main(string[] args)` is the conventional WPF entry point signature and the `args` parameter is part of the standard `STAThread` entry contract. Removing it would be a gratuitous signature change with no benefit. Recorded as an explicit non-goal.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None — this is a pure internal refactor with zero behavior change. The change opts out of spec deltas (`skip_specs: true` in `.openspec.yaml`).

## Impact

- **Code**: 7 source files trimmed (Core and WPF app), 3 test files trimmed, 1 interface file trimmed.
- **Behavior**: None. No durable capability changes, no external API surface changes, no configuration or data migration.
- **Dependencies**: None.
- **Build**: Eliminates the IDE0051/IDE0052/IDE0060/IDE0005/CA1508/CA1812/RCS1213/RCS1163 findings on the touched files. All builds and tests remain green.

## Non-Goals

- No behavior, data, UI, or durable-capability changes.
- Do not touch XAML-bound members, DI-resolved types, reflection/test-only seams, interface implementations, or spec-required members.
- Do not clean unused `using` directives in files not listed above.
- Do not alter `.opencode/**`, `docs/backlog.md`, `openspec/specs/**`, or unrelated source.
- Do not touch unused lambda parameters in tests (delegate signatures require them).
- Do not remove the `args` parameter from `Program.Main` — it is the conventional WPF entry point signature and removing it has no benefit.
- Do not reformat or restructure code beyond the minimal removal.

## Dependencies

None — standalone refactor. Tier 1 and tier 2 are archived; this change does not depend on them.
