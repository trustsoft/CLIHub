## 1. Preflight

- [ ] 1.1 Confirm a green baseline: `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` before editing.
- [ ] 1.2 Re-read `src/CLIHub/Composition/ServiceRegistration.cs` and confirm it still matches the 57-statement inventory in `design.md` (D2). If registrations drifted, update `design.md` first.
- [ ] 1.3 Confirm the guard rails that constrain the work: `DependencyRegistration_IsRestrictedToCompositionRoots` (all `Add*` calls stay in this file) and `ApplicationSessionAndTrayWorkflows_AreRegisteredAndLegacyWindowIsNot` (literal registration strings stay in this file).

## 2. Extract feature modules one at a time

Move statements verbatim into private static methods of `ServiceRegistration` (Core shape:
`private static IServiceCollection AddXServices(this IServiceCollection services)`). After each step run
`dotnet build CLIHub.sln`; never move more than one module before building. Do not reorder statements within
a module; do not touch the entry-point header (Core call, `AddWindowsInfrastructure(guard)`, `AddLogging`).

- [ ] 2.1 Extract `AddStartupServices` (13 statements; design.md D2 row 1).
- [ ] 2.2 Extract `AddLaunchWindowServices` (12 statements; includes `IPathDisplayStyleTarget` forwarder).
- [ ] 2.3 Extract `AddTrayServices` (6 statements; includes `ITrayHost` forwarder).
- [ ] 2.4 Extract `AddHotkeyServices` (4 statements; `GlobalHotkeyService` factory stays verbatim; relocate `Func<IHotkeyStartupRegistrar>` next to its target per design.md D3).
- [ ] 2.5 Extract `AddSettingsServices` (11 statements; the 12-dependency `SettingsViewModel` factory moves as one block).
- [ ] 2.6 Extract `AddUpdateServices` (6 statements).
- [ ] 2.7 Extract `AddSharedUiServices` (5 statements); the entry point now reads as the module table of contents plus the three infrastructure calls.

## 3. Composition guard test

- [ ] 3.1 Add `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs`: provider sweep over the WPF-safe set with `Assert.Same` identity checks, following the existing Core and app-layer test styles (design.md D5). Do not resolve `LaunchWindow`, `SettingsWindow`, `WhatsNewWindow`, or `GlobalHotkeyService`; cover those via `ServiceDescriptor` inspection.
- [ ] 3.2 Run `dotnet test tests/CLIHub.Tests` and confirm the new test and all existing `AddClIHubServices_*` registration tests pass.

## 4. Final gates

- [ ] 4.1 `dotnet build CLIHub.sln`.
- [ ] 4.2 `dotnet test CLIHub.sln`, explicitly confirming `ArchitectureBoundaryTests.DependencyRegistration_IsRestrictedToCompositionRoots` and `ArchitectureBoundaryTests.ApplicationSessionAndTrayWorkflows_AreRegisteredAndLegacyWindowIsNot` pass.
- [ ] 4.3 Confirm both `AddClIHubServices` overloads, their XML docs, and `Startup/ApplicationHost.cs` are unchanged (no public API or caller diff).
- [ ] 4.4 `openspec validate --all`.
- [ ] 4.5 `graphify update .` to refresh the knowledge graph after the source change.

## 5. Wrap-up

- [ ] 5.1 Confirm the diff touches only `src/CLIHub/Composition/ServiceRegistration.cs` and the new `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs`.
- [ ] 5.2 Report the result and ask the user for commit approval; do not commit autonomously (repository policy).
