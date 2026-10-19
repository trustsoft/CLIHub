## Why

The application composition root `src/CLIHub/Composition/ServiceRegistration.cs` registers 57 services in
one flat method: startup and lifetime services, the launch window with its coordinators, controllers, and
view models, tray, hotkeys, settings with a `SettingsViewModel` factory resolving 12 services inline, and
updates/whats-new are interleaved, punctuated by `Func<T>` accessor registrations. Ownership of each
registration is unclear and the method no longer reads as a table of contents. Core already solved the same
problem: `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs` delegates to small feature methods
(`AddConfigurationServices`, `AddLogoServices`, `AddProjectServices`, `AddPluginServices`,
`AddAgentServices`, `AddScanningServices`, `AddUpdateServices`), and `AddWindowsInfrastructure` groups the
platform adapters. The application layer should mirror that pattern.

## What Changes

- Split the body of `AddClIHubServices` in `src/CLIHub/Composition/ServiceRegistration.cs` into seven
  private feature-module methods in the same file: `AddStartupServices`, `AddLaunchWindowServices`,
  `AddTrayServices`, `AddHotkeyServices`, `AddSettingsServices`, `AddUpdateServices`, and
  `AddSharedUiServices`.
- Keep the public entry points and their signatures unchanged; the entry point retains exactly the three
  infrastructure calls (Core services, Windows infrastructure, Serilog logging) followed by the module calls.
- Move every registration statement verbatim into its module; no service, lifetime, factory, or
  registration-order change.
- Add an application-layer composition guard test (`tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs`)
  mirroring the style of the existing Core `ServiceCollectionExtensionsTests`, so the reorganization and
  future edits are pinned by a resolution sweep.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This change is internal code organization; runtime behavior, the service graph, lifetimes, and the
  public composition API are unchanged. No capability spec is modified; `skip_specs: true` applies as with
  the prior organizational change `add-architecture-enforcement`.

## Impact

- `src/CLIHub/Composition/ServiceRegistration.cs` — internal restructure only; both `AddClIHubServices`
  overloads keep their signatures, so the single production caller (`Startup/ApplicationHost.cs`) and the
  tests are unaffected.
- `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs` — new guard test.
- No production behavior, configuration, XAML, documentation, or spec changes.

## Non-goals

- No service rewrites, lifetime changes, or new abstractions (no facades, decorators, or module interfaces).
- No moving services between Core, Core.Windows, and the application layer. `ProjectEnricher` is already
  correctly placed in Core's `AddProjectServices`.
- No "fixing" of pre-existing quirks (for example the transient-window `Func<>` accessors), even where a
  different lifetime might look better.
- No changes to `ServiceCollectionExtensions.cs` (Core) or `WindowsServiceCollectionExtensions.cs`.
