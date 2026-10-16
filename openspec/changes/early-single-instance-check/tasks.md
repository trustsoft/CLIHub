## 1. Registration changes

- [x] 1.1 Add optional `SingleInstanceGuard? guard = null` parameter to `AddWindowsInfrastructure`; when provided, register the instance instead of the lazy concrete singleton; verify with an updated `WindowsServiceCollectionExtensionsTests` covering both parameterless and explicit-instance paths
- [x] 1.2 Thread the optional guard through `AddClIHubServices(services, SingleInstanceGuard? guard = null)` and verify existing parameterless test call sites compile untouched
- [x] 1.3 Add a unit test asserting the provider disposes the pre-acquired guard instance (mutex released on provider disposal); if the container does not dispose instance registrations, fall back to the provider-created owner wrapper from design.md D2 and re-verify

## 2. Early acquisition in the host

- [x] 2.1 In `ApplicationHost.Start`, acquire `SingleInstanceGuard` as the first statement; when the guard reports a second instance, call `SignalActivation()` and return before any other work; verify with a test exercising the early-exit path with a guard replacement where feasible, otherwise a review-level check plus manual verification
- [x] 2.2 Wrap pre-provider steps (directory init, logging config, DI build) so an exception after guard acquisition disposes the guard before rethrowing; verify by extending `ApplicationHostTests` failure-path coverage
- [x] 2.3 Register the pre-acquired guard into the service collection from `ApplicationHost` and confirm `ApplicationBootstrapper` flow resolves the same instance; verify by running `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`

## 3. Documentation and validation

- [x] 3.1 Update `docs/architecture/startup.md`: new runtime asset, second-instance exit point, application implementation file references
- [x] 3.2 Update the startup failure-policy table for the silent second-instance exit; verify the doc renders without broken references
- [x] 3.3 Run `openspec validate --all`; verify no validation errors for `early-single-instance-check`
