## Context

`src/CLIHub/Composition/ServiceRegistration.cs` is a single 57-statement registration method plus three
infrastructure calls. Core and Core.Windows already use grouped feature methods inside one composition file
each. An architecture test,
`tests/CLIHub.Tests/Architecture/ArchitectureBoundaryTests.cs:DependencyRegistration_IsRestrictedToCompositionRoots`,
allows `AddSingleton`/`AddTransient`/`AddScoped` only in the three existing composition files, so the
reorganization must stay inside `ServiceRegistration.cs` — which is exactly the Core pattern.

Existing app-layer registration coverage is scattered but real: `ApplicationBootstrapperTests`,
`ApplicationLifetimeRegistrationTests`, `HotkeyStartupRegistrarTests`, `PluginInitializationServiceTests`,
`PreferenceApplierTests`, `ReleaseNotesStartupCoordinatorTests`, `StartupPreferencesApplierTests`,
`TrayMenuBuilderRegistrationTests`, and `UpdateWorkflowTests` each assert one slice (descriptor shape or a
narrow resolution). No test sweeps the whole graph. Two tests also pin file content:
`ApplicationSessionAndTrayWorkflows_AreRegisteredAndLegacyWindowIsNot` asserts the literal strings
`IApplicationSession, ApplicationSession`, `TrayStateProjection`, `TrayCommandHandlers` in
`ServiceRegistration.cs` (they survive: the statements stay in this file), and
`DependencyRegistration_IsRestrictedToCompositionRoots` (satisfied by D1).

## Goals / Non-Goals

**Goals:** make `ServiceRegistration.cs` read as a table of contents of feature modules mirroring Core;
keep every registration statement byte-identical; add one whole-graph guard test.

**Non-Goals:** behavior change, lifetime changes, new abstractions, cross-layer moves, spec changes.

## Decisions

### D1 - One file, private static module methods (mirror Core)

All seven modules are `private static` methods of `ServiceRegistration`, using the same
`private static IServiceCollection AddXServices(this IServiceCollection services)` shape Core uses for its
private modules. A file-per-module layout was rejected because it would require extending the
`DependencyRegistration_IsRestrictedToCompositionRoots` allowlist and diverges from the Core precedent.

### D2 - Module boundaries

The entry point keeps exactly the three infrastructure calls — `AddClIHubCoreServices(...)`,
`AddWindowsInfrastructure(guard)`, `AddLogging(...AddSerilog(Log.Logger, dispose: true))` — then calls the
modules in reading order: startup, launch window, tray, hotkeys, settings, updates, shared UI.

| Module | Count | Contents (current line numbers in ServiceRegistration.cs) | Rationale / notes |
|---|---|---|---|
| `AddStartupServices` | 13 | `IApplicationLifetime→WpfApplicationLifetime` (55), `IApplicationOperationLifetime` (56), `IInstanceCoordinator` (57), `IStartupStateLoader` (58), `IOptionalStartupCoordinator` (59), `IApplicationSession` (61), `IApplicationBootstrapper` (62), `IApplicationStartupUi→WpfApplicationStartupUi` (63), `Func<IApplicationStartupUi>` (64), `IPluginInitializationService` (69), `IStartupPreferencesApplier` (70), `IUpdateStartupCoordinator` (102), `IReleaseNotesStartupCoordinator` (101) | Everything owned by the boot/shutdown pipeline. `PluginInitializationService` is here because its consumer is `StartupStateLoader`; the two startup coordinators are here (not in their domains) so this module lists the full boot sequence, mirroring how `ApplicationBootstrapper` executes them. |
| `AddLaunchWindowServices` | 12 | `IAgentCommandWorkflow→AgentCommandWorkflow` (68), `ProjectModelCatalog` (75), `LaunchCommandCoordinator` (76), `LaunchWindowActionBuilder` (77), `PromptState` (71), `ProjectPaneController` (82), `AgentPaneController` (83), `UpdateControlViewModel` (86), `StatusMessageCoordinator` (87), `LaunchWindowViewModel` (88), `IPathDisplayStyleTarget→LaunchWindowViewModel` (89, forwarder), `LaunchWindow` (90) | The active WPF shell and everything that exists to serve it. `AgentCommandWorkflow` is placed here (its namespace is `CLIHub.Launching`; primary consumer `LaunchCommandCoordinator`) with the cross-module consumer `TrayCommandHandlers` noted. `PromptState` (namespace `ViewModels.Shared`) is launch-window prompt state consumed by `LaunchWindow` and `ProjectPaneController`. |
| `AddTrayServices` | 6 | `TrayMenuBuilder` (78), `TrayStateProjection` (79), `TrayCommandHandlers` (80), `ITrayActions→TrayActions` (81), `TrayIconController` (91), `ITrayHost→TrayIconController` (92, forwarder) | Tray host stack, matching the existing `TrayMenuBuilderRegistrationTests` trio. |
| `AddHotkeyServices` | 4 | `GlobalHotkeyService` factory lambda (94: resolves `LaunchWindow`, tray toggle callback, `ILogger<GlobalHotkeyService>`), `IGlobalHotkeyService→GlobalHotkeyService` (98, forwarder), `Func<IHotkeyStartupRegistrar>` (66), `IHotkeyStartupRegistrar→HotkeyStartupRegistrar` (100) | The `GlobalHotkeyService` factory is the one place launch window and tray meet; it moves as one unit. Per D3 the `Func<IHotkeyStartupRegistrar>` accessor relocates next to its target. |
| `AddSettingsServices` | 11 | `IPreferenceApplier→PreferenceApplier` (104), `IRuntimePreferenceTarget→ProcessRuntimePreferenceTarget` (99), `IAccentPreferenceTarget→WpfAccentPreferenceTarget` (105), `IAgentManagementSection→AgentManagementViewModel` (106, transient), `IProjectRegistrySection→ProjectRegistryViewModel` (107, transient), `SettingsApplicationService` (108), `AccentSelectionMemory` (109), `SettingsViewModel` 12-dependency factory (110-122, transient), `SettingsWindow` (123, transient), `Func<SettingsWindow>` (124), `ISettingsLauncher→SettingsLauncher` (125) | Settings window stack plus the runtime preference-application chain (`PreferenceApplier` → `IRuntimePreferenceTarget`/`IAccentPreferenceTarget`), which is settings plumbing consumed at startup and on preference change. |
| `AddUpdateServices` | 6 | `IUpdateWorkflow→UpdateWorkflow` (60), `WhatsNewViewModel` (127), `Func<IUpdateRequestSource>→WhatsNewViewModel` (128), `WhatsNewWindow` (130, transient), `Func<WhatsNewWindow>` (131), `IReleaseNotesLauncher→ReleaseNotesLauncher` (132) | Same name as Core's private module (different class; no conflict). `IUpdateWorkflow` is cross-cutting (consumers: tray projection/handlers, `WhatsNewViewModel`, `UpdateControlViewModel`, `ApplicationSession`, `SettingsViewModel`, `ShellCoordinator`, `UpdateStartupCoordinator`), so the update workflow anchors this module rather than startup. |
| `AddSharedUiServices` | 5 | `IProjectDialogService→ProjectDialogService` (72), `IUserNotificationService→UserNotificationService` (73), `IExternalLauncher→ExternalLauncher` (74), `LogoImageSourceCache` (84), `PathToImageConverter` (85) | UI services with no single owner: dialogs, notifications, external launching, imaging/converter wiring. |

Total: 13 + 12 + 6 + 4 + 11 + 6 + 5 = 57. `ProjectEnricher` is registered in Core's `AddProjectServices`
and is untouched. `MainWindow` stays unregistered (pinned by the architecture test).

### D3 - Accessors and forwarders live with their target

Every `Func<T>` accessor and port-forwarder is placed in the module that owns its target service
(`Func<IHotkeyStartupRegistrar>` moves from startup-adjacent position into `AddHotkeyServices`). This is
safe today and stays safe: these registrations are deferred factories, and the current file already proves
order-independence (`Func<IHotkeyStartupRegistrar>` at line 66 precedes its target at line 100).

### D4 - Statements move verbatim; order is provably non-observable

- No service type is registered twice in the application layer (each of the 57 statements registers a
  distinct service type), so MS.DI last-wins semantics can never be triggered by reordering.
- All forwarders (`IPathDisplayStyleTarget`, `ITrayHost`, `IGlobalHotkeyService`) and all `Func<>`
  accessors resolve lazily via `sp`, not at registration time.
- Order-sensitive wiring that must not move into a module: the entry-point header. `AddLogging` with
  `AddSerilog(Log.Logger, dispose: true)` stays in the entry point (one call site, appended after Core's
  `AddLogging()`); the `guard` parameter flows only through `AddWindowsInfrastructure(guard)` and no module
  registers `SingleInstanceGuard`; the `default-logo.png` path computation stays in the entry point.
- Pre-existing quirks to preserve, not fix: `Func<SettingsWindow>` and `Func<WhatsNewWindow>` sit over
  transient windows, so each invocation yields a new window; the `SettingsViewModel` factory resolves 12
  services inline; `GlobalHotkeyService`'s factory eagerly depends on `LaunchWindow` (so resolving it
  constructs the WPF window — relevant to the guard test, see D5).

### D5 - Guard test mirrors the Core test style, avoiding WPF window construction

New `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs` in `tests/CLIHub.Tests` (net10.0-windows):

- A sweep test building the provider from `AddClIHubServices()` and resolving the WPF-safe set: coordinators,
  builders, view models, and the services existing tests already resolve (`IApplicationLifetime`,
  `IApplicationOperationLifetime`, `IApplicationBootstrapper`, `IApplicationSession`, `IUpdateWorkflow`,
  `LaunchWindowViewModel`, `StatusMessageCoordinator`, `LaunchCommandCoordinator`, `SettingsApplicationService`,
  `AccentSelectionMemory`, tray/hotkey/settings/updates ports). Do **not** resolve `LaunchWindow`,
  `SettingsWindow`, `WhatsNewWindow`, or `GlobalHotkeyService` (each constructs a WPF window or its
  dependencies on xUnit's non-STA thread); assert those via `ServiceDescriptor` inspection, the style
  `TrayMenuBuilderRegistrationTests` already uses.
- Identity checks with `Assert.Same` where cheap and construction-safe: `IPathDisplayStyleTarget` ↔
  `LaunchWindowViewModel`, plus singleton `Assert.Same` on repeated resolution for a representative port set,
  mirroring Core's `AddClIHubCoreServices_MapsUpdatePortsToOneService`.

### D6 - Public API unchanged

Both `AddClIHubServices` overloads keep their signatures and XML docs; the single production caller
(`Startup/ApplicationHost.cs:69`) and all existing tests compile unchanged.

## Risks / Trade-offs

- A statement is misplaced or dropped during the move — mitigated by the 57-statement inventory in D2, the
  new resolution sweep, and the existing per-feature registration tests.
- Textual reordering hides a real order dependency — mitigated by the duplicate-type and deferred-factory
  analysis in D4; if implementation finds a duplicate service type it must stop and update this design.

## Migration Plan

None. Internal restructure; no data, config, or API migration.

## Open Questions

- Exact membership of the resolution sweep: extend only over types that construct safely off the STA thread;
  demote any resolution that throws to descriptor inspection rather than forcing it.
- Whether `AddStartupServices` should be split into lifetime vs. startup if 13 statements read poorly —
  default is one module, matching the agreed boundary.
